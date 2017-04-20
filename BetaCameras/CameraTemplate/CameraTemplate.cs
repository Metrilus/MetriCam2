using Metrilus.Util;
using System;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// This is a template for creating new camera wrappers, and also a dummy camera.
    /// </summary>
    /// <remarks>
    /// Follow this guide if you want to add a new camera implementation:
    /// 1. Start by reading @link page_adding_a_new_camera Adding a new camera @endlink.
    /// 2. Find everything named DUMMY, DEMO, and TODO. Read, understand, adapt and/or delete it.
    /// 3. Add MetriCam2 to the project's references.
    /// 4. Implement Camera's methods.
    /// 5. Work through all comments and update them (i.e. remove remarks like "This method is implicitely called [...]" and "Device-specific implementation of [...]").
    /// 6. (optional) Implement ActivateChannelImpl and DeactivateChannelImpl.
    /// 7. Delete this how-to.
    /// </remarks>
    public class CameraTemplate : Camera
    {
        #region Types
        /// <summary>
        /// Defines the custom channel names for easier handling.
        /// </summary>
        /// <remarks>Similar to MetriCam2.ChannelNames for standard channel names.</remarks>
        public class CustomChannelNames
        {
            public const string DummyNoiseChannel = "DummyNoiseChannel";
        }
        #endregion

        #region [DUMMY] Delete In Actual Camera Implementations
        // The declarations in this region are just mock-ups for demo purposes.
        // You do not want to use them in your camera implementation.

        #region Types
        /// <summary>[DUMMY] Trigger mode for image aquisition.</summary>
        public enum TriggerModeDummy
        {
            /// <summary>The camera continously acquires frames. Non-processed frames are dropped.</summary>
            FREERUN,
            /// <summary>The frame acquisition is triggered by trying to fetch a frame.</summary>
            SOFTWARE,
            /// <summary>The frame acquisition is triggerd by a signal of a camera-type specific hardware pin of the camera.</summary>
            HARDWARE
        };
        #endregion
        private int __gain_dummy; // TODO: delete me
        private bool __auto_gain_dummy; // TODO: delete me
        private TriggerModeDummy __trigger_mode_dummy; // TODO: delete me
        private const int __noiseImageWidth = 640; // TODO: delete me
        private const int __noiseImageHeight = 480; // TODO: delete me
        #endregion

        #region Public Properties
        #region [DUMMY] Delete In Actual Camera Implementations
        // The properties in this region are just mock-ups for demo purposes.
        // You do not want to use them in your camera implementation.

        /// <summary>
        /// [DUMMY] Enable/disable auto gain.
        /// </summary>
        public bool AutoGain
        {
            // This implementation is just for DEMO purposes and should be replaced in an actual camera implementation.
            get { return __auto_gain_dummy; }
            set
            {
                if (value == __auto_gain_dummy)
                    return;

                __auto_gain_dummy = value;
                // simulate a gain value set by the camera's autogain.
                if (__gain_dummy < 50)
                    __gain_dummy *= 2;
                else
                    __gain_dummy /= 2;
            }
        }
        private ParamDesc<int> GainDesc
        {
            // This implementation is just for DEMO purposes and should be replaced in an actual camera implementation.
            get
            {
                return new RangeParamDesc<int>(0, 100)
                {
                    Description = "Gain factor.",
                    Unit = "%",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }
        /// <summary>
        /// [DUMMY] Gain in [%].
        /// </summary>
        public int Gain
        {
            // This implementation is just for DEMO purposes and should be replaced in an actual camera implementation.
            get
            {
                return __gain_dummy;
            }
            set
            {
                AutoGain = false;
                __gain_dummy = value;
            }
        }

        private ParamDesc<TriggerModeDummy> TriggerModeDesc
        {
            get
            {
                return new ListParamDesc<TriggerModeDummy>(typeof(TriggerModeDummy))
                {
                    Description = "Trigger mode.",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        /// <summary>
        /// [DUMMY] The currently selected trigger mode.
        /// </summary>
        public TriggerModeDummy TriggerMode
        {
            get { return __trigger_mode_dummy; }
            set { __trigger_mode_dummy = value; }
        }

        #region Parameters for Unit Tests
        private ParamDesc<bool> BoolParamDesc
        {
            get
            {
                return new ParamDesc<bool>()
                {
                    Description = "BoolParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public bool BoolParam { get; set; }

        private ParamDesc<byte> ByteParamDesc
        {
            get
            {
                return new ParamDesc<byte>()
                {
                    Description = "ByteParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public byte ByteParam { get; set; }

        private ParamDesc<short> ShortParamDesc
        {
            get
            {
                return new ParamDesc<short>()
                {
                    Description = "ShortParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public short ShortParam { get; set; }

        private ParamDesc<int> IntParamDesc
        {
            get
            {
                return new ParamDesc<int>()
                {
                    Description = "IntParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public int IntParam { get; set; }

        private ParamDesc<long> LongParamDesc
        {
            get
            {
                return new ParamDesc<long>()
                {
                    Description = "LongParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public long LongParam { get; set; }

        private ParamDesc<float> FloatParamDesc
        {
            get
            {
                return new ParamDesc<float>()
                {
                    Description = "FloatParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public float FloatParam { get; set; }

        private ParamDesc<double> DoubleParamDesc
        {
            get
            {
                return new ParamDesc<double>()
                {
                    Description = "DoubleParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public double DoubleParam { get; set; }

        private RangeParamDesc<byte> ByteRangeParamDesc
        {
            get
            {
                return new RangeParamDesc<byte>(10, 100)
                {
                    Description = "ByteRangeParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public byte ByteRangeParam { get; set; }

        private RangeParamDesc<short> ShortRangeParamDesc
        {
            get
            {
                return new RangeParamDesc<short>(10, 100)
                {
                    Description = "ShortRangeParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public short ShortRangeParam { get; set; }

        private RangeParamDesc<int> IntRangeParamDesc
        {
            get
            {
                return new RangeParamDesc<int>(10, 100)
                {
                    Description = "IntRangeParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public int IntRangeParam { get; set; }

        private RangeParamDesc<long> LongRangeParamDesc
        {
            get
            {
                return new RangeParamDesc<long>(10, 100)
                {
                    Description = "LongRangeParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public long LongRangeParam { get; set; }

        private RangeParamDesc<float> FloatRangeParamDesc
        {
            get
            {
                return new RangeParamDesc<float>(10.0f, 100.0f)
                {
                    Description = "FloatRangeParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public float FloatRangeParam { get; set; }

        private RangeParamDesc<double> DoubleRangeParamDesc
        {
            get
            {
                return new RangeParamDesc<double>(10.0, 100.0)
                {
                    Description = "DoubleRangeParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public double DoubleRangeParam { get; set; }

        private ListParamDesc<TriggerModeDummy> EnumListParamDesc
        {
            get
            {
                return new ListParamDesc<TriggerModeDummy>(typeof(TriggerModeDummy))
                {
                    Description = "EnumListParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        public TriggerModeDummy EnumListParam { get; set; }

        private ListParamDesc<float> FloatListParamDesc
        {
            get
            {
                System.Collections.Generic.List<float> allowedValues = new System.Collections.Generic.List<float>();
                allowedValues.Add(16f);
                allowedValues.Add(18.0f);
                allowedValues.Add(20.5f);
                allowedValues.Add(24.07f);
                ListParamDesc<float> res = new ListParamDesc<float>(allowedValues)
                {
                    Description = "FloatListParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
                return res;
            }
        }
        public float FloatListParam { get; set; }

        private ParamDesc<Point3f> Point3fParamDesc
        {
            get
            {
                ParamDesc<Point3f> res = new ParamDesc<Point3f>()
                {
                    Description = "Point3fParam",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                };
                return res;
            }
        }
        public Point3f Point3fParam { get; set; }
        #endregion
        #endregion
        #endregion

        #region Constructor
        public CameraTemplate()
            : base()
        {
            log.Warn(Name + ": TODO: Implement Constructor().");

            // enableImplicitThreadSafety makes camera implementations more thread-safe, but costs a bit of performance.
            // See documentation of enableImplicitThreadSafety.
            enableImplicitThreadSafety = true;
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

            log.Warn(Name + ": TODO: Implement LoadAllAvailableChannels().");
            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.DummyNoiseChannel, typeof(FloatCameraImage)));
        }

        /// <summary>
        /// Device-specific implementation of Connect.
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Connect"/>
        protected override void ConnectImpl()
        {
            log.Warn(Name + ": TODO: Implement ConnectImpl().");
            log.Warn(Name + ": TODO: Set modelName.");
        }

        /// <summary>
        /// Device-specific implementation of Disconnect.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Disconnect"/>
        protected override void DisconnectImpl()
        {
            log.Warn(Name + ": TODO: Implement DisconnectImpl().");
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        /// <seealso cref="Camera.Update"/>
        protected override void UpdateImpl()
        {
            log.Warn(Name + ": TODO: Implement UpdateImpl().");
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        /// <seealso cref="Camera.CalcChannel"/>
        protected override CameraImage CalcChannelImpl(string channelName)
        {
            log.Warn(Name + ": TODO: Implement CalcChannel().");
            switch (channelName)
            {
                case CustomChannelNames.DummyNoiseChannel:
                    return CalcDummyNoiseChannel();
            }
            throw new NotImplementedException();
        }
        #endregion
        #endregion

        #region Private Methods
        /// <summary>Computes (image) data for the fake Noise channel.</summary>
        /// <returns>Noisy image data.</returns>
        /// <seealso cref="Camera.CalcChannel"/>
        private FloatCameraImage CalcDummyNoiseChannel()
        {
            FloatCameraImage dummyNoiseImage = new FloatCameraImage(__noiseImageWidth, __noiseImageHeight);
            // TODO: Add channel data (here: noise)
            return dummyNoiseImage;
        }
        #endregion
    }
}
