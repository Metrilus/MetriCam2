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
        private readonly VisionaryT cam;
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
        internal Device(string ipAddress, VisionaryT cam, MetriLog log)
        {
            this.cam  = cam;
            this.log  = log;

            try
            {
                sockData = new TcpClient(ipAddress, TCP_PORT_BLOBSERVER);
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}: Failed to connect to IP {1}{2}Reason: {3}", cam.Name, ipAddress, Environment.NewLine, ex.Message);
                log.Error(msg);
                throw new Exceptions.ConnectionFailedException(msg, ex);
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
            if (!Utils.SyncCoLa(streamData))
            {
                string msg = string.Format("{0}: Could not sync to CoLa bus", cam.Name);
                log.Error(msg);
                throw new IOException(msg);
            }

            log.Debug("Start getting frame");

            byte[] buffer = new byte[0];
            if (!Utils.Receive(streamData, ref buffer, 4))
            {
                string msg = string.Format("{0}: Could not read package length", cam.Name);
                log.Error(msg);
                throw new IOException(msg);
            }
            uint pkgLength = BitConverter.ToUInt32(buffer, 0);
            pkgLength = Utils.ConvertEndiannessUInt32(pkgLength);

            if (!Utils.Receive(streamData, ref buffer, (int)pkgLength))
            {
                string msg = string.Format("{0}: Could not read package payload", cam.Name);
                log.Error(msg);
                throw new IOException(msg);
            }

            // check buffer content
            int offset = 0;

            ushort protocolVersion = BitConverter.ToUInt16(buffer, offset);
            protocolVersion = Utils.ConvertEndiannessUInt16(protocolVersion);
            offset += 2;

            byte packetType = buffer[offset];
            offset += 1;

            if (0x0001 != protocolVersion)
            {
                string msg = string.Format("{0}: The protocol version is not 0x0001 as expected: {1:X4}", cam.Name, protocolVersion);
                log.Error(msg);
                throw new InvalidDataException(msg);
            }
            if (0x62 != packetType)
            {
                string msg = string.Format("{0}: The packet type is not 0x62 as expected: {1:X2}", cam.Name, packetType);
                log.Error(msg);
                throw new InvalidDataException(msg);
            }

            return buffer.Skip(offset).ToArray();
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
