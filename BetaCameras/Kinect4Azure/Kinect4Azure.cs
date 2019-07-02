using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.AzureKinect;
using Metrilus.Util;
using MetriCam2.Exceptions;

namespace MetriCam2.Cameras
{
    public class Kinect4Azure : Camera, IDisposable
    {
        private Device _device = null;
        private Capture _capture = null;
        private bool _disposed = false;
        private object _lock = new object();

        private Dictionary<string, RigidBodyTransformation> _extrinsicsCache = new Dictionary<string, RigidBodyTransformation>();
        private Dictionary<string, IProjectiveTransformation> _intrinsicsCache = new Dictionary<string, IProjectiveTransformation>();
        private K4AColorResolution _lastValidColorResolution = K4AColorResolution.r720p; //Last valid mode != off
        private K4ADepthMode _lastValidDepthMode = K4ADepthMode.WFOV_Unbinned; //Last valid mode != off

        public enum K4AColorResolution
        {
            Off = 0,
            r720p = 1,
            r1080p = 2,
            r1440p = 3,
            r1536p = 4,
            r2160p = 5,
            r3072p = 6
        }

        public enum K4AFPS
        {
            fps5 = 0,
            fps15 = 1,
            fps30 = 2
        }

        public enum K4ADepthMode
        {
            Off = 0,
            NFOV_2x2Binned = 1,
            NFOV_Unbinned = 2,
            WFOV_2x2Binned = 3,
            WFOV_Unbinned = 4,
            //PassiveIR = 5
        }

        internal enum Intrinsics
        {
            Cx,
            Cy,
            Fx,
            Fy,
            K1,
            K2,
            K3,
            K4,
            K5,
            K6,
            Codx,
            Cody,
            P2,
            P1,
            MetricRadius
        }

        #region Properties

        public override string Vendor
        {
            get { return "Microsoft"; }
        }

        private ColorResolution _colorResolution = Microsoft.AzureKinect.ColorResolution.r720p;
        public K4AColorResolution ColorResolution
        {
            get
            {
                return (K4AColorResolution)_colorResolution;
            }

            set
            {
                if ((ColorResolution)value != _colorResolution)
                {
                    _colorResolution = (ColorResolution)value;
                    if(value != K4AColorResolution.Off)
                    {
                        _lastValidColorResolution = value;
                    }
                    if (IsConnected)
                    {
                        RestartCamera();
                    }
                }
            }
        }

        ListParamDesc<K4AColorResolution> ColorResolutionDesc
        {
            get
            {
                ListParamDesc<K4AColorResolution> res = new ListParamDesc<K4AColorResolution>(typeof(K4AColorResolution))
                {
                    Description = "Resolution of the Color Image",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };

                return res;
            }
        }

        private FPS _fps = FPS.fps30;
        public K4AFPS Fps
        {
            get
            {
                return (K4AFPS)_fps;
            }
        }

        ListParamDesc<K4AFPS> FpsDesc
        {
            get
            {
                ListParamDesc<K4AFPS> res = new ListParamDesc<K4AFPS>(typeof(K4AFPS))
                {
                    Description = "Frames per second",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                };

                return res;
            }
        }

        private DepthMode _depthMode = Microsoft.AzureKinect.DepthMode.WFOV_Unbinned;
        public K4ADepthMode DepthMode
        {
            get
            {
                return (K4ADepthMode)_depthMode;
            }

            set
            {
                if ((DepthMode)value != _depthMode)
                {
                    _depthMode = (DepthMode)value;
                    if (value != K4ADepthMode.Off)
                    {
                        _lastValidDepthMode = value;
                    }
                    if (IsConnected)
                    {
                        RestartCamera();
                    }
                }
            }
        }

        ListParamDesc<K4ADepthMode> DepthModeDesc
        {
            get
            {
                ListParamDesc<K4ADepthMode> res = new ListParamDesc<K4ADepthMode>(typeof(K4ADepthMode))
                {
                    Description = "Depth Mode",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };

                return res;
            }
        }
        #endregion

#if !NETSTANDARD2_0
        public override Icon CameraIcon { get => Properties.Resources.MSIcon; }
#endif

        public Kinect4Azure() : base("Kinect4Azure")
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
            Channels.Add(cr.RegisterChannel(ChannelNames.Distance));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
        }

        protected override void ConnectImpl()
        {
            bool haveSerial = !string.IsNullOrWhiteSpace(SerialNumber);

            if (!haveSerial)
            {
                _device = Device.Open(0);
                this.SerialNumber = _device.SerialNum;
            }
            else
            {
                for(int i = 0; i < Device.GetInstalledCount(); i++)
                {
                    Device tmpDev = Device.Open(i);
                    if (SerialNumber == tmpDev.SerialNum)
                    {
                        _device = tmpDev;
                        break;
                    }
                    tmpDev.Dispose();
                }
            }

            if (ActiveChannels.Count == 0)
            {
                AddToActiveChannels(ChannelNames.Color);
                AddToActiveChannels(ChannelNames.ZImage);
                AddToActiveChannels(ChannelNames.Distance);
                AddToActiveChannels(ChannelNames.Intensity);
            }

            RestartCamera();
        }

        private void RestartCamera()
        {
            lock(_lock)
            {
                _device.StopCameras();
                if (DepthMode == K4ADepthMode.WFOV_Unbinned)
                {
                    _fps = FPS.fps15;
                }
                else
                {
                    _fps = FPS.fps30;
                }

                bool synchronizedImagesOnly = _colorResolution != Microsoft.AzureKinect.ColorResolution.Off && _depthMode != Microsoft.AzureKinect.DepthMode.Off; //Off-mode does not support synchronization

                _device.StartCameras(new DeviceConfiguration
                {
                    ColorFormat = Microsoft.AzureKinect.ImageFormat.ColorBGRA32,
                    ColorResolution = _colorResolution,
                    DepthMode = _depthMode,
                    SynchronizedImagesOnly = synchronizedImagesOnly,
                    CameraFPS = _fps,
                });
            }
        }

        protected override void DisconnectImpl()
        {
            _intrinsicsCache.Clear();
            _extrinsicsCache.Clear();

            if (null != _device)
            {
                _device.StopCameras();
                _device.Dispose();
            }
        }

        protected override void UpdateImpl()
        {
            lock(_lock)
            {
                try
                {
                    _capture = _device.GetCapture();
                }
                catch (Microsoft.AzureKinect.Exception e)
                {
                    string msg = $"{Name}: getting new capture failed: {e.Message}";
                    log.Error(msg);
                    throw new ImageAcquisitionFailedException(msg);
                }
            }
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            if (null == _capture)
            {
                throw new ImageAcquisitionFailedException("Call 'update' before calculating an image");
            }

            switch (channelName)
            {
                case ChannelNames.Color:
                    return CalcColor();

                case ChannelNames.ZImage:
                    return CalcZImage();

                case ChannelNames.Distance:
                    return CalcDistanceImage();

                case ChannelNames.Intensity:
                    return CalcIntensityImage();
            }

            throw new ImageAcquisitionFailedException($"Channel {channelName} not supported!");
        }

        unsafe private ColorCameraImage CalcColor()
        {
            if (_capture.Color == null)
            {
                throw new ImageAcquisitionFailedException($"Cannot acquire color channel. Please check, if it has been turned off.");
            }

            int height = _capture.Color.HeightPixels;
            int width = _capture.Color.WidthPixels;

            if (_capture.Color.Format != Microsoft.AzureKinect.ImageFormat.ColorBGRA32)
            {
                throw new ImageAcquisitionFailedException($"Expected format ColorBGRA32, found format {_capture.Color.Format.ToString()}");
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle imageRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte* source = (byte*)_capture.Color.Buffer;
            byte* target = (byte*)(void*)bmpData.Scan0;
            for (int y = 0; y < height; y++)
            {
                byte* sourceLine = source + y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    target[0] = *sourceLine++;
                    target[1] = *sourceLine++;
                    target[2] = *sourceLine++;
                    target += 3;
                    sourceLine++;
                }
            }

            bitmap.UnlockBits(bmpData);
            return new ColorCameraImage(bitmap);
        }

        unsafe private FloatCameraImage CalcZImage()
        {
            if (_capture.Depth == null)
            {
                throw new ImageAcquisitionFailedException($"Cannot acquire depth channel. Please check, if it has been turned off.");
            }

            int height = _capture.Depth.HeightPixels;
            int width = _capture.Depth.WidthPixels;

            if (_capture.Depth.Format != Microsoft.AzureKinect.ImageFormat.Depth16)
            {
                throw new ImageAcquisitionFailedException($"Expected format Depth16, found format {_capture.Depth.Format.ToString()}");
            }

            FloatCameraImage depthData = new FloatCameraImage(width, height);
            short* source = (short*)_capture.Depth.Buffer;

            for (int i = 0; i < depthData.Length; i++)
            {
                depthData[i] = *source++ / 1000.0f;
            }

            return depthData;
        }

        unsafe private FloatCameraImage CalcIntensityImage()
        {
            int height = _capture.IR.HeightPixels;
            int width = _capture.IR.WidthPixels;

            if (_capture.IR.Format != Microsoft.AzureKinect.ImageFormat.IR16)
            {
                throw new ImageAcquisitionFailedException($"Expected format IR16, found format {_capture.IR.Format.ToString()}");
            }

            FloatCameraImage irData = new FloatCameraImage(width, height);
            short* source = (short*)_capture.IR.Buffer;

            for (int i = 0; i < irData.Length; i++)
            {
                irData[i] = *source++;
            }

            return irData;
        }

        private FloatCameraImage CalcDistanceImage()
        {
            FloatCameraImage zImage = CalcZImage();
            ProjectiveTransformationRational projTrans = GetIntrinsics(ChannelNames.ZImage) as ProjectiveTransformationRational;
            Point3fCameraImage p3fImage = projTrans.ZImageToWorld(zImage);
            return p3fImage.ToFloatCameraImage();
        }

        public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            string keyName = channelName == ChannelNames.Color ? $"{channelName}_{ColorResolution.ToString()}" : $"{channelName}_{DepthMode.ToString()}";
            if (_intrinsicsCache.ContainsKey(keyName) && _intrinsicsCache[keyName] != null)
            {
                return _intrinsicsCache[keyName];
            }

            Calibration calibration = _device.GetCalibration();
            float metricRadius = 0.0f;
            Calibration.Intrinsics intrinsics;
            int width;
            int height;

            switch (channelName)
            {
                case ChannelNames.Color:
                    intrinsics = calibration.color_camera_calibration.intrinsics;
                    width = calibration.color_camera_calibration.resolution_width;
                    height = calibration.color_camera_calibration.resolution_height;
                    metricRadius = calibration.color_camera_calibration.metric_radius;
                    break;

                case ChannelNames.ZImage:
                case ChannelNames.Intensity:
                case ChannelNames.Distance:
                    intrinsics = calibration.depth_camera_calibration.intrinsics;
                    width = calibration.depth_camera_calibration.resolution_width;
                    height = calibration.depth_camera_calibration.resolution_height;
                    metricRadius = calibration.depth_camera_calibration.metric_radius;
                    break;

                default:
                    string msg = string.Format("{0}: no valid intrinsics for channel {1}", Name, channelName);
                    log.Error(msg);
                    throw new System.Exception(msg);
            }

            IProjectiveTransformation projTrans = new ProjectiveTransformationRational(
                width,
                height,
                intrinsics.parameters[(int)Intrinsics.Fx],
                intrinsics.parameters[(int)Intrinsics.Fy],
                intrinsics.parameters[(int)Intrinsics.Cx],
                intrinsics.parameters[(int)Intrinsics.Cy],
                intrinsics.parameters[(int)Intrinsics.K1],
                intrinsics.parameters[(int)Intrinsics.K2],
                intrinsics.parameters[(int)Intrinsics.K3],
                intrinsics.parameters[(int)Intrinsics.K4],
                intrinsics.parameters[(int)Intrinsics.K5],
                intrinsics.parameters[(int)Intrinsics.K6],
                intrinsics.parameters[(int)Intrinsics.P1],
                intrinsics.parameters[(int)Intrinsics.P2],
                metricRadius);

            _intrinsicsCache[keyName] = projTrans;
            return projTrans;
        }

        public override RigidBodyTransformation GetExtrinsics(string channelFromName, string channelToName)
        {
            string keyName = $"{channelFromName}_{channelToName}";
            if (_extrinsicsCache.ContainsKey(keyName) && _extrinsicsCache[keyName] != null)
            {
                return _extrinsicsCache[keyName];
            }

            Calibration calibration = _device.GetCalibration();
            RotationMatrix rotMat;
            Point3f translation;

            if (channelFromName == ChannelNames.Color && (channelToName == ChannelNames.Distance || channelToName == ChannelNames.ZImage || channelToName == ChannelNames.Intensity))
            {
                rotMat = new RotationMatrix(calibration.depth_camera_calibration.extrinsics.rotation);
                translation = new Point3f(
                    calibration.depth_camera_calibration.extrinsics.translation[0] / 1000f,
                    calibration.depth_camera_calibration.extrinsics.translation[1] / 1000f,
                    calibration.depth_camera_calibration.extrinsics.translation[2] / 1000f);
            }
            else if ((channelFromName == ChannelNames.Distance || channelFromName == ChannelNames.ZImage || channelFromName == ChannelNames.Intensity) && channelToName == ChannelNames.Color)
            {
                rotMat = new RotationMatrix(calibration.color_camera_calibration.extrinsics.rotation);
                translation = new Point3f(
                    calibration.color_camera_calibration.extrinsics.translation[0] / 1000f,
                    calibration.color_camera_calibration.extrinsics.translation[1] / 1000f,
                    calibration.color_camera_calibration.extrinsics.translation[2] / 1000f);
            }
            else
            {
                string msg = string.Format("{0}: no valid extrinsics from channel {1} to {2}", Name, channelFromName, channelToName);
                log.Error(msg);
                throw new System.Exception(msg);
            }

            RigidBodyTransformation rbt = new RigidBodyTransformation(rotMat, translation);
            _extrinsicsCache[keyName] = rbt;
            return rbt;
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            if (channelName == ChannelNames.Color && ColorResolution == K4AColorResolution.Off)
            {
                log.Debug($"Set color resolution to {_lastValidColorResolution.ToString()}");
                ColorResolution = _lastValidColorResolution;
            }

            if ((channelName == ChannelNames.Distance || channelName == ChannelNames.ZImage) && DepthMode == K4ADepthMode.Off)
            {
                log.Debug($"Set depth mode to {_lastValidDepthMode.ToString()}");
                DepthMode = _lastValidDepthMode;
            }
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            if (channelName == ChannelNames.Color)
            {
                ColorResolution = K4AColorResolution.Off;
                return;
            }

            int numberActivatedDepthChannels = 0;
            if(IsChannelActive(ChannelNames.Intensity))
            {
                numberActivatedDepthChannels++;
            }
            if (IsChannelActive(ChannelNames.Distance))
            {
                numberActivatedDepthChannels++;
            }
            if (IsChannelActive(ChannelNames.ZImage))
            {
                numberActivatedDepthChannels++;
            }
            //If only one depth channels is activated and we deactivate it, the depth mode can be set to "off".
            if(numberActivatedDepthChannels == 1 && (channelName == ChannelNames.Intensity || channelName == ChannelNames.Distance || channelName == ChannelNames.ZImage))
            {
                DepthMode = K4ADepthMode.Off;
            }
        }

        private void CheckConnected([CallerMemberName] String propertyName = "")
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(string.Format("The property '{0}' can only be read or written when the camera is connected!", propertyName));
            }
        }
    }
}
