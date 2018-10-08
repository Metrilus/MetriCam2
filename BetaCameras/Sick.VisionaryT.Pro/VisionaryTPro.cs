// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using Metrilus.Util;
using MetriCam2.Exceptions;
using MetriCam2.Cameras.Internal.Sick;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// MetriCam2 wrapper for Sick Visionary-T Pro cameras.
    /// </summary>
    public class VisionaryTPro : Camera
    {
        private Thread _updateThread;
        private Socket _socket;
        private char[] _frontJsonData = null;
        private char[] _backJsonData = null;
        private int _intensityJsonOffset;
        private int _intensityJsonSize;
        private int _distanceJsonOffset;
        private int _distanceJsonSize;
        private int _imageHeight = 0;
        private int _imageWidth = 0;
        private AutoResetEvent _frameAvailable = new AutoResetEvent(false);
        private CancellationTokenSource _cancelUpdateThreadSource;
        private ProjectiveTransformationZhang _intrinsics = null;
        private const int NumFrameRetries = 3;
        private string _updateThreadError = null;

        private ParamDesc<string> IPAddressDesc
        {
            get
            {
                return new ParamDesc<string>()
                {
                    Description = "IP address",
                    ReadableWhen = ParamDesc.ConnectionStates.Disconnected | ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
            }
        }

        public string IPAddress { get; set; }


        private ParamDesc<int> PortDesc
        {
            get
            {
                return new ParamDesc<int>()
                {
                    Description = "Port",
                    ReadableWhen = ParamDesc.ConnectionStates.Disconnected | ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
            }
        }
        public int Port { get; set; } = 2115;

#if !NETSTANDARD2_0
        public override Icon CameraIcon => Properties.Resources.SickIcon;
#endif

        public VisionaryTPro()
            : base()
        {
            
        }

        #region MetriCam2 Camera Interface

        public new string Name { get => "Visionary-T-Pro"; }
        public new string Vendor { get => "Sick"; }

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;

            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
            Channels.Add(cr.RegisterChannel(ChannelNames.Distance));
        }


        protected override void ConnectImpl()
        {
            _updateThreadError = null;
            _updateThread = new Thread(new ThreadStart(UpdateLoop));
            _cancelUpdateThreadSource = new CancellationTokenSource();

            if (string.IsNullOrEmpty(IPAddress))
            {
                string msg = string.Format("IP address is not set. It must be set before connecting.");
                log.Error(msg);
                throw ExceptionBuilder.Build(typeof(ConnectionFailedException), Name, "error_connectionFailed", msg);
            }

            try
            {
                IPAddress camIP = System.Net.IPAddress.Parse(IPAddress);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ep = new IPEndPoint(camIP, Port);
                _socket.Connect(ep);
            }
            catch(Exception)
            {
                string msg = $"Error connecting to IP address '{IPAddress}:{Port}'.";
                log.Error(msg);
                throw ExceptionBuilder.Build(typeof(ConnectionFailedException), Name, "error_connectionFailed", msg);
            }

            ActivateChannel(ChannelNames.Distance);
            ActivateChannel(ChannelNames.Intensity);
            SelectChannel(ChannelNames.Intensity);

            _updateThread.Start();
        }

        protected override void DisconnectImpl()
        {
            _cancelUpdateThreadSource.Cancel();
            _updateThread.Join();
            _socket.Close();
            _frameAvailable.Reset();
        }

        protected override void UpdateImpl()
        {
            _frameAvailable.WaitOne();

            if (null != _updateThreadError)
            {
                Disconnect();
                throw new ImageAcquisitionFailedException(_updateThreadError);
            }

            _frontJsonData = _backJsonData;
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Intensity:
                    return ParseImage(_frontJsonData, _intensityJsonOffset, _intensityJsonSize, 1.0f);
                case ChannelNames.Distance:
                    return ParseImage(_frontJsonData, _distanceJsonOffset, _distanceJsonSize);
            }
            
            log.Error(Name + ": Invalid channelname: " + channelName);
            return null;
        }

        #endregion

        private void UpdateLoop()
        {
            int consecutiveFailCounter = 0;
            while (!_cancelUpdateThreadSource.Token.IsCancellationRequested)
            {
                try
                {
                    // 0 - CameraModel
                    // 1 - distance Image
                    // 2 - intensity Image
                    SyncCola();
                    uint jsonSize = ReceiveDataSize();
                    _backJsonData = ReceiveJsonString(jsonSize);
                    if(null == _intrinsics)
                    {
                        string json = new string(_backJsonData);
                        var frameData = JsonConvert.DeserializeObject<List<CameraObject>>(json);
                        _intrinsics = ParseIntrinsics(frameData[0]);
                        _imageWidth = frameData[1].Data.Data.Width;
                        _imageHeight = frameData[1].Data.Data.Height;

                        //Determine the offsets of intensity and distance image, which are stable over time.
                        string needle = "\"data\":\"";
                        int start_first_img = json.IndexOf(needle, 0) + needle.Length;
                        int end_first_img = json.IndexOf("\"", start_first_img);
                        _distanceJsonOffset = start_first_img;
                        _distanceJsonSize = end_first_img - start_first_img;
                        int start_second_img = json.IndexOf(needle, end_first_img) + needle.Length;
                        int end_second_img = json.IndexOf("\"", start_second_img);
                        _intensityJsonOffset = start_second_img;
                        _intensityJsonSize = end_second_img - start_second_img;

                        if ("uint16" != frameData[1].Data.Data.ImageType
                        &&  "uint16" != frameData[2].Data.Data.ImageType)
                        {
                            string msg = $"{Name}: Frame data has unexpected format: '{frameData[1].Data.Data.ImageType}', expected: 'uint16'";
                            log.Error(msg);
                            throw new ImageAcquisitionFailedException(msg);
                        }

                        if ("little" != frameData[1].Data.Data.Pixels.endian
                        &&  "little" != frameData[2].Data.Data.Pixels.endian)
                        {
                            string msg = $"{Name}: Frame data has unexpected endian: '{frameData[1].Data.Data.Pixels.endian}', expected: 'little'";
                            log.Error(msg);
                            throw new ImageAcquisitionFailedException(msg);
                        }
                    }
                    _frameAvailable.Set();
                }
                catch(Exception e)
                {
                    consecutiveFailCounter++;
                    if (consecutiveFailCounter > NumFrameRetries)
                    {
                        string msg = $"{Name}: Receive failed more than {NumFrameRetries} times in a row. Shutting down update loop.";
                        log.Error(msg);
                        log.Error(e.Message);
                        _updateThreadError = msg;
                        _frameAvailable.Set();
                        break;
                    }
                }

                // reset counter after sucessfull fetch
                consecutiveFailCounter = 0;
            }
        }

        private void SyncCola()
        {
            int elements = 0;
            while(elements < 4)
            {
                byte[] buffer = new byte[1];
                _socket.Receive(buffer, 1, SocketFlags.None);

                if(buffer[0] == 0x02)
                {
                    elements++;
                }
                else
                {
                    elements = 0;
                }
            }
        }

        private uint ReceiveDataSize()
        {
            uint bufferSize = sizeof(uint);
            byte[] buffer = new byte[bufferSize];
            _socket.Receive(buffer, (int)bufferSize, SocketFlags.None);
            Array.Reverse(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        private char[] ReceiveJsonString(uint dataSize)
        {
            byte[] buffer = new byte[dataSize];
            uint bytesReceived = 0;

            while(bytesReceived < dataSize)
            {
                int received = _socket.Receive(buffer, (int)bytesReceived, (int)(dataSize - bytesReceived), SocketFlags.None, out SocketError error);
                bytesReceived += (uint)received;
            }
            try
            {
                return Encoding.UTF8.GetChars(buffer);
            }
            catch(Exception e)
            {
                string msg = $"{Name}: Invalid UTF-8 byte sequence received: {e.Message}";
                log.Error(msg);
                throw new ImageAcquisitionFailedException(msg);
            }
        }

        private ProjectiveTransformationZhang ParseIntrinsics(CameraObject metaObj)
        {
            if (null == metaObj.Data.IntrinsicK)
            {
                string msg = $"{Name}: Invalid camera meta information received";
                log.Error(msg);
                throw new ImageAcquisitionFailedException(msg);
            }

            return new ProjectiveTransformationZhang(
                metaObj.Data.ImageWidth,
                metaObj.Data.ImageHeight,
                metaObj.Data.IntrinsicK[0][0],
                metaObj.Data.IntrinsicK[1][1],
                metaObj.Data.IntrinsicK[0][2],
                metaObj.Data.IntrinsicK[1][2],
                metaObj.Data.SensorToWorldDistortion[0][0],
                metaObj.Data.SensorToWorldDistortion[1][0],
                metaObj.Data.SensorToWorldDistortion[2][0],
                metaObj.Data.SensorToWorldDistortion[3][0],
                metaObj.Data.SensorToWorldDistortion[4][0]
                );
        }

        private unsafe FloatCameraImage ParseImage(char[] base64Data, int offset, int length, float scaleFactor = 1000.0f)
        {
            byte[] raw = Convert.FromBase64CharArray(base64Data, offset, length);

            FloatCameraImage image = new FloatCameraImage(_imageWidth, _imageHeight);

            fixed (byte* rawData = raw)
            {
                ushort* ushortData = (ushort*)rawData;
                if (scaleFactor != 1.0f)
                {
                    for (int i = 0; i < image.Length; i++)
                    {
                        image[i] = *ushortData / scaleFactor;
                        ushortData++;
                    }
                }
                else
                {
                    for (int i = 0; i < image.Length; i++)
                    {
                        image[i] = *ushortData;
                        ushortData++;
                    }
                }
            }
            
            return image;
        }

        public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            if (null == _intrinsics)
            {
                // wait for first frameset to arrive and check availablity of intrinsics again
                UpdateImpl();

                if (null == _intrinsics)
                {
                    string msg = $"{Name}: No intrinsics available";
                    log.Error(msg);
                    throw new MetriCam2Exception(msg);
                }
            }

            return _intrinsics;
        }
    }
}
