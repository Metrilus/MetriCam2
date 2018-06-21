// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Metrilus.Logging;
using System.IO;

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
        /// <param name="ipAddress">IP address of client</param>
        /// <param name="cam">MetriCam2 camera object used for exceptions</param>
        /// <param name="log">MetriLog</param>
        internal Device(string ipAddress, Camera cam, MetriLog log)
        {
            this.cam  = cam;
            this.log  = log;

            try
            {
                sockData = new TcpClient(ipAddress, TCP_PORT_BLOBSERVER);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed to connect to IP={0}, reasons={1}", ipAddress, ex.Message);
                ExceptionBuilder.Throw(ex.GetType(), cam, ex);
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
        internal byte[] GetFrameData()
        {
            log.Debug("Start getting frame");
            List<byte> data = new List<byte>();
            byte[] buffer = new byte[FRAGMENT_SIZE];
            int numBytesRead = 0;

            // first read and check header
            numBytesRead = streamData.Read(buffer, 0, buffer.Length);
            // The header is at least 11 bytes long
            if (numBytesRead < 11)
            {
                ExceptionBuilder.Throw(typeof(IOException), cam, "error_getData", "Not enough bytes received: " + numBytesRead + " (expected 11 or more)");
            }

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

            if (0x02020202 != magicWord)
            {
                string msg = string.Format("The framing header is not 0x02020202 as expected: {0:X8}", magicWord);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }
            if (0x0001 != protocolVersion)
            {
                string msg = string.Format("The protocol version is not 0x0001 as expected: {0:X4}", protocolVersion);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }
            if (0x62 != packetType)
            {
                string msg = string.Format("The packet type is not 0x62 as expected: {0:X2}", packetType);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }

            // get actual frame data
            data.AddRange(buffer.Take(numBytesRead));
            pkgLength++; // checksum byte
            int alreadyReceived = numBytesRead - 8;
            int bytesRemaining = (int)pkgLength - alreadyReceived;

            while (bytesRemaining > 0)
            {
                numBytesRead = streamData.Read(buffer, 0, (int)Math.Min(FRAGMENT_SIZE, bytesRemaining));
                if (0 == numBytesRead)
                {
                    string msg = "Failed to read raw frame data from camera.";
                    log.Error(msg);
                    ExceptionBuilder.Throw(typeof(IOException), cam, "error_getData", msg);
                }
                data.AddRange(buffer.Take(numBytesRead));
                bytesRemaining -= numBytesRead;
            }

            log.Debug("Done getting frame");
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
