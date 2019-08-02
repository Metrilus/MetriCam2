// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Threading;
using MetriCam2.Cameras.Internal.Sick;
using Metrilus.Util;
using MetriCam2.Exceptions;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// MetriCam2 wrapper for Sick V3S100 cameras.
    /// </summary>
    public class VisionaryT : Camera
    {
        #region Private Variables
        private const int NumFrameRetries = 3;
        private Thread _updateThread;
        private CancellationTokenSource _cancelUpdateThreadSource = new CancellationTokenSource();
        private AutoResetEvent _frameAvailable = new AutoResetEvent(false);
        private object _frontLock = new object();
        private object _backLock = new object();
        private string _updateThreadError = null; // Hand errors from update loop thread to application thread

        // ipAddress to connect to
        private string ipAddress;
        // device handle
        private Device _device;
        private Control _control;

        // image data contains information about the current frame e.g. width and height
        private FrameData _frontFrameData;
        private FrameData _backFrameData;
        // camera properties
        private int width;
        private int height;
        #endregion

        #region Public Properties

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.SickIcon; }
#endif

        private ParamDesc<string> IPAddressDesc
        {
            get
            {
                return new ParamDesc<string>()
                {
                    Description = "IP address",
                    ReadableWhen = ParamDesc.ConnectionStates.Disconnected | ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
            }
        }

        /// <summary>
        /// IP Address of camera. Has to be set before connecting.
        /// </summary>
        public string IPAddress
        {
            get => ipAddress;
            set => ipAddress = value;
        }

        private RangeParamDesc<int> IntegrationTimeDesc
        {
            get
            {
                return new RangeParamDesc<int>(80, 4000)
                {
                    Description = "Integration time",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                    Unit = "μs",
                };
            }
        }
        /// <summary>
        /// Integration time of ToF-sensor [us].
        /// </summary>
        public int IntegrationTime
        {
            get => _control.GetIntegrationTime();
            set => _control.SetIntegrationTime(value);
        }

        private ListParamDesc<VisionaryTCoexistenceMode> CoexistenceModeDesc
        {
            get
            {
                return new ListParamDesc<VisionaryTCoexistenceMode>(typeof(VisionaryTCoexistenceMode))
                {
                    Description = "Coexistence mode",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }

        /// <summary>
        /// Coexistence mode (modulation frequency) of ToF-sensor.
        /// </summary>
        public VisionaryTCoexistenceMode CoexistenceMode
        {
            get => _control.GetCoexistenceMode();
            set => _control.SetCoexistenceMode(value);
        }

        private ParamDesc<int> WidthDesc
        {
            get
            {
                return new ParamDesc<int>()
                {
                    Description = "Width of images.",
                    Unit = "px",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected
                };
            }
        }

        /// <summary>
        /// Width of images.
        /// </summary>
        public int Width => width;

        private ParamDesc<int> HeightDesc
        {
            get
            {
                return new ParamDesc<int>()
                {
                    Description = "Height of images.",
                    Unit = "px",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected
                };
            }
        }

        /// <summary>
        /// Height of images.
        /// </summary>
        public int Height => height;

        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new instance of V3S 100 camera.
        /// </summary>
        public VisionaryT()
            : base()
        {
            ipAddress = "";
            _device = null;
            _frontFrameData = null;
            _backFrameData = null;
            _updateThread = null;
            width = 0;
            height = 0;
        }
        #endregion

        #region MetriCam2 Camera Interface
        #region MetriCam2 Camera Interface Properties
        /// <summary>The camera's name.</summary>
        public override string Name { get => "Visionary-T"; }
        public override string Vendor { get => "Sick"; }
        #endregion

        #region MetriCam2 Camera Interface Methods
        /// <summary>
        /// Resets list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
        /// Channels are: Intensity, Distance, Confidence and 3D
        /// </summary>
        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;

            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
            Channels.Add(cr.RegisterChannel(ChannelNames.Distance));
            Channels.Add(cr.RegisterChannel(ChannelNames.ConfidenceMap));
            Channels.Add(cr.RegisterCustomChannel(ChannelNames.RawConfidenceMap, typeof(UShortImage)));
            Channels.Add(cr.RegisterChannel(ChannelNames.Point3DImage));
        }

        /// <summary>
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        protected override void ConnectImpl()
        {
            _updateThreadError = null;

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                string msg = string.Format("IP address is not set. It must be set before connecting.");
                log.Error(msg);
                throw ExceptionBuilder.Build(typeof(ConnectionFailedException), Name, "error_connectionFailed", msg);
            }
            _device = new Device(ipAddress, this, log);

            _control = new Control(log, ipAddress);
            _control.StartStream();
            SerialNumber = _control.GetSerialNumber();

            // select intensity channel
            ActivateChannel(ChannelNames.Intensity);
            SelectChannel(ChannelNames.Intensity);

            _updateThread = new Thread(new ThreadStart(UpdateLoop));
            _updateThread.Start();
        }

        /// <summary>
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        protected override void DisconnectImpl()
        {
            _cancelUpdateThreadSource.Cancel();
            _updateThread.Join();
            _updateThread = null;
            _control.Close();
            _control = null;
            _device.Disconnect();
            _device = null;
        }

        /// <summary>
        /// Updates data buffers of all channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        protected override void UpdateImpl()
        {
            _frameAvailable.WaitOne();

            if (null != _updateThreadError)
            {
                Disconnect();
                throw new ImageAcquisitionFailedException(_updateThreadError);
            }

            lock (_backLock)
            lock (_frontLock)
            {
                _frontFrameData = _backFrameData;
                _backFrameData = null;
                _frameAvailable.Reset();
            }

            width = _frontFrameData.Width;
            height = _frontFrameData.Height;
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        protected override ImageBase CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Intensity:
                    return CalcIntensity();
                case ChannelNames.Distance:
                    return CalcDistance();
                case ChannelNames.ConfidenceMap:
                    return CalcConfidenceMap(CalcRawConfidenceMap());
                case ChannelNames.RawConfidenceMap:
                    return CalcRawConfidenceMap();
                case ChannelNames.Point3DImage:
                    return Calc3D();
            }
            log.Error(Name + ": Invalid channelname: " + channelName);
            return null;
        }

        #endregion
        #endregion

        #region Internal Methods
        private void UpdateLoop()
        {
            int consecutiveFailCounter = 0;
            while (!_cancelUpdateThreadSource.Token.IsCancellationRequested)
            {
                try
                {
                    lock (_backLock)
                    {
                        byte[] imageBuffer = _device.GetFrameData();
                        _backFrameData = new FrameData(imageBuffer, this, log);
                        _frameAvailable.Set();
                    }
                }
                catch
                {
                    consecutiveFailCounter++;
                    if (consecutiveFailCounter > NumFrameRetries)
                    {
                        string msg = $"{Name}: Receive failed more than {NumFrameRetries} times in a row. Shutting down update loop.";
                        log.Error(msg);
                        _updateThreadError = msg;
                        _frameAvailable.Set();
                        break;
                    }
                }

                // reset counter after sucessfull fetch
                consecutiveFailCounter = 0;
            } // while

            _cancelUpdateThreadSource = new CancellationTokenSource();
        }


        /// <summary>
        /// Calculates the intensity channel.
        /// </summary>
        /// <returns>Intensity image</returns>
        private FloatImage CalcIntensity()
        {
            FloatImage result;
            lock (_frontLock)
            {
                result = new FloatImage(_frontFrameData.Width, _frontFrameData.Height);
                int start = _frontFrameData.IntensityStartOffset;
                for (int i = 0; i < _frontFrameData.Height; ++i)
                {
                    for (int j = 0; j < _frontFrameData.Width; ++j)
                    {
                        // take two bytes and create integer (little endian)
                        uint value = (uint)_frontFrameData.ImageBuffer[start + 1] << 8 | (uint)_frontFrameData.ImageBuffer[start + 0];
                        result[i, j] = (float)value;
                        start += 2;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the distance channel.
        /// </summary>
        /// <returns>Distance image</returns>
        private FloatImage CalcDistance()
        {
            FloatImage result;
            // FIXME: distance calculation
            lock (_frontLock)
            {
                result = new FloatImage(_frontFrameData.Width, _frontFrameData.Height);
                int start = _frontFrameData.DistanceStartOffset;
                for (int i = 0; i < _frontFrameData.Height; ++i)
                {
                    for (int j = 0; j < _frontFrameData.Width; ++j)
                    {
                        // take two bytes and create integer (little endian)
                        uint value = (uint)_frontFrameData.ImageBuffer[start + 1] << 8 | (uint)_frontFrameData.ImageBuffer[start + 0];
                        result[i, j] = (float)value * 0.001f;
                        start += 2;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the confidence map.
        /// </summary>
        /// <returns>Confidence map</returns>
        private UShortImage CalcRawConfidenceMap()
        {
            UShortImage result;
            lock (_frontLock)
            {
                result = new UShortImage(_frontFrameData.Width, _frontFrameData.Height);
                int start = _frontFrameData.ConfidenceStartOffset;
                for (int i = 0; i < _frontFrameData.Height; ++i)
                {
                    for (int j = 0; j < _frontFrameData.Width; ++j)
                    {
                        // take two bytes and create integer (little endian)
                        result[i, j] = (ushort)((ushort)_frontFrameData.ImageBuffer[start + 1] << 8 | (ushort)_frontFrameData.ImageBuffer[start + 0]);
                        start += 2;
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// According to the xml records being sent by the camera as initial sequence, a confidence image comes with 16 bits per pixel such that the lowest possible value is 0 and the highest possible value is 65535.
        /// Scale this range to [0, 1].
        /// </summary>
        /// <param name="rawConfidenceMap">Confidence map as provided by the camera</param>
        /// <returns>Confidence map as float image scaled to [0, 1] range</returns>
        private FloatImage CalcConfidenceMap(UShortImage rawConfidenceMap)
        {
            int width = rawConfidenceMap.Width;
            int height = rawConfidenceMap.Height;
            float scaling = 1.0f / (float)ushort.MaxValue;

            FloatImage confidenceMap = new FloatImage(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    confidenceMap[y, x] = scaling * rawConfidenceMap[y, x];
                }
            }

            return confidenceMap;
        }
        /// <summary>
        /// Calculates the 3D point cloud from received data.
        /// </summary>
        /// <returns>point cloud</returns>
        private Point3fImage Calc3D()
        {
            Point3fImage result;
            lock (_frontLock)
            {
                result = new Point3fImage(_frontFrameData.Width, _frontFrameData.Height);

                float cx = _frontFrameData.CX;
                float cy = _frontFrameData.CY;
                float fx = _frontFrameData.FX;
                float fy = _frontFrameData.FY;
                float k1 = _frontFrameData.K1;
                float k2 = _frontFrameData.K2;

                FloatImage distances = CalcDistance();

                for (int i = 0; i < _frontFrameData.Height; ++i)
                {
                    for (int j = 0; j < _frontFrameData.Width; ++j)
                    {
                        float distance = distances[i, j];

                        // we map from image coordinates with origin top left and x horizontal (right) and y vertical 
                        // (downwards) to camera coordinates with origin in center and x to the left and y upwards (seen 
                        // from the sensor position)
                        double xp = (cx - j) / fx;
                        double yp = (cy - i) / fy;

                        // correct the camera distortion
                        double r2 = xp * xp + yp * yp;
                        double r4 = r2 * r2;
                        double k = 1 + k1 * r2 + k2 * r4;
                        double xd = xp * k;
                        double yd = yp * k;

                        double z = distance / Math.Sqrt(xd * xd + yd * yd + 1.0);
                        double x = xd * z;
                        double y = yd * z;

                        // create point and save it
                        Point3f point = new Point3f((float)-x, (float)-y, (float)z);
                        result[i, j] = point;
                    }
                }
            }
            return result;
        }
        #endregion
    }
}
