using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras
{
    internal sealed class CoLaBClient : IDisposable
    {
        private TcpClient _client;

        private MemoryStream _upstreamBuffer;
        private BinaryWriter _upstreamWriter;

        internal const byte _stx = 0x02;
        internal const byte _space = 0x20;
        private byte[] _downstreamHeaderBuffer;

        private CoLaBClient()
        {
            _client = new TcpClient();
        }

        internal static CoLaBClient Connect(IPEndPoint remoteEndPoint)
        {
            // Construct the Client object
            CoLaBClient client = new CoLaBClient();

            try
            {
                // Attempt to Connect to the remote TCP/IP end-point
                client._client.Connect(remoteEndPoint);
                return client;
            }
            catch
            {
                try
                {
                    // Dispose the Client that failed to Connect
                    client.Dispose();
                }
                catch
                {
                    // Suppress secondary exceptions
                }

                // Rethrow the original exception
                throw;
            }
        }

        ~CoLaBClient()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if ((disposing) && (null != _upstreamWriter))
            {
                _upstreamWriter.Dispose();
                _upstreamWriter = null;
            }

            if ((disposing) && (null != _upstreamBuffer))
            {
                _upstreamBuffer.Dispose();
                _upstreamBuffer = null;
            }

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

        public bool IsConnected => _client.Connected;

        #region Telegrams: Upstream

        public CoLaBTelegram SendTelegram(CoLaCommandType commandType, string commandName, Action<BinaryWriter> writeTelegram, int acknowledgeTimeout)
        {
            // Convert the Command-Type to a String
            string commandString;
            string acknowledgeType;
            switch (commandType)
            {
                case CoLaCommandType.Read: commandString = $"sRN " + commandName; acknowledgeType = "sRA"; break;
                case CoLaCommandType.Write: commandString = $"sWN " + commandName; acknowledgeType = "sWA"; break;
                case CoLaCommandType.Method: commandString = $"sMN " + commandName; acknowledgeType = "sAN"; break;
                case CoLaCommandType.Event: commandString = $"sEN " + commandName; acknowledgeType = "sEA"; break;
                default: throw new ArgumentException("Unknown or unsupported CoLa command type: " + commandType.ToString(), nameof(commandType));
            }

            // Initialize the Upstream Buffer on first telegram
            if (null == _upstreamWriter)
            {
                _upstreamBuffer = new MemoryStream();
                _upstreamWriter = new BinaryWriter(_upstreamBuffer, Encoding.ASCII);

                _upstreamWriter.Write(new byte[] { _stx, _stx, _stx, _stx, 0x00, 0x00, 0x00, 0x00 });
            }
            else
            {
                _upstreamBuffer.Seek(8, SeekOrigin.Begin);
            }

            // Generate the Telegram
            _upstreamWriter.Write(Encoding.ASCII.GetBytes(commandString));

            if (null != writeTelegram)
            {
                _upstreamWriter.Write(_space);
                writeTelegram(_upstreamWriter);
            }

            int length = (int)(_upstreamBuffer.Position - 8L);

            // Encode the Telegram Size
            _upstreamBuffer.Seek(4, SeekOrigin.Begin);
            for (int i = 3; i >= 0; --i)
            {
                _upstreamWriter.Write((byte)(length >> (i * 8)));
            }

            // Calculate the Telegram Checksum
            byte checksum = 0;
            for (int i = 0; i < length; ++i)
            {
                checksum ^= (byte)_upstreamBuffer.ReadByte();
            }

            _upstreamWriter.Write(checksum);

            // Send the Telegram
            _client.Client.Send(_upstreamBuffer.GetBuffer(), 0, (int)_upstreamBuffer.Position, SocketFlags.None);

            // Look for Acknowledgement
            _client.Client.ReceiveTimeout = acknowledgeTimeout;
            CoLaBTelegram acknowledgement;
            try
            {
                Task<CoLaBTelegram> acknowledgementTask = ReceiveTelegramAsync();
                acknowledgementTask.Wait();
                acknowledgement = acknowledgementTask.Result;
            }
            catch (SocketException socketException)
            {
                switch (socketException.SocketErrorCode)
                {
                    // Timeouts indicate that the port is not configured for the binary dialect
                    case SocketError.TimedOut:
                        throw new CoLaBException($"Upstream CoLa (binary) Telegram not acknowledged: CoLa (binary) dialect ignored", socketException);

                    // Other socket errors are unknown
                    default:
                        throw new CoLaBException($"Upstream CoLa (binary) Telegram not acknowledged: socket error", socketException);
                }
            }

            if ((null == acknowledgement) || (acknowledgement.CommandPrefix != acknowledgeType) || (acknowledgement.CommandName != commandName))
            {
                throw new CoLaBException($"Upstream CoLa (binary) Telegram acknowledged incorrectly: expected `{acknowledgeType} {commandName}`");
            }

            return acknowledgement;
        }

        public CoLaBTelegram SendTelegram(CoLaCommandType commandType, string commandName, int acknowledgeTimeout)
        {
            return SendTelegram(commandType, commandName, writeTelegram: null, acknowledgeTimeout: acknowledgeTimeout);
        }

        #endregion Telegrams: Upstream

        #region Telegrams: Downstream

        private Task<int> ReceiveAsync(byte[] buffer, int offset, int size)
        {
            IAsyncResult asyncResult = _client.Client.BeginReceive(buffer, offset, size, SocketFlags.None, null, null);
            return Task<int>.Factory.FromAsync(asyncResult, _client.Client.EndReceive);
        }

        internal async Task<CoLaBTelegram> ReceiveTelegramAsync()
        {
            // Initialize the Upstream Buffer on first receive
            if (null == _downstreamHeaderBuffer)
            {
                _downstreamHeaderBuffer = new byte[4 + 4];
            }

            // Receive the Telegram Header
            int receivedBytes = await ReceiveAsync(_downstreamHeaderBuffer, 0, 4 + 4);
            if (0 == receivedBytes) return null;

            // Validate the Header
            if ((8 != receivedBytes) || (_stx != _downstreamHeaderBuffer[0])
                                     || (_stx != _downstreamHeaderBuffer[1])
                                     || (_stx != _downstreamHeaderBuffer[2])
                                     || (_stx != _downstreamHeaderBuffer[3]))
            {
                throw new CoLaBException("Unexpected or Corrupt CoLa (binary) Header in downstream message");
            }

            // Decode the Telegram Length
            int length = (_downstreamHeaderBuffer[4] << 24)
                       | (_downstreamHeaderBuffer[5] << 16)
                       | (_downstreamHeaderBuffer[6] << 8)
                       | (_downstreamHeaderBuffer[7]);

            // Receive the Telegram Body
            byte[] telegram = new byte[length + 1];
            int telegramPosition = 0;
            do
            {
                receivedBytes = await ReceiveAsync(telegram, telegramPosition, telegram.Length - telegramPosition);
                if (0 == receivedBytes)
                {
                    throw new CoLaBException($"Incomplete CoLa (binary) Telegram: {telegramPosition} of {length + 1} downstream bytes received");
                }

                telegramPosition += receivedBytes;
            }
            while (telegramPosition < telegram.Length);

            // Construct and Return CoLa (binary) telegram structure
            return new CoLaBTelegram(telegram);
        }

        #endregion Telegrams: Downstream
    }
}
