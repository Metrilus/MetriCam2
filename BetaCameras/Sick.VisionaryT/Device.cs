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
        private const int FRAGMENT_SIZE       = 1024;
        private const string HEARTBEAT_MSG    = "BlbReq";
        #endregion

        #region Private Variables
        private readonly string ipAddress;
        private readonly Camera cam;
        private MetriLog log;
        private TcpClient sockData;
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
        internal Device(string ip, Camera cam, MetriLog log)
        {
            ipAddress = ip;
            this.cam  = cam;
            this.log  = log;

            try
            {
                sockData = new TcpClient(ipAddress, TCP_PORT_BLOBSERVER);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed to connect to IP={0}, reasons={1}", ipAddress, ex.Message);
                ExceptionBuilder.Throw(ex.GetType(), cam, "error_connectionFailed", "Unable to connect to camera.");
            }

            streamData = sockData.GetStream();

            // say "hello" to camera
            byte[] hbBytes = Encoding.ASCII.GetBytes(HEARTBEAT_MSG);
            streamData.Write(hbBytes, 0, hbBytes.Length);
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Gets the raw frame data from camera.
        /// </summary>
        /// <remarks>Data is checked for correct protocol version and packet type.</remarks>
        /// <returns>Raw frame</returns>
        internal byte[] Stream_GetFrame()
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
        /// Close connections to both ports.
        /// </summary>
        internal void Disconnect()
        {
            try
            {
                streamData.Close();
                sockData.Close();
            }
            catch (Exception ex)
            {
                log.Error("Close on sockets/streams failed: " + ex.Message);
            }
        }
        #endregion
    }
}
