using System;

namespace MetriCam2.Cameras
{
    public sealed class TiM561 : IDisposable
    {
        public const int DefaultSOPASPort = 2112;

        private string _ipAddress;
        private int _port;
        private CoLaBClient _client;

        public TiM561(string ipAddress, int port = DefaultSOPASPort)
        {
            _ipAddress = ipAddress;
            _port = port;
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

        public bool Connect()
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
                _client = new CoLaBClient(_ipAddress, _port);
                _client.SendTelegram("sMN", "SetAccessMode", (telegramWriter) =>
                {
                    telegramWriter.Write(new byte[] { 0x03, 0xf4, 0x72, 0x47, 0x44 });
                });

                return true;
            }
            else return false;
        }

        public bool Disconnect()
        {
            // Dispose Client if necessary
            if (null != _client)
            {
                bool disconnecting = _client.IsConnected;

                _client.Dispose();
                _client = null;

                return disconnecting;
            }

            return false;
        }

        #endregion Connection & Disconnection
    }
}
