using Metrilus.Util;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using ToFCameraWrapper;
using System.Runtime;
using System.Diagnostics;

namespace MetriCam2.Cameras
{
    public unsafe class BaslerToF : Camera
    {

        #region Private Fields
        private ToFCamera camera;
        private int width;
        private int height;

        private UInt16[] bufferIntensity;
        private UInt16[] bufferConfidence;
        private Coord3D[] bufferPoint3f;
        private AutoResetEvent dataAvailable = new AutoResetEvent(false);

        private FloatCameraImage depthImage;
        private UShortCameraImage intensityImage;
        private UShortCameraImage confidenceImage;
        private Point3fCameraImage point3fImage;

        private Object dataLock = new Object();

        private float exposureMilliseconds = 10.0f;
        private bool filterTemporal = false;
        private bool filterSpatial = true;
        private bool isMaster = false;

        private ulong m_TriggerDelay;
        private const ulong c_TriggerBaseDelay = 250000000;    // 250 ms
        private double m_SyncTriggerRate;
        // Readout time. [ns]
        // Though basically a constant inherent to the ToF camera, the exact value may still change in future firmware releases.
        private const ulong c_ReadoutTime = 21000000;
        #endregion

        public bool IsFirst { get; set; }

        #region Private Constants
        private const float MinExposureMilliseconds = 0.1f;
        private const float MaxExposureMilliseconds = 25.0f;
        #endregion

        #region Constructor
        public BaslerToF()
            : base()
        {
            camera = new ToFCamera();
            width = 640;
            height = 480;
        }

        ~BaslerToF() { /* empty */ }
        #endregion

        #region Properties
        private RangeParamDesc<float> ExposureDesc
        {
            get
            {
                return new RangeParamDesc<float>(MinExposureMilliseconds, MaxExposureMilliseconds)
                {
                    Description = "Exposure time",
                    Unit = "ms",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        /// <summary>
        /// Gets or sets the exposure time in milliseconds.
        /// </summary>
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
                    exposureMilliseconds = float.Parse(camera.GetParameterValue("ExposureTime")) / 1000.0f;
                }
            }
        }

        private ParamDesc<bool> FilterTemporalDesc
        {
            get
            {
                return new ParamDesc<bool>()
                {
                    Description = "Temporal Filter",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }

        /// <summary>
        /// Get/Set the state of the temporal filter.
        /// </summary>
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

        private ParamDesc<bool> FilterSpatialDesc
        {
            get
            {
                return new ParamDesc<bool>()
                {
                    Description = "Spatial Filter",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }

        /// <summary>
        /// Get/Set the state of the spatial filter.
        /// </summary>
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
        #region MetriCam2 Camera Interface Methods
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
        /// <remarks>This method is implicitely called by <see cref="Camera.ActivateChannel"/> inside a camera lock.</remarks>
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
        /// <remarks>This method is implicitely called by <see cref="Camera.DeactivateChannel"/> inside a camera lock.</remarks>
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
        /// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Connect"/>
        protected override void ConnectImpl()
        {
            if (camera.IsOpen())
                return;

            CameraList cameras = ToFCamera.EnumerateCameras();

            camera.Open(ToFCamera.EnumerateCameras().Find(camInfo => camInfo.SerialNumber.Equals(SerialNumber)));

            camera.SetParameterValue("GevIEEE1588", "true");
            camera.SetParameterValue("TriggerMode", "On");
            camera.SetParameterValue("TriggerSource", "SyncTimer");

           // camera.SetParameterValue("ExposureAuto", "On");

            camera.SetParameterValue("ComponentSelector", "Range");
            camera.SetParameterValue("ComponentEnable", "true");
            camera.SetParameterValue("PixelFormat", "Coord3D_ABC32f");
            camera.SetParameterValue("ComponentSelector", "Intensity");
            camera.SetParameterValue("ComponentEnable", "true");
            camera.SetParameterValue("ComponentSelector", "Confidence");
            camera.SetParameterValue("ComponentEnable", "true");

            modelName = "BaslerToF";
            IsConnected = true; // sic!

            // Disable auto exposure -> causes large regions of invalid pixels
/*
            Exposure = exposureMilliseconds;

            // Enable/Disable temporal filtering
            FilterTemporal = filterTemporal;

            // Enable/Disable spatial filtering
            FilterSpatial = filterSpatial;            
            */
            // Activate Channels before streaming starts;
            // Activate standard channels if no channels are selected 
            if (ActiveChannels.Count == 0)
            {
                ActivateChannel(ChannelNames.Point3DImage);
                ActivateChannel(ChannelNames.Distance);
                ActivateChannel(ChannelNames.Intensity);
                ActivateChannel(ChannelNames.ConfidenceMap);
            }
            else
            {
                // Even though MetriCam calls ActivateChannelImpl() for all active channels after Connect() has completed, we need to activate these channels before starting to grab
                foreach (MetriCam2.ChannelRegistry.ChannelDescriptor s in ActiveChannels)
                {
                    ActivateChannelImpl(s.Name);
                }
            }
            StartGrabbing();
        }

        /// <summary>
        /// Device-specific implementation of Disconnect.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Disconnect"/>
        protected override void DisconnectImpl()
        {
            StopGrabbing();
            camera.Close();
        }

        protected void StartGrabbing()
        {
            camera.ImageGrabbed += ImageGrabbedHandler;
            camera.StartGrabbing();
        }

        protected void StopGrabbing()
        {
            camera.StopGrabbing();
            camera.ImageGrabbed -= ImageGrabbedHandler;
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
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
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                Coord3D c = bufferPoint3f[y * width + x];
                                point3fImage[y, x] = new Point3f(c.x * 0.001f, c.y * 0.001f, c.z * 0.001f);
                            }
                        }
                    }

                    if (IsChannelActive(ChannelNames.Distance))
                    {
                        depthImage = new FloatCameraImage(width, height);
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                Coord3D c = bufferPoint3f[y * width + x];
                                Point3f p = new Point3f(c.x * 0.001f, c.y * 0.001f, c.z * 0.001f);
                                depthImage[y, x] = p.GetLength();
                            }
                        }
                    }
                   

                    if (IsChannelActive(ChannelNames.Intensity))
                    {
                        intensityImage = new UShortCameraImage(width, height);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                intensityImage[y, x] = bufferIntensity[y * width + x];
                            }
                        }
                    }

                    if (IsChannelActive(ChannelNames.ConfidenceMap))
                    {
                        confidenceImage = new UShortCameraImage(width, height);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                confidenceImage[y, x] = bufferConfidence[y * width + x];
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
                    return depthImage;
                case ChannelNames.Intensity:
                    return intensityImage.ToFloatCameraImage();
                case ChannelNames.Point3DImage:
                    return point3fImage;
            }
            throw new NotImplementedException();
        }
        #endregion
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

        private void Copy(IntPtr source, ushort[] destination, int startIndex, int length)
        {
            unsafe
            {
                var sourcePtr = (ushort*)source;
                for (int i = startIndex; i < startIndex + length; ++i)
                {
                    destination[i] = *sourcePtr++;
                }
            }
        }
        #endregion


        public void FindMaster()
        {
            Console.WriteLine("Waiting for cameras to negotiate master role ...\n");

                //
                // Wait until a master camera (if any) and the slave cameras have been chosen.
                // Note that if a PTP master clock is present in the subnet, all TOF cameras
                // ultimately assume the slave role.
                //
                    camera.ExecuteCommand("GevIEEE1588DataSetLatch");

            while (camera.GetParameterValue("GevIEEE1588StatusLatched") == "Listening")
            {
                // Latch GevIEEE1588 status.
                camera.ExecuteCommand("GevIEEE1588DataSetLatch");
                Console.Write(".");
                System.Threading.Thread.Sleep(1000);
            }

            if (camera.GetParameterValue("GevIEEE1588StatusLatched") == "Master")
            {
                isMaster = true;
            }
            else
            {
                isMaster = false;
            }
        }

        /*
         Make sure that all slave clocks are in sync with the master clock.

         For each camera with slave role: Check how much the slave clocks deviate from the master clock.
         Wait until deviation is lower than a preset threshold.
         */

        public void syncCameras()
        {
            // Maximum allowed offset from master clock. 
            const long tsOffsetMax = 10000;
            Console.WriteLine("Wait until offsets from master clock have settled below {0} ns\n", tsOffsetMax);

            // Check all slaves for deviations from master clock.
            if (false == isMaster)
            {
                long tsOffset;
                do
                {
                    tsOffset = GetMaxAbsGevIEEE1588OffsetFromMasterInTimeWindow(1.0, 0.1);
                } while (tsOffset >= tsOffsetMax);
            }
            
        }

        public long GetMaxAbsGevIEEE1588OffsetFromMasterInTimeWindow(double timeToMeasureSec, double timeDeltaSec)
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
                    camera.ExecuteCommand("GevIEEE1588DataSetLatch");
                    // Maximum of offsets from master.
                    long currOffset = long.Parse(camera.GetParameterValue("GevIEEE1588OffsetFromMaster"));
                    maxOffset = Math.Max(maxOffset, Math.Abs(currOffset));
                    // Increase number of samples.
                    n++;
                }
                System.Threading.Thread.Sleep(1);
            } while (currTime <= timeToMeasureSec);
            // Return maximum of offsets from master for given time interval.
            return maxOffset;
        }

        /*
         Set trigger delay for each camera.

         A trigger delay is set that is equal to or longer than the exposure time of the camera. 
         A timestamp is read from the first camera.
         Calculation of synchronous free run timestamps is based on this timestamp.
         Calculate synchronous free run timestamp by adding trigger delay.
         First camera starts after triggerBaseDelay, nth camera is triggered after a delay of 
         triggerBaseDelay +  n * ( triggerDelay + safety margin).
         For an explanation of the calculation of trigger delays and synchronous free run trigger rate, 
         please have a look at the documentation block at the top of this file!
         */
        public void setTriggerDelays()
        {

            // Current timestamp
            ulong timestamp = 0;
            ulong syncStartTimestamp;

            // The low and high part of the timestamp
            ulong tsLow, tsHigh;

            // Initialize trigger delay.
            m_TriggerDelay = 0;

            Console.WriteLine("Configuring start time and trigger delays ...\n");

            //
            // Cycle through cameras and set trigger delay.
            //


            //
            // Read timestamp and exposure time.
            // Calculation of synchronous free run timestamps will all be based 
            // on timestamp and exposure time(s) of first camera.
            //
            if (IsFirst)
            {
                // Latch timestamp registers.
                camera.ExecuteCommand("TimestampLatch");

                // Read the two 32-bit halves of the 64-bit timestamp. 
                tsLow = ulong.Parse(camera.GetParameterValue("TimestampLow"));
                tsHigh = ulong.Parse(camera.GetParameterValue("TimestampHigh"));

                // Assemble 64-bit timestamp and keep it.
                timestamp = tsLow + (tsHigh << 32);
                Console.WriteLine("Reading time stamp from first camera.\ntimestamp = {0}\n", timestamp);

                Console.WriteLine("Reading exposure times from first camera:");

                // Get exposure time count (in case of HDR there will be 2, otherwise 1).
                int nExpTimes = int.Parse(camera.GetParameterMaximum("ExposureTimeSelector")) + 1;

                // Sum up exposure times.
                for (int l = 0; l < nExpTimes; l++)
                {
                    camera.SetParameterValue("ExposureTimeSelector", l.ToString());
                    ulong expTime = ulong.Parse(camera.GetParameterValue("ExposureTime"));
                    Console.WriteLine("exposure time {0} = ", l);
                    m_TriggerDelay += (1000 * expTime);   // Convert from us -> ns
                }

                Console.WriteLine("Calculating trigger delay.");

                // Add readout time.
                m_TriggerDelay += (uint)(nExpTimes - 1) * c_ReadoutTime;

                // Add safety margin for clock jitter.
                m_TriggerDelay += 1000000;

                // Calculate synchronous trigger rate.
                Console.WriteLine("Calculating maximum synchronous trigger rate ... ");
                m_SyncTriggerRate = 1000000000 / m_TriggerDelay;

                // If the calculated value is greater than the maximum supported rate, 
                // adjust it. 
                double maxSyncRate = double.Parse(camera.GetParameterMaximum("SyncRate"));
                if (m_SyncTriggerRate > maxSyncRate)
                {
                    m_SyncTriggerRate = maxSyncRate;
                }

                // Print trigger delay and synchronous trigger rate.
                Console.WriteLine("Trigger delay = {0} ms", m_TriggerDelay / 1000000);
                Console.WriteLine("Setting synchronous trigger rate to {0} fps\n", m_SyncTriggerRate);
            }

            // Set synchronization rate.
            camera.SetParameterValue("SyncRate", m_SyncTriggerRate.ToString());

            // Calculate new timestamp by adding trigger delay.
            // First camera starts after triggerBaseDelay, nth camera is triggered 
            // after a delay of triggerBaseDelay +  n * triggerDelay.
            syncStartTimestamp = timestamp + c_TriggerBaseDelay + m_TriggerDelay;

            // Disassemble 64-bit timestamp.
            tsHigh = syncStartTimestamp >> 32;
            tsLow = syncStartTimestamp - (tsHigh << 32);

            // Set synchronization start time parameters.
            camera.SetParameterValue("SyncStartLow", tsLow.ToString());
            camera.SetParameterValue("SyncStartHigh", tsHigh.ToString());

            // Latch synchronization start time & synchronization rate registers.
            // Until the values have been latched, they won't have any effect.
            camera.ExecuteCommand("SyncUpdate");


        }
    }
}
