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

                // reset frame available to force a new frame with the correct resolution 
                _frameAvailable.Reset();
            }
        }
        private ParamDesc<bool> Resolution100kDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "100k resolution (352x264px)";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }
        #endregion

        #region Trigger Mode
        /// <summary>
        /// The camera trigger mode.
        /// </summary>
        private O3D3xxTriggerMode _triggerMode = O3D3xxTriggerMode.FreeRun;
        public O3D3xxTriggerMode TriggerMode
        {
            get
            {
                if (!IsConnected)
                {
                    return O3D3xxTriggerMode.FreeRun;
                }

                DoEdit((_edit) => {
                    _triggerMode = (O3D3xxTriggerMode)Convert.ToInt32(_app.GetParameter("TriggerMode"), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                });

                return _triggerMode;
            }
            set
            {
                _triggerMode = value;
                if (!IsConnected)
                {
                    return;
                }

                DoEdit((_edit) => {
                    _app.SetParameter("TriggerMode", ((int)value).ToString());
                });
            }
        }
        private ListParamDesc<O3D3xxTriggerMode> TriggerModeDesc
        {
            get
            {
                ListParamDesc<O3D3xxTriggerMode> res = new ListParamDesc<O3D3xxTriggerMode>(typeof(O3D3xxTriggerMode))
                {
                    Description = "Trigger mode",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected
                };
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
                if (!IsConnected)
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
                if (!IsConnected)
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
                RangeParamDesc<int> res = new RangeParamDesc<int>(-1, 3)
                {
                    Description = "Frequency Channel",
                    Unit = "",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected
                };
                return res;
            }
        }
        #endregion

        #region Framerate
        /// <summary>
        /// The camera framerate.
        /// </summary>
        private float _framerate = 25f;
        public float Framerate
        {
            get
            {
                if (!IsConnected)
                {
                    return -1f;
                }

                DoEdit((_edit) => {
                    _framerate = Convert.ToSingle(_appImager.GetParameter("FrameRate"), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                });

                return _framerate;
            }
            set
            {
                if(value <= 0f)
                {
                    return;
                }

                _framerate = value;
                if (!IsConnected)
                {
                    return;
                }

                DoEdit((_edit) => {
                    _appImager.SetParameter("FrameRate", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                });
            }
        }
        private RangeParamDesc<float> FramerateDesc
        {
            get
            {
                RangeParamDesc<float> res = new RangeParamDesc<float>(-1f, 25f)
                {
                    Description = "Framerate",
                    Unit = "fps",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected
                };
                return res;
            }
        }
        #endregion

        #region Integration Time Mode
        private O3D3xxIntegrationMode _integrationMode = O3D3xxIntegrationMode.TwoIntegrationTimes;
        public O3D3xxIntegrationMode IntegrationTimeMode
        {
            get
            {
                DoEdit((_edit) => {
                    string[] typeString = _appImager.GetParameter("Type").Split('_');
                    _integrationMode = (O3D3xxIntegrationMode)EnumUtils.GetEnum(typeof(O3D3xxIntegrationMode), typeString[1]);
                });

                return _integrationMode;
            }
        }
        private ListParamDesc<O3D3xxIntegrationMode> IntegrationTimeModeDesc
        {
            get
            {
                ListParamDesc<O3D3xxIntegrationMode> res = new ListParamDesc<O3D3xxIntegrationMode>(typeof(O3D3xxIntegrationMode))
                {
                    Description = "Number of different Integration used to process a single frame.",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                };
                return res;
            }
        }
        #endregion

        #region BackgroundDistance
        private O3D3xxBackgroundDistanceMode _backgroundDistance = O3D3xxBackgroundDistanceMode.MoreThan30;
        public O3D3xxBackgroundDistanceMode BackgroundDistance
        {
            get
            {
                DoEdit((_edit) => {
                    string[] typeString = _appImager.GetParameter("Type").Split('_');
                    _backgroundDistance = (O3D3xxBackgroundDistanceMode)EnumUtils.GetEnum(typeof(O3D3xxBackgroundDistanceMode), typeString[0]);
                });

                return _backgroundDistance;
            }
        }

        private ListParamDesc<O3D3xxBackgroundDistanceMode> BackgroundDistanceDesc
        {
            get
            {
                ListParamDesc<O3D3xxBackgroundDistanceMode> res = new ListParamDesc<O3D3xxBackgroundDistanceMode>(typeof(O3D3xxBackgroundDistanceMode))
                {
                    Description = "",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                };
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
                if (!IsConnected)
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
                if(value <= 0)
                {
                    return;
                }

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
                RangeParamDesc<int> res = new RangeParamDesc<int>(-1, 10000)
                {
                    Description = "Integration time",
                    Unit = "us",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected
                };
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

        private const int MininumReceiveTimeout = 500;
        private int _receiveTimeout = -1;
        /// <summary>
        /// The camera receive timeout.
        /// </summary>
        /// <remarks>
        /// * Will automatically be set to (1 / <see cref="Framerate"/>) + 50 during <see cref="Camera.Connect"/> unless it has been set before.
        /// * Minimum: 500 ms.
        /// </remarks>
        public int ReceiveTimeout
        {
            get => _receiveTimeout;
            set => _receiveTimeout = Math.Max(MininumReceiveTimeout, value);
        }
        private ParamDesc<int> ReceiveTimeoutDesc
        {
            get
            {
                ParamDesc<int> res = new ParamDesc<int>
                {
                    Description = "Receive timeout",
                    Unit = "ms",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected
                };
                return res;
            }
        }
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
        private const int _maxApplications = 32;
        private const int _maxConsecutiveReceiveFails = 3;
        private const string _applicationName = "_MetriCam2";
        private Socket _clientSocket;
        private Mode _configurationMode = Mode.Run;
        private int _applicationId = -1;

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
        private string _updateThreadError = null; // Hand errors from update loop thread to application thread
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="applicationId">
        /// ID of the camera application that will be loaded during connect.
        /// Warning: Omitting this parameter creates a new application with default MetriCam 2 parameter values.
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

        /// <summary>
        /// Default constructor. Creates a new application with default MetriCam 2 parameter values.
        /// </summary>
        public O3D3xx()
            : this(-1)
        {
        }
        #endregion

        #region MetriCam2 Camera Interface
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
            Channels.Add(cr.RegisterCustomChannel(ChannelNames.RawConfidenceMap, typeof(ByteImage)));
        }

        /// <summary>
        /// Device-specific implementation of Connect.
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Connect"/>
        protected override void ConnectImpl()
        {
            _updateThreadError = null;

            if (String.IsNullOrWhiteSpace(CameraIP))
            {
                throw new ConnectionFailedException($"{Name}: No camera IP specified");
            }

            SetConfigurationMode(Mode.Edit);
            string protocolVersion = _device.GetParameter("PcicProtocolVersion");
            if (_applicationId == -1)
            {
                Application[] apps = _server.GetApplicationList();
                int deleted = CleanupApplications(apps);
                if (apps.Length - deleted == _maxApplications)
                {
                    throw new InvalidOperationException(
                        $"{Name}: Maximum number of applications on the device reached. " +
                        $"Please either delete one of them or specify one for MetriCam2 to use");
                }

                _applicationId = _edit.CreateApplication();
                _edit.EditApplication(_applicationId);
                _app.SetParameter("Name", _applicationName);
                _app.SetParameter("Description", "MetriCam2 default application.");
                TriggerMode = _triggerMode;
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
                    _edit.StopEditingApplication();
                    SetConfigurationMode(Mode.Run);
                    throw ExceptionBuilder.Build(typeof(ArgumentException), Name, "error_invalidApplicationId", _applicationId.ToString());
                }
            }

            if (-1 == ReceiveTimeout)
            {
                // user has not set a receive timeout before connect
                float framerate = Convert.ToSingle(_appImager.GetParameter("FrameRate"));
                ReceiveTimeout = (int)(1000f / framerate) + 50;
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
            SelectChannel(ChannelNames.Distance);
            _clientSocket = ConnectSocket(CameraIP, ImageOutputPort);

            _updateThread.Start();
        }

        private int CleanupApplications(Application[] apps)
        {
            int deleted = 0;
            foreach(Application app in apps)
            {
                if(app.Name == _applicationName)
                {
                    _edit.DeleteApplication(app.Index);
                    deleted++;
                }
            }
            return deleted;
        }

        private void UpdateLoop()
        {
            int consecutiveFailCounter = 0;
            byte[] hdrBuffer = new byte[16];

            while (!_cancelUpdateThreadSource.Token.IsCancellationRequested)
            {
                try
                {
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
                }
                catch (SocketException e)
                {
                    // Ignore timeouts in edit mode
                    if (_configurationMode == Mode.Edit)
                    {
                        continue;
                    }

                    // Ignore timeouts in triggered mode
                    if (_triggerMode == O3D3xxTriggerMode.FreeRun)
                    {
                        consecutiveFailCounter++;
                        log.Warn($"{Name}: SocketException: {e.Message}");

                        if (consecutiveFailCounter >= _maxConsecutiveReceiveFails)
                        {
                            string msg = $"{Name}: Receive failed more than {_maxConsecutiveReceiveFails} times in a row. Shutting down update loop.";
                            log.Error(msg);
                            _updateThreadError = msg;
                            _frameAvailable.Set();
                            break;
                        }
                    }

                    continue;
                }

                consecutiveFailCounter = 0;
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
            CloseSocket();
            _updateThread.Join();
        }

        private void CloseSocket()
        {
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Disconnect(true);
            _clientSocket.Close();
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

            if (null != _updateThreadError)
            {
                Disconnect();
                throw new ImageAcquisitionFailedException(_updateThreadError);
            }

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
        protected override ImageBase CalcChannelImpl(string channelName)
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
            throw ExceptionBuilder.Build(typeof(ArgumentException), Name, "error_invalidChannelName", channelName);
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
        private FloatImage CalcConfidenceMap(ByteImage rawConfidenceMap)
        {
            int width = rawConfidenceMap.Width;
            int height = rawConfidenceMap.Height;

            FloatImage confidenceMap = new FloatImage(width, height);
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
            socket.ReceiveTimeout = ReceiveTimeout;

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
            Exception caughtException = null;

            // Try to connect to IP first
            if (IPAddress.TryParse(server, out IPAddress ipAddress))
            {
                try
                {
                    IPEndPoint ipe = new IPEndPoint(ipAddress, port);
                    Socket socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(ipe);
                    if (socket.Connected)
                    {
                        return socket;
                    }
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            }

            // Try to connect to host name
            IPHostEntry hostEntry = Dns.GetHostEntry(server);

            // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
            // an exception that occurs when the host IP Address is not compatible with the address family
            // (typical in the IPv6 case).
            foreach (IPAddress address in hostEntry.AddressList)
            {
                IPEndPoint ipe = new IPEndPoint(address, port);
                Socket socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipe);
                if (socket.Connected)
                {
                    return socket;
                }
            }

            if (caughtException != null)
            {
                throw caughtException;
            }
            else
            {
                throw new WebException(String.Format("Could not establish TCP connection to {0}:{1}.", server, port.ToString()));
            }
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

            int address = int.Parse(strAddress, System.Globalization.NumberStyles.HexNumber);

            SendARP(address, 0, mac, ref length);
            string macAddress = BitConverter.ToString(mac, 0, length);
            return macAddress;
        }

        private FloatImage CalcAmplidueImge()
        {
            FloatImage amplitudeImage = new FloatImage(_width, _height);

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

        private FloatImage CalcDistanceImage()
        {
            FloatImage distanceImage = new FloatImage(_width, _height);

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

        private Point3fImage CalcPoint3fImage()
        {
            Point3fImage point3fImage = new Point3fImage(_width, _height);
            FloatImage xImage = new FloatImage(_width, _height);
            FloatImage yImage = new FloatImage(_width, _height);
            FloatImage zImage = new FloatImage(_width, _height);

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

        private FloatImage CalcZImage()
        {
            FloatImage zImage = new FloatImage(_width, _height);

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

        private ByteImage CalcRawConfidanceImage()
        {
            ByteImage rawConfidenceImage = new ByteImage(_width, _height);

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
            if (Mode.Run != _configurationMode)
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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  