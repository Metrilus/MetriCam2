// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MetriCam2.Exceptions;
using System.Drawing;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// Intel RealSense R200 implementation
    /// </summary>
    public class R200 : Camera
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        #region Types
        #endregion

        #region Private Fields
        private PXCMSession session;
        private PXCMSenseManager pp;
        private List<PXCMCapture.DeviceInfo> deviceInfo = new List<PXCMCapture.DeviceInfo>();
        private List<int> devicesUIDs = new List<int>();
        private Dictionary<string, PXCMCapture.Device.StreamProfile> profiles = new Dictionary<string, PXCMCapture.Device.StreamProfile>();
        private PXCMCapture.Sample sample;
        private ListParamDesc<string> colorProfiles;
        private ListParamDesc<string> depthProfiles;

        private ColorCameraImage colorImage;
        private FloatCameraImage depthImage;

        private PXCMCalibration.StreamCalibration calibDataColor;
        private PXCMCalibration.StreamTransform calibTransColor;
        private PXCMCalibration.StreamCalibration calibDataDepth;
        private PXCMCalibration.StreamTransform calibTransDepth;

        private long lastTimeStamp = 0;
        private int widthColor = 640;//1920;
        private int heightColor = 480;//1080;
        private int fpsColor = 30;

        private int widthZImage = 480;
        private int heightZImage = 360;
        private int fpsZImage = 30;
        #endregion

        #region Public Properties
        private ParamDesc<string> ColorProfileDesc
        {
            get
            {
                return colorProfiles;
            }
        }

        private ParamDesc<string> DepthProfileDesc
        {
            get
            {
                return depthProfiles;
            }
        }

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.RealSenseIcon; }
#endif

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public R200()
            : base()
        {
            log.Info(Name + ": Constructor");
            session = PXCMSession.CreateInstance();
            if (null == session)
            {
                log.Debug("'session' is null");
                string msg = "Is a RealSense R200 camera connected to your PC and the driver installed?";
                log.Debug(msg);
                throw new NullReferenceException(msg);
            }
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

            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
        }

        /// <summary>
        /// Device-specific implementation of Connect.
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Connect"/>
        protected override void ConnectImpl()
        {
            log.EnterMethod();

            if (deviceInfo.Count == 0)
            {
                ScanForCameras();
            }

            if (deviceInfo.Count == 0)
            {
                log.Error(Name + ": No devices found.");
                throw ExceptionBuilder.Build(typeof(ConnectionFailedException), Name, "No devices found.");
            }

            int deviceIdx;
            for (deviceIdx = 0; deviceIdx < deviceInfo.Count; deviceIdx++)
            {
                if (deviceInfo[deviceIdx].name != "Intel(R) RealSense(TM) 3D Camera R200")
                {
                    continue;
                }

                log.Debug("RealSense R200 found");
                break;
            }

            if (deviceIdx >= deviceInfo.Count)
            {
                log.Error("Failed to find Intel Real Sense R200 camera!");
                throw ExceptionBuilder.Build(typeof(ConnectionFailedException), Name, "No R200 found!");
            }

            ScanForProfiles(deviceIdx);

            // Create an instance of the PXCSenseManager interface
            pp = PXCMSenseManager.CreateInstance();

            if (pp == null)
            {
                log.Error(Name + ": Failed to create an SDK pipeline object");
                return;
            }

            pp.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, widthColor, heightColor, fpsColor);
            pp.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, widthZImage, heightZImage, fpsZImage);

            pxcmStatus retStat = pp.Init();
            if (retStat < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                log.Error(Name + ": Init() failed.");
                throw ExceptionBuilder.Build(typeof(ConnectionFailedException), Name, "Failed to Initialize Real Sense SDK");
            }

            pp.captureManager.device.ResetProperties(PXCMCapture.StreamType.STREAM_TYPE_ANY);

            // Find calibration data
            PXCMProjection projection = pp.captureManager.device.CreateProjection();
            PXCMCalibration calib = projection.QueryInstance<PXCMCalibration>();
            retStat = calib.QueryStreamProjectionParameters(PXCMCapture.StreamType.STREAM_TYPE_COLOR, out calibDataColor, out calibTransColor);
            if (retStat < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                log.WarnFormat("Could not get calibration for color channel.");
            }
            retStat = calib.QueryStreamProjectionParameters(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, out calibDataDepth, out calibTransDepth);
            if (retStat < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                log.WarnFormat("Could not get calibration for depth channel.");
            }
            projection.Dispose();

            log.InfoFormat("Color calibration: f=({0}; {1}) c=({2}; {3}) k=({4}; {5}; {6}) p=({7}; {8})",
                calibDataColor.focalLength.x, calibDataColor.focalLength.y,
                calibDataColor.principalPoint.x, calibDataColor.principalPoint.y,
                calibDataColor.radialDistortion[0], calibDataColor.radialDistortion[1], calibDataColor.radialDistortion[2],
                calibDataColor.tangentialDistortion[0], calibDataColor.tangentialDistortion[1]
                );

            log.InfoFormat("Color translation: t=({0}; {1}; {2})",
                calibTransColor.translation[0], calibTransColor.translation[1], calibTransColor.translation[2]
                );

            log.InfoFormat("Depth calibration: f=({0}; {1}) c=({2}; {3}) k=({4}; {5}; {6}) p=({7}; {8})",
                calibDataDepth.focalLength.x, calibDataDepth.focalLength.y,
                calibDataDepth.principalPoint.x, calibDataDepth.principalPoint.y,
                calibDataDepth.radialDistortion[0], calibDataDepth.radialDistortion[1], calibDataDepth.radialDistortion[2],
                calibDataDepth.tangentialDistortion[0], calibDataDepth.tangentialDistortion[1]
                );

            log.InfoFormat("Depth translation: t=({0}; {1}; {2})",
                calibTransDepth.translation[0], calibTransDepth.translation[1], calibTransDepth.translation[2]
                );
            ActivateChannel(ChannelNames.ZImage);
            ActivateChannel(ChannelNames.Color);
            SelectChannel(ChannelNames.ZImage);

            log.LeaveMethod();
        }

        /// <summary>
        /// Device-specific implementation of Disconnect.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Disconnect"/>
        protected override void DisconnectImpl()
        {
            pp.Close();
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Update"/>
        protected override unsafe void UpdateImpl()
        {
            bool synced = true;
            // Wait until a frame is ready: Synchronized or Asynchronous
            pxcmStatus status;
            status = pp.AcquireFrame(synced);
            if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                log.Error(Name + ": " + status.ToString());
                return;
            }

            // get image
            sample = pp.QuerySample();

            long imgTS = sample.color.timeStamp;
            if (imgTS <= lastTimeStamp)
            {
                throw new TimeoutException("THIS IS NOT A TIMEOUT!");
            }
                lastTimeStamp = imgTS;
            
            // color image
            PXCMImage.ImageData colorData;
            if (sample.color != null)
            {
                sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);
                Bitmap bmp = new Bitmap(sample.color.info.width, sample.color.info.height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, sample.color.info.width, sample.color.info.height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                memcpy(bmpData.Scan0, colorData.planes[0], new UIntPtr(3 * (uint)sample.color.info.width * (uint)sample.color.info.height));
                bmp.UnlockBits(bmpData);
                Bitmap bmp32 = bmp.Clone(new Rectangle(0, 0, widthColor, heightColor), System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                colorImage = new ColorCameraImage(bmp32);
                sample.color.ReleaseAccess(colorData);
            }
            // depth
            PXCMImage.ImageData depthData;
            if (sample.depth != null)
            {
                sample.depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_F32, out depthData);
                depthImage = new FloatCameraImage(sample.depth.info.width, sample.depth.info.height);
                CopyImageWithStride(sample.depth.info.width, sample.depth.info.height, 4, depthData, new IntPtr(depthImage.Data));
                sample.depth.ReleaseAccess(depthData);
            }

            pp.ReleaseFrame();
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        /// <seealso cref="Camera.CalcChannel"/>
        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.ZImage:
                    return CalcZImage();
                case ChannelNames.Color:
                    return colorImage;
            }

            throw ExceptionBuilder.Build(typeof(ArgumentException), Name, "error_invalidChannelName", channelName);
        }

        public override RigidBodyTransformation GetExtrinsics(string channelFromName, string channelToName)
        {
            log.Info("Trying to load rigid body transformation from file.");
            try
            {
                return base.GetExtrinsics(channelFromName, channelToName);
            }
            catch { /* empty */ }

            log.Info("Rigid body transformation not found. Using R200 factory extrinsics as projective transformation");

            if (channelFromName != ChannelNames.ZImage && channelToName != ChannelNames.Color)
            {
                log.Error("Transformation: " + channelFromName + " => " + channelToName + " is not available");
                throw new ArgumentException("Transformation: " + channelFromName + " => " + channelToName + " is not available");
            }
            // TODO, do not use identity
            return new RigidBodyTransformation(RotationMatrix.Identity, new Point3f(calibTransColor.translation[0], calibTransColor.translation[1], calibTransColor.translation[2]));
        }
        /// <summary>
        /// Overrides the standard GetIntrinsic method.
        /// </summary>
        /// <param name="channelName">The channel name.</param>
        /// <returns>The ProjectiveTransformationZhang</returns>
        /// <remarks>The method first searches for a pt file on disk. If this fails it is able to provide internal intrinsics for ZImage channel.</remarks>
        public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            IProjectiveTransformation result = null;
            log.Info("Trying to load projective transformation from file.");
            try
            {
                return base.GetIntrinsics(channelName);
            }
            catch { /* empty */ }

            if (result == null)
            {
                log.Info("Projective transformation file not found. Using R200 factory intrinsics as projective transformation.");
                switch (channelName)
                {
                    case ChannelNames.ZImage:
                        result = new ProjectiveTransformationZhang(widthZImage, heightZImage, calibDataDepth.focalLength.x, calibDataDepth.focalLength.y, calibDataDepth.principalPoint.x, calibDataDepth.principalPoint.y, calibDataDepth.radialDistortion[0], calibDataDepth.radialDistortion[1], calibDataDepth.radialDistortion[2], calibDataDepth.tangentialDistortion[0],
                            calibDataDepth.tangentialDistortion[1]);
                        break;
                    case ChannelNames.Color:
                        result = new ProjectiveTransformationZhang(widthColor, heightColor, calibDataColor.focalLength.x, calibDataColor.focalLength.y, calibDataColor.principalPoint.x, calibDataColor.principalPoint.y, calibDataColor.radialDistortion[0], calibDataColor.radialDistortion[1], calibDataColor.radialDistortion[2], calibDataColor.tangentialDistortion[0],
                        calibDataColor.tangentialDistortion[1]);
                        break;
                    default:
                        log.Error("Unsupported channel in GetIntrinsics().");
                        throw new ArgumentException("Unsupported channel " + channelName + " in GetIntrinsics().");
                }
            }
            return result;
        }
        #endregion

        #region Private Methods
        private FloatCameraImage CalcZImage()
        {
            lock (cameraLock)
            {
                float factor = 1 / 1000.0f;
                int height = depthImage.Height;
                int width = depthImage.Width;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        depthImage[y, x] *= factor;
                    }
                }
            }
            return depthImage;
        }

        private void ScanForCameras()
        {
            log.EnterMethod();

            deviceInfo.Clear();
            devicesUIDs.Clear();

            PXCMSession.ImplDesc desc1;
            PXCMCapture.DeviceInfo dinfo = null;

            PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
            desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;

            for (int i = 0; ; i++)
            {
                if (session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    break;
                }
                PXCMCapture capture;
                if (session.CreateImpl<PXCMCapture>(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    continue;
                }
                for (int j = 0; ; j++)
                {
                    if (capture.QueryDeviceInfo(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR)
                    {
                        break;
                    }
                    deviceInfo.Add(dinfo);
                    devicesUIDs.Add(desc1.iuid);
                }
                capture.Dispose();
            }
        }

        private void ScanForProfiles(int deviceIndex)
        {
            PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
            desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;
            desc.iuid = devicesUIDs[deviceIndex];
            int current_device_iuid = desc.iuid;
            desc.cuids[0] = PXCMCapture.CUID;

            profiles.Clear();

            List<string> colorStrings = new List<string>();
            List<string> depthStrings = new List<string>();
            PXCMCapture capture;
            PXCMCapture.DeviceInfo dinfo2 = deviceInfo[deviceIndex];
            if (session.CreateImpl<PXCMCapture>(desc, out capture) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                PXCMCapture.Device device = capture.CreateDevice(dinfo2.didx);
                if (device != null)
                {
                    PXCMCapture.Device.StreamProfileSet profile = new PXCMCapture.Device.StreamProfileSet();

                    for (int s = 0; s < PXCMCapture.STREAM_LIMIT; s++)
                    {
                        PXCMCapture.StreamType st = PXCMCapture.StreamTypeFromIndex(s);
                        if (((int)dinfo2.streams & (int)st) != 0)
                        {
                            int num = device.QueryStreamProfileSetNum(st);
                            for (int p = 0; p < num; p++)
                            {
                                if (device.QueryStreamProfileSet(st, p, out profile) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                                PXCMCapture.Device.StreamProfile sprofile = profile[st];
                                string profileText = ProfileToString(sprofile);
                                try
                                {
                                    profiles.Add(profileText, sprofile);
                                }
                                catch
                                {
                                }
                                switch (st)
                                {
                                    case PXCMCapture.StreamType.STREAM_TYPE_COLOR:
                                        colorStrings.Add(profileText);
                                        break;
                                    case PXCMCapture.StreamType.STREAM_TYPE_DEPTH:
                                        depthStrings.Add(profileText);
                                        break;
                                }
                            }
                        }
                    }

                    device.Dispose();
                }
                capture.Dispose();
            }
            colorProfiles = new ListParamDesc<string>(colorStrings)
            {
                Description = "Color Profiles",
                ReadableWhen = ParamDesc.ConnectionStates.Connected,
            };
            depthProfiles = new ListParamDesc<string>(depthStrings)
            {
                Description = "Depth Profiles",
                ReadableWhen = ParamDesc.ConnectionStates.Connected,
            };
        }

        private string ProfileToString(PXCMCapture.Device.StreamProfile pinfo)
        {
            string line = "Unknown ";
            if (Enum.IsDefined(typeof(PXCMImage.PixelFormat), pinfo.imageInfo.format))
            {
                line = pinfo.imageInfo.format.ToString().Substring(13) + " " + pinfo.imageInfo.width + "x" + pinfo.imageInfo.height + "x";
            }
            else
            {
                line += pinfo.imageInfo.width + "x" + pinfo.imageInfo.height + "x";
            }
            if (pinfo.frameRate.min != pinfo.frameRate.max)
            {
                line += pinfo.frameRate.min + "-" + pinfo.frameRate.max;
            }
            else
            {
                float fps = (pinfo.frameRate.min != 0) ? pinfo.frameRate.min : pinfo.frameRate.max;
                line += fps;
            }
            return line;
        }

        private unsafe void CopyImageWithStride(int width, int height, int scale, PXCMImage.ImageData data, IntPtr destination)
        {
            int length = width * scale;
            byte *dest = (byte*)destination;

            // one memcpy per line
            for (int y = 0; y < height; ++y)
            {
                byte *src = (byte*)data.planes[0] + y * data.pitches[0];
                memcpy(new IntPtr(dest), new IntPtr(src), new UIntPtr((uint)length));
                dest += length;
            }
        }
        #endregion
        #endregion
    }
}
