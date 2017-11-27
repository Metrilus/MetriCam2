using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SICK_TIM561_Client
{
    internal sealed class CoLaBClient : IDisposable
    {
        private TcpClient _client;

        private MemoryStream _upstreamBuffer;
        private BinaryWriter _upstreamWriter;

        private const byte _stx = 0x02;
        private byte[] _downstreamHeaderBuffer;

        internal CoLaBClient(string ipAddress, int port)
        {
            _client = new TcpClient(ipAddress, port);
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

        public void SendTelegram(string commandType, string commandName, Action<BinaryWriter> writeTelegram)
        {
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
            _upstreamWriter.Write(Encoding.ASCII.GetBytes(commandType + " " + commandName + " "));
            writeTelegram(_upstreamWriter);
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

            CoLaBTelegram ack = ReceiveTelegram();
            if ((null == ack) || (ack.CommandType != "sAN") || (ack.CommandName != commandName))
            {
                throw new CoLaBException($"Upstream CoLa (binary) Telegram not acknowledged: expected `sAN {commandName}`");
            }
        }

        #endregion Telegrams: Upstream

        #region Telegrams: Downstream

        private CoLaBTelegram ReceiveTelegram()
        {
            // Initialize the Upstream Buffer on first receive
            if (null == _downstreamHeaderBuffer)
            {
                _downstreamHeaderBuffer = new byte[4 + 4];
            }

            // Receive the Telegram Header
            int receivedBytes = _client.Client.Receive(_downstreamHeaderBuffer, 0, 4 + 4, SocketFlags.None);
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
            receivedBytes = _client.Client.Receive(telegram, 0, length + 1, SocketFlags.None);
            if (receivedBytes < length + 1)
            {
                throw new CoLaBException($"Incomplete CoLa (binary) Telegram: {receivedBytes} of {length + 1} downstream bytes received");
            }

            // Construct and Return CoLa (binary) telegram structure
            return new CoLaBTelegram(telegram);
        }

        #endregion Telegrams: Downstream
    }
}
