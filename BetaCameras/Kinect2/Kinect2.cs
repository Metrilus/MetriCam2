// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Exceptions;
using Metrilus.Util;
using Microsoft.Kinect;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// MetriCam2 Wrapper for Microsoft Kinect2.
    /// </summary>
    public class Kinect2 : Camera
    {
        /// <summary>
        /// Defines the custom channel names for easier handling.
        /// </summary>
        /// <remarks>Similar to MetriCam2.ChannelNames for standard channel names.</remarks>
        public static class CustomChannelNames
        {
            public const string BodyIndex = "BodyIndex";
            public const string LongExposureIR = "LongExposureIR";
        }

        #region Protected Fields
        protected ushort[] depthFrameData = null;
        protected int depthWidth;
        protected int depthHeight;
        protected int depthWidthMinusOne;
        protected float[,] distanceMap = null;
        protected byte[] colorFrameData = null;
        protected readonly int bytesPerPixel = 4;
        #endregion

        #region Private Fields
        private object newFrameLock = new object();
        private AutoResetEvent dataAvailable;
        

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for depth/IR frames
        /// </summary>
        private MultiSourceFrameReader multiReader = null;

        /// <summary>
        /// Intermediate storage for receiving frame data from the sensor
        /// </summary>
        private FrameDescription depthFrameDescription;
        private float[,] depthImageBuffer;

        private ushort[] irFrameData = null;
        private Body[] bodies = null;

        private ushort[] longExposureIRData = null;
        private byte[] bodyIndexData = null;

        private MultiSourceFrameReference multiFrameReference;

        private long absoluteTimeStampAtFrame0;
        private long relativeTimeStampAtFrame0;
        private long lastTimeStamp = 0;
        private long timestampDepth = -1;
        private long timestampIR = -1;
        private long timestampColor = -1;
        #endregion

        #region Public Fields
        public CoordinateMapper coordinateMapper;
        #endregion

        #region Public Properties
        public CoordinateMapper Coordinates
        {
            get
            {
                return coordinateMapper;
            }
        }

        public int ColorWidth
        {
            get;
            private set;
        }

        public int ColorHeight
        {
            get;
            private set;
        }

        /// <summary>
        /// Information about body parts.
        /// </summary>
        /// <remarks>Position information of body parts is given in the manufacturer coordinate system, where x- and y-axis are negated compared to the Metrlus camera coordinate system.</remarks>
        public Body[] Bodies
        {
            get
            {
                return bodies;
            }
        }

        public bool CalcHandPatches
        {
            get;
            set;
        }

        /// <summary>Name of camera vendor.</summary>
        public override string Vendor
        {
            get { return "Microsoft"; }
        }

        /// <summary>
        /// Optional timeout for method <see cref="UpdateImpl"/>. If set to Timeout.Infinite (-1), the update-timeout is deactivated.
        /// </summary>
        public int UpdateTimeoutMilliseconds
        {
            get;
            set;
        }
        #endregion

        #region Constructor
        public Kinect2()
            : base("Kinect2")
        {
            dataAvailable = new AutoResetEvent(false);
            CalcHandPatches = true;
            UpdateTimeoutMilliseconds = Timeout.Infinite;
            enableImplicitThreadSafety = true;
            //depthHandBuffer = new float[(int)Width, (int)Height];
            //ampHandBuffer = new float[(int)Width, (int)Height];
        }
        #endregion

        #region MetriCam2 Camera Interface
        #region MetriCam2 Camera Interface Properties

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.KinectIcon; }
#endif

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
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterChannel(ChannelNames.Point3DImage));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.LongExposureIR, typeof(FloatImage)));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.BodyIndex, typeof(FloatImage)));
        }

        /// <summary>
        /// Activate a channel.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        protected override void ActivateChannelImpl(string channelName)
        {
            // Obsolete exception: id 001

            if (IsConnected)
            {
                multiReader.MultiSourceFrameArrived -= Reader_MultiSourceFrameArrived;
            }
            AddToActiveChannels(channelName);// manually add here, because OpenMultiReader uses IsChannelActive
            if (IsConnected)
            {
                OpenMultiReader();

                multiReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        /// <summary>
        /// Deactivate a channel to save time in <see cref="Update"/>.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        protected override void DeactivateChannelImpl(string channelName)
        {
            if (IsConnected)
            {
                multiReader.MultiSourceFrameArrived -= Reader_MultiSourceFrameArrived;
            }
            ActiveChannels.Remove(GetChannelDescriptor(channelName));// manually remove here, because OpenMultiReader uses IsChannelActive
            if (IsConnected)
            {
                OpenMultiReader();
                multiReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        /// <summary>
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Connect"/>
        protected override void ConnectImpl()
        {
            // only one sensor is supported
            this.kinectSensor = KinectSensor.GetDefault();

            if (this.kinectSensor == null)
            {
                throw ExceptionBuilder.BuildFromID(typeof(ConnectionFailedException), this, 002);
            }

            // open the sensor
            this.kinectSensor.Open();

            OpenMultiReader();

            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            depthWidth = depthFrameDescription.Width;
            depthWidthMinusOne = depthWidth - 1;
            depthHeight = depthFrameDescription.Height;

            // allocate space to store the pixels being received and converted
            this.depthImageBuffer = new float[depthHeight, depthWidth];
            this.depthFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
            this.irFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            if (this.multiReader != null)
            {
                this.multiReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
            }            

            // Opening the sensor may take a short while
            const int NumRetriesIsOpen = 30;
            int isOpenWaits = 0;
            while (!kinectSensor.IsOpen && isOpenWaits++ < NumRetriesIsOpen)
            {
                System.Threading.Thread.Sleep(100);
            }
            if (isOpenWaits >= NumRetriesIsOpen)
            {
                throw ExceptionBuilder.BuildFromID(typeof(ConnectionFailedException), this, 003);
            }

            if (ActiveChannels.Count == 0)
            {
                ActivateChannel(ChannelNames.Amplitude);
                ActivateChannel(ChannelNames.Distance);
                ActivateChannel(ChannelNames.Point3DImage);

                if (string.IsNullOrWhiteSpace(SelectedChannel))
                {
                    SelectChannel(ChannelNames.Distance);
                }
            }

            // UniqueKinectId may not instantly be available.
            log.Debug("Getting serial number");
            const int NumRetriesUniqueKinectId = 40;
            int i = 0;
            do
            {
                serialNumber = kinectSensor.UniqueKinectId;
                System.Threading.Thread.Sleep(50);
            } while (serialNumber == "" && i++ < NumRetriesUniqueKinectId);
            if (i >= NumRetriesUniqueKinectId)
            {
                throw ExceptionBuilder.BuildFromID(typeof(ConnectionFailedException), this, 004);
            }

            absoluteTimeStampAtFrame0 = 0;
            relativeTimeStampAtFrame0 = 0;

            log.DebugFormat("{0}: Connected", Name);
        }

        /// <summary>
        /// Device-specific implementation of Disconnect.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Disconnect"/>
        protected override void DisconnectImpl()
        {
            // make sure Update is finished
            dataAvailable.Set();

            if (this.multiReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                this.multiReader.Dispose();
                this.multiReader = null;
            }

            if (this.kinectSensor != null)
            {
                try
                {
                    this.kinectSensor.Close();
                    this.kinectSensor = null;
                }
                catch
                {
                    // ignore exception
                }
            }
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Update"/>
        protected override void UpdateImpl()
        {
            // TODO: This method could yield rather asynchronous channels. If necessary: Try to find a mechanism that updates frames that are already fetched when waiting for others that are not yet available.
            MultiSourceFrame multiSourceFrame = null;
            bool bodyIndexRequired = IsChannelActive(CustomChannelNames.BodyIndex);
            bool depthRequired = IsChannelActive(ChannelNames.Distance) || IsChannelActive(ChannelNames.Point3DImage);
            bool amplitudeRequired = IsChannelActive(ChannelNames.Amplitude);
            bool colorRequired = IsChannelActive(ChannelNames.Color);
            bool longExposureIRRequired = IsChannelActive(CustomChannelNames.LongExposureIR);

            do
            {
                if(!dataAvailable.WaitOne(UpdateTimeoutMilliseconds))
                {
                    throw ExceptionBuilder.BuildFromID(typeof(MetriCam2Exception), this, 005);
                }

                lock (newFrameLock)
                {
                    try
                    {
                        if (multiFrameReference != null)
                        {
                            multiSourceFrame = multiFrameReference.AcquireFrame();
                        }
                    }
                    catch (Exception)
                    {
                        // ignore if the frame is no longer available
                        continue;// throw
                    }
                }

                try
                {
                    // fetch depth?
                    if (depthRequired)
                    {
                        DepthFrameReference depthFrameReference = multiSourceFrame.DepthFrameReference;
                        if (depthFrameReference != null)
                        {
                            // always synchornize on depth frames if possible.
                            if (lastTimeStamp == GetAbsoluteTimeStamp(depthFrameReference.RelativeTime.Ticks))
                            {
                                continue;
                            }
                            using (DepthFrame depthFrame = depthFrameReference.AcquireFrame())
                            {
                                if (depthFrame == null)
                                {
                                    continue;
                                }

                                depthFrameDescription = depthFrame.FrameDescription;
                                int depthWidth = depthFrameDescription.Width;
                                int depthHeight = depthFrameDescription.Height;
                                if ((depthWidth * depthHeight) == this.depthFrameData.Length)
                                {
                                    lock (this.depthFrameData)
                                    {
                                        depthFrame.CopyFrameDataToArray(this.depthFrameData);
                                        lastTimeStamp = GetAbsoluteTimeStamp(depthFrameReference.RelativeTime.Ticks);
                                        timestampDepth = lastTimeStamp;
                                    }
                                    depthRequired = false;
                                }
                            }
                        }
                    }

                    // fetch IR?
                    if (amplitudeRequired)
                    {
                        InfraredFrameReference irFrameReference = multiSourceFrame.InfraredFrameReference;
                        if (irFrameReference != null)
                        {
                            // If depth data is inactive, synchronize on IR frames. If depth and IR are inactive, we synchronize on color frames.
                            if (!(IsChannelActive(ChannelNames.Distance) || IsChannelActive(ChannelNames.Point3DImage)) && lastTimeStamp == GetAbsoluteTimeStamp(irFrameReference.RelativeTime.Ticks))
                            {
                                continue;
                            }

                            using (InfraredFrame irFrame = irFrameReference.AcquireFrame())
                            {
                                if (irFrame == null)
                                {
                                    continue;
                                }

                                FrameDescription irFrameDescription = irFrame.FrameDescription;
                                int irWidth = irFrameDescription.Width;
                                int irHeight = irFrameDescription.Height;
                                if ((irWidth * irHeight) == this.irFrameData.Length)
                                {
                                    lock (this.irFrameData)
                                    {
                                        irFrame.CopyFrameDataToArray(this.irFrameData);
                                        lastTimeStamp = GetAbsoluteTimeStamp(irFrameReference.RelativeTime.Ticks);
                                        timestampIR = lastTimeStamp;
                                    }
                                    amplitudeRequired = false;
                                }
                            }
                        }
                    }

                    // (always) fetch body frame
                    BodyFrameReference bodyFrameReference = multiSourceFrame.BodyFrameReference;
                    if (bodyFrameReference != null)
                    {
                        using (BodyFrame bodyFrame = bodyFrameReference.AcquireFrame())
                        {
                            if (bodyFrame != null)
                            {
                                this.bodies = new Body[bodyFrame.BodyCount];
                                using (bodyFrame)
                                {
                                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                                }
                            }
                            else
                            {
                                // TODO: check if channel is activated.
                            }
                        }
                    }

                    // fetch color?
                    if (colorRequired)
                    {
                        ColorFrameReference colorFrameReference = multiSourceFrame.ColorFrameReference;
                        if (colorFrameReference == null)
                        {
                            continue;
                        }
                        // If depth and IR data is inactive, synchronize on color frames. If color, depth and IR are inactive, we don't care for synchronization.
                        if (!(IsChannelActive(ChannelNames.Distance) || IsChannelActive(ChannelNames.Point3DImage) || IsChannelActive(ChannelNames.Amplitude)) && lastTimeStamp == GetAbsoluteTimeStamp(colorFrameReference.RelativeTime.Ticks))
                        {
                            continue;
                        }

                        using (ColorFrame colorFrame = colorFrameReference.AcquireFrame())
                        {
                            //FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                            //int cWidth = colorFrameDescription.Width;
                            //int cHeight = colorFrameDescription.Width;
                            if (colorFrame != null)
                            {
                                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                                {
                                    lock (this.colorFrameData)
                                    {
                                        colorFrame.CopyConvertedFrameDataToArray(this.colorFrameData, ColorImageFormat.Bgra);
                                        lastTimeStamp = GetAbsoluteTimeStamp(colorFrameReference.RelativeTime.Ticks);
                                        timestampColor = lastTimeStamp;
                                    }
                                }
                                colorRequired = false;
                            }
                        }
                    }

                    // fetch long exposure IR? (this is independent of the IR images and are acquired at the same rate, so every new frame also
                    // has one of these.)
                    if (longExposureIRRequired)
                    {
                        LongExposureInfraredFrameReference longExposureIRFrameRef = multiSourceFrame.LongExposureInfraredFrameReference;
                        using (LongExposureInfraredFrame longIRFrame = longExposureIRFrameRef.AcquireFrame())
                        {
                            if (longIRFrame == null)
                            {
                                continue;
                            }

                            int longIRWidth = longIRFrame.FrameDescription.Width;
                            int longIRHeight = longIRFrame.FrameDescription.Height;
                            if (longExposureIRData == null || (longIRWidth * longIRHeight) != longExposureIRData.Length)
                            {
                                longExposureIRData = new ushort[longIRWidth * longIRHeight];
                            }
                            longIRFrame.CopyFrameDataToArray(longExposureIRData);
                            longExposureIRRequired = false;
                        }
                    }

                    // fetch body index frames?
                    if (bodyIndexRequired)
                    {
                        BodyIndexFrameReference bodyIndexFrameRef = multiSourceFrame.BodyIndexFrameReference;
                        using (BodyIndexFrame bodyIndexFrame = bodyIndexFrameRef.AcquireFrame())
                        {
                            if (bodyIndexFrame == null)
                            {
                                log.Debug("bodyIndexFrame is NULL.");
                                continue;
                            }

                            int bodyIndexWidth = bodyIndexFrame.FrameDescription.Width;
                            int bodyIndexHeight = bodyIndexFrame.FrameDescription.Height;
                            if (bodyIndexData == null || (bodyIndexWidth * bodyIndexHeight) != bodyIndexData.Length)
                            {
                                bodyIndexData = new byte[bodyIndexWidth * bodyIndexHeight];
                            }
                            bodyIndexFrame.CopyFrameDataToArray(bodyIndexData);
                            bodyIndexRequired = false;
                        }
                    }
                }
                catch (Exception)
                {
                    // ignore if the frame is no longer available
                }
                finally
                {
                    multiSourceFrame = null;
                }
            } while (depthRequired || colorRequired || bodyIndexRequired || longExposureIRRequired || amplitudeRequired);
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
                    return CalcAmplitudes();
                case ChannelNames.Distance:
                    return CalcDistances();
                case ChannelNames.Color:
                    return CalcColor();
                case ChannelNames.Point3DImage:
                    return Calc3DCoordinates();
                case CustomChannelNames.LongExposureIR:
                    return CalcLongExposureIR();
                case CustomChannelNames.BodyIndex:
                    return CalcBodyIndex();
            }
            log.Error("Unexpected Channel in CalcChannel().");
            return null;
        }

        /// <summary>
        /// Overrides the standard GetIntrinsic method.
        /// </summary>
        /// <param name="channelName">The channel name.</param>
        /// <returns>The ProjectiveTransformationRational</returns>
        /// <remarks>The method first searches for a pt file on disk. If this fails it is able to provide internal intrinsics for amplitude / depth channel.</remarks>
        public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            IProjectiveTransformation result = null;

            log.Info("Trying to load projective transformation from file.");
            try
            {
                result = base.GetIntrinsics(channelName);
            }
            catch { /* empty */ }

            if (result == null)
            {
                log.Info("Projective transformation file not found.");
                log.Info("Using Kinect factory intrinsics as projective transformation.");
                switch (channelName)
                {
                    case ChannelNames.Amplitude:
                    case ChannelNames.Distance:
                        result = GetFactoryIRIntrinsics();
                        break;
                    default:
                        log.Error("Unsupported channel in GetIntrinsics().");
                        return null;
                }
            }
            return result;
        }
        #endregion
        #endregion

        #region Public Methods
        /// <summary>
        /// Read out the factory intrinsics for the IR/Depth channel. The principal point in x-direction depends on the prperty <see cref="FlipX"/.>
        /// </summary>
        /// <returns>Factory IR intrinsics.</returns>
        public IProjectiveTransformation GetFactoryIRIntrinsics()
        {
            CameraIntrinsics intrinsics = Coordinates.GetDepthCameraIntrinsics();
            for (int i = 0; i < 100 && intrinsics.FocalLengthX == 0; i++)
            {
                intrinsics = Coordinates.GetDepthCameraIntrinsics();
                System.Threading.Thread.Sleep(100);
            }

            float principalPointX = depthWidthMinusOne - intrinsics.PrincipalPointX; //Principal point in x-direction needs to be mirrored, since native Kinect images are flipped.

            return new ProjectiveTransformationRational(depthWidth, depthHeight, intrinsics.FocalLengthX, intrinsics.FocalLengthY, principalPointX, intrinsics.PrincipalPointY, intrinsics.RadialDistortionSecondOrder, intrinsics.RadialDistortionFourthOrder, intrinsics.RadialDistortionSixthOrder, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// Map 3D coordinates to color pixel coordinates.
        /// </summary>
        /// <param name="coordinates3D">3D coordinates of the depth sensor in Metrilus camera coordinate system (x-axis points in right direction, y-axis in bottom direction and z-axis towards the scene).</param>
        /// <returns>Image coordinates of the color camera.</returns>
        public Point2f[] GetFactoryMappingFrom3DToColor(Point3f[] coordinates3D)
        {
            CameraSpacePoint[] irPoints = new CameraSpacePoint[coordinates3D.Length];
            ColorSpacePoint[] colorPoints = new ColorSpacePoint[coordinates3D.Length];

            for (int i = 0; i < coordinates3D.Length; i++)
            {
                irPoints[i] = new CameraSpacePoint()
                {
                    X = -coordinates3D[i].X, //X Axis of Kinect coordinate system points in opposite direction compared to Metrilus camera coordinates
                    Y = -coordinates3D[i].Y, //Y Axis of Kinect coordinate system points in opposite direction compared to Metrilus camera coordiantes
                    Z = coordinates3D[i].Z
                };
            }

            Coordinates.MapCameraPointsToColorSpace(irPoints, colorPoints);

            Point2f[] colorCoordinates = new Point2f[irPoints.Length];

            for (int i = 0; i < colorCoordinates.Length; i++)
            {
                colorCoordinates[i] = new Point2f(ColorWidth - 1 - colorPoints[i].X, colorPoints[i].Y);
            }

            return colorCoordinates;
        }

        public float MaximumDistanceValue
        {
            get
            {
                if (multiReader != null)
                {
                    return (float)multiReader.KinectSensor.DepthFrameSource.DepthMaxReliableDistance / 1000.0f;
                }
                return -1;
            }
        }
        #endregion

        #region Protected Methods
        protected void CalcDistanceMap()
        {
            if (distanceMap != null)
            {
                return;
            }

            Microsoft.Kinect.PointF[] mapPoints = coordinateMapper.GetDepthFrameToCameraSpaceTable();
            distanceMap = new float[depthHeight, depthWidth];
            for (int y = 0; y < depthHeight; y++)
            {
                for (int x = 0; x < depthWidth; x++)
                {
                    int idx = y * depthWidth + x;
                    double a = mapPoints[idx].X;
                    double b = mapPoints[idx].Y;
                    a *= a;
                    b *= b;
                    distanceMap[y, x] = (float)Math.Sqrt(1 + a + b);
                }
            }
        }

        /// <summary>
        /// Calculates the distance data for the current frame.
        /// </summary>
        /// <returns>Distance map with dimensions Width and Height.</returns>
        protected virtual FloatImage CalcDistances()
        {
            CalcDistanceMap();

            FloatImage img = new FloatImage(depthWidth, depthHeight);

            lock (this.depthFrameData)
            {
                for (int y = 0; y < depthHeight; y++)
                {
                    for (int x = 0; x < depthWidth; x++)
                    {
                        // mirror image
                        int idx = y * depthWidth + depthWidthMinusOne - x;
                        img[y, x] = depthFrameData[idx] * distanceMap[y, depthWidthMinusOne - x] * 0.001f;
                    }
                }
            }            
            return img;
        }

        /// <summary>
        /// Calculates the color image for the current frame.
        /// </summary>
        /// <returns>Color image.</returns>
        protected virtual unsafe ColorImage CalcColor()
        {
            lock (this.colorFrameData)
            {
                Bitmap bmp = new Bitmap(ColorWidth, ColorHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                BitmapData bData = bmp.LockBits(new Rectangle(new Point(0, 0), new Size(ColorWidth, ColorHeight)), ImageLockMode.WriteOnly, bmp.PixelFormat);

                System.Runtime.InteropServices.Marshal.Copy(this.colorFrameData, 0, bData.Scan0, bData.Width * bData.Height * this.bytesPerPixel);

                bmp.UnlockBits(bData);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipX); //Kinect images are flipped in x-direction

                return new ColorImage(bmp);
            }
        }

        protected virtual FloatImage CalcAmplitudes()
        {
            FloatImage img = new FloatImage(depthWidth, depthHeight);

            lock (this.irFrameData)
            {
                for (int y = 0; y < this.depthHeight; y++)
                {
                    for (int x = 0; x < this.depthWidth; x++)
                    {
                        // mirror image
                        int idx = y * depthWidth + depthWidthMinusOne - x;
                        img[y, x] = (float)this.irFrameData[idx];
                    }
                }
            }
            return img;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">Object sending the event</param>
        /// <param name="e">Event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            lock (newFrameLock)
            {
                multiFrameReference = e.FrameReference;
                dataAvailable.Set();
            }
        }

        /// <summary>
        /// Opens the MultiSourceFrameReader
        /// </summary>
        private void OpenMultiReader()
        {
            FrameSourceTypes types = FrameSourceTypes.Body;// for now, Body cannot be disabled.

            if (IsChannelActive(ChannelNames.Distance) || IsChannelActive(ChannelNames.Point3DImage))
            {
                types |= FrameSourceTypes.Depth;
            }
            if (IsChannelActive(ChannelNames.Amplitude))
            {
                types |= FrameSourceTypes.Infrared;
            }
            if (IsChannelActive(ChannelNames.Color))
            {
                types |= FrameSourceTypes.Color;
            }
            if (IsChannelActive(CustomChannelNames.LongExposureIR))
            {
                types |= FrameSourceTypes.LongExposureInfrared;
            }
            if (IsChannelActive(CustomChannelNames.BodyIndex))
            {
                types |= FrameSourceTypes.BodyIndex;
            }
            if (multiReader != null)
            {
                multiReader.IsPaused = true;
                multiReader.Dispose();
                multiReader = null;
                GC.Collect();
            }
            multiReader = kinectSensor.OpenMultiSourceFrameReader(types);
            multiReader.IsPaused = false;

            if (IsChannelActive(ChannelNames.Color))
            {
                FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
                if (null == colorFrameData
                    || ColorWidth != colorFrameDescription.Width
                    || ColorHeight != colorFrameDescription.Height)
                {
                    // Color channel is active and has changed
                    ColorWidth = colorFrameDescription.Width;
                    ColorHeight = colorFrameDescription.Height;
                    this.colorFrameData = new byte[ColorWidth * ColorHeight * this.bytesPerPixel];
                }
            }
            else
            {
                // Color channel is deactivated
                ColorWidth = 0;
                ColorHeight = 0;
                colorFrameData = null;
            }
        }

        /// <summary>
        /// Calculates the 3-D point cloud using the native SDK functions.
        /// </summary>
        /// <returns>3-D point coordinates in meters.</returns>
        /// <remarks>Format is [height, width, coordinate] with coordinate in {0,1,2} for x, y and z.</remarks>
        private Point3fImage Calc3DCoordinates()
        {
            CameraSpacePoint[] worldPoints = new CameraSpacePoint[depthHeight * depthWidth];
            Point3fImage img = new Point3fImage(depthWidth, depthHeight);

            lock (this.depthFrameData)
            {
                coordinateMapper.MapDepthFrameToCameraSpace(depthFrameData, worldPoints);

                for (int y = 0; y < depthHeight; y++)
                {
                    for (int x = 0; x < depthWidth; x++)
                    {
                        // mirror image
                        int idx = y * depthWidth + depthWidthMinusOne - x;
                        img[y, x] = new Point3f(worldPoints[idx].X * -1, worldPoints[idx].Y * -1, worldPoints[idx].Z);
                    }
                }
            }
            return img;
        }

        private FloatImage CalcBodyIndex()
        {
            FloatImage result = new FloatImage(depthWidth, depthHeight);

            for (int y = 0; y < this.depthHeight; y++)
            {
                for (int x = 0; x < this.depthWidth; x++)
                {
                    // mirror image
                    int idx = y * depthWidth + depthWidthMinusOne - x;
                    result[y, x] = (float)this.bodyIndexData[idx];
                }
            }

            return result;
        }

        private FloatImage CalcLongExposureIR()
        {
            FloatImage result = new FloatImage(depthWidth, depthHeight);

            for (int y = 0; y < this.depthHeight; y++)
            {
                for (int x = 0; x < this.depthWidth; x++)
                {
                    // mirror image
                    int idx = y * depthWidth + depthWidthMinusOne - x;
                    result[y, x] = (float)this.longExposureIRData[idx];
                }
            }

            return result;
        }        

        private long GetAbsoluteTimeStamp(long relativeTime)
        {
            if (absoluteTimeStampAtFrame0 == 0)
            {
                absoluteTimeStampAtFrame0 = DateTime.Now.Ticks;
                relativeTimeStampAtFrame0 = relativeTime;
            }

            return absoluteTimeStampAtFrame0 + (relativeTime - relativeTimeStampAtFrame0);
        }
        #endregion
    }
}
