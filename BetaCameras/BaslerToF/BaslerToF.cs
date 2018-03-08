using Metrilus.Util;
using System;
using System.Threading;
using ToFCameraWrapper;
using MetriCam2.Exceptions;
using MetriCam2.Enums;
using MetriCam2.Attributes;

namespace MetriCam2.Cameras
{
    public class BaslerToF : Camera
    {
        #region Private Fields
        private ToFCamera camera;
        private int width;
        private int height;

        private static double syncTriggerRate;
        private UInt16[] bufferIntensity;
        private UInt16[] bufferConfidence;
        private Coord3D[] bufferPoint3f;
        private AutoResetEvent dataAvailable = new AutoResetEvent(false);

        private FloatCameraImage distanceImage;
        private UShortCameraImage intensityImage;
        private UShortCameraImage confidenceImage;
        private Point3fCameraImage point3fImage;

        private Object dataLock = new Object();

        private float exposureMilliseconds = 10.0f;
        private bool filterTemporal = false;
        private bool filterSpatial = true;

        private const ulong c_TriggerBaseDelay = 250000000;    // 250 ms
        // Readout time. [ns]
        // Though basically a constant inherent to the ToF camera, the exact value may still change in future firmware releases.
        private const ulong c_ReadoutTime = 21000000;
        #endregion

        #region Properties
        public bool IsMaster { get; private set; }
        private static ulong TriggerDelay { get; set; }
        #endregion

        #region Private Constants
        private const float MinExposureMilliseconds = 0.1f;
        private const float MaxExposureMilliseconds = 25.0f;
        #endregion

        #region Constructor
        public BaslerToF()
            : base()
        {
            IsMaster = false;
            width = 640;
            height = 480;
            modelName = "BaslerToF";
        }

        ~BaslerToF()
        {
            Disconnect();
        }
        #endregion

        #region Properties
#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.BaslerIcon; }
#endif

        /// <summary>
        /// Gets or sets the exposure time in milliseconds.
        /// </summary>
        [Description("Exposure", "Exposure time")]
        [Unit(Unit.Milliseconds)]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        [Range(MinExposureMilliseconds, MaxExposureMilliseconds)]
        public float Exposure
        {
            get
            {
                return exposureMilliseconds;
            }
            set
            {
                if (value < MinExposureMilliseconds || value > MaxExposureMilliseconds)
                {
                    ExceptionBuilder.Throw(typeof(ArgumentOutOfRangeException), this, "error_setParameter", String.Format("The exposure time must be between {0} and {1} ms.", MinExposureMilliseconds, MaxExposureMilliseconds));
                    return;
                }

                exposureMilliseconds = value;
                if (IsConnected)
                {
                    camera.SetParameterValue("ExposureTime", (exposureMilliseconds * 1000.0f).ToString("0.0")); // SDK unit is Microseconds

                    // Read out exposure time (the exposure time cannot be set continuously)
                    exposureMilliseconds = float.Parse(camera.GetParameterValue("ExposureTime"), System.Globalization.CultureInfo.InvariantCulture) / 1000.0f;
                }
            }
        }

        /// <summary>
        /// Get/Set the state of the temporal filter.
        /// </summary>
        [Description("FilterTemporal", "Temporal Filter")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public bool FilterTemporal
        {
            get
            {
                return filterTemporal;
            }
            set
            {
                filterTemporal = value;
                if (IsConnected)
                {
                    camera.SetParameterValue("FilterTemporal", filterTemporal.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Get/Set the state of the spatial filter.
        /// </summary>
        [Description("FilterSpatial", "Spatial Filter")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public bool FilterSpatial
        {
            get
            {
                return filterSpatial;
            }
            set
            {
                filterSpatial = value;
                if (IsConnected)
                {
                    camera.SetParameterValue("FilterSpatial", filterSpatial.ToString().ToLower());
                }
            }
        }
        #endregion

        #region MetriCam2 Camera Interface
        /// <summary>
        /// Resets list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
        /// </summary>
        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Distance));
            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
            Channels.Add(cr.RegisterChannel(ChannelNames.Point3DImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.ConfidenceMap));
        }

        /// <summary>
        /// Device-specific implementation of <see cref="Camera.ActivateChannel"/>.
        /// Activate a channel.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        /// <remarks>This method is implicitly called by <see cref="Camera.ActivateChannel"/> inside a camera lock.</remarks>
        protected override void ActivateChannelImpl(string channelName)
        {
            if (!IsConnected)
            {
                return;
            }

            // not supported currently
        }

        /// <summary>
        /// Device-specific implementation of <see cref="DeactivateChannel"/>.
        /// Deactivate a channel to save time in <see cref="Update"/>.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        /// <remarks>This method is implicitly called by <see cref="Camera.DeactivateChannel"/> inside a camera lock.</remarks>
        protected override void DeactivateChannelImpl(string channelName)
        {
            if (!IsConnected)
            {
                return;
            }

            // not supported currently
        }

        /// <summary>
        /// Device-specific implementation of Connect.
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Connect"/>
        protected override void ConnectImpl()
        {
            log.EnterMethod();

            if (null != camera)
            {
                log.Debug("A camera object exists already.");
                return;
            }

            try
            {
                camera = new ToFCamera();
            }
            catch (CameraException ex)
            {
                throw new ConnectionFailedException(string.Format("Error creating camera object: {0} in {1}", ex.GetType().Name, ex.Source), ex);
            }

            CameraList cameras = ToFCamera.EnumerateCameras();
            CameraInfo ci;

            // check if we want a specific camera or any camera
            if (string.IsNullOrEmpty(SerialNumber))
            {
                if (0 == cameras.Count)
                {
                    throw new ConnectionFailedException("No cameras found");
                }
                // use first camera in list
                ci = cameras[0];
            }
            else
            {
                ci = cameras.Find(camInfo => camInfo.SerialNumber.Equals(SerialNumber));
                if (ci == default(CameraInfo))
                {
                    throw new ConnectionFailedException(string.Format("No camera available with the SN: {0}", SerialNumber));
                }
            }
            camera.Open(ci);

            camera.SetParameterValue("GevIEEE1588", "true");
            //camera.SetParameterValue("ExposureAuto", "On");

            camera.SetParameterValue("ComponentSelector", "Range");
            camera.SetParameterValue("ComponentEnable", "true");
            camera.SetParameterValue("PixelFormat", "Coord3D_ABC32f");

            camera.SetParameterValue("ComponentSelector", "Intensity");
            camera.SetParameterValue("ComponentEnable", "true");

            camera.SetParameterValue("ComponentSelector", "Confidence");
            camera.SetParameterValue("ComponentEnable", "true");

            IsConnected = true; // sic!

            // Disable auto exposure -> causes large regions of invalid pixels

            //Exposure = exposureMilliseconds;

            //// Enable/Disable temporal filtering
            //FilterTemporal = filterTemporal;

            //// Enable/Disable spatial filtering
            //FilterSpatial = filterSpatial;            
            
            // Activate Channels before streaming starts;
            // Activate default channels if no channels are selected
            if (0 == ActiveChannels.Count)
            {
                ActivateChannel(ChannelNames.Point3DImage);
                ActivateChannel(ChannelNames.Distance);
                ActivateChannel(ChannelNames.Intensity);
                ActivateChannel(ChannelNames.ConfidenceMap);
            }
            else
            {
                // Even though MetriCam calls ActivateChannelImpl() for all active channels after Connect() has completed, we need to activate these channels before starting to grab
                foreach (ChannelRegistry.ChannelDescriptor cd in ActiveChannels)
                {
                    ActivateChannelImpl(cd.Name);
                }
            }

            StartGrabbing();
        }

        /// <summary>
        /// Device-specific implementation of Disconnect.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Disconnect"/>
        protected override void DisconnectImpl()
        {
            StopGrabbing();
            camera.Close();
            camera.Dispose();
            camera = null;
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitly called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Update"/>
        protected override void UpdateImpl()
        {
            dataAvailable.WaitOne();
            lock (dataLock)
            {
                unsafe
                {
                    if (IsChannelActive(ChannelNames.Point3DImage))
                    {
                        point3fImage = new Point3fCameraImage(width, height);
                        for (int y = 0, i = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++, i++)
                            {
                                Coord3D c = bufferPoint3f[i];
                                point3fImage[y, x] = new Point3f(c.x, c.y, c.z) * 0.001f;
                            }
                        }
                    }

                    if (IsChannelActive(ChannelNames.Distance))
                    {
                        distanceImage = new FloatCameraImage(width, height);
                        for (int y = 0, i = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++, i++)
                            {
                                Coord3D c = bufferPoint3f[i];
                                distanceImage[y, x] = (new Point3f(c.x, c.y, c.z) * 0.001f).GetLength();
                            }
                        }
                    }

                    if (IsChannelActive(ChannelNames.Intensity))
                    {
                        intensityImage = new UShortCameraImage(width, height);

                        for (int y = 0, i = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++, i++)
                            {
                                intensityImage[y, x] = bufferIntensity[i];
                            }
                        }
                    }

                    if (IsChannelActive(ChannelNames.ConfidenceMap))
                    {
                        confidenceImage = new UShortCameraImage(width, height);

                        for (int y = 0, i = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++, i++)
                            {
                                confidenceImage[y, x] = bufferConfidence[i];
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        /// <seealso cref="Camera.CalcChannel"/>
        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.ConfidenceMap:
                    return CalcConfidenceMap(confidenceImage);
                case ChannelNames.Distance:
                    return distanceImage;
                case ChannelNames.Intensity:
                    return intensityImage.ToFloatCameraImage();
                case ChannelNames.Point3DImage:
                    return point3fImage;
            }
            throw new NotImplementedException();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// According to the Basler operation manual, a 16-bit unsigned integer value is generated per pixel.
        /// Scale this raw confidence map to [0, 1] range.
        /// </summary>
        /// <param name="rawConfidenceMap">16-bit unsigned int raw confidence map as provided by camera</param>
        /// <returns>Confidence map as float image with intensities between 0 and 1</returns>
        private FloatCameraImage CalcConfidenceMap(UShortCameraImage rawConfidenceMap)
        {
            int width = rawConfidenceMap.Width;
            int height = rawConfidenceMap.Height;
            float scaling = 1.0f / ushort.MaxValue;

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

        private void ImageGrabbedHandler(Object sender, ImageGrabbedEventArgs e)
        {
            if (e.status == GrabResultStatus.Timeout)
            {
                log.DebugFormat("Timeout occurred. Acquisition stopped.");
                e.stop = true; // Request to stop image acquisition
                return;
            }
            if (e.status != GrabResultStatus.Ok)
            {
                log.DebugFormat("Image was not grabbed successfully.");
                return;
            }
            try
            {
                lock (dataLock)
                {
                    int size = width * height;
                    unsafe
                    {
                        // If only distance channel is active, e.parts contains only one image buffer
                        // If only intensity channel is active, e.parts contains both range and intensity image buffer
                        // If only confidence channel is active, e.parts contains both range and confidence image buffer
                        // If both intensity and confidence channels are active, e.parts contains range, intensity, and confidence image buffer
                        // => Distance image is always e.parts[0]
                        // => Intensity image is always e.parts[1] if active
                        // => Confidence image is either e.parts[1] if intensity channel is not active or e.parts[2]
                        // First part is range data
                        if (IsChannelActive(ChannelNames.Point3DImage))
                        {
                            bufferPoint3f = e.parts[0].data as Coord3D[];
                        }

                        // Second part is intensity data
                        if (IsChannelActive(ChannelNames.Intensity))
                        {
                            bufferIntensity = e.parts[1].data as UInt16[];
                        }

                        // Third part is confidence data
                        if (IsChannelActive(ChannelNames.ConfidenceMap))
                        {
                            bufferConfidence = e.parts[2].data as UInt16[];
                        }
                    }
                }
                dataAvailable.Set();
            }
            catch (Exception ex)
            {
                log.DebugFormat("Exception in Handler: " + ex.Message);
                camera.Close();
            }
        }

        private void StartGrabbing()
        {
            camera.ImageGrabbed += ImageGrabbedHandler;
            camera.StartGrabbing();
        }

        private void StopGrabbing()
        {
            camera.StopGrabbing();
            camera.ImageGrabbed -= ImageGrabbedHandler;
        }
        #endregion

        /// <summary>
        /// Enables interference-free, simultaneous operation of multiple cameras. Please initialize the individual cameras 
        /// before this call as synchronization might depend on several camera parameters such as exposure time.
        /// </summary>
        /// <param name="cameras">Cameras which should be synchronized.</param>
        /// <remarks>
        /// If cameras are not connected yet, the will be connected by this method.
        /// Synchronization of independent groups of cameras is not properly supported.
        /// </remarks>
        public static void InitializeSynchronizedAcquisition(BaslerToF[] cameras)
        {
            // set up trigger mode:
            for (int i = 0; i < cameras.Length; i++)
            {
                if (!cameras[i].IsConnected)
                {
                    cameras[i].Connect();
                }
                cameras[i].StopGrabbing();
                cameras[i].camera.SetParameterValue("TriggerMode", "On");
                cameras[i].camera.SetParameterValue("TriggerSource", "SyncTimer");
            }
            log.Info("Waiting for cameras to negotiate master role ...");
            // negotiate master:
            int nMaster;
            do
            {
                nMaster = 0;

                // Wait until a master camera (if any) and the slave cameras have been chosen.
                // Note that if a PTP master clock is present in the subnet, all TOF cameras
                // ultimately assume the slave role.
                //
                for (int i = 0; i < cameras.Length; ++i)
                {
                    ToFCamera camera = cameras[i].camera;
                    camera.ExecuteCommand("GevIEEE1588DataSetLatch");

                    while (camera.GetParameterValue("GevIEEE1588StatusLatched") == "Listening")
                    {
                        // Latch GevIEEE1588 status.
                        camera.ExecuteCommand("GevIEEE1588DataSetLatch");
                        System.Threading.Thread.Sleep(1000);
                    }

                    if (camera.GetParameterValue("GevIEEE1588StatusLatched") == "Master")
                    {
                        cameras[i].IsMaster = true;
                        nMaster++;
                    }
                    else
                    {
                        cameras[i].IsMaster = false;
                    }
                }
            } while (nMaster > 1);    // Repeat until there is at most one master left.

            // Use this variable to check whether there is an external master clock.
            bool externalMasterClock = true;

            for (int i = 0; i < cameras.Length; ++i)
            {
                if (cameras[i].IsMaster)
                {
                    log.InfoFormat("Camera {0} is master.", i);
                    externalMasterClock = false;
                }
            }

            if (true == externalMasterClock)
            {
                log.Info("External master clock present in subnet: All cameras are slaves.");
            }
            // Synchronize clocks:
            // Maximum allowed offset from master clock. 
            const long tsOffsetMax = 10000;
            log.InfoFormat("Wait until offsets from master clock have settled below {0} ns", tsOffsetMax);

            for (int camIdx = 0; camIdx < cameras.Length; camIdx++)
            {
                // Check all slaves for deviations from master clock.
                if (!cameras[camIdx].IsMaster)
                {
                    long tsOffset;
                    do
                    {
                        tsOffset = GetMaxAbsGevIEEE1588OffsetFromMasterInTimeWindow(cameras[camIdx], 1.0, 0.1);
                        log.InfoFormat("max offset of cam {0} = {1} ns", camIdx, tsOffset);
                    } while (tsOffset >= tsOffsetMax);
                }
            }
            // Set trigger delays:
            // Current timestamp
            ulong timestamp = 0;
            ulong syncStartTimestamp;

            // The low and high part of the timestamp
            ulong tsLow, tsHigh;

            // Initialize trigger delay.
            TriggerDelay = 0;

            log.Info("Configuring start time and trigger delays ...");

            //
            // Cycle through cameras and set trigger delay.
            //
            for (int camIdx = 0; camIdx < cameras.Length; camIdx++)
            {
                log.InfoFormat("Camera {0} : ", camIdx);

                //
                // Read timestamp and exposure time.
                // Calculation of synchronous free run timestamps will all be based 
                // on timestamp and exposure time(s) of first camera.
                //
                if (camIdx == 0)
                {
                    // Latch timestamp registers.
                    cameras[camIdx].camera.ExecuteCommand("TimestampLatch");

                    // Read the two 32-bit halves of the 64-bit timestamp. 
                    tsLow = ulong.Parse(cameras[camIdx].camera.GetParameterValue("TimestampLow"));
                    tsHigh = ulong.Parse(cameras[camIdx].camera.GetParameterValue("TimestampHigh"));

                    // Assemble 64-bit timestamp and keep it.
                    timestamp = tsLow + (tsHigh << 32);
                    log.InfoFormat("Reading time stamp from first camera.\ntimestamp = {0}\n", timestamp);

                    log.Info("Reading exposure times from first camera:");

                    // Get exposure time count (in case of HDR there will be 2, otherwise 1).
                    int nExpTimes = int.Parse(cameras[camIdx].camera.GetParameterMaximum("ExposureTimeSelector")) + 1;

                    // Sum up exposure times.
                    for (int l = 0; l < nExpTimes; l++)
                    {
                        cameras[camIdx].camera.SetParameterValue("ExposureTimeSelector", l.ToString());
                        ulong expTime = ulong.Parse(cameras[camIdx].camera.GetParameterValue("ExposureTime"));
                        log.InfoFormat("exposure time {0} = ", l);
                        TriggerDelay += (1000 * expTime);   // Convert from us -> ns
                    }

                    log.Info("Calculating trigger delay.");

                    // Add readout time.
                    TriggerDelay += (uint)(nExpTimes - 1) * c_ReadoutTime;

                    // Add safety margin for clock jitter.
                    TriggerDelay += 1000000;

                    // Calculate synchronous trigger rate.
                    log.InfoFormat("Calculating maximum synchronous trigger rate ... ");
                    syncTriggerRate = 1000000000 / ((uint)cameras.Length * TriggerDelay);

                    // If the calculated value is greater than the maximum supported rate, 
                    // adjust it. 
                    double maxSyncRate = double.Parse(cameras[camIdx].camera.GetParameterMaximum("SyncRate"));
                    if (syncTriggerRate > maxSyncRate)
                    {
                        syncTriggerRate = maxSyncRate;
                    }

                    // Print trigger delay and synchronous trigger rate.
                    log.InfoFormat("Trigger delay = {0} ms", TriggerDelay / 1000000);
                    log.InfoFormat("Setting synchronous trigger rate to {0} fps\n", syncTriggerRate);
                }

                // Set synchronization rate.
                cameras[camIdx].camera.SetParameterValue("SyncRate", syncTriggerRate.ToString());

                // Calculate new timestamp by adding trigger delay.
                // First camera starts after triggerBaseDelay, nth camera is triggered 
                // after a delay of triggerBaseDelay +  n * triggerDelay.
                syncStartTimestamp = timestamp + c_TriggerBaseDelay + (uint)camIdx * TriggerDelay;

                // Disassemble 64-bit timestamp.
                tsHigh = syncStartTimestamp >> 32;
                tsLow = syncStartTimestamp - (tsHigh << 32);

                // Set synchronization start time parameters.
                cameras[camIdx].camera.SetParameterValue("SyncStartLow", tsLow.ToString());
                cameras[camIdx].camera.SetParameterValue("SyncStartHigh", tsHigh.ToString());

                // Latch synchronization start time & synchronization rate registers.
                // Until the values have been latched, they won't have any effect.
                cameras[camIdx].camera.ExecuteCommand("SyncUpdate");
            }
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].StartGrabbing();
            }
        }

        private static long GetMaxAbsGevIEEE1588OffsetFromMasterInTimeWindow(BaslerToF camera, double timeToMeasureSec, double timeDeltaSec)
        {
            System.Diagnostics.Stopwatch stopwatch;
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Maximum of offsets from master
            long maxOffset = 0;
            // Number of samples
            int n = 0;
            // Current time
            double currTime;
            do
            {
                // Update current time.
                currTime = stopwatch.Elapsed.TotalSeconds;
                if (currTime >= n * timeDeltaSec)
                {
                    // Time for next sample has elapsed.
                    // Latch IEEE1588 data set to get offset from master.
                    camera.camera.ExecuteCommand("GevIEEE1588DataSetLatch");
                    // Maximum of offsets from master.
                    long currOffset = long.Parse(camera.camera.GetParameterValue("GevIEEE1588OffsetFromMaster"));
                    maxOffset = Math.Max(maxOffset, Math.Abs(currOffset));
                    // Increase number of samples.
                    n++;
                }
                System.Threading.Thread.Sleep(1);
            } while (currTime <= timeToMeasureSec);
            // Return maximum of offsets from master for given time interval.
            return maxOffset;
        }
    }
}
