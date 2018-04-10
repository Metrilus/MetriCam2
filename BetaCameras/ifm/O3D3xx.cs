// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using CookComputing.XmlRpc;
using MetriCam2.Cameras.IFM;
using Metrilus.Util;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MetriCam2.Exceptions;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// MetriCam2 Wrapper for ifm O3D3xx cameras.
    /// </summary>
    public class O3D3xx : Camera
    {
        [DllImport("iphlpapi.dll")]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);

        private enum Mode
        {
            Run = 0,
            Edit = 1,
        }

        #region Public Properties

        #region Frequency Channel
        /// <summary>
        /// Frequency channel
        /// </summary>
        private int _frequencyChannel = 0;
        public int FrequencyChannel
        {
            get
            {
                if (!this.IsConnected)
                {
                    return -1;
                }
                SetConfigurationMode(Mode.Edit);
                edit.EditApplication(applicationId);

                _frequencyChannel = Convert.ToInt32(appImager.GetParameter("Channel"));

                SetConfigurationMode(Mode.Run);
                return _frequencyChannel;
            }
            set
            {
                _frequencyChannel = value;
                if (!this.IsConnected)
                {
                    return;
                }
                SetConfigurationMode(Mode.Edit);
                edit.EditApplication(applicationId);
                appImager.SetParameter("Channel", value.ToString());
                app.Save();
                edit.StopEditingApplication();
                SetConfigurationMode(Mode.Run);
            }
        }

        private RangeParamDesc<int> FrequencyChannelDesc
        {
            get
            {
                RangeParamDesc<int> res = new RangeParamDesc<int>(0, 3);
                res.Description = "Frequency Channel"; ;
                res.Unit = "";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }
        #endregion

        #region Framerate
        /// <summary>
        /// The camera framerate.
        /// </summary>
        private int _framerate = 25;
        public int Framerate
        {
            get
            {
                if (!this.IsConnected)
                {
                    return -1;
                }
                SetConfigurationMode(Mode.Edit);
                edit.EditApplication(applicationId);
                _framerate = Convert.ToInt32(appImager.GetParameter("FrameRate"));
                edit.StopEditingApplication();
                SetConfigurationMode(Mode.Run);
                return _framerate;
            }
            set
            {
                _framerate = value;
                if (!IsConnected)
                {
                    return;
                }
                SetConfigurationMode(Mode.Edit);
                edit.EditApplication(applicationId);
                appImager.SetParameter("FrameRate", value.ToString());
                app.Save();
                edit.StopEditingApplication();
                SetConfigurationMode(Mode.Run);
            }
        }
        private RangeParamDesc<int> FramerateDesc
        {
            get
            {
                RangeParamDesc<int> res = new RangeParamDesc<int>(0, 25);
                res.Description = "Framerate"; ;
                res.Unit = "fps";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }
        #endregion

        #region Integration Time
        /// <summary>
        /// Integration (exposure time)
        /// </summary>
        private int _exposureTime = 1234;
        public int IntegrationTime
        {
            get
            {
                if (!this.IsConnected)
                {
                    return -1;
                }
                SetConfigurationMode(Mode.Edit);
                edit.EditApplication(applicationId);
                _exposureTime = Convert.ToInt32(appImager.GetParameter("ExposureTime"));
                edit.StopEditingApplication();
                SetConfigurationMode(Mode.Run);
                return _exposureTime;
            }
            set
            {
                _exposureTime = value;
                if (!IsConnected)
                {
                    return;
                }
                SetConfigurationMode(Mode.Edit);
                edit.EditApplication(applicationId);
                appImager.SetParameter("ExposureTime", value.ToString());
                app.Save();
                edit.StopEditingApplication();
                SetConfigurationMode(Mode.Run);
            }
        }
        private RangeParamDesc<int> IntegrationTimeDesc
        {
            get
            {
                RangeParamDesc<int> res = new RangeParamDesc<int>(0, 10000);
                res.Description = "Integration time"; ;
                res.Unit = "us";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }
        #endregion

        #region Camera IP
        /// <summary>
        /// IP Address of camera.
        /// </summary>
        public string CameraIP { get; set; }
        private ParamDesc<String> CameraIPDesc
        {
            get
            {
                ParamDesc<String> res = new ParamDesc<String>();
                res.Description = "IP address of camera";
                res.Unit = "IPv4";
                res.ReadableWhen = ParamDesc.ConnectionStates.Disconnected | ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }
        #endregion

        #region XML-RPC Port
        /// <summary>
        /// XML RPC Port (80).
        /// </summary>
        public int XMLRPCPort { get; set; }
        private ParamDesc<String> XMLRPCPortDesc
        {
            get
            {
                ParamDesc<String> res = new ParamDesc<String>();
                res.Description = "XML-RPC port";
                res.Unit = "";
                res.ReadableWhen = ParamDesc.ConnectionStates.Disconnected | ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }
        #endregion

        #region Image Output Port
        /// <summary>
        /// Image output port (50010).
        /// </summary>
        public int ImageOutputPort { get; set; }
        private ParamDesc<String> ImageOutputPortDesc
        {
            get
            {
                ParamDesc<String> res = new ParamDesc<String>();
                res.Description = "Image output port";
                res.Unit = "";
                res.ReadableWhen = ParamDesc.ConnectionStates.Disconnected | ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }
        #endregion

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.IfmIcon; }
#endif
        #endregion

        #region Private Fields
        private Thread _updateThread;
        private bool _cancelUpdateThread = false;
        private object updateLock = new object();
        private AutoResetEvent _frameAvailable = new AutoResetEvent(false);
        private byte[] _frameBuffer = new byte[0];

        private Socket clientSocket;
        private Mode configurationMode = Mode.Run;
        private int triggeredMode = 1;
        private int applicationId;
        private int width;
        private int height;

        private FloatCameraImage _amplitudeImage;
        private FloatCameraImage _distanceImage;
        private Point3fCameraImage _point3fImage;
        private FloatCameraImage _zImage;
        private ByteCameraImage _rawConfidenceImage;

        private ISession session;
        private IDevice device;
        private IAppImager appImager;
        private IApp app;
        private IEdit edit;
        private IEditDevice editeDevice;
        private IServer server;

        private string serverUrl;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="applicationId">
        /// ID of the camera application that will be loaded during connect.
        /// Warning: Omitting this parameter deletes all applications from the camera and creates a new application with default MetriCam 2 parameter values.
        /// </param>
        public O3D3xx(int applicationId = -1)
                : base(modelName: "O3D3xx")
        {
            CameraIP = "192.168.1.172";
            XMLRPCPort = 80;
            ImageOutputPort = 50010;
            session = XmlRpcProxyGen.Create<ISession>();
            device = XmlRpcProxyGen.Create<IDevice>();
            appImager = XmlRpcProxyGen.Create<IAppImager>();
            app = XmlRpcProxyGen.Create<IApp>();
            edit = XmlRpcProxyGen.Create<IEdit>();
            editeDevice = XmlRpcProxyGen.Create<IEditDevice>();
            server = XmlRpcProxyGen.Create<IServer>();
            this.applicationId = applicationId;
            _updateThread = new Thread(new ThreadStart(UpdateLoop));
        }
        #endregion

        #region MetriCam2 Camera Interface
        #region MetriCam2 Camera Interface Properties
        #endregion

        #region MetriCam2 Camera Interface Methods
        /// <summary>
        /// Resets list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
        /// </summary>
        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;

            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Amplitude));
            Channels.Add(cr.RegisterChannel(ChannelNames.Distance));
            Channels.Add(cr.RegisterChannel(ChannelNames.Point3DImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.ConfidenceMap));
            Channels.Add(cr.RegisterCustomChannel(ChannelNames.RawConfidenceMap, typeof(ByteCameraImage)));
        }

        /// <summary>
        /// Device-specific implementation of Connect.
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Connect"/>
        protected override void ConnectImpl()
        {
            SetConfigurationMode(Mode.Edit);
            string protocolVersion = device.GetParameter("PcicProtocolVersion");
            if (applicationId == -1)
            {
                for (int i = 1; i < 33; i++)
                {
                    try
                    {
                        edit.DeleteApplication(i);
                        log.Debug($"Deleted application: {i}");
                    }
                    catch { /* empty */ }
                }
                applicationId = edit.CreateApplication();
                edit.EditApplication(applicationId);
                triggeredMode = 1;
                app.SetParameter("TriggerMode", triggeredMode.ToString());
                appImager.SetParameter("ExposureTime", _exposureTime.ToString());
                appImager.SetParameter("FrameRate", _framerate.ToString());
            }
            else
            {
                try
                {
                    edit.EditApplication(applicationId);
                }
                catch (Exception)
                {
                    ExceptionBuilder.Throw(typeof(ArgumentException), this, "error_invalidApplicationId", applicationId.ToString());
                }
            }

            int clippingTop = Convert.ToInt32(appImager.GetParameter("ClippingTop"));
            int clippingBottom = Convert.ToInt32(appImager.GetParameter("ClippingBottom"));
            int clippingLeft = Convert.ToInt32(appImager.GetParameter("ClippingLeft"));
            int clippingRight = Convert.ToInt32(appImager.GetParameter("ClippingRight"));
            width = clippingRight - clippingLeft + 1; // indices are zero based --> +1
            height = clippingBottom - clippingTop + 1;

            app.Save();
            edit.StopEditingApplication();
            device.SetParameter("ActiveApplication", applicationId.ToString());
            device.Save();
            SetConfigurationMode(Mode.Run);

            ActivateChannel(ChannelNames.Amplitude);
            ActivateChannel(ChannelNames.Distance);
            ActivateChannel(ChannelNames.Point3DImage);
            ActivateChannel(ChannelNames.ZImage);
            ActivateChannel(ChannelNames.ConfidenceMap);
            ActivateChannel(ChannelNames.RawConfidenceMap);
            SelectChannel(ChannelNames.Amplitude);
            clientSocket = ConnectSocket(CameraIP, ImageOutputPort);

            _updateThread.Start();
        }

        private void UpdateLoop()
        {
            while (!_cancelUpdateThread)
            {
                byte[] hdrBuffer = new byte[16];
                byte[] frameBuffer = new byte[0];

                Receive(clientSocket, hdrBuffer, 0, 16, 300000);
                var str = Encoding.Default.GetString(hdrBuffer, 5, 9);
                Int32.TryParse(str, out int frameSize);

                if (frameBuffer.Length != frameSize)
                {
                    frameBuffer = new byte[frameSize];
                }
                Receive(clientSocket, frameBuffer, 0, frameSize, 300000);

                lock (updateLock)
                {
                    _frameBuffer = frameBuffer;
                    _frameAvailable.Set();
                }
            } // while

            _cancelUpdateThread = false;
        }

        /// <summary>
        /// Serial number of camera. The camera does not provide a serial number therefore we use the MAC address.
        /// </summary>
        public override string SerialNumber { get => RequestMACAddress(CameraIP); }

        /// <summary>
        /// Name of camera vendor.
        /// </summary>
        public override string Vendor { get => "ifm"; }

        /// <summary>
        /// Device-specific implementation of Disconnect.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Disconnect"/>
        protected override void DisconnectImpl()
        {
            _cancelUpdateThread = true;
            _updateThread.Join();
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Update"/>
        protected override void UpdateImpl()
        {
            if (!IsConnected)
            {
                return;
            }
            lock (updateLock)
            {
                _frameAvailable.WaitOne();

                _amplitudeImage = new FloatCameraImage(width, height);
                _distanceImage = new FloatCameraImage(width, height);
                _point3fImage = new Point3fCameraImage(width, height);
                _rawConfidenceImage = new ByteCameraImage(width, height);
                _zImage = new FloatCameraImage(width, height);

                FloatCameraImage xImage = new FloatCameraImage(width, height);
                FloatCameraImage yImage = new FloatCameraImage(width, height);
                

                // Response shall start with ASCII string "0000star"
                using (MemoryStream stream = new MemoryStream(_frameBuffer))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // All units in mm, thus we need to divide by 1000 to obtain meters
                    const float factor = 1.0f / 1000.0f;

                    // After the start sequence of 8 bytes, the images follow in chunks
                    // Each image chunk contains 36 bytes header including information such as image dimensions and pixel format.
                    // We do not need to parse the header of each chunk as it is should be the same for every frame (except for a timestamp)
                    reader.BaseStream.Position = 44;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            _amplitudeImage[y, x] = (float)reader.ReadUInt16();
                        }
                    }

                    reader.BaseStream.Position += 36;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            _distanceImage[y, x] = (float)reader.ReadUInt16();
                            _distanceImage[y, x] *= factor;
                        }
                    }

                    reader.BaseStream.Position += 36;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            xImage[y, x] = (float)reader.ReadInt16();
                            xImage[y, x] *= factor;
                        }
                    }

                    reader.BaseStream.Position += 36;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            yImage[y, x] = (float)reader.ReadInt16();
                            yImage[y, x] *= factor;
                        }
                    }

                    reader.BaseStream.Position += 36;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            _zImage[y, x] = (float)reader.ReadInt16();
                            _zImage[y, x] *= factor;
                        }
                    }

                    reader.BaseStream.Position += 36;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            _rawConfidenceImage[y, x] = reader.ReadByte();
                        }
                    }
                } // using memstream

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        _point3fImage[y, x] = new Point3f(xImage[y, x], yImage[y, x], _zImage[y, x]);
                    }
                }

                if (_amplitudeImage == null 
                || _distanceImage == null 
                || _point3fImage == null 
                || _zImage == null 
                || _rawConfidenceImage == null)
                {
                    string msg = $"{Name}: One or more images failed to be computed";
                    log.Error(msg);
                    throw new ImageAcquisitionFailedException(msg);
                }
                _frameAvailable.Reset();
            }
        }
            
        

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        /// <seealso cref="Camera.CalcChannel"/>
        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Amplitude:
                    return _amplitudeImage;
                case ChannelNames.Distance:
                    return _distanceImage;
                case ChannelNames.Point3DImage:
                    return _point3fImage;
                case ChannelNames.ZImage:
                    return _zImage;
                case ChannelNames.ConfidenceMap:
                    return CalcConfidenceMap(_rawConfidenceImage);
                case ChannelNames.RawConfidenceMap:
                    return _rawConfidenceImage;
            }
            ExceptionBuilder.Throw(typeof(ArgumentException), this, "error_invalidChannelName", channelName);
            return null;
        }
        #endregion
        #endregion

        #region Private Methods
        /// <summary>
        /// Converts raw confidence image to [0, 1] range.
        /// According to sensor operating instructions, the highest bits indicates whether a pixel is valid or not.
        /// More information about invalid bits would be encoded in the remaining bits.
        /// </summary>
        /// <param name="rawConfidenceMap">Raw confidence image as provided by camera</param>
        /// <returns>Confidence image as float image in range [0, 1]</returns>
        private FloatCameraImage CalcConfidenceMap(ByteCameraImage rawConfidenceMap)
        {
            int width = rawConfidenceMap.Width;
            int height = rawConfidenceMap.Height;

            FloatCameraImage confidenceMap = new FloatCameraImage(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool invalid = (rawConfidenceMap[y, x] & 0x80) > 0;
                    confidenceMap[y, x] = invalid ? 0.0f : 1.0f;
                }
            }
            return confidenceMap;
        }

        private void Receive(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int received = 0;  // how many bytes is already received
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                {
                    throw new TimeoutException("Timeout while receiving data");
                }

                try
                {
                    received += socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably empty, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                    {
                        throw;  // any serious error occurred
                    }
                }
            } while (received < size);
        }

        private string SetConfigurationMode(Mode state)
        {
            string res;
            if (configurationMode == state)
            {
                return "0";
            }

            switch(state)
            {
                case Mode.Edit:
                    SetUrls();
                    res = session.SetOperatingMode(Mode.Edit.ToString());
                    break;

                case Mode.Run:
                default:
                    res = session.SetOperatingMode(Mode.Run.ToString());
                    res = session.CancelSession();
                    break;
            }
            configurationMode = state;
            return res;
        }

        private string RequestSessionId()
        {
            try
            {
                return server.RequestSession("", "");
            }
            catch (Exception)
            {
                throw;
            }
        }

        private Socket ConnectSocket(string server, int port)
        {
            Socket s = null;
            IPHostEntry hostEntry = null;
            Exception caughtException = null;

            // Get host related information.    
            try
            {
                hostEntry = Dns.GetHostEntry(server);

                // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
                // an exception that occurs when the host IP Address is not compatible with the address family
                // (typical in the IPv6 case).
                foreach (IPAddress address in hostEntry.AddressList)
                {
                    IPEndPoint ipe = new IPEndPoint(address, port);
                    Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);

                    if (tempSocket.Connected)
                    {
                        s = tempSocket;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            if (s == null) // this case will occur if the camera is directly connected to the PC
            {
                //Try to connect directly without DNS address resolution
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(server), port);
                Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.Connect(ipe);

                if (tempSocket.Connected)
                {
                    s = tempSocket;
                }
                else
                {
                    if (caughtException != null)
                    {
                        throw caughtException;
                    }
                    else
                    {
                        throw new WebException(String.Format("Could not establish TCP connection to {0}:{1}.", server, port.ToString()));
                    }
                }
            }
            return s;
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private void SetUrls()
        {
            serverUrl = "http://" + CameraIP + ":" + XMLRPCPort.ToString() + "/api/rpc/v1/com.ifm.efector/";
            server.Url = serverUrl;
            session.Url = serverUrl + "session_" + RequestSessionId() + "/";
            edit.Url = session.Url + "edit/";
            device.Url = edit.Url + "device/";
            app.Url = session.Url + "edit/application/";
            appImager.Url = session.Url + "edit/application/imager_001";
        }

        /// <summary>
        /// Requests the MAC address using Address Resolution Protocol
        /// </summary>
        /// <param name="IP">The IP.</param>
        /// <returns>the MAC address</returns>
        private string RequestMACAddress(string ip)
        {
            byte[] mac = new byte[6];
            int length = mac.Length;

            IPAddress tempA = IPAddress.Parse(ip);
            byte[] bytes = tempA.GetAddressBytes();
            string strAddress = "";
            for (int index = bytes.Length - 1; index >= 0; index--)
            {
                string strTemp = bytes[index].ToString("x");
                if (strTemp.Length == 1)
                    strAddress += "0";
                strAddress += strTemp;
            }

            int address = int.Parse(strAddress, System.Globalization.NumberStyles.HexNumber); ;

            SendARP(address, 0, mac, ref length);
            string macAddress = BitConverter.ToString(mac, 0, length);
            return macAddress;
        }

        private void UpdateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Disconnect(true);
            SetConfigurationMode(Mode.Edit);
            edit.DeleteApplication(applicationId);
            device.Save();
            SetConfigurationMode(Mode.Run);
        }
        #endregion
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  