// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using MetriCam2.Enums;
using MetriCam2.Attributes;
using System.Collections.Generic;

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

        /// <summary>
        /// [DUMMY] Gain in [%].
        /// </summary>
        [Description("Gain", "Gain Factor")]
        [Unit("%")]
        [Range(0, 100)]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
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

        /// <summary>
        /// [DUMMY] The currently selected trigger mode.
        /// </summary>
        [Description("Trigger Mode")]
        [AllowedValueList(typeof(TriggerModeDummy))]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public TriggerModeDummy TriggerMode
        {
            get { return __trigger_mode_dummy; }
            set { __trigger_mode_dummy = value; }
        }

        #region Parameters for Unit Tests

        [Description("BoolParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public bool BoolParam { get; set; }

        [Description("ByteParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public byte ByteParam { get; set; }

        [Description("ShortParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public short ShortParam { get; set; }

        [Description("IntParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public int IntParam { get; set; }

        [Description("LongParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public long LongParam { get; set; }

        [Description("FloatParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public float FloatParam { get; set; }

        [Description("FloatParam", "FloatParam with 1 decimal and a list of allowed values")]
        [AllowedValueList(new float[] { 8.3f, 10.0f, 15.0f, 25.1f }, "SingleDecimal")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public float FormatedFloatWithAllowedValues { get; set; }

        [Description("DoubleParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public double DoubleParam { get; set; }

        [Description("ByteRangeParam")]
        [Range(10, 100)]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public byte ByteRangeParam { get; set; }

        [Description("ShortRangeParam")]
        [Range(10, 100)]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public short ShortRangeParam { get; set; }

        [Description("IntRangeParam")]
        [Range(10, 100)]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public int IntRangeParam { get; set; }

        [Description("LongRangeParam")]
        [Range(10, 100)]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public long LongRangeParam { get; set; }

        [Description("FloatRangeParam")]
        [Range(10.0f, 100.0f)]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public float FloatRangeParam { get; set; }

        [Description("DoubleRangeParam")]
        [Range(10.0d, 100.0d)]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public double DoubleRangeParam { get; set; }

        [Description("EnumListParam")]
        [AllowedValueList(typeof(TriggerModeDummy))]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public TriggerModeDummy EnumListParam { get; set; }

        [Description("FloatListParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        [AllowedValueList(new float[] { 16f, 18f, 20.5f, 24.07f })]
        public float FloatListParam { get; set; }

        [Description("Point3fParam")]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
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
