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

        #region 100kMode
        /// <summary>
        /// Control pixel binning.
        /// </summary>
        public bool Resolution100k
        {
            get
            {
                if (!IsConnected)
                {
                    return false;
                }

                int imageResolution = -1;
                DoEdit((_edit) => {
                    imageResolution = Convert.ToInt32(_appImager.GetParameter("Resolution"));
                });

                // a Resolution value of 1 means binning is disabled (i.e. 100k pixels resolution)
                return (1 == imageResolution);
            }
            set
            {
                if (!IsConnected)
                {
                    return;
                }

                DoEdit((_edit) => {
                    string res = _appImager.SetParameter("Resolution", (value ? "1" : "0"));
                    GetResolution();
                });
            }
        }
        private ParamDesc<bool> Resolution100kDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "100k px resolution";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }
        #endregion

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

                DoEdit((_edit) => {
                    _frequencyChannel = Convert.ToInt32(_appImager.GetParameter("Channel"));
                });

                return _frequencyChannel;
            }
            set
            {
                _frequencyChannel = value;
                if (!this.IsConnected)
                {
                    return;
                }

                DoEdit((_edit) => {
                    _appImager.SetParameter("Channel", value.ToString());
                });
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

                DoEdit((_edit) => {
                    _framerate = Convert.ToInt32(_appImager.GetParameter("FrameRate"));
                });

                return _framerate;
            }
            set
            {
                _framerate = value;
                if (!IsConnected)
                {
                    return;
                }

                DoEdit((_edit) => {
                    _appImager.SetParameter("FrameRate", value.ToString());
                });
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

                DoEdit((_edit) => {
                    _exposureTime = Convert.ToInt32(_appImager.GetParameter("ExposureTime"));
                });

                return _exposureTime;
            }
            set
            {
                _exposureTime = value;
                if (!IsConnected)
                {
                    return;
                }

                DoEdit((_edit) => {
                    _appImager.SetParameter("ExposureTime", value.ToString());
                });
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
        public string CameraIP { get; set; } = null;
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
        private CancellationTokenSource _cancelUpdateThreadSource = new CancellationTokenSource();
        private object _frontLock = new object();
        private object _backLock = new object();
        private AutoResetEvent _frameAvailable = new AutoResetEvent(false);
        private byte[] _backBuffer = new byte[0];
        private byte[] _frontBuffer = new byte[0];
        private const float _scalingFactor = 1.0f / 1000.0f; // All units in mm, thus we need to divide by 1000 to obtain meters

        private Socket _clientSocket;
        private Mode _configurationMode = Mode.Run;
        private int _triggeredMode = 1;
        private int _applicationId;
        private int _width;
        private int _height;

        private ISession _session;
        private IDevice _device;
        private IAppImager _appImager;
        private IApp _app;
        private IEdit _edit;
        private IEditDevice _editeDevice;
        private IServer _server;

        private string _serverUrl;
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
            XMLRPCPort = 80;
            ImageOutputPort = 50010;
            _session = XmlRpcProxyGen.Create<ISession>();
            _device = XmlRpcProxyGen.Create<IDevice>();
            _appImager = XmlRpcProxyGen.Create<IAppImager>();
            _app = XmlRpcProxyGen.Create<IApp>();
            _edit = XmlRpcProxyGen.Create<IEdit>();
            _editeDevice = XmlRpcProxyGen.Create<IEditDevice>();
            _server = XmlRpcProxyGen.Create<IServer>();
            _applicationId = applicationId;
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
            if(String.IsNullOrWhiteSpace(CameraIP))
            {
                throw new ConnectionFailedException($"{Name}: No camera IP specified");
            }

            SetConfigurationMode(Mode.Edit);
            string protocolVersion = _device.GetParameter("PcicProtocolVersion");
            if (_applicationId == -1)
            {
                for (int i = 1; i < 33; i++)
                {
                    try
                    {
                        _edit.DeleteApplication(i);
                        log.Debug($"Deleted application: {i}");
                    }
                    catch { /* empty */ }
                }
                _applicationId = _edit.CreateApplication();
                _edit.EditApplication(_applicationId);
                _triggeredMode = 1;
                _app.SetParameter("TriggerMode", _triggeredMode.ToString());
                _appImager.SetParameter("ExposureTime", _exposureTime.ToString());
                _appImager.SetParameter("FrameRate", _framerate.ToString());
            }
            else
            {
                try
                {
                    _edit.EditApplication(_applicationId);
                }
                catch (Exception)
                {
                    ExceptionBuilder.Throw(typeof(ArgumentException), this, "error_invalidApplicationId", _applicationId.ToString());
                }
            }

            GetResolution();

            _app.Save();
            _edit.StopEditingApplication();
            _device.SetParameter("ActiveApplication", _applicationId.ToString());
            _device.Save();
            SetConfigurationMode(Mode.Run);

            ActivateChannel(ChannelNames.Amplitude);
            ActivateChannel(ChannelNames.Distance);
            ActivateChannel(ChannelNames.Point3DImage);
            ActivateChannel(ChannelNames.ZImage);
            ActivateChannel(ChannelNames.ConfidenceMap);
            ActivateChannel(ChannelNames.RawConfidenceMap);
            SelectChannel(ChannelNames.Amplitude);
            _clientSocket = ConnectSocket(CameraIP, ImageOutputPort);

            _updateThread.Start();
        }

        private void UpdateLoop()
        {
            while (!_cancelUpdateThreadSource.Token.IsCancellationRequested)
            {
                byte[] hdrBuffer = new byte[16];
                Receive(_clientSocket, hdrBuffer, 0, 16, 300000);
                var str = Encoding.Default.GetString(hdrBuffer, 5, 9);
                Int32.TryParse(str, out int frameSize);

                lock (_backLock)
                {
                    if (_backBuffer.Length != frameSize)
                    {
                        _backBuffer = new byte[frameSize];
                    }
                    Receive(_clientSocket, _backBuffer, 0, frameSize, 300000);
                    _frameAvailable.Set();
                }
            } // while

            _cancelUpdateThreadSource = new CancellationTokenSource();
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
            _cancelUpdateThreadSource.Cancel();
            _updateThread.Join();

            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Disconnect(true);
            SetConfigurationMode(Mode.Edit);
            _edit.DeleteApplication(_applicationId);
            _device.Save();
            SetConfigurationMode(Mode.Run);
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Update"/>
        protected override void UpdateImpl()
        {
            _frameAvailable.WaitOne();

            lock (_backLock)
            lock (_frontLock)
            {
                byte[] tmpRef = _frontBuffer;
                _frontBuffer = _backBuffer;
                _backBuffer = tmpRef;
                _frameAvailable.Reset();
            }
        }



        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        /// <seealso cref="Camera.CalcChannel"/>
        protected override CameraImage CalcChannelImpl(string channelName)
        {
            lock(_frontLock)
            {
                switch (channelName)
                {
                    case ChannelNames.Amplitude:
                        return CalcAmplidueImge();
                    case ChannelNames.Distance:
                        return CalcDistanceImage();
                    case ChannelNames.Point3DImage:
                        return CalcPoint3fImage();
                    case ChannelNames.ZImage:
                        return CalcZImage();
                    case ChannelNames.ConfidenceMap:
                        return CalcConfidenceMap(CalcRawConfidanceImage());
                    case ChannelNames.RawConfidenceMap:
                        return CalcRawConfidanceImage();
                }
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
            if (_configurationMode == state)
            {
                return "0";
            }

            switch (state)
            {
                case Mode.Edit:
                    SetUrls();
                    res = _session.SetOperatingMode(((int)Mode.Edit).ToString());
                    break;

                case Mode.Run:
                default:
                    res = _session.SetOperatingMode(((int)Mode.Run).ToString());
                    res = _session.CancelSession();
                    break;
            }
            _configurationMode = state;
            return res;
        }

        private string RequestSessionId()
        {
            try
            {
                return _server.RequestSession("", "");
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

        private void SetUrls()
        {
            _serverUrl = "http://" + CameraIP + ":" + XMLRPCPort.ToString() + "/api/rpc/v1/com.ifm.efector/";
            _server.Url = _serverUrl;
            _session.Url = _serverUrl + "session_" + RequestSessionId() + "/";
            _edit.Url = _session.Url + "edit/";
            _device.Url = _edit.Url + "device/";
            _app.Url = _session.Url + "edit/application/";
            _appImager.Url = _session.Url + "edit/application/imager_001";
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

        private FloatCameraImage CalcAmplidueImge()
        {
            FloatCameraImage amplitudeImage = new FloatCameraImage(_width, _height);

            using (MemoryStream stream = new MemoryStream(_frontBuffer))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.BaseStream.Position = CalcBufferPosition(0);
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        amplitudeImage[y, x] = (float)reader.ReadUInt16();
                    }
                }
            }

            return amplitudeImage;
        }

        private FloatCameraImage CalcDistanceImage()
        {
            FloatCameraImage distanceImage = new FloatCameraImage(_width, _height);

            using (MemoryStream stream = new MemoryStream(_frontBuffer))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.BaseStream.Position = CalcBufferPosition(1);
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        distanceImage[y, x] = (float)reader.ReadUInt16() * _scalingFactor;
                    }
                }
            }

            return distanceImage;
        }

        private Point3fCameraImage CalcPoint3fImage()
        {
            Point3fCameraImage point3fImage = new Point3fCameraImage(_width, _height);
            FloatCameraImage xImage = new FloatCameraImage(_width, _height);
            FloatCameraImage yImage = new FloatCameraImage(_width, _height);
            FloatCameraImage zImage = new FloatCameraImage(_width, _height);

            using (MemoryStream stream = new MemoryStream(_frontBuffer))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.BaseStream.Position = CalcBufferPosition(2);
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        xImage[y, x] = (float)reader.ReadInt16() * _scalingFactor;
                    }
                }

                reader.BaseStream.Position += 36;
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        yImage[y, x] = (float)reader.ReadInt16() * _scalingFactor;
                    }
                }

                reader.BaseStream.Position += 36;
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        zImage[y, x] = (float)reader.ReadInt16() * _scalingFactor;
                    }
                }
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    point3fImage[y, x] = new Point3f(xImage[y, x], yImage[y, x], zImage[y, x]);
                }
            }

            return point3fImage;
        }

        private FloatCameraImage CalcZImage()
        {
            FloatCameraImage zImage = new FloatCameraImage(_width, _height);

            using (MemoryStream stream = new MemoryStream(_frontBuffer))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.BaseStream.Position = CalcBufferPosition(4);
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        zImage[y, x] = (float)reader.ReadInt16() * _scalingFactor;
                    }
                }
            }

            return zImage;
        }

        private ByteCameraImage CalcRawConfidanceImage()
        {
            ByteCameraImage rawConfidenceImage = new ByteCameraImage(_width, _height);

            using (MemoryStream stream = new MemoryStream(_frontBuffer))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.BaseStream.Position = CalcBufferPosition(5);
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        rawConfidenceImage[y, x] = reader.ReadByte();
                    }
                }
            }

            return rawConfidenceImage;
        }

        private long CalcBufferPosition(int image)
        {
            // Position |     0     |     1    | 2 | 3 | 4 |      5        |
            // -------------------------------------------------------------
            // Image    | amplitude | distance | x | y | z | rawConfidence |

            // After the start sequence of 8 bytes, the images follow in chunks
            // Each image chunk contains 36 bytes header including information such as image dimensions and pixel format.
            // We do not need to parse the header of each chunk as it is should be the same for every frame (except for a timestamp)
            return 44 + image * (_height * _width * 2 + 36);
        }

        private void DoEdit(Action<IEdit> editAction)
        {
            if(Mode.Run != _configurationMode)
            {
                throw new InvalidOperationException($"{Name}: can't edit settings unless camera is in Run Mode");
            }
            SetConfigurationMode(Mode.Edit);
            try
            {
                _edit.EditApplication(_applicationId);
                editAction(_edit);
                _app.Save();
            }
            finally
            {
                _edit.StopEditingApplication();
                SetConfigurationMode(Mode.Run);
            }
        }

        private void GetResolution()
        {
            int clippingTop = Convert.ToInt32(_appImager.GetParameter("ClippingTop"));
            int clippingBottom = Convert.ToInt32(_appImager.GetParameter("ClippingBottom"));
            int clippingLeft = Convert.ToInt32(_appImager.GetParameter("ClippingLeft"));
            int clippingRight = Convert.ToInt32(_appImager.GetParameter("ClippingRight"));
            _width = clippingRight - clippingLeft + 1; // indices are zero based --> +1
            _height = clippingBottom - clippingTop + 1;
        }
        #endregion
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  