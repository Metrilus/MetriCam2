using System;
using System.Drawing;
using System.Net;
using System.Threading;
using MetriCam2.Exceptions;
using Metrilus.Util;

namespace MetriCam2.Cameras
{
    public sealed class TiM561 : Camera, IDisposable
    {
        public const int DefaultSOPASPort = 2112;

        private const string _vendorName = "SICK";
        private const string _modelName = "TiM561";
        private const string _logPrefix = _vendorName + " " + _modelName;

        private const int _timeoutMilliseconds = 30000 + 1000;

        private bool _disposed;

        private IPEndPoint _remoteEndPoint;
        private CoLaBClient _client;
        private Thread _downstreamThread;

        private CoLaBTelegram _lastTelegram;
        private AutoResetEvent _downstreamTurnstile;

        private UInt32 _serialNumber;
        private UInt16 _scanCounter;
        private UInt32 _timeStamp;
        private string _channelName;

        private float _scalingFactor;
        private int _startingAngle;
        private int _angularStepWidth;
        private UInt16[] _radii;
        private float[,] _directions;

#if !NETSTANDARD2_0
        public override Icon CameraIcon => Properties.Resources.SickTiMIcon;
#endif

        public TiM561(IPEndPoint remoteEndPoint)
            : base(_modelName)
        {
            _disposed = false;
            _downstreamTurnstile = new AutoResetEvent(false);

            _remoteEndPoint = remoteEndPoint;

            _scalingFactor = Single.NaN;
            _startingAngle = Int32.MinValue;
            _angularStepWidth = Int32.MinValue;
            _radii = null;
            _directions = null;
        }

        public TiM561(IPAddress address, int port = DefaultSOPASPort)
            : this(new IPEndPoint(address, port))
        {
        }

        public TiM561(string ipString, int port = DefaultSOPASPort)
            : this(IPAddress.Parse(ipString), port)
        {
        }

        ~TiM561()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            // Raise the Disposed Flag
            _disposed = true;

            // If the Downstream Thread is still live, give it time to end quietly
            if ((disposing) && (null != _downstreamThread))
            {
                if (!_downstreamThread.Join(500))
                {
                    _downstreamThread.Abort();
                }

                _downstreamThread = null;
            }

            // Dispose the TCP/IP client
            if ((disposing) && (null != _client))
            {
                _client.Dispose();
                _client = null;
            }

            // Dispose of the Turnstile
            if ((disposing) && (null != _downstreamTurnstile))
            {
                _downstreamTurnstile.Dispose();
                _downstreamTurnstile = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region Connection & Disconnection

        protected override void ConnectImpl()
        {
            try
            {
                // Reinitialize a Disconnected Client
                if ((null != _client) && (!_client.IsConnected))
                {
                    _client.Dispose();
                    _client = null;
                }

                // Connect when necessary
                if (null == _client)
                {
                    _client = CoLaBClient.Connect(_remoteEndPoint);
                    log.Debug($"{_logPrefix}: connected to {_remoteEndPoint}");

                    try
                    {
                        // Request Scan-Data Telegrams
                        CoLaBTelegram acknowledgement = _client.SendTelegram(CoLaCommandType.Event, "LMDscandata", (telegramWriter) =>
                        {
                            telegramWriter.Write(0x01);
                        }, acknowledgeTimeout: _timeoutMilliseconds);

                        if (acknowledgement.Data[acknowledgement.Offset] != 0)
                        {
                            log.Debug($"{_logPrefix}: CoLa (binary) telegram subscription established");
                        }
                        else
                        {
                            ExceptionBuilder.Throw(typeof(MetriCam2.Exceptions.ConnectionFailedException), this, "error_connectionFailed", "CoLa (binary) telegram subscription failed");
                        }
                    }
                    catch
                    {
                        // Dispose the TCP/IP Client immediately
                        try
                        {
                            _client.Dispose();
                            _client = null;
                        }
                        catch
                        {
                            // Suppress secondary exceptions
                        }

                        // Rethrow the original exception
                        throw;
                    }

                    // Begin receiving telegrams in a background thread
                    _downstreamThread = new Thread(DownstreamThreadProc);
                    _downstreamThread.Name = _vendorName + " " + _modelName;
                    _downstreamThread.IsBackground = true;
                    _downstreamThread.Start();
                }
            }
            catch (MetriCam2Exception)
            {
                // Pass MetriCam API Exceptions without alteration
                throw;
            }
            catch (Exception foreignException)
            {
                // Wrap and Throw other exceptions encountered during Connection
                ExceptionBuilder.Throw(typeof(MetriCam2.Exceptions.ConnectionFailedException), this, foreignException);
            }
        }

        protected override void DisconnectImpl() => Dispose();

        #endregion Connection & Disconnection

        private void DownstreamThreadProc()
        {
            while (!_disposed)
            {
                // Fetch a Telegram and Signal any waiting threads
                // (Note: this will always be a Scan-Data telegram because the thread will be suspended for all other communications.)
                _lastTelegram = _client.ReceiveTelegram();
                _downstreamTurnstile.Set();
            }
        }

        protected override void UpdateImpl()
        {
            if ((null == _client) || (!_client.IsConnected) || (null == _downstreamTurnstile))
            {
                throw new InvalidOperationException($"{_logPrefix} disconnected");
            }

            try
            {
                // Wait for the next Scan-Data Telegram
                _downstreamTurnstile.WaitOne();
                CoLaBTelegram scanDataTelegram = _lastTelegram;
                NetworkBinaryReader scanData = new NetworkBinaryReader(scanDataTelegram);

                // Miscellaneous Metadata
                UInt16 versionNumber = scanData.ReadUInt16();
                if (1 != versionNumber) log.Warn($"{_logPrefix}: unexpected version number in telegram: {versionNumber}");
                scanData.Skip(2);

                UInt32 serialNumber = scanData.ReadUInt32();
                if (_serialNumber != serialNumber)
                {
                    _serialNumber = serialNumber;
                    base.serialNumber = serialNumber.ToString();
                }

                scanData.Skip(1);
                byte deviceStatus = scanData.ReadByte();
                if (0 != deviceStatus) log.Error($"{_logPrefix}: unexpected device status: {deviceStatus}");

                scanData.Skip(2);
                _scanCounter = scanData.ReadUInt16();

                scanData.Skip(4);
                _timeStamp = scanData.ReadUInt32();

                // Skip: input states, output states, reserved bytes, frequency information, encoders
                scanData.Skip(2 + 2 + 2 + 4 + 4 + 2);

                // 16-bit Channels                
                UInt16 channelCount16 = scanData.ReadUInt16();
                if (1 != channelCount16) log.Error($"{_logPrefix}: unexpected number of 16-bit channels: {channelCount16}");

                _channelName = scanData.ReadString(5);

                _scalingFactor = scanData.ReadSingle() * 0.001f;
                float scalingOffset = scanData.ReadSingle();

                int startingAngle = scanData.ReadInt32();
                int angularStepWidth = (int)scanData.ReadUInt16();
                if ((_startingAngle != startingAngle) || (_angularStepWidth != angularStepWidth))
                {
                    _startingAngle = startingAngle;
                    _angularStepWidth = angularStepWidth;
                    _directions = null;
                }

                int dataCount = (int)scanData.ReadUInt16();

                // Initialize or Reinitialize array of Polar Radii
                if (null == _radii)
                {
                    _radii = new UInt16[dataCount];
                    _directions = null;
                }
                else if (_radii.Length != dataCount)
                {
                    Array.Resize(ref _radii, dataCount);
                    _directions = null;
                }

                // Read 16-bit unsigned pixels (reversed)
                for (int i = (dataCount - 1); i >= 0; --i)
                {
                    _radii[i] = scanData.ReadUInt16();
                }
            }
            catch (MetriCam2Exception)
            {
                // Pass MetriCam API Exceptions without alteration
                throw;
            }
            catch (Exception foreignException)
            {
                // Wrap and Throw other exceptions encountered during Connection
                ExceptionBuilder.Throw(typeof(MetriCam2.Exceptions.ImageAcquisitionFailedException), this, foreignException);
            }
        }

        private FloatCameraImage CalcDistance()
        {
            FloatCameraImage image = new FloatCameraImage(_radii.Length, 1)
            {
                ChannelName = _channelName,
                FrameNumber = _scanCounter,
                TimeStamp = _timeStamp
            };

            for (int x = 0; x < _radii.Length; ++x)
            {
                image[x] = _scalingFactor * (float)_radii[x];
            }

            return image;
        }

        private Point3fCameraImage CalcPoint3DImage()
        {
            // Recalculate Directions if necessary
            if (null == _directions)
            {
                int stepCount = _radii.Length;
                _directions = new float[stepCount, 2];
                for (int j = 0; j < stepCount; ++j)
                {
                    double theta = Math.PI * (double)(_startingAngle + ((stepCount - 1 - j) * _angularStepWidth)) / (180.0 * 10000.0);
                    _directions[j, 0] = (float)Math.Cos(theta);
                    _directions[j, 1] = (float)Math.Sin(theta);
                }
            }

            // Generate 3D Data
            Point3fCameraImage image = new Point3fCameraImage(_radii.Length, 1)
            {
                ChannelName = _channelName,
                FrameNumber = _scanCounter,
                TimeStamp = _timeStamp
            };

            for (int x = 0; x < _radii.Length; ++x)
            {
                float r = _scalingFactor * (float)_radii[x];
                image[x] = new Point3f(x: (r * _directions[x, 0]),
                                       y: 0.0f,
                                       z: (r * _directions[x, 1]));
            }

            return image;
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Distance:
                    return CalcDistance();

                case ChannelNames.Point3DImage:
                    return CalcPoint3DImage();
            }

            log.Error("Invalid channelname: " + channelName);
            return null;
        }

        #region Channel Information

        protected override void LoadAllAvailableChannels()
        {
            // The 2D Laser Scanner only yields a single channel of equally-spaced distance values
            Channels.Clear();
            Channels.Add(ChannelRegistry.Instance.RegisterChannel(ChannelNames.Distance));
            Channels.Add(ChannelRegistry.Instance.RegisterChannel(ChannelNames.Point3DImage));
        }

        #endregion Channel Information

        #region Miscellaneous Meta-Data

        public override string Vendor => _vendorName;

        #endregion Miscellaneous Meta-Data
    }
}
