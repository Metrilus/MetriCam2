using System;
using System.Collections.Generic;
using System.Drawing;
using Metrilus.Util;
using MetriCam2.Exceptions;
using Microsoft.Azure.Kinect.Sensor;
using System.Buffers;
#if !NETSTANDARD2_0
using System.Drawing.Imaging;
#endif

namespace MetriCam2.Cameras
{
    public class Kinect4Azure : Camera, IDisposable
    {
        private Device _device = null;
        private Capture _capture = null;
        private bool _disposed = false;

        private Dictionary<string, RigidBodyTransformation> _extrinsicsCache = new Dictionary<string, RigidBodyTransformation>();
        private Dictionary<string, ProjectiveTransformation> _intrinsicsCache = new Dictionary<string, ProjectiveTransformation>();
        private K4AColorResolution _lastValidColorResolution = K4AColorResolution.R720p; //Last valid mode != off
        private K4ADepthMode _lastValidDepthMode = K4ADepthMode.WFOV_Unbinned; //Last valid mode != off

        public enum K4AColorResolution
        {
            Off = 0,
            R720p = 1,
            R1080p = 2,
            R1440p = 3,
            R1536p = 4,
            R2160p = 5,
            R3072p = 6
        }

        public enum K4AFPS
        {
            FPS5 = 0,
            FPS15 = 1,
            FPS30 = 2
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

        public override string Vendor { get => "Microsoft"; }

        private ColorResolution _colorResolution = Microsoft.Azure.Kinect.Sensor.ColorResolution.R720p;
        public K4AColorResolution ColorResolution
        {
            get => (K4AColorResolution)_colorResolution;

            set
            {
                if ((ColorResolution)value != _colorResolution)
                {
                    lock (cameraLock)
                    {
                        _colorResolution = (ColorResolution)value;
                        if (value != K4AColorResolution.Off)
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
        }

        private ListParamDesc<K4AColorResolution> ColorResolutionDesc
        {
            get
            {
                ListParamDesc<K4AColorResolution> res = new ListParamDesc<K4AColorResolution>(typeof(K4AColorResolution))
                {
                    Description = "Resolution of the Color channel",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };

                return res;
            }
        }

        private FPS _fps = FPS.FPS30;
        public K4AFPS Fps { get => (K4AFPS)_fps; }

        private ListParamDesc<K4AFPS> FpsDesc
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

        private DepthMode _depthMode = Microsoft.Azure.Kinect.Sensor.DepthMode.WFOV_Unbinned;
        public K4ADepthMode DepthMode
        {
            get => (K4ADepthMode)_depthMode;

            set
            {
                if ((DepthMode)value != _depthMode)
                {
                    lock (cameraLock)
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
        }

        private ListParamDesc<K4ADepthMode> DepthModeDesc
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
        public override Icon CameraIcon
        {
            get
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Properties.Resources.MSIcon))
                {
                    return new Icon(ms);
                }
            }
        }
#endif

        public Kinect4Azure() : base("Azure Kinect")
        {
            enableImplicitThreadSafety = true;
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

            for (int i = 0; i < Device.GetInstalledCount(); i++)
            {
                try
                {
                    Device tmpDev = Device.Open(i);
                    if (!haveSerial)
                    {
                        _device = tmpDev;
                        break;
                    }
                    if (SerialNumber == tmpDev.SerialNum)
                    {
                        _device = tmpDev;
                        break;
                    }
                    tmpDev.Dispose();
                }
                catch (AzureKinectException e)
                {
                    log.Warn($"Could not open Azure Kinect device number {i}. The device is probably already in use: {e.Message}");
                }
            }

            if (null == _device)
            {
                string msg = "No available Azure Kinect device found.";
                log.Error(msg);
                throw new ConnectionFailedException(msg);
            }

            SerialNumber = _device.SerialNum;

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
            _device.StopCameras();
            if (DepthMode == K4ADepthMode.WFOV_Unbinned)
            {
                _fps = FPS.FPS15;
            }
            else
            {
                _fps = FPS.FPS30;
            }

            bool synchronizedImagesOnly = _colorResolution != Microsoft.Azure.Kinect.Sensor.ColorResolution.Off && _depthMode != Microsoft.Azure.Kinect.Sensor.DepthMode.Off; //Off-mode does not support synchronization

            _device.StartCameras(new DeviceConfiguration
            {
                ColorFormat = Microsoft.Azure.Kinect.Sensor.ImageFormat.ColorBGRA32,
                ColorResolution = _colorResolution,
                DepthMode = _depthMode,
                SynchronizedImagesOnly = synchronizedImagesOnly,
                CameraFPS = _fps,
            });

            //We need to call "Update" here, otherwise we can run into problems:
            //if "RestartCamera" is called between "Update" and "CalcChannel", the images can have the "old resolution", whereas 
            //GetIntrinsics already returns the new intrinsics.
            IsConnected = true; //Otherwise update will fail after "Connect".
            Update();
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
            try
            {
                _capture = _device.GetCapture(TimeSpan.FromSeconds(2));
            }
            catch (AzureKinectException e)
            {
                string msg = $"{Name}: getting new capture failed: {e.Message}";
                log.Error(msg);
                throw new ImageAcquisitionFailedException(msg, e);
            }
        }

        protected override ImageBase CalcChannelImpl(string channelName)
        {
            if (null == _capture)
            {
                throw new ImageAcquisitionFailedException($"Call '{nameof(Update)}' before calculating an image");
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

            throw new ImageAcquisitionFailedException($"Channel '{channelName}' not supported!");
        }

        unsafe private ColorImage CalcColor()
        {
            if (_capture.Color == null)
            {
                throw new ImageAcquisitionFailedException($"Cannot acquire '{ChannelNames.Color}' channel. Please check if it has been deactivated.");
            }

            int height = _capture.Color.HeightPixels;
            int width = _capture.Color.WidthPixels;

            if (_capture.Color.Format != Microsoft.Azure.Kinect.Sensor.ImageFormat.ColorBGRA32)
            {
                throw new ImageAcquisitionFailedException($"Expected format ColorBGRA32, found format {_capture.Color.Format.ToString()}");
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle imageRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            MemoryHandle sourceHandle = _capture.Color.Memory.Pin();
            byte* source = (byte*)sourceHandle.Pointer;
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

            sourceHandle.Dispose();
            bitmap.UnlockBits(bmpData);

            return new ColorImage(bitmap);
        }

        unsafe private FloatImage CalcZImage()
        {
            if (_capture.Depth == null)
            {
                throw new ImageAcquisitionFailedException($"Cannot acquire depth channel. Please check if it has been deactivated.");
            }

            int height = _capture.Depth.HeightPixels;
            int width = _capture.Depth.WidthPixels;

            if (_capture.Depth.Format != Microsoft.Azure.Kinect.Sensor.ImageFormat.Depth16)
            {
                throw new ImageAcquisitionFailedException($"Expected format Depth16, found format {_capture.Depth.Format.ToString()}");
            }

            FloatImage depthData = new FloatImage(width, height);
            MemoryHandle sourceHandle = _capture.Depth.Memory.Pin();
            short* source = (short*)sourceHandle.Pointer;

            for (int i = 0; i < depthData.Length; i++)
            {
                depthData[i] = *source++ / 1000.0f;
            }

            sourceHandle.Dispose();

            return depthData;
        }

        unsafe private FloatImage CalcIntensityImage()
        {
            int height = _capture.IR.HeightPixels;
            int width = _capture.IR.WidthPixels;

            if (_capture.IR.Format != Microsoft.Azure.Kinect.Sensor.ImageFormat.IR16)
            {
                throw new ImageAcquisitionFailedException($"Expected format IR16, found format {_capture.IR.Format.ToString()}");
            }

            FloatImage irData = new FloatImage(width, height);           
            MemoryHandle sourceHandle = _capture.IR.Memory.Pin();
            short* source = (short*)sourceHandle.Pointer;

            for (int i = 0; i < irData.Length; i++)
            {
                irData[i] = *source++;
            }

            sourceHandle.Dispose();

            return irData;
        }

        private FloatImage CalcDistanceImage()
        {
            FloatImage zImage = CalcZImage();
            ProjectiveTransformationRational projTrans = GetIntrinsics(ChannelNames.ZImage) as ProjectiveTransformationRational;
            Point3fImage p3fImage = projTrans.ZImageToWorld(zImage);
            return p3fImage.ToFloatImage();
        }

        public override ProjectiveTransformation GetIntrinsics(string channelName)
        {
            string keyName = channelName == ChannelNames.Color ? $"{channelName}_{ColorResolution.ToString()}" : $"{channelName}_{DepthMode.ToString()}";
            if (_intrinsicsCache.ContainsKey(keyName) && _intrinsicsCache[keyName] != null)
            {
                return _intrinsicsCache[keyName];
            }

            Calibration calibration = _device.GetCalibration();
            float metricRadius = 0.0f;
            Microsoft.Azure.Kinect.Sensor.Intrinsics intrinsics;
            int width;
            int height;

            switch (channelName)
            {
                case ChannelNames.Color:
                    intrinsics = calibration.ColorCameraCalibration.Intrinsics;
                    width = calibration.ColorCameraCalibration.ResolutionWidth;
                    height = calibration.ColorCameraCalibration.ResolutionHeight;
                    metricRadius = calibration.ColorCameraCalibration.MetricRadius;
                    break;

                case ChannelNames.ZImage:
                case ChannelNames.Intensity:
                case ChannelNames.Distance:
                    intrinsics = calibration.DepthCameraCalibration.Intrinsics;
                    width = calibration.DepthCameraCalibration.ResolutionWidth;
                    height = calibration.DepthCameraCalibration.ResolutionHeight;
                    metricRadius = calibration.DepthCameraCalibration.MetricRadius;
                    break;

                default:
                    string msg = $"{Name}: no valid intrinsics for channel {channelName}";
                    log.Error(msg);
                    throw new System.Exception(msg);
            }

            ProjectiveTransformation projTrans = new ProjectiveTransformationRational(
                width,
                height,
                intrinsics.Parameters[(int)Intrinsics.Fx],
                intrinsics.Parameters[(int)Intrinsics.Fy],
                intrinsics.Parameters[(int)Intrinsics.Cx],
                intrinsics.Parameters[(int)Intrinsics.Cy],
                intrinsics.Parameters[(int)Intrinsics.K1],
                intrinsics.Parameters[(int)Intrinsics.K2],
                intrinsics.Parameters[(int)Intrinsics.K3],
                intrinsics.Parameters[(int)Intrinsics.K4],
                intrinsics.Parameters[(int)Intrinsics.K5],
                intrinsics.Parameters[(int)Intrinsics.K6],
                intrinsics.Parameters[(int)Intrinsics.P1],
                intrinsics.Parameters[(int)Intrinsics.P2],
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
            RigidBodyTransformation rbt = null;

            if (channelFromName == ChannelNames.Color && (channelToName == ChannelNames.Distance || channelToName == ChannelNames.ZImage || channelToName == ChannelNames.Intensity))
            {
                rbt = GetExtrinsics(channelToName, channelFromName).GetInverted();
            }
            else if ((channelFromName == ChannelNames.Distance || channelFromName == ChannelNames.ZImage || channelFromName == ChannelNames.Intensity) && channelToName == ChannelNames.Color)
            {
                RotationMatrix rotMat = new RotationMatrix(calibration.ColorCameraCalibration.Extrinsics.Rotation);
                Point3f translation = new Point3f(
                    calibration.ColorCameraCalibration.Extrinsics.Translation[0] / 1000f,
                    calibration.ColorCameraCalibration.Extrinsics.Translation[1] / 1000f,
                    calibration.ColorCameraCalibration.Extrinsics.Translation[2] / 1000f);
                rbt = new RigidBodyTransformation(rotMat, translation);
            }
            else
            {
                string msg = $"{Name}: no valid extrinsics from channel {channelFromName} to {channelToName}";
                log.Error(msg);
                throw new System.Exception(msg);
            }
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
    }
}
