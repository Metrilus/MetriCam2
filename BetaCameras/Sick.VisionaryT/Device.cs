// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Metrilus.Logging;

namespace MetriCam2.Cameras.Internal.Sick
{
    /// <summary>
    /// This class handles the communication between the camera and the client.
    /// </summary>
    internal class Device
    {
        #region Private Constants
        private const int TCP_PORT_BLOBSERVER = 2113;
        private const int TCP_PORT_SOPAS      = 2112;
        private readonly byte[] START_STX     = { 0x02, 0x02, 0x02, 0x02 };
        // The following command was generated with the python sample
        private readonly byte[] commandServiceLevel = { 0x02, 0x02, 0x02, 0x02, 0x00, 0x00, 0x00, 0x17, 0x73, 0x4d,
            0x4e, 0x20, 0x53, 0x65, 0x74, 0x41, 0x63, 0x63, 0x65, 0x73, 0x73, 0x4d, 0x6f, 0x64, 0x65, 0x20, 0x04, 0xed, 0x78, 0x4b, 0xaa, 0x45 };
        private const int FRAGMENT_SIZE       = 1024;
        private const string HEARTBEAT_MSG    = "BlbReq";
        #endregion

        #region Private Variables
        private string ipAddress;
        private Camera cam;
        private MetriLog log;
        private TcpClient sockControl;
        private TcpClient sockData;
        private NetworkStream streamControl;
        private NetworkStream streamData;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new Device instance which can be used to handle the low level TCP communication
        /// between camera and client.
        /// </summary>
        /// <param name="ip">IP address of client</param>
        /// <param name="cam">MetriCam2 camera object used for exceptions</param>
        /// <param name="log">MetriLog</param>
        public Device(string ip, Camera cam, MetriLog log)
        {
            ipAddress = ip;
            this.cam  = cam;
            this.log  = log;

            sockControl   = null;
            sockData      = null;
            streamControl = null;
            streamData    = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Tells the device that there is a streaming channel.
        /// </summary>
        public void Control_InitStream()
        {
            log.Debug("Initializing streaming");
            byte[] toSend = AddFraming("sMN GetBlobClientConfig");
            byte[] receive = new byte[50];

            // send ctrl message
            streamControl.Write(toSend, 0, toSend.Length);

            // get response
            if (streamControl.Read(receive, 0, receive.Length) == 0)
            {
                log.Error("Got no answer from camera");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_setParameter", "Failed to init stream.");
            }
            else
            {
                string response = Encoding.ASCII.GetString(receive);
                log.DebugFormat("Got response: {0}", response);
            }
            log.Debug("Done: Initializing streaming");
        }

        /// <summary>
        /// Starts streaming the data by calling "PLAYSTART" method on the device.
        /// </summary>
        public void Control_StartStream()
        {
            log.Debug("Starting Stream");
            byte[] toSend = AddFraming("sMN PLAYSTART");
            byte[] receive = new byte[14];

            // send ctrl message
            streamControl.Write(toSend, 0, toSend.Length);

            // get response
            if (streamControl.Read(receive, 0, receive.Length) == 0)
            {
                log.Error("Got no answer from camera");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_setParameter", "Failed to start stream.");
            }
            else
            {
                string response = Encoding.ASCII.GetString(receive);
                log.DebugFormat("Got response: {0}", response);
            }

            log.Debug("Done: Starting Stream");
        }

        /// <summary>
        /// Stops the data stream on the device.
        /// </summary>
        public void Control_StopStream()
        {
            log.Debug("Stopping Stream");
            byte[] toSend = AddFraming("sMN PLAYSTOP");
            byte[] receive = new byte[14];

            // send ctrl message
            streamControl.Write(toSend, 0, toSend.Length);

            // get response
            if (streamControl.Read(receive, 0, receive.Length) == 0)
            {
                log.Error("Got no answer from camera");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_setParameter", "Failed to stop stream.");
            }
            else
            {
                string response = Encoding.ASCII.GetString(receive);
                log.DebugFormat("Got response: {0}", response);
            }

            log.Debug("Done: Stopping Stream");
        }

        /// <summary>
        /// Sets the access mode on the device.
        /// </summary>
        public void Control_SetServiceAccessMode()
        {
            byte[] receive = new byte[28];
            // TODO: Instead of using hard-coded command, implement propper command builder.
            streamControl.Write(commandServiceLevel, 0, commandServiceLevel.Length);

            // get response
            if (streamControl.Read(receive, 0, receive.Length) == 0)
            {
                log.Error("Got no answer from camera");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_setParameter", "Failed to stop stream.");
            }
            else
            {
                string response = Encoding.ASCII.GetString(receive);
                log.DebugFormat("Got response: {0}", response);
            }

            log.Debug("Done: Setting access mode");
        }
        public void Control_SetIntegrationTime(int value)
        {
            log.Debug("Setting integration time");
            Control_WriteVariable("integrationTimeUs", value);
            log.Debug("Done: Setting integration time");
        }
        public int Control_GetIntegrationTime()
        {
            log.Debug("Getting integration time");
            int value = Control_ReadVariable("integrationTimeUs");
            log.Debug("Done: Getting integration time");

            return value;
        }
        private void Control_WriteVariable(string name, int value)
        {
            List<byte> bytes = new List<byte>(Encoding.ASCII.GetBytes("sWN " + name + " "));
            byte[] valueBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(valueBytes);
            }
            bytes.AddRange(valueBytes);
            byte[] toSend = AddFraming(bytes.ToArray());
            byte[] receive = new byte[14];

            // send ctrl message
            streamControl.Write(toSend, 0, toSend.Length);

            // get response
            if (streamControl.Read(receive, 0, receive.Length) == 0)
            {
                log.Error("Got no answer from camera");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_setParameter", "Failed to start stream.");
            }
            else
            {
                string response = Encoding.ASCII.GetString(receive);
                log.DebugFormat("Got response: {0}", response);
            }
        }
        private int Control_ReadVariable(string name)
        {
            List<byte> bytes = new List<byte>(Encoding.ASCII.GetBytes("sRN " + name));
            byte[] toSend = AddFraming(bytes.ToArray());
            byte[] receiveHeader = new byte[8];

            // send ctrl message
            streamControl.Write(toSend, 0, toSend.Length);

            // get response
            if (streamControl.Read(receiveHeader, 0, receiveHeader.Length) == 0)
            {
                log.Error("Got no answer from camera");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_setParameter", "Failed to start stream.");
                return 0;
            }
            else
            {
                // TODO: Check if header matches expected.
                // Remark: On big endian machines this won't work:
                byte[] payloadLengthBytes = new byte[] { receiveHeader[7], receiveHeader[6], receiveHeader[5], receiveHeader[4] };
                int payloadLength = BitConverter.ToInt32(payloadLengthBytes, 0);
                byte[] receivePayload = new byte[payloadLength + 1];
                streamControl.Read(receivePayload, 0, receivePayload.Length);
                // last byte is checksum, the 4 bytes before are the response value in big endian.
                int l = receivePayload.Length;
                byte[] responseValueBytes = new byte[] { receivePayload[l - 5], receivePayload[l - 4], receivePayload[l - 3], receivePayload[l - 2] };
                int value = BitConverter.ToInt32(responseValueBytes, 0);
                log.DebugFormat("Got value: {0}", value);
                return value;
            }
        }
        /// <summary>
        /// Gets the raw frame data from camera.
        /// </summary>
        /// <remarks>Data is checked for correct protocol version and packet type.</remarks>
        /// <returns>Raw frame</returns>
        public byte[] Stream_GetFrame()
        {
            log.Debug("Start getting frame");
            List<byte> data = new List<byte>();
            byte[] buffer = new byte[FRAGMENT_SIZE];
            int read = 0;

            // first read and check header
            read = streamData.Read(buffer, 0, buffer.Length);
            if (read < 11)
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_getData", "Not enough bytes received: " + read);

            // check buffer content
            uint magicWord, pkgLength;
            ushort protocolVersion;
            byte packetType;
            magicWord = BitConverter.ToUInt32(buffer, 0);
            pkgLength = BitConverter.ToUInt32(buffer, 4);
            protocolVersion = BitConverter.ToUInt16(buffer, 8);
            packetType = buffer[10];

            // take care of endianness
            magicWord = Utils.ConvertEndiannessUInt32(magicWord);
            pkgLength = Utils.ConvertEndiannessUInt32(pkgLength);
            protocolVersion = Utils.ConvertEndiannessUInt16(protocolVersion);

            if (magicWord != 0x02020202)
            {
                log.ErrorFormat("MagicWord is wrong. Got {0}", magicWord);
                ExceptionBuilder.Throw(typeof(Exception), cam, "error_getData", "MagicWord is wrong.");
            }
            if (protocolVersion != 0x0001)
            {
                log.ErrorFormat("ProtocolVersion is wrong. Got {0}", protocolVersion);
                ExceptionBuilder.Throw(typeof(Exception), cam, "error_getData", "ProtocolVersion is wrong.");
            }
            if (packetType != 0x62)
            {
                log.ErrorFormat("PacketType is wrong. Got {0}", packetType);
                ExceptionBuilder.Throw(typeof(Exception), cam, "error_getData", "PacketType is wrong.");
            }

            // get actual frame data
            data.AddRange(buffer.Take(read));
            pkgLength++; // checksum byte
            int alreadyReceived = read - 8;

            while (alreadyReceived < pkgLength)
            {
                read = streamData.Read(buffer, 0, (int)Math.Min(FRAGMENT_SIZE, pkgLength - alreadyReceived));
                if (read == 0)
                {
                    log.Error("Failed to receive bytes from camera.");
                    ExceptionBuilder.Throw(typeof(Exception), cam, "error_getData", "Failed to read Frame.");
                }
                data.AddRange(buffer.Take(read));
                alreadyReceived += read;
            }

            log.Debug("Done: Start getting frame");
            return data.ToArray();
        }

        /// <summary>
        /// This methods establishes TCP connections to control and data port.
        /// </summary>
        public void Connect()
        {
            try
            {
                sockControl = new TcpClient(ipAddress, TCP_PORT_SOPAS);
                sockData = new TcpClient(ipAddress, TCP_PORT_BLOBSERVER);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed to connect to IP={0}, reasons={1}", ipAddress, ex.Message);
                ExceptionBuilder.Throw(ex.GetType(), cam, "error_connectionFailed", "Unable to connect to camera.");
            }

            streamControl = sockControl.GetStream();
            streamData = sockData.GetStream();

            // say "hello" to camera
            byte[] hbBytes = Encoding.ASCII.GetBytes(HEARTBEAT_MSG);
            streamData.Write(hbBytes, 0, hbBytes.Length);
        }

        /// <summary>
        /// Close connections to both ports.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                streamControl.Close();
                streamData.Close();
                sockControl.Close();
                sockData.Close();
            }
            catch (Exception ex)
            {
                log.Error("Close on sockets/streams failed: " + ex.Message);
            }
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Binary framing used to serialize commands.
        /// Adds START_STX, size and checksum.
        /// </summary>
        /// <param name="payload">actual command</param>
        /// <returns>framed message</returns>
        private byte[] AddFraming(string payload)
        {
            // transform to ASCII (1 byte per character)
            byte[] bytes = Encoding.ASCII.GetBytes(payload);

            return AddFraming(bytes);
        }

        private byte[] AddFraming(byte[] bytes)
        {
            // calculate sizes and prepare message
            int msgSize = bytes.Length + START_STX.Length + 1 + 4; // +1 for checksum, +4 for size of payload
            uint payloadSize = (uint)bytes.Length;
            byte[] payloadSizeBytes = BitConverter.GetBytes(payloadSize);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payloadSizeBytes);
            }
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
        private byte ChkSumCola(byte[] value)
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

        /// <summary>
        /// CheckSum: XOR over all bytes.
        /// </summary>
        /// <param name="value">string to compute checksum for</param>
        /// <returns>checksum byte</returns>
        private byte ChkSumCola(string value)
        {
            return ChkSumCola(Encoding.ASCII.GetBytes(value));
        }

        /// <summary>
        /// Gets the byte array as hex string. Used for debugging purpose only.
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>hex string</returns>
        private string GetHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes);
        }
        #endregion
    }
}
