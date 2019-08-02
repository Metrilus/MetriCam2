// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// Intel RealSense F200 implementation
    /// </summary>
    public class F200 : Camera
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        #region Private Fields
        private PXCMSession session;
        private PXCMSenseManager pp;
        private List<PXCMCapture.DeviceInfo> deviceInfo = new List<PXCMCapture.DeviceInfo>();
        private List<int> devicesUIDs = new List<int>();
        private Dictionary<string, PXCMCapture.Device.StreamProfile> profiles = new Dictionary<string, PXCMCapture.Device.StreamProfile>();
        private PXCMCapture.Sample sample;
        private ListParamDesc<string> colorProfiles;
        private ListParamDesc<string> depthProfiles;
        private ListParamDesc<string> irProfiles;
        private string currentColorProfile;
        private string currentDepthProfile;
        private string currentIRProfile;

        private FloatImage depthImage;
        private ByteImage irImage;
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

        private ParamDesc<string> IRProfileDesc
        {
            get
            {
                return irProfiles;
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
        public F200()
            : base()
        {
            log.Info(Name + "Constructor");
            session = PXCMSession.CreateInstance();
        }
        #endregion

        #region MetriCam2 Camera Interface
        #region MetriCam2 Camera Interface Properties
        // TOD: Add Depth IR Color Profile Properties
        #endregion

        #region MetriCam2 Camera Interface Methods
        /// <summary>
        /// Resets list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
        /// </summary>
        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;

            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
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
            if (deviceInfo.Count == 0)
                ScanForCameras();

            if (deviceInfo.Count == 0)
            {
                log.Error(Name + "No device found.");
                return;
            }

            int deviceIndex = 0;

            ScanForProfiles(deviceIndex);

            /* Create an instance of the PXCSenseManager interface */
            pp = PXCMSenseManager.CreateInstance();

            if (pp == null)
            {
                log.Error(Name + "Failed to create an SDK pipeline object");
                return;
            }
            
            pp.captureManager.FilterByDeviceInfo(deviceInfo[deviceIndex]);

            //TODO: change this to work with properties
            currentColorProfile = "YUY2 1920x1080x30";
            currentDepthProfile = "DEPTH 640x480x60";
            currentIRProfile = "Y8 640x480x60";

            PXCMCapture.Device.StreamProfileSet currentProfileSet = new PXCMCapture.Device.StreamProfileSet();
            currentProfileSet[PXCMCapture.StreamType.STREAM_TYPE_COLOR] = profiles[currentColorProfile];
            currentProfileSet[PXCMCapture.StreamType.STREAM_TYPE_DEPTH] = profiles[currentDepthProfile];
            currentProfileSet[PXCMCapture.StreamType.STREAM_TYPE_IR] = profiles[currentIRProfile];

            /* Set Color & Depth Resolution */
            for (int s = 0; s < PXCMCapture.STREAM_LIMIT; s++)
            {
                PXCMCapture.StreamType st = PXCMCapture.StreamTypeFromIndex(s);
                PXCMCapture.Device.StreamProfile info = currentProfileSet[st];
                if (info.imageInfo.format != 0)
                {
                    Single fps = info.frameRate.max;
                    pp.EnableStream(st, info.imageInfo.width, info.imageInfo.height, fps);
                }
            }
            if (pp.Init() >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
            }
            else
            {
                log.Error(Name + "An error occured.");
            }
            ActivateChannel(ChannelNames.Intensity);
            ActivateChannel(ChannelNames.ZImage);
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
            /* Wait until a frame is ready: Synchronized or Asynchronous */
            pxcmStatus status;
            status = pp.AcquireFrame(synced);
            if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
                log.Error(Name + ": error" + status.ToString());

            /* Display images */
            sample = pp.QuerySample();

            PXCMImage.ImageData depthData;
            sample.depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_F32, out depthData);
            depthImage = new FloatImage(sample.depth.info.width, sample.depth.info.height);
            fixed (float* depthImageData = depthImage.Data)
            {
                memcpy(new IntPtr(depthImageData), depthData.planes[0], new UIntPtr((uint)sample.depth.info.width * (uint)sample.depth.info.height * (uint)sizeof(float)));
            }
            sample.depth.ReleaseAccess(depthData);

            PXCMImage.ImageData irData;
            sample.ir.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out irData);
            irImage = new ByteImage(sample.ir.info.width, sample.ir.info.height);
            fixed (byte* irImageData = irImage.Data)
            {
                memcpy(new IntPtr(irImageData), irData.planes[0], new UIntPtr((uint)sample.ir.info.width * (uint)sample.ir.info.height * (uint)sizeof(byte)));
            }
            sample.ir.ReleaseAccess(irData);

            pp.ReleaseFrame();
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        /// <seealso cref="Camera.CalcChannel"/>
        protected override ImageBase CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.ZImage:
                    return CalcZImage();
                case ChannelNames.Intensity:
                    return irImage;
            }

            throw ExceptionBuilder.Build(typeof(ArgumentException), Name, "error_invalidChannelName", channelName);
        }
        #endregion

        #region Private Methods
        private FloatImage CalcZImage()
        {
            float factor = 1/1000.0f;
            int height = depthImage.Height;
            int width = depthImage.Width;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    depthImage[y, x] *= factor;
                }
            }
            return depthImage;
        }
        private void ScanForCameras()
        {
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
                    break;
                PXCMCapture capture;
                if (session.CreateImpl<PXCMCapture>(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR)
                    continue;
                for (int j = 0; ; j++)
                {
                    if (capture.QueryDeviceInfo(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) 
                        break;
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
            List<string> irStrings = new List<string>();
            List<string> temp = new List<string>();
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
                                    case PXCMCapture.StreamType.STREAM_TYPE_IR:
                                        irStrings.Add(profileText);
                                        break;
                                    default:
                                        temp.Add(profileText);
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
                ReadableWhen = ParamDesc.ConnectionStates.Disconnected,
            };
            depthProfiles = new ListParamDesc<string>(depthStrings)
            {
                Description = "Depth Profiles",
                ReadableWhen = ParamDesc.ConnectionStates.Disconnected,
            };
            irProfiles = new ListParamDesc<string>(irStrings)
            {
                Description = "IR Profiles",
                ReadableWhen = ParamDesc.ConnectionStates.Disconnected,
            };
        }

        private string ProfileToString(PXCMCapture.Device.StreamProfile pinfo)
        {
            string line = "Unknown ";
            if (Enum.IsDefined(typeof(PXCMImage.PixelFormat), pinfo.imageInfo.format))
                line = pinfo.imageInfo.format.ToString().Substring(13) + " " + pinfo.imageInfo.width + "x" + pinfo.imageInfo.height + "x";
            else
                line += pinfo.imageInfo.width + "x" + pinfo.imageInfo.height + "x";
            if (pinfo.frameRate.min != pinfo.frameRate.max)
            {
                line += (float)pinfo.frameRate.min + "-" +
                      (float)pinfo.frameRate.max;
            }
            else
            {
                float fps = (pinfo.frameRate.min != 0) ? pinfo.frameRate.min : pinfo.frameRate.max;
                line += fps;
            }
            return line;
        }
        #endregion
        #endregion
    }
}
