using System;
using System.Net;
using MetriCam2.Exceptions;
using Metrilus.Util;

namespace MetriCam2.Cameras
{
    public sealed class TiM561 : Camera, IDisposable
    {
        public const int DefaultSOPASPort = 2112;
        private const string _logPrefix = "SICK TiM5xx";

        private IPEndPoint _remoteEndPoint;
        private CoLaBClient _client;

        public TiM561(IPEndPoint remoteEndPoint)
        {
            _remoteEndPoint = remoteEndPoint;
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
            if ((disposing) && (null != _client))
            {
                _client.Dispose();
                _client = null;
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
                        // Log-In to the Device with the password from the documentation
                        _client.SendTelegram("sMN", "SetAccessMode", (telegramWriter) =>
                        {
                            telegramWriter.Write(new byte[] { 0x03, 0xf4, 0x72, 0x47, 0x44 });
                        });

                        log.Debug($"{_logPrefix}: CoLa (binary) protocol authentication complete");
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

        protected override void UpdateImpl()
        {
            throw new NotImplementedException();
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            throw new NotImplementedException();
        }

        #region Channel Information

        protected override void LoadAllAvailableChannels()
        {
            // The 2D Laser Scanner only yields a single channel of equally-spaced distance values
            Channels.Clear();
            Channels.Add(ChannelRegistry.Instance.RegisterChannel(ChannelNames.Distance));
        }

        #endregion Channel Information
    }
}
