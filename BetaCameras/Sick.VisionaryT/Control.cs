using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Metrilus.Logging;

namespace MetriCam2.Cameras.Internal.Sick
{
    internal class Control
    {
        private const int TCP_PORT_SOPAS = 2112;
        private readonly byte[] START_STX = { 0x02, 0x02, 0x02, 0x02 };

        private readonly MetriLog log;
        private readonly NetworkStream streamControl;
        private readonly TcpClient sockControl;

        internal AccessModes _accessMode { get; private set; }

        public Control(MetriLog log, string ipAddress)
        {
            this.log = log;

            try
            {
                sockControl = new TcpClient(ipAddress, TCP_PORT_SOPAS);
                streamControl = sockControl.GetStream();
            }
            catch (Exception ex)
            {
                string msg = string.Format("Failed to connect to IP={0}, reasons={1}", ipAddress, ex.Message);
                log.Error(msg);
                throw new Exceptions.ConnectionFailedException(msg, ex);
            }

            _accessMode = GetAccessMode();
            InitStream();
        }

        internal void Close()
        {
            streamControl.Close();
            sockControl.Close();
        }

        /// <summary>
        /// Tells the device that there is a streaming channel.
        /// </summary>
        private void InitStream()
        {
            log.Debug("InitStream");
            SendCommand("sMN GetBlobClientConfig");

            bool success = ReceiveResponse(out byte[] payload, out byte checkSum);
            if (!success)
            {
                throw new InvalidOperationException("Failed to init control stream.");
            }
        }

        /// <summary>
        /// Starts streaming the data by calling "PLAYSTART" method on the device.
        /// </summary>
        internal void StartStream()
        {
            log.Debug("Starting data stream");
            SendCommand("sMN PLAYSTART");

            bool success = ReceiveResponse(out byte[] payload, out byte checkSum);
            if (!success)
            {
                throw new InvalidOperationException("Failed to start stream.");
            }
        }

        private AccessModes GetAccessMode()
        {
            SendCommand("sMN GetAccessMode");

            bool success = ReceiveResponse(out byte[] payload, out byte checkSum);
            if (!success)
            {
                throw new InvalidOperationException("Failed to get access mode.");
            }

            byte value = payload[payload.Length - 1];
            log.DebugFormat("Got access mode: {0}", (AccessModes)value);
            return (AccessModes)value;
        }

        /// <summary>
        /// Sets the access mode on the device.
        /// </summary>
        internal void SetAccessMode(AccessModes newMode)
        {
            if (newMode == _accessMode)
            {
                log.DebugFormat("Skipping SetAccessMode: New access mode ({0}) is the same as the current one.", newMode);
                return;
            }

            if ((int)newMode < (int)_accessMode)
            {
                log.DebugFormat("Skipping SetAccessMode: New access mode ({0}) lower than the current one.", newMode);
                return;
            }

            byte[] dig;
            switch (newMode)
            {
                case AccessModes.Operator:
                    dig = new byte[] { 59, 117, 101, 94 };
                    break;
                case AccessModes.Maintenance:
                    dig = new byte[] { 85, 119, 0, 230 };
                    break;
                case AccessModes.AuthorizedClient:
                    dig = new byte[] { 251, 53, 108, 222 };
                    break;
                case AccessModes.Service:
                    dig = new byte[] { 237, 120, 75, 170 };
                    break;
                default:
                    throw new NotImplementedException($"Changing to access level of {newMode} is not supported by MetriCam 2.");
            }

            byte[] commandAsBytes = Encoding.ASCII.GetBytes("sMN SetAccessMode ");
            Array.Resize(ref commandAsBytes, commandAsBytes.Length + 1 + dig.Length);
            int offsetOfMode = commandAsBytes.Length - dig.Length - 1;
            commandAsBytes[offsetOfMode] = (byte)newMode;
            for (int i = commandAsBytes.Length - dig.Length, j = 0; i < commandAsBytes.Length; i++, j++)
            {
                commandAsBytes[i] = dig[j];
            }
            SendCommand(commandAsBytes);

            bool success = ReceiveResponse(out byte[] payload, out byte checkSum);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to set access mode to {newMode} (no response).");
            }

            byte cmdSuccess = payload[payload.Length - 1];
            if (0 == cmdSuccess)
            {
                throw new InvalidOperationException($"Failed to set access mode to {newMode} (not successful).");
            }

            _accessMode = newMode;
            log.DebugFormat("Access mode set to {0}", newMode);
        }

        internal string GetSerialNumber()
        {
            log.Debug("Getting serial number");
            return ReadVariableString("SerialNumber");
        }

        internal void SetIntegrationTime(int value)
        {
            SetAccessMode(AccessModes.Service);
            log.Debug("Setting integration time");
            WriteVariable("integrationTimeUs", value);
        }
        internal int GetIntegrationTime()
        {
            log.Debug("Getting integration time");
            return ReadVariableInt32("integrationTimeUs");
        }

        internal void SetCoexistenceMode(VisionaryTCoexistenceMode value)
        {
            SetAccessMode(AccessModes.AuthorizedClient);
            log.Debug("Setting coexistence mode / modulation frequency");
            WriteVariable("modFreq", (byte)value);
        }
        internal VisionaryTCoexistenceMode GetCoexistenceMode()
        {
            log.Debug("Getting coexistence mode / modulation frequency");
            return (VisionaryTCoexistenceMode)ReadVariableByte("modFreq");
        }

        private void WriteVariable(string name, byte value)
        {
            SendCommand("sWN " + name, value);

            bool success = ReceiveResponse(out byte[] payload, out byte checkSum);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to write variable {name} (no response).");
            }
            if (!CheckWriteAcknowledge(payload))
            {
                throw new InvalidOperationException($"Failed to write variable {name} (not successful).");
            }
        }

        private void WriteVariable(string name, int value)
        {
            SendCommand("sWN " + name, value);

            bool success = ReceiveResponse(out byte[] payload, out byte checkSum);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to write variable {name} (no response).");
            }
            if (!CheckWriteAcknowledge(payload))
            {
                throw new InvalidOperationException($"Failed to write variable {name} (not successful).");
            }
        }

        private bool CheckWriteAcknowledge(byte[] payload)
        {
            byte[] writeAck = Encoding.ASCII.GetBytes("sWA");
            for (int i = 0; i < writeAck.Length; i++)
            {
                if (writeAck[i] != payload[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if <paramref name="payload"/> begins with the error marker.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns>True if <paramref name="payload"/> starts with the error marker, false otherwise.</returns>
        private bool IsErrorResponse(byte[] payload)
        {
            byte[] errorResponse = Encoding.ASCII.GetBytes("sFA");
            for (int i = 0; i < errorResponse.Length; i++)
            {
                if (errorResponse[i] != payload[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ReadVariable(string name, out byte[] payload, out byte checkSum)
        {
            SendCommand("sRN " + name);

            bool success = ReceiveResponse(out payload, out checkSum);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to read variable {name} (no response).");
            }
            if (IsErrorResponse(payload))
            {
                throw new InvalidOperationException($"Failed to read variable {name} (not successful).");
            }
        }

        private int ReadVariableInt32(string name)
        {
            ReadVariable(name, out byte[] payload, out byte checkSum);
            Int32 value = Utils.FromBigEndianInt32(payload, payload.Length - 4);
            log.DebugFormat("Got value: {0}", value);
            return value;
        }

        private byte ReadVariableByte(string name)
        {
            ReadVariable(name, out byte[] payload, out byte checkSum);
            byte value = payload[payload.Length - 1];
            log.DebugFormat("Got value: {0}", value);
            return value;
        }

        private string ReadVariableString(string name)
        {
            ReadVariable(name, out byte[] payload, out byte checkSum);

            string value = Encoding.ASCII.GetString(payload).Remove(0, 4 + name.Length).TrimStart();
            StringBuilder printable = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((byte)c < 32)
                {
                    continue;
                }
                printable.Append(c);
            }
            value = printable.ToString();
            log.DebugFormat("Got value: {0}", value);
            return value;
        }

        #region Frame Wrapping

        /// <summary>
        /// Binary framing used to serialize commands.
        /// Adds START_STX, length and checksum.
        /// </summary>
        /// <param name="payload">actual command</param>
        /// <returns>framed message</returns>
        private byte[] AddFraming(byte[] bytes)
        {
            // calculate sizes and prepare message
            int msgSize = bytes.Length + START_STX.Length + 1 + 4; // +1 for checksum, +4 for size of payload
            uint payloadSize = (uint)bytes.Length;
            byte[] payloadSizeBytes = Utils.ToBigEndian(payloadSize);
            byte[] message = new byte[msgSize];
            byte checksum = ChkSumCola(bytes);

            // build message
            int i;
            for (i = 0; i < START_STX.Length; ++i)
            {
                message[i] = START_STX[i];
            }
            for (int j = 0; j < 4; ++j)
            {
                message[i++] = payloadSizeBytes[j];
            }
            for (int j = 0; j < bytes.Length; ++j)
            {
                message[i++] = bytes[j];
            }
            message[i] = checksum;

            return message;
        }

        /// <summary>
        /// CheckSum: XOR over all bytes.
        /// </summary>
        /// <param name="value">byte array to compute checksum for</param>
        /// <returns>checksum byte</returns>
        private static byte ChkSumCola(byte[] value)
        {
            if (value.Length == 0)
                return 0x00;
            if (value.Length == 1)
                return value[0];

            byte x = value[0];
            for (int i = 1; i < value.Length; ++i)
                x ^= value[i];

            return x;
        }

        #endregion Frame Wrapping

        private void SendCommand(byte[] command)
        {
            byte[] telegram = AddFraming(command);
            streamControl.Write(telegram, 0, telegram.Length);
        }

        private void SendCommand(string command)
        {
            SendCommand(Encoding.ASCII.GetBytes(command));
        }

        private void SendCommand(string command, byte value)
        {
            List<byte> bytes = new List<byte>(Encoding.ASCII.GetBytes(command + " "));
            bytes.Add(value);
            SendCommand(bytes.ToArray());
        }

        private void SendCommand(string command, int value)
        {
            List<byte> bytes = new List<byte>(Encoding.ASCII.GetBytes(command + " "));
            byte[] valueBytes = Utils.ToBigEndian(value);
            bytes.AddRange(valueBytes);
            SendCommand(bytes.ToArray());
        }

        private bool ReceiveResponse(out byte[] payload, out byte checkSum)
        {
            payload = new byte[0];
            checkSum = 0;

            byte[] receiveHeader = new byte[8];
            if (streamControl.Read(receiveHeader, 0, receiveHeader.Length) == 0)
            {
                log.Error("Got no response from camera");
                return false;
            }

            // first 4 bytes must be STX
            for (int i = 0; i < START_STX.Length; i++)
            {
                if (START_STX[i] != receiveHeader[i])
                {
                    log.Error($"Response did not start with {nameof(START_STX)}.");
                    return false;
                }
            }

            Int32 payloadLength = Utils.FromBigEndianInt32(receiveHeader, 4);

            byte[] receivePayload = new byte[payloadLength + 1]; // 1 Byte for checksum
            streamControl.Read(receivePayload, 0, receivePayload.Length);

            payload = new byte[payloadLength];
            Array.Copy(receivePayload, payload, payloadLength);

            checkSum = receivePayload[receivePayload.Length - 1];

            log.DebugFormat("Received paylod: {0}", Encoding.ASCII.GetString(payload));
            return true;
        }
    }
}
