using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Metrilus.Util;
using MetriCam2.Cameras.Pico.ZenseAPI;

namespace MetriCam2.Cameras
{
    public class Zense : Camera, IDisposable
    {
        private bool _disposed = false;
        private Dictionary<string, IProjectiveTransformation> intrinsicsCache = new Dictionary<string, IProjectiveTransformation>();
        private Frame _currentDepthFrame = new Frame();
        private Frame _currentColorFrame = new Frame();
        private Frame _currentIRFrame = new Frame();
        private FrameMode _currentDepthMode = new FrameMode();
        private FrameMode _currentColorMode = new FrameMode();
        private FrameMode _currentIRMode = new FrameMode();
        private int _maxUpdate = 8;
        private static bool _isInitialized = false;

        public int DeviceCount
        {
            get
            {
                CheckReturnStatus(Methods.GetDeviceCount(out int count));
                return count;
            }
        }

        public DepthRange Range
        {
            get
            {
                CheckReturnStatus(Methods.GetDepthRange(DeviceIndex, out DepthRange range));
                return range;
            }
            set
            {
                CheckReturnStatus(Methods.SetDepthRange(DeviceIndex, value));
            }
        }

        public unsafe Resolution Resolution
        {
            get
            {
                CheckReturnStatus(Methods.GetFrameMode(DeviceIndex, FrameType.RGBFrame, out FrameMode mode));

                switch(mode.resolutionHeight)
                {
                    case 360:
                        if (640 == mode.resolutionWidth)
                        {
                            return Resolution.R360P;
                        }
                        break;

                    case 720:
                        if (1280 == mode.resolutionWidth)
                        {
                            return Resolution.R720P;
                        }
                        break;

                    case 1080:
                        if (1920 == mode.resolutionWidth)
                        {
                            return Resolution.R1080P;
                        }
                        break;
                }

                throw new Exception("adsfasdf");
            }

            set
            {
                CheckReturnStatus(Methods.GetFrameMode(DeviceIndex, FrameType.RGBFrame, out FrameMode mode));

                switch (value)
                {
                    case Resolution.R360P:
                        mode.resolutionHeight = 360;
                        mode.resolutionWidth = 640;
                        break;

                    case Resolution.R720P:
                        mode.resolutionHeight = 720;
                        mode.resolutionWidth = 1280;
                        break;

                    case Resolution.R1080P:
                        mode.resolutionHeight = 1080;
                        mode.resolutionWidth = 1920;
                        break;
                }

                CheckReturnStatus(Methods.SetFrameMode(DeviceIndex, FrameType.RGBFrame, &mode));
            }
        }

        private int DeviceIndex { set; get; }

#if !NETSTANDARD2_0
        public override Icon CameraIcon { get => Properties.Resources.Pico; }
#endif


        public Zense() : base()
        {
            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (IsConnected)
                DisconnectImpl();

            if (disposing)
            {
                // dispose managed resources
            }

            _disposed = true;
        }

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
        }

        protected unsafe override void ConnectImpl()
        {
            // check if SDK is initialized
            if (!_isInitialized)
            {
                CheckReturnStatus(Methods.Initialize());
                _isInitialized = true;
            }

            if(String.IsNullOrEmpty(SerialNumber))
            {
                DeviceIndex = 0;
                CheckReturnStatus(Methods.OpenDevice(DeviceIndex));
                SerialNumber = GetStringProperty(DeviceIndex, PropertyType.SN_Str);
            }
            else
            {
                GetDeviceIndexFromSerial(SerialNumber);
            }

            if (ActiveChannels.Count == 0)
            {
                AddToActiveChannels(ChannelNames.Color);
                AddToActiveChannels(ChannelNames.ZImage);
            }
        }

        protected override void DisconnectImpl()
        {
            CheckReturnStatus(Methods.CloseDevice(DeviceIndex));
            CheckReturnStatus(Methods.Shutdown());
        }

        protected override void UpdateImpl()
        {
            for (int i = 0; i < _maxUpdate; i ++)
            {
                CheckReturnStatus(Methods.ReadNextFrame(DeviceIndex));

                CheckReturnStatus(Methods.GetFrameMode(DeviceIndex, FrameType.RGBFrame, out _currentColorMode));
                CheckReturnStatus(Methods.GetFrameMode(DeviceIndex, FrameType.DepthFrame, out _currentDepthMode));
                CheckReturnStatus(Methods.GetFrameMode(DeviceIndex, FrameType.IRFrame, out _currentIRMode));

                if (IsChannelActive(ChannelNames.Color)
                && ReturnStatus.OK != Methods.GetFrame(DeviceIndex, FrameType.RGBFrame, out _currentColorFrame))
                {
                    continue;
                }

                if (IsChannelActive(ChannelNames.ZImage)
                && ReturnStatus.OK != Methods.GetFrame(DeviceIndex, FrameType.DepthFrame, out _currentDepthFrame))
                {
                    continue;
                }

                if (IsChannelActive(ChannelNames.Intensity)
                && ReturnStatus.OK != Methods.GetFrame(DeviceIndex, FrameType.IRFrame, out _currentIRFrame))
                {
                    continue;
                }

                // success
                return;
            }

            throw new Exception("update failed");
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch(channelName)
            {
                case ChannelNames.Intensity:
                    return CalcIRImage(_currentIRMode.resolutionWidth, _currentIRMode.resolutionHeight, _currentIRFrame);
                case ChannelNames.Color:
                    return CalcColor(_currentColorMode.resolutionWidth, _currentColorMode.resolutionHeight, _currentColorFrame);
                case ChannelNames.ZImage:
                    return CalcZImage(_currentDepthMode.resolutionWidth, _currentDepthMode.resolutionHeight, _currentDepthFrame);
            }

            throw new Exception("asdfas");
        }

        unsafe private FloatCameraImage CalcIRImage(int width, int height, Frame frame)
        {
            FloatCameraImage IRData = new FloatCameraImage(width, height);
            IRData.TimeStamp = this.TimeStamp;
            ushort* source = (ushort*)frame.pFrameData;

            for (int y = 0; y < height; y++)
            {
                ushort* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    IRData[y, x] = *sourceLine++;
                }
            }

            return IRData;
        }

        unsafe private FloatCameraImage CalcZImage(int width, int height, Frame frame)
        {
            FloatCameraImage depthData = new FloatCameraImage(width, height);
            depthData.TimeStamp = this.TimeStamp;
            ushort* source = (ushort*)frame.pFrameData;

            for (int y = 0; y < height; y++)
            {
                ushort* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    depthData[y, x] = ((float)(*sourceLine++)) / 1000; // mm -> m
                }
            }

            return depthData;
        }

        unsafe private ColorCameraImage CalcColor(int width, int height, Frame frame)
        {

            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Rectangle imageRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte* source = (byte*)frame.pFrameData;
            byte* target = (byte*)(void*)bmpData.Scan0;
            for (int y = 0; y < height; y++)
            {
                byte* sourceLine = source + y * width * 3;
                for (int x = 0; x < width; x++)
                {
                    target[2] = *sourceLine++;
                    target[1] = *sourceLine++;
                    target[0] = *sourceLine++;
                    target += 3;
                }
            }

            bitmap.UnlockBits(bmpData);
            ColorCameraImage image = new ColorCameraImage(bitmap);
            image.TimeStamp = this.TimeStamp;

            return image;
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            if(IsConnected)
            {
                FrameType type = GetFrameTypeFromChannelName(channelName);
                switch(type)
                {
                    // WORKAROUND
                    // StartFrame throws an error for RGBFrame
                    // that should be ignored (pico support)
                    case FrameType.RGBFrame:
                        Methods.StartFrame(DeviceIndex, type);
                        break;

                    case FrameType.IRFrame:
                        if(this.IsChannelActive(ChannelNames.ZImage))
                        {
                            throw new Exception("Can't have it all. Either depth or ir.");
                        }
                        SetUint8Property(DeviceIndex, PropertyType.DataMode_UInt8, (byte)DataMode.IR_30);
                        CheckReturnStatus(Methods.StartFrame(DeviceIndex, type));
                        break;

                    case FrameType.DepthFrame:
                        if (this.IsChannelActive(ChannelNames.Intensity))
                        {
                            throw new Exception("Can't have it all. Either depth or ir.");
                        }
                        SetUint8Property(DeviceIndex, PropertyType.DataMode_UInt8, (byte)DataMode.Depth_30);
                        CheckReturnStatus(Methods.StartFrame(DeviceIndex, type));
                        break;
                }
            }
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            if(IsConnected)
            {
                FrameType type = GetFrameTypeFromChannelName(channelName);
                CheckReturnStatus(Methods.StopFrame(DeviceIndex, type));
            }
        }

        unsafe public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            FrameType frameType = GetFrameTypeFromChannelName(channelName);
            CheckReturnStatus(Methods.GetFrameMode(DeviceIndex, frameType, out FrameMode mode));
            string keyName = $"{channelName}_{mode.resolutionWidth}x{mode.resolutionHeight}";
            if (intrinsicsCache.ContainsKey(keyName) && intrinsicsCache[keyName] != null)
            {
                return intrinsicsCache[keyName];
            }


            SensorType sensorType = SensorType.DepthSensor;
            if(channelName == ChannelNames.Color)
            {
                sensorType = SensorType.RgbSensor;
            }
            CheckReturnStatus(Methods.GetCameraParameters(DeviceIndex, sensorType, out CameraParameters intrinsics));
            

            var projTrans = new ProjectiveTransformationZhang(
                mode.resolutionWidth,
                mode.resolutionHeight,
                (float)intrinsics.fx,
                (float)intrinsics.fy,
                (float)intrinsics.cx,
                (float)intrinsics.cy,
                (float)intrinsics.k1,
                (float)intrinsics.k2,
                (float)intrinsics.k3,
                (float)intrinsics.p1,
                (float)intrinsics.p2);

            intrinsicsCache[keyName] = projTrans;

            return projTrans;
        }

        private FrameType GetFrameTypeFromChannelName(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Color:
                    return FrameType.RGBFrame;
                case ChannelNames.ZImage:
                    return FrameType.DepthFrame;
                case ChannelNames.Intensity:
                    return FrameType.IRFrame;
                default:
                    throw new Exception("asdfsaf");
            }
        }

        private void CheckReturnStatus(ReturnStatus status)
        {
            switch (status)
            {
                case ReturnStatus.NoDeviceConnected:
                    throw new Exception("No Device Connected");

                case ReturnStatus.InvalidDeviceIndex:
                    throw new Exception("Invalid Device Index");

                case ReturnStatus.DevicePointerIsNull:
                    throw new Exception("Device Pointer is null");

                case ReturnStatus.InvalidFrameType:
                    throw new Exception("Invalid Frame Type");

                case ReturnStatus.FramePointerIsNull:
                    throw new Exception("Frame pointer is null");

                case ReturnStatus.NoPropertyValueGet:
                case ReturnStatus.NoPropertyValueSet:
                    throw new Exception("No property value");

                case ReturnStatus.PropertyPointerIsNull:
                    throw new Exception("Invalid property");

                case ReturnStatus.PropertySizeNotEnough:
                    throw new Exception("Property buffer insufficient");

                case ReturnStatus.InvalidDepthRange:
                    throw new Exception("Invalid depth range");

                case ReturnStatus.ReadNextFrameError:
                    throw new Exception("Error reading next frame");

                case ReturnStatus.InputPointerIsNull:
                    throw new Exception("Input pointer is null");

                case ReturnStatus.CameraNotOpened:
                    throw new Exception("Camera not opened");

                case ReturnStatus.InvalidCameraType:
                    throw new Exception("Invalid camera type");

                case ReturnStatus.InvalidParams:
                    throw new Exception("Invalid Parameter");
            }
        }

        private int GetDeviceIndexFromSerial(string serial)
        {
            for(int i = 0; i < DeviceCount; i++)
            {
                CheckReturnStatus(Methods.OpenDevice(i));
                if(serial == GetStringProperty(i, PropertyType.SN_Str))
                {
                    return i;
                }

                CheckReturnStatus(Methods.CloseDevice(i));
            }

            throw new Exception(String.Format("Camera with S/N '{0}' not found.", serial));
        }

        private unsafe string GetStringProperty(int deviceIndex, PropertyType type)
        {
            int size = 64;
            char[] serial = new char[size];
            fixed (char* s = serial)
            {
                CheckReturnStatus(Methods.GetProperty(deviceIndex, type, (IntPtr)s, &size));
                return Marshal.PtrToStringAnsi((IntPtr)s);
            }

            throw new Exception("Failed to receive property: " + type.ToString());
        }

        private unsafe int GetInt32Property(int deviceIndex, PropertyType type)
        {
            int i = -1;
            int size = sizeof(int);
            CheckReturnStatus(Methods.GetProperty(deviceIndex, type, (IntPtr)(&i), &size));
            return i;
        }

        private unsafe byte GetUint8Property(int deviceIndex, PropertyType type)
        {
            byte b = 1;
            int size = sizeof(byte);
            CheckReturnStatus(Methods.GetProperty(deviceIndex, type, (IntPtr)(&b), &size));
            return b;
        }

        private unsafe void SetUint8Property(int deviceIndex, PropertyType type, byte value)
        {
            CheckReturnStatus(Methods.SetProperty(deviceIndex, type, (IntPtr)(&value), sizeof(byte)));
        }
    }
}
