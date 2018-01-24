// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using MetriCam2.Cameras.Internal.Sick;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// MetriCam2 Wrapper for V3S100 Cameras.
    /// </summary>
    public class VisionaryT : Camera
    {
        #region Private Variables
        // ipAddress to connect to
        private string ipAddress;
        // device handle
        private Device device;
        // frame buffer
        private byte[] imageBuffer;
        // image data contains information about the current frame e.g. width and height
        private FrameData imageData;
        // camera properties
        private int width;
        private int height;
        #endregion

        #region Public Properties

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => V3S.Properties.Resources.SickIcon; }
#endif

        private ParamDesc<string> IPAddressDesc
        {
            get
            {
                return new ParamDesc<string>()
                {
                    Description = "IP address of camera.",
                    ReadableWhen = ParamDesc.ConnectionStates.Disconnected | ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
            }
        }

        /// <summary>
        /// IP Address of camera. Has to be set before connecting.
        /// </summary>
        public string IPAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; }
        }
        private RangeParamDesc<int> IntegrationTimeDesc
        {
            get
            {
                return new RangeParamDesc<int>(80, 4000)
                {
                    Description = "Integration time of camera in microseconds.",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                    Unit = "Microseconds",
                };
            }
        }
        /// <summary>
        /// Integration time of ToF-sensor.
        /// </summary>
        public int IntegrationTime
        {
            get
            {
                return device.Control_GetIntegrationTime();
            }
            set
            {
                device.Control_SetServiceAccessMode();
                device.Control_SetIntegrationTime(value);
            }
        }
        private ParamDesc<int> WidthDesc
        {
            get
            {
                return new ParamDesc<int>()
                {
                    Description = "Width of images.",
                    Unit = "px",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected
                };
            }
        }

        /// <summary>
        /// Width of images.
        /// </summary>
        public int Width
        {
            get { return width; }
        }

        private ParamDesc<int> HeightDesc
        {
            get
            {
                return new ParamDesc<int>()
                {
                    Description = "Height of images.",
                    Unit = "px",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected
                };
            }
        }

        /// <summary>
        /// Height of images.
        /// </summary>
        public int Height
        {
            get { return height; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new instance of V3S 100 camera.
        /// </summary>
        public VisionaryT()
            : base()
        {
            ipAddress   = "";
            imageBuffer = null;
            device      = null;
            imageData   = null;
            width       = 0;
            height      = 0;
        }
        #endregion

        #region MetriCam2 Camera Interface
        #region MetriCam2 Camera Interface Properties
        /// <summary>The camera's name.</summary>
        public new string Name
        {
            get
            {
                return "Visionary-T";
            }
        }
        #endregion

        #region MetriCam2 Camera Interface Methods
        /// <summary>
        /// Resets list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
        /// Channels are: Intensity, Distance, Confidence and 3D
        /// </summary>
        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;

            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
            Channels.Add(cr.RegisterChannel(ChannelNames.Distance));
            Channels.Add(cr.RegisterChannel(ChannelNames.ConfidenceMap));
            Channels.Add(cr.RegisterCustomChannel(ChannelNames.RawConfidenceMap, typeof(UShortCameraImage)));
            Channels.Add(cr.RegisterChannel(ChannelNames.Point3DImage));
        }

        /// <summary>
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        protected override void ConnectImpl()
        {
            if (ipAddress == null || ipAddress == "")
            {
                log.Error("IP Address is not set.");
                ExceptionBuilder.Throw(typeof(MetriCam2.Exceptions.ConnectionFailedException), this, "error_connectionFailed", "IP Address is not set! Set it before connecting!");
            }
            device = new Device(ipAddress, this, log);
            device.Connect();
            device.Control_InitStream();
            device.Control_StartStream();

            // select intensity channel
            ActivateChannel(ChannelNames.Intensity);
            SelectChannel(ChannelNames.Intensity);

            this.UpdateImpl();
        }

        /// <summary>
        /// Loads the intrisic parameters from the camera.
        /// </summary>
        /// <param name="channelName">Channel for which intrisics are loaded.</param>
        /// <returns>ProjectiveTransformationZhang object holding the intrinsics.</returns>
        public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            if (channelName == ChannelNames.Intensity || channelName == ChannelNames.Distance)
            {
                ProjectiveTransformationZhang proj;
                lock (cameraLock)
                {
                    proj = new ProjectiveTransformationZhang(imageData.Width,
                    imageData.Height,
                    imageData.FX,
                    imageData.FY,
                    imageData.CX,
                    imageData.CY,
                    imageData.K1,
                    imageData.K2,
                    0,
                    0,
                    0);
                }
                return proj;
            }
            throw new ArgumentException(string.Format("Channel {0} intrinsics not supported.", channelName));
        }
        /// <summary>
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        protected override void DisconnectImpl()
        {
            device.Control_StopStream();
            device.Disconnect();
            device = null;
        }

        /// <summary>
        /// Updates data buffers of all channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        protected override void UpdateImpl()
        {
            imageBuffer = device.Stream_GetFrame();
            imageData   = new FrameData(imageBuffer, this, log);
            width       = imageData.Width;
            height      = imageData.Height;

            // read data and update properties
            imageData.Read();
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
            case ChannelNames.Intensity:
                return CalcIntensity();
            case ChannelNames.Distance:
                return CalcDistance();
            case ChannelNames.ConfidenceMap:
                return CalcConfidenceMap(CalcRawConfidenceMap());
            case ChannelNames.RawConfidenceMap:
                return CalcRawConfidenceMap();
            case ChannelNames.Point3DImage:
                return Calc3D();
            }
            log.Error("Invalid channelname: " + channelName);
            return null;
        }

        #endregion
        #endregion

        #region Internal Methods
        /// <summary>
        /// Calculates the intensity channel.
        /// </summary>
        /// <returns>Intensity image</returns>
        private FloatCameraImage CalcIntensity()
        {
            FloatCameraImage result;
            lock (cameraLock)
            {
                result = new FloatCameraImage(imageData.Width, imageData.Height);
                result.TimeStamp = (long)imageData.TimeStamp;
                int start = imageData.IntensityStartOffset;
                for (int i = 0; i < imageData.Height; ++i)
                {
                    for (int j = 0; j < imageData.Width; ++j)
                    {
                        // take two bytes and create integer (little endian)
                        uint value = (uint)imageBuffer[start + 1] << 8 | (uint)imageBuffer[start + 0];
                        result[i, j] = (float)value;
                        start += 2;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the distance channel.
        /// </summary>
        /// <returns>Distance image</returns>
        private FloatCameraImage CalcDistance()
        {
            FloatCameraImage result;
            // FIXME: distance calculation
            lock (cameraLock)
            {
                result = new FloatCameraImage(imageData.Width, imageData.Height);
                result.TimeStamp = (long)imageData.TimeStamp;
                int start = imageData.DistanceStartOffset;
                for (int i = 0; i < imageData.Height; ++i)
                {
                    for (int j = 0; j < imageData.Width; ++j)
                    {
                        // take two bytes and create integer (little endian)
                        uint value = (uint)imageBuffer[start + 1] << 8 | (uint)imageBuffer[start + 0];
                        result[i, j] = (float)value * 0.001f;
                        start += 2;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the confidence map.
        /// </summary>
        /// <returns>Confidence map</returns>
        private UShortCameraImage CalcRawConfidenceMap()
        {
            UShortCameraImage result;
            lock (cameraLock)
            {
                result = new UShortCameraImage(imageData.Width, imageData.Height);
                result.TimeStamp = (long)imageData.TimeStamp;
                int start = imageData.ConfidenceStartOffset;
                for (int i = 0; i < imageData.Height; ++i)
                {
                    for (int j = 0; j < imageData.Width; ++j)
                    {
                        // take two bytes and create integer (little endian)
                        result[i, j] = (ushort) ((ushort)imageBuffer[start + 1] << 8 | (ushort)imageBuffer[start + 0]);
                        start += 2;
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// According to the xml records being sent by the camera as initial sequence, a confidence image comes with 16 bits per pixel such that the lowest possible value is 0 and the highest possible value is 65535.
        /// Scale this range to [0, 1].
        /// </summary>
        /// <param name="rawConfidenceMap">Confidence map as provided by the camera</param>
        /// <returns>Confidence map as float image scaled to [0, 1] range</returns>
        private FloatCameraImage CalcConfidenceMap(UShortCameraImage rawConfidenceMap)
        {
            int width = rawConfidenceMap.Width;
            int height = rawConfidenceMap.Height;
            float scaling = 1.0f / (float) ushort.MaxValue;

            FloatCameraImage confidenceMap = new FloatCameraImage(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    confidenceMap[y, x] = scaling * rawConfidenceMap[y, x];
                }
            }

            return confidenceMap;
        }
        /// <summary>
        /// Calculates the 3D point cloud from received data.
        /// </summary>
        /// <returns>point cloud</returns>
        private Point3fCameraImage Calc3D()
        {
            Point3fCameraImage result;
            lock (cameraLock)
            {
                result = new Point3fCameraImage(imageData.Width, imageData.Height);
                result.TimeStamp = (long)imageData.TimeStamp;

                float cx = imageData.CX;
                float cy = imageData.CY;
                float fx = imageData.FX;
                float fy = imageData.FY;
                float k1 = imageData.K1;
                float k2 = imageData.K2;
                float f2rc = imageData.F2RC;

                FloatCameraImage distances = CalcDistance();

                for (int i = 0; i < imageData.Height; ++i)
                {
                    for (int j = 0; j < imageData.Width; ++j)
                    {
                        int depth = (int)(distances[i, j] * 1000);

                        // we map from image coordinates with origin top left and x horizontal (right) and y vertical 
                        // (downwards) to camera coordinates with origin in center and x to the left and y upwards (seen 
                        // from the sensor position)
                        double xp = (cx - j) / fx; 
                        double yp = (cy - i) / fy;

                        // correct the camera distortion
                        double r2 = xp * xp + yp * yp;
                        double r4 = r2 * r2;
                        double k = 1 + k1 * r2 + k2 * r4;
                        double xd = xp * k;
                        double yd = yp * k;
    
                        double s0 = Math.Sqrt(xd * xd + yd * yd + 1.0); 
                        double x = xd * depth / s0; 
                        double y = yd * depth / s0;
                        double z =  depth / s0 - f2rc;

                        x /= 1000;
                        y /= 1000;
                        z /= 1000;

                        // create point and save it
                        Point3f point = new Point3f((float)-x, (float)-y, (float)z);
                        result[i, j] = point;
                    }
                }
            }
            return result;
        }
        #endregion
    }
}
