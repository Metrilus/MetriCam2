// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
using System.Collections.Generic;
using MetriCam2.Attributes;
using System.Threading;
using MetriCam2.Enums;
#if NETSTANDARD2_0
#else
using System.Drawing.Imaging;
#endif


namespace MetriCam2.Cameras
{
    public class RealSense2 : Camera, IDisposable
    {
        #region Private Members
        private RealSense2API.RS2Context _context;
        private RealSense2API.RS2Pipeline _pipeline;
        private RealSense2API.RS2Config _config;
        private RealSense2API.RS2Frame _currentColorFrame = new RealSense2API.RS2Frame(IntPtr.Zero);
        private RealSense2API.RS2Frame _currentDepthFrame = new RealSense2API.RS2Frame(IntPtr.Zero);
        private RealSense2API.RS2Frame _currentLeftFrame = new RealSense2API.RS2Frame(IntPtr.Zero);
        private RealSense2API.RS2Frame _currentRightFrame = new RealSense2API.RS2Frame(IntPtr.Zero);
        private float _depthScale = 0.0f;
        private bool _disposed = false;
        private bool _updatingPipeline = false;
        private Dictionary<string, ProjectiveTransformationZhang> _intrinsics = new Dictionary<string, ProjectiveTransformationZhang>();
        private Dictionary<string, RigidBodyTransformation> _extrinsics = new Dictionary<string, RigidBodyTransformation>();
        #endregion

        #region enums
        public enum EmitterMode
        {
            OFF = 0,
            ON = 1,
            AUTO = 2
        }

        public enum PowerLineMode
        {
            OFF = 0,
            FREQ_50HZ = 1,
            FREQ_60HZ = 2,
            AUTO = 3
        }
        #endregion

        #region RealSense2 Camera Properties

        #region Device Information
        public override string Vendor { get => "Intel"; }

        private string _model = "RealSense2";
        public override string Model
        {
            get => _model;
        }

        public override string Name { get => Model; }

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.RealSense2Icon; }
#endif
        #endregion

        #region Not implemented Properties
        // Not implemented options (mostly because not supported by D435):
        // - VISUAL_PRESET (functionallity provided by profiles built into metricam)
        // - ACCURACY
        // - MOTION_RANGE
        // - FILTER_OPTION
        // - CONFIDENCE_THRESHOLD
        // - TOTAL_FRAME_DROPS
        // - AUTO_EXPOSURE_MODE
        // - MOTION_MODULE_TEMPERATURE
        // - ENABLE_MOTION_CORRECTION
        // - COLOR_SCHEME
        // - HISTOGRAM_EQUALIZATION_ENABLED
        // - MIN_DISTANCE
        // - MAX_DISTANCE
        // - TEXTURE_SOURCE
        // - FILTER_MAGNITUDE
        // - FILTER_SMOOTH_ALPHA
        // - FILTER_SMOOTH_DELTA
        #endregion

        #region Color Resolution
        private Point2i _colorResolution = new Point2i(640, 480);
        public List<Point2i> ColorResolutionList
        {
            get
            {
                List<Point2i> resolutions = new List<Point2i>();
                resolutions.Add(new Point2i(640, 480));

                if (this.IsConnected)
                    resolutions = RealSense2API.GetSupportedResolutions(_pipeline, RealSense2API.SensorName.COLOR);

                return resolutions;
            }
        }

        [Unit(Unit.Pixel)]
        [Description("Resolution (Color Sensor)", "Resolution of the color images in pixel")]
        [AllowedValueList(nameof(ColorResolutionList), nameof(TypeConversion.Point2iToResolution))]
        [AccessState(readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public Point2i ColorResolution
        {
            get { return _colorResolution; }
            set
            {
                if (value == _colorResolution)
                    return;

                ThrowIfBusy("color resolution");

                _updatingPipeline = true;
                StopPipeline();
                DeactivateChannelImpl(ChannelNames.Color);
                _colorResolution = value;
                ActivateChannelImpl(ChannelNames.Color);
                StartPipeline();
                _updatingPipeline = false;
            }
        }
        #endregion

        #region Color FPS
        private int _colorFPS = 30;
        public List<int> ColorFPSList
        {
            get
            {
                List<int> framerates = new List<int>();
                framerates.Add(30);

                if (this.IsConnected)
                {
                    framerates = RealSense2API.GetSupportedFrameRates(_pipeline, RealSense2API.SensorName.COLOR);
                    framerates.Sort();
                }

                return framerates;
            }
        }

        [Unit(Unit.FPS)]
        [Description("FPS (Color Sensor)", "Frames per second of the color sensor")]
        [AllowedValueList(nameof(ColorFPSList))]
        [AccessState(readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public int ColorFPS
        {
            get { return _colorFPS; }
            set
            {
                if (value == _colorFPS)
                    return;

                ThrowIfBusy("color fps");

                _updatingPipeline = true;
                StopPipeline();
                DeactivateChannelImpl(ChannelNames.Color);
                _colorFPS = value;
                ActivateChannelImpl(ChannelNames.Color);
                StartPipeline();
                _updatingPipeline = false;
            }
        }
        #endregion

        #region Depth Resolution
        private Point2i _depthResolution = new Point2i(640, 480);
        public List<Point2i> DepthResolutionList
        {
            get
            {
                List<Point2i> resolutions = new List<Point2i>();
                resolutions.Add(new Point2i(640, 480));

                if (this.IsConnected)
                    resolutions = RealSense2API.GetSupportedResolutions(_pipeline, RealSense2API.SensorName.STEREO);

                return resolutions;
            }
        }

        [Unit(Unit.Pixel)]
        [Description("Resolution (Depth Sensor)", "Resolution of the depth images in pixel")]
        [AllowedValueList(nameof(DepthResolutionList), nameof(TypeConversion.Point2iToResolution))]
        [AccessState(readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
                    writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public Point2i DepthResolution
        {
            get { return _depthResolution; }
            set
            {
                if (value == _depthResolution)
                    return;

                ThrowIfBusy("depth resolution");

                _updatingPipeline = true;
                StopPipeline();

                if (IsChannelActive(ChannelNames.ZImage))
                    DeactivateChannelImpl(ChannelNames.ZImage);

                if (IsChannelActive(ChannelNames.Left))
                    DeactivateChannelImpl(ChannelNames.Left);

                if (IsChannelActive(ChannelNames.Right))
                    DeactivateChannelImpl(ChannelNames.Right);

                _depthResolution = value;

                if (IsChannelActive(ChannelNames.ZImage))
                    ActivateChannelImpl(ChannelNames.ZImage);

                if (IsChannelActive(ChannelNames.Left))
                    ActivateChannelImpl(ChannelNames.Left);

                if (IsChannelActive(ChannelNames.Right))
                    ActivateChannelImpl(ChannelNames.Right);

                StartPipeline();
                _updatingPipeline = false;
            }
        }
        #endregion

        #region Depth FPS
        private int _depthFPS = 30;
        public List<int> DepthFPSList
        {
            get
            {
                List<int> framerates = new List<int>();
                framerates.Add(30);

                if (this.IsConnected)
                {
                    framerates = RealSense2API.GetSupportedFrameRates(_pipeline, RealSense2API.SensorName.STEREO);
                    framerates.Sort();
                }

                return framerates;
            }
        }

        [Unit(Unit.FPS)]
        [Description("FPS (Depth Sensor)", "Currently set frames per second the stereo sensor operates with")]
        [AccessState(readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
                    writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        [AllowedValueList(nameof(DepthFPSList))]
        public int DepthFPS
        {
            get { return _depthFPS; }
            set
            {
                if (value == _depthFPS)
                    return;

                ThrowIfBusy("depth fps");

                _updatingPipeline = true;
                StopPipeline();

                if (IsChannelActive(ChannelNames.ZImage))
                    DeactivateChannelImpl(ChannelNames.ZImage);

                if (IsChannelActive(ChannelNames.Left))
                    DeactivateChannelImpl(ChannelNames.Left);

                if (IsChannelActive(ChannelNames.Right))
                    DeactivateChannelImpl(ChannelNames.Right);

                _depthFPS = value;

                if (IsChannelActive(ChannelNames.ZImage))
                    ActivateChannelImpl(ChannelNames.ZImage);

                if (IsChannelActive(ChannelNames.Left))
                    ActivateChannelImpl(ChannelNames.Left);

                if (IsChannelActive(ChannelNames.Right))
                    ActivateChannelImpl(ChannelNames.Right);

                StartPipeline();
                _updatingPipeline = false;
            }
        }
        #endregion

        #region Firmware Version
        [Description("Firmware", "Version string of the firmware currently operating on the camera")]
        [AccessState(readableWhen: ConnectionStates.Connected)]
        public string Firmware
        {
            get
            {
                if (!this.IsConnected)
                    throw new Exception("Can't read firmware version unless the camera is connected");

                return RealSense2API.GetFirmwareVersion(_pipeline);
            }
        }
        #endregion

        #region Backlight Compensation
        /// <summary>
        /// Enable / disable color backlight compensation
        /// </summary>
        [Description("Backlight Compensation", "Enable / disable color backlight compensation")]
        [AccessState(readableWhen: ConnectionStates.Connected)]
        public bool BacklightCompensation
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.BACKLIGHT_COMPENSATION, "Backlight Compensation", RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.BACKLIGHT_COMPENSATION, "Backlight Compensation", RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION, value ? 1.0f : 0.0f);
            }
        }
        #endregion

        #region Brightness
        /// <summary>
        /// Color image brightness
        /// </summary>
        [Range(nameof(BrightnessRange))]
        [Description("Brightness (Color Sensor)", "Color image brightness")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public int Brightness
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.BRIGHTNESS, "Brightness", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.BRIGHTNESS, "Brightness", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(BrightnessRange, value, 0);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS, (float)value);
            }
        }

        public Range<int> BrightnessRange
        {
            get
            {
                Range<int> range = new Range<int>(0, 1);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.BRIGHTNESS, RealSense2API.SensorName.COLOR);
                    range = new Range<int>((int)option.min, (int)option.max);
                }

                return range;
            }
        }
        #endregion

        #region Contrast
        /// <summary>
        /// Color image contrast
        /// </summary>
        [Range(nameof(ContrastRange))]
        [Description("Contrast (Color Sensor)", "Color image contrast")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public int Contrast
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.CONTRAST, "Contrast", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.CONTRAST, "Contrast", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(ContrastRange, value, 0);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST, (float)value);
            }
        }

        public Range<int> ContrastRange
        {
            get
            {
                Range<int> range = new Range<int>(0, 1);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.CONTRAST, RealSense2API.SensorName.COLOR);
                    range = new Range<int>((int)option.min, (int)option.max);
                }

                return range;
            }
        }
        #endregion

        #region Exposure (Color)
        /// <summary>
        /// Controls exposure time of color camera. Setting any value will disable auto exposure
        /// </summary>
        [Description("Exposure (Color Sensor)", "Controls exposure time of color camera. Setting any value will disable auto exposure")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(ExposureColorRange))]
        public int ExposureColor
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, "ExposureColor", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, "ExposureColor", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(ExposureColorRange, value, 0);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE, (float)value);
            }
        }

        public Range<int> ExposureColorRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.EXPOSURE, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Auto Exposure (Color)
        /// <summary>
        /// Enable / disable color image auto-exposure
        /// </summary>
        [Description("Auto Exposure (Color Sensor)", "Enable / disable color image auto-exposure")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool AutoExposureColor
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, "AutoExposureColor", RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_EXPOSURE) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, "AutoExposureColor", RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_EXPOSURE, value ? 1.0f : 0.0f);
            }
        }
        #endregion

        #region Auto Exposure Priority (Color)
        /// <summary>
        /// Limit exposure time when auto-exposure is ON to preserve constant fps rate
        /// </summary>
        [Description("Auto Exposure Priority (Color Sensor)", "Limit exposure time when auto-exposure is ON to preserve constant fps rate")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool AutoExposurePriorityColor
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.AUTO_EXPOSURE_PRIORITY, "AutoExposurePriorityColor", RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.AUTO_EXPOSURE_PRIORITY) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.AUTO_EXPOSURE_PRIORITY, "AutoExposurePriorityColor", RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.AUTO_EXPOSURE_PRIORITY, value ? 1.0f : 0.0f);
            }
        }
        #endregion

        #region Exposure (Depth)
        /// <summary>
        /// Controls exposure time of depth camera. Setting any value will disable auto exposure
        /// </summary>
        [Description("Exposure (Depth Sensor)", "Controls exposure time of depth camera. Setting any value will disable auto exposure")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(ExposureDepthRange))]
        public int ExposureDepth
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, "ExposureDepth", RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, "ExposureDepth", RealSense2API.SensorName.STEREO);
                var option = QueryOption(RealSense2API.Option.EXPOSURE, RealSense2API.SensorName.STEREO);

                // step size for depth exposure is 20
                float adjusted_value = AdjustValue(option.min, option.max, value, option.step);
                ValidateRange<int>(ExposureDepthRange, value, (int)adjusted_value, true);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE, adjusted_value);
            }
        }

        public Range<int> ExposureDepthRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.EXPOSURE, RealSense2API.SensorName.STEREO);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Auto Exposure (Depth)
        /// <summary>
        /// Enable / disable depth image auto-exposure
        /// </summary>
        [Description("Auto Exposure (Depth Sensor)", "Enable / disable depth image auto-exposure")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool AutoExposureDepth
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, "AutoExposureDepth", RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ENABLE_AUTO_EXPOSURE) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, "AutoExposureDepth", RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ENABLE_AUTO_EXPOSURE, value ? 1.0f : 0.0f);
            }
        }
        #endregion

        #region Gain (Color)
        /// <summary>
        /// Color image gain
        /// </summary>
        [Description("Gain (Color Sensor)", "Color image gain")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(GainColorRange))]
        public int GainColor
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.GAIN, "GainColor", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.GAIN, "GainColor", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(GainColorRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN, (float)value);
            }
        }

        public Range<int> GainColorRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.GAIN, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Gain (Depth)
        /// <summary>
        /// Depth image gain
        /// </summary>
        [Description("Gain (Depth Sensor)", "Depth image gain")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(GainDepthRange))]
        public int GainDepth
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.GAIN, "GainDepth", RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.GAIN);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.GAIN, "GainDepth", RealSense2API.SensorName.STEREO);
                ValidateRange<int>(GainDepthRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.GAIN, (float)value);
            }
        }

        public Range<int> GainDepthRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.GAIN, RealSense2API.SensorName.STEREO);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Gamma
        /// <summary>
        /// Color image Gamma
        /// </summary>
        [Description("Gamma", "Color image gamma")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(GammaRange))]
        public int Gamma
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.GAMMA, "Gamma", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.GAMMA, "Gamma", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(GammaRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA, (float)value);
            }
        }

        public Range<int> GammaRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.GAMMA, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Hue
        /// <summary>
        /// Color image Hue
        /// </summary>
        [Description("Hue", "Color image hue")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(HueRange))]
        public int Hue
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.HUE, "Hue", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.HUE, "Hue", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(HueRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE, (float)value);
            }
        }

        public Range<int> HueRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.HUE, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Saturation
        /// <summary>
        /// Color image Saturation
        /// </summary>
        [Description("Saturation", "Color image Saturation")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(SaturationRange))]
        public int Saturation
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.SATURATION, "Saturation", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.SATURATION, "Saturation", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(SaturationRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION, (float)value);
            }
        }

        public Range<int> SaturationRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.SATURATION, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Sharpness
        /// <summary>
        /// Color image Sharpness
        /// </summary>
        [Description("Sharpness", "Color image Sharpness")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(SharpnessRange))]
        public int Sharpness
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.SHARPNESS, "Sharpness", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.SHARPNESS, "Sharpness", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(SharpnessRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS, (float)value);
            }
        }

        public Range<int> SharpnessRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.SHARPNESS, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region White Balance
        /// <summary>
        /// Controls white balance of color image. Setting any value will disable auto white balance
        /// </summary>
        [Description("White Balance", "Controls white balance of color image. Setting any value will disable auto white balance.")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(WhiteBalanceRange))]
        public int WhiteBalance
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.WHITE_BALANCE, "WhiteBalance", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.WHITE_BALANCE, "WhiteBalance", RealSense2API.SensorName.COLOR);
                var option = QueryOption(RealSense2API.Option.WHITE_BALANCE, RealSense2API.SensorName.COLOR);

                // step size for depth white balance is 10
                float adjusted_value = AdjustValue(option.min, option.max, value, option.step);
                ValidateRange<int>(WhiteBalanceRange, value, (int)adjusted_value, true);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE, adjusted_value);
            }
        }

        public Range<int> WhiteBalanceRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.WHITE_BALANCE, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Auto White Balance
        /// <summary>
        /// Enable / disable auto-white-balance
        /// </summary>
        [Description("Auto White Balance", "Enable / disable auto-white-balance")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool AutoWhiteBalance
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE, "AutoWhiteBalance", RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE, "AutoWhiteBalance", RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE, value ? 1.0f : 0.0f);
            }
        }
        #endregion

        #region Laser Power
        /// <summary>
        /// Manual laser power in mw. applicable only when laser power mode is set to Manual
        /// </summary>
        [Description("Laser Power", "Manual laser power in mw. applicable only when laser power mode is set to Manual")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Unit("mW")]
        [Range(nameof(LaserPowerRange))]
        public int LaserPower
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.LASER_POWER, "LaserPower", RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.LASER_POWER, "LaserPower", RealSense2API.SensorName.STEREO);
                var option = QueryOption(RealSense2API.Option.LASER_POWER, RealSense2API.SensorName.STEREO);

                // step size for depth laser power is 30
                float adjusted_value = AdjustValue(option.min, option.max, value, option.step);
                ValidateRange<int>(LaserPowerRange, value, (int)adjusted_value, true);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER, adjusted_value);
            }
        }

        public Range<int> LaserPowerRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.LASER_POWER, RealSense2API.SensorName.STEREO);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Laser Emitter Mode
        /// <summary>
        /// Power of the DS5 projector
        /// </summary>
        [Description("Laser Mode", "Power of the DS5 projector")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [AllowedValueList(typeof(EmitterMode))]
        public EmitterMode LaserMode
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.EMITTER_ENABLED, "EmmiterMode", RealSense2API.SensorName.STEREO);
                return (EmitterMode)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EMITTER_ENABLED);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.EMITTER_ENABLED, "EmmiterMode", RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EMITTER_ENABLED, (float)value);
            }
        }
        #endregion

        #region Frame Queue Size (Color)
        /// <summary>
        /// Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa
        /// </summary>
        [Description("Frame Queue Size (Color Sensor)",
            "Max number of frames you can hold at a given time.Increasing this number will reduce frame drops but increase latency, and vice versa")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(FrameQueueSizeColorRange))]
        public int FrameQueueSizeColor
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, "FrameQueueSizeColor", RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, "FrameQueueSizeColor", RealSense2API.SensorName.COLOR);
                ValidateRange<int>(FrameQueueSizeColorRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE, (float)value);
            }
        }

        public Range<int> FrameQueueSizeColorRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.FRAMES_QUEUE_SIZE, RealSense2API.SensorName.COLOR);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Frame Queue Size (Depth)
        /// <summary>
        /// Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa
        /// </summary>
        [Description("Frame Queue Size (DepthSensor)",
            "Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(FrameQueueSizeDepthRange))]
        public int FrameQueueSizeDepth
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, "FrameQueueSizeDepth", RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, "FrameQueueSizeDepth", RealSense2API.SensorName.STEREO);
                ValidateRange<int>(FrameQueueSizeDepthRange, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE, (float)value);
            }
        }

        public Range<int> FrameQueueSizeDepthRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.FRAMES_QUEUE_SIZE, RealSense2API.SensorName.STEREO);
                    res = new Range<int>((int)option.min, (int)option.max);
                }
                return res;
            }
        }
        #endregion

        #region Power Frequency Mode
        /// <summary>
        /// Power Line Frequency control for anti-flickering Off/50Hz/60Hz/Auto
        /// </summary>
        [Description("Power Frequency Mode", "Power Line Frequency control for anti-flickering")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [AllowedValueList(typeof(PowerLineMode))]
        public PowerLineMode PowerFrequencyMode
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.POWER_LINE_FREQUENCY, "PowerFrequencyMode", RealSense2API.SensorName.COLOR);
                return (PowerLineMode)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.POWER_LINE_FREQUENCY, "PowerFrequencyMode", RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY, (float)value);
            }
        }
        #endregion

        #region ASIC Temperature
        /// <summary>
        /// Current Asic Temperature
        /// </summary>
        [Description("Asic Temperature", "Current Asic Temperature")]
        [AccessState(readableWhen: ConnectionStates.Connected)]
        [Unit(Unit.DegreeCelsius)]
        public float ASICTemp
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.ASIC_TEMPERATURE, "ASICTemp", RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ASIC_TEMPERATURE);
            }
        }
        #endregion

        #region Error Polling
        /// <summary>
        /// Enable / disable polling of camera internal errors
        /// </summary>
        [Description("Error Polling", "Enable / disable polling of camera internal errors")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool EnableErrorPolling
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.ERROR_POLLING_ENABLED, "EnableErrorPolling", RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ERROR_POLLING_ENABLED) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ERROR_POLLING_ENABLED, "EnableErrorPolling", RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ERROR_POLLING_ENABLED, value ? 1.0f : 0.0f);
            }
        }
        #endregion

        #region Projector Temperature
        /// <summary>
        /// Current Projector Temperature in °C
        /// </summary>
        [Description("Projector Temperature", "Current Projector Temperature in °C")]
        [AccessState(readableWhen: ConnectionStates.Connected)]
        [Unit(Unit.DegreeCelsius)]
        public float ProjectorTemp
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.PROJECTOR_TEMPERATURE, "ProjectorTemp", RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.PROJECTOR_TEMPERATURE);
            }
        }
        #endregion

        #region Output Trigger
        /// <summary>
        /// Enable / disable trigger to be outputed from the camera to any external device on every depth frame
        /// </summary>
        [Description("Output Trigger", "Enable / disable trigger to be outputed from the camera to any external device on every depth frame")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool OutputTrigger
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.OUTPUT_TRIGGER_ENABLED, "OutputTrigger", RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.OUTPUT_TRIGGER_ENABLED) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.OUTPUT_TRIGGER_ENABLED, "OutputTrigger", RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.OUTPUT_TRIGGER_ENABLED, value ? 1.0f : 0.0f);
            }
        }
        #endregion

        #region Depth Units
        /// <summary>
        /// Number of meters represented by a single depth unit
        /// </summary>
        [Description("Depth Units", "Number of meters represented by a single depth unit")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Unit(Unit.Meter)]
        [Range(nameof(DepthUnitsRange))]
        public float DepthUnits
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.DEPTH_UNITS, "DepthUnits", RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.DEPTH_UNITS, "DepthUnits", RealSense2API.SensorName.STEREO);
                ValidateRange<float>(DepthUnitsRange, value, 0f);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS, value);
            }
        }

        public Range<float> DepthUnitsRange
        {
            get
            {
                Range<float> res = new Range<float>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.DEPTH_UNITS, RealSense2API.SensorName.STEREO);
                    res = new Range<float>(option.min, option.max);
                }
                return res;
            }
        }
        #endregion

        #endregion

        #region IDisposable implementation
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

            _config.Delete();
            _pipeline.Delete();
            _context.Delete();
            _disposed = true;
        }
        #endregion

        #region Constructor
        public RealSense2()
        {
            _context = RealSense2API.CreateContext();
            _pipeline = RealSense2API.CreatePipeline(_context);
            _config = RealSense2API.CreateConfig();

            RealSense2API.DisableAllStreams(_config);
        }

        ~RealSense2()
        {
            Dispose(false);
        }
        #endregion

        #region MetriCam2 Camera Interface Methods

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.Distance));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));

            Channels.Add(cr.RegisterCustomChannel(ChannelNames.Left, typeof(FloatCameraImage)));
            Channels.Add(cr.RegisterCustomChannel(ChannelNames.Right, typeof(FloatCameraImage)));
        }


        protected override void ConnectImpl()
        {
            bool haveSerial = !string.IsNullOrWhiteSpace(SerialNumber);

            if (haveSerial)
            {
                RealSense2API.EnableDevice(_config, SerialNumber);
            }

            if (ActiveChannels.Count == 0)
            {
                ActivateChannel(ChannelNames.Color);
                ActivateChannel(ChannelNames.ZImage);
            }

            StartPipeline();

            if(!haveSerial)
            {
                this.SerialNumber = RealSense2API.GetDeviceInfo(_pipeline, RealSense2API.CameraInfo.SERIAL_NUMBER);
            }

            _model = RealSense2API.GetDeviceInfo(_pipeline, RealSense2API.CameraInfo.NAME);


            RealSense2API.RS2Device dev = RealSense2API.GetActiveDevice(_pipeline);

            if (!RealSense2API.AdvancedModeEnabled(dev))
            {
                RealSense2API.EnabledAdvancedMode(dev, true);
            }

            _depthScale = RealSense2API.GetDepthScale(_pipeline);
        }

        private void StopPipeline()
        {
            if (_pipeline.Running)
                RealSense2API.PipelineStop(_pipeline);
            else
            {
                string msg = "RealSense2: Can't stop the pipeline since it is not running";
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }
                
        }

        private void StartPipeline()
        {
            if(!RealSense2API.CheckConfig(_pipeline, _config))
            {
                string msg = "RealSense2: No camera that supports the current configuration detected";
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            if (_pipeline.Running)
                RealSense2API.PipelineStop(_pipeline);

            RealSense2API.PipelineStart(_pipeline, _config);
        }

        protected override void DisconnectImpl()
        {
            try
            {
                _intrinsics.Clear();
                _extrinsics.Clear();
                StopPipeline();
            }
            catch(Exception e)
            {
                log.Warn(e.Message);
            }
        }

        protected override void UpdateImpl()
        {
            while(_updatingPipeline)
            {
                // wait for pipeline to restart with new settings
                Thread.Sleep(50);
            }

            if (!_pipeline.Running)
            {
                string msg = "RealSense2: Can't update camera since pipeline is not running";
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            RealSense2API.ReleaseFrame(_currentColorFrame);
            RealSense2API.ReleaseFrame(_currentDepthFrame);
            RealSense2API.ReleaseFrame(_currentLeftFrame);
            RealSense2API.ReleaseFrame(_currentRightFrame);

            bool getColor = IsChannelActive(ChannelNames.Color);
            bool getDepth = IsChannelActive(ChannelNames.ZImage);
            bool getLeft = IsChannelActive(ChannelNames.Left);
            bool getRight = IsChannelActive(ChannelNames.Right);
            bool haveColor = false;
            bool haveDepth = false;
            bool haveLeft = false;
            bool haveRight = false;

            while (true)
            {
                RealSense2API.RS2Frame data = RealSense2API.PipelineWaitForFrames(_pipeline, 5000);

                if (!data.IsValid() || data.Handle == IntPtr.Zero)
                {
                    RealSense2API.ReleaseFrame(data);
                    continue;
                }

                int frameCount = RealSense2API.FrameEmbeddedCount(data);
                log.Debug(string.Format("RealSense2: Got {0} Frames", frameCount));


                // extract all frames
                for (int j = 0; j < frameCount; j++)
                {
                    RealSense2API.RS2Frame frame = RealSense2API.FrameExtract(data, j);
                    RealSense2API.FrameAddRef(frame);

                    // what kind of frame did we get?
                    RealSense2API.RS2StreamProfile profile = RealSense2API.GetStreamProfile(frame);
                    RealSense2API.GetStreamProfileData(profile, out RealSense2API.Stream stream, out RealSense2API.Format format, out int index, out int uid, out int framerate);

                    log.Debug(string.Format("RealSense2: Analyzing frame {0}", j + 1));
                    log.Debug(string.Format("RealSense2: stream {0}", stream.ToString()));
                    log.Debug(string.Format("RealSense2: format {0}", format.ToString()));


                    switch (stream)
                    {
                        case RealSense2API.Stream.COLOR:
                            if (getColor)
                            {
                                RealSense2API.ReleaseFrame(_currentColorFrame);
                                _currentColorFrame = frame;
                                haveColor = true;
                            }
                            break;
                        case RealSense2API.Stream.DEPTH:
                            if (getDepth)
                            {
                                RealSense2API.ReleaseFrame(_currentDepthFrame);
                                _currentDepthFrame = frame;
                                haveDepth = true;
                            }
                            break;
                        case RealSense2API.Stream.INFRARED:
                            if (index == 1)
                            {
                                if (getLeft)
                                {
                                    RealSense2API.ReleaseFrame(_currentLeftFrame);
                                    _currentLeftFrame = frame;
                                    haveLeft = true;
                                }
                            }
                            else if (index == 2)
                            {
                                if (getRight)
                                {
                                    RealSense2API.ReleaseFrame(_currentRightFrame);
                                    _currentRightFrame = frame;
                                    haveRight = true;
                                }
                            }
                            break;
                    }
                }

                RealSense2API.ReleaseFrame(data);

                if (((getColor && haveColor) || !getColor)
                && ((getDepth && haveDepth) || !getDepth)
                && ((getLeft && haveLeft) || !getLeft)
                && ((getRight && haveRight) || !getRight))
                    break;
            }
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch(channelName)
            {
                case ChannelNames.Color:
                    return CalcColor();

                case ChannelNames.ZImage:
                    return CalcZImage();

                case ChannelNames.Distance:
                    return CalcDistanceImage();

                case ChannelNames.Left:
                    return CalcIRImage(_currentLeftFrame);

                case ChannelNames.Right:
                    return CalcIRImage(_currentRightFrame);
            }

            log.Error("Unexpected ChannelName in CalcChannel().");
            return null;
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            RealSense2API.Stream stream = RealSense2API.Stream.ANY;
            RealSense2API.Format format = RealSense2API.Format.ANY;
            int res_x = 640;
            int res_y = 480;
            int fps = 30;
            int index = -1;

            if (channelName == ChannelNames.Color)
            {
                stream = RealSense2API.Stream.COLOR;
                format = RealSense2API.Format.RGB8;

                res_x = ColorResolution.X;
                res_y = ColorResolution.Y;
                fps = ColorFPS;
                index = -1;
            }
            else if (channelName == ChannelNames.ZImage
                || channelName == ChannelNames.Distance)
            {
                // Distance and ZImage channel access the same data from
                // the realsense2 device
                // so check if one of them was already active
                // and skip activating the DEPTH stream in that case

                if (channelName == ChannelNames.ZImage
                    && IsChannelActive(ChannelNames.Distance))
                {
                    return;
                }

                if (channelName == ChannelNames.Distance
                    && IsChannelActive(ChannelNames.ZImage))
                {
                    return;
                }

                stream = RealSense2API.Stream.DEPTH;
                format = RealSense2API.Format.Z16;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = DepthFPS;
                index = -1;
            }
            else if (channelName == ChannelNames.Left)
            {
                stream = RealSense2API.Stream.INFRARED;
                format = RealSense2API.Format.Y8;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = 1;
            }
            else if (channelName == ChannelNames.Right)
            {
                stream = RealSense2API.Stream.INFRARED;
                format = RealSense2API.Format.Y8;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = DepthFPS;
                index = 2;
            }
            else
            {
                string msg = string.Format("RealSense2: Channel not supported {0}", channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            bool running = _pipeline.Running;

            if (running)
            {
                _updatingPipeline = true;
                StopPipeline();
            }
                

            RealSense2API.ConfigEnableStream(_config, stream, index, res_x, res_y, format, fps);


            if(running)
            {
                StartPipeline();
                _updatingPipeline = false;
            }
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            RealSense2API.Stream stream = RealSense2API.Stream.ANY;

            if (channelName == ChannelNames.Color)
            {
                stream = RealSense2API.Stream.COLOR;
            }
            else if (channelName == ChannelNames.ZImage
            || channelName == ChannelNames.Distance)
            {
                // Distance and ZImage channel access the same data from
                // the realsense2 device
                // so check if one of them is still active
                // and skip deactivating the DEPTH stream in that case

                if (channelName == ChannelNames.ZImage
                    && IsChannelActive(ChannelNames.Distance))
                {
                    return;
                }

                if (channelName == ChannelNames.Distance
                    && IsChannelActive(ChannelNames.ZImage))
                {
                    return;
                }

                stream = RealSense2API.Stream.DEPTH;
            }
            else if (channelName == ChannelNames.Left)
            {
                stream = RealSense2API.Stream.INFRARED;
            }
            else if (channelName == ChannelNames.Right)
            {
                stream = RealSense2API.Stream.INFRARED;
            }

            _currentColorFrame = new RealSense2API.RS2Frame(IntPtr.Zero);
            _currentDepthFrame = new RealSense2API.RS2Frame(IntPtr.Zero);
            _currentLeftFrame = new RealSense2API.RS2Frame(IntPtr.Zero);
            _currentRightFrame = new RealSense2API.RS2Frame(IntPtr.Zero);

            bool running = _pipeline.Running;
            

            if (running)
            {
                _updatingPipeline = true;
                StopPipeline();
            } 

            RealSense2API.ConfigDisableStream(_config, stream);

            if (running)
            {
                StartPipeline();
                _updatingPipeline = false;
            }
        }

        unsafe public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            // first check if intrinsics for requested channel have been cached already
            if(_intrinsics.TryGetValue(channelName, out ProjectiveTransformationZhang cachedIntrinsics))
            {
                return cachedIntrinsics;
            }

            RealSense2API.RS2StreamProfile profile = GetProfileFromSensor(channelName);
            if (!profile.IsValid())
            {
                // try to get profile from captured frame
                profile = GetProfileFromCapturedFrames(channelName);
            }

            RealSense2API.Intrinsics intrinsics = RealSense2API.GetIntrinsics(profile);

            if(intrinsics.model != RealSense2API.DistortionModel.BROWN_CONRADY)
            {
                string msg = string.Format("RealSense2: intrinsics distrotion model {0} does not match Metrilus.Util", intrinsics.model.ToString());
                log.Error(msg);
                throw new Exception(msg);
            }

            var projTrans = new ProjectiveTransformationZhang(
                intrinsics.width,
                intrinsics.height,
                intrinsics.fx,
                intrinsics.fy,
                intrinsics.ppx,
                intrinsics.ppy,
                intrinsics.coeffs[0],
                intrinsics.coeffs[1],
                intrinsics.coeffs[4],
                intrinsics.coeffs[2],
                intrinsics.coeffs[3]);

            _intrinsics.Add(channelName, projTrans);

            return projTrans;
        }

        private RealSense2API.RS2StreamProfile GetProfileFromSensor(string channelName)
        {
            string sensorName;
            Point2i refResolution;

            switch (channelName)
            {
                case ChannelNames.Color:
                    sensorName = RealSense2API.SensorName.COLOR;
                    refResolution = ColorResolution;
                    break;
                case ChannelNames.ZImage:
                case ChannelNames.Left:
                case ChannelNames.Right:
                case ChannelNames.Distance:
                default:
                    sensorName = RealSense2API.SensorName.STEREO;
                    refResolution = DepthResolution;
                    break;
            }

            RealSense2API.RS2Sensor sensor = RealSense2API.GetSensor(_pipeline, sensorName);
            RealSense2API.RS2StreamProfilesList list = RealSense2API.GetStreamProfileList(sensor);
            int count = RealSense2API.GetStreamProfileListCount(list);

            for (int i = 0; i < count; i++)
            {
                RealSense2API.RS2StreamProfile p = RealSense2API.GetStreamProfile(list, i);
                Point2i resolution = RealSense2API.GetStreamProfileResolution(p);
                if (resolution == refResolution)
                {
                    return p;
                }
            }

            return new RealSense2API.RS2StreamProfile(IntPtr.Zero);
        }

        private RealSense2API.RS2StreamProfile GetProfileFromCapturedFrames(string channelName)
        {
            RealSense2API.RS2Frame frame;

            switch (channelName)
            {
                case ChannelNames.Color:
                    frame = _currentColorFrame;
                    break;

                case ChannelNames.ZImage:
                    frame = _currentDepthFrame;
                    break;

                case ChannelNames.Left:
                    frame = _currentLeftFrame;
                    break;

                case ChannelNames.Right:
                    frame = _currentRightFrame;
                    break;

                default:
                    string msg = string.Format("RealSense2: stream profile for channel {0} not available", channelName);
                    log.Error(msg);
                    throw new ArgumentException(msg, nameof(channelName));
            }

            if (!frame.IsValid())
            {
                string msg = string.Format("RealSense2: Can't get channel profile for {0} without having at least one frame available.", channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            return RealSense2API.GetStreamProfile(frame);
        }

        unsafe public override RigidBodyTransformation GetExtrinsics(string channelFromName, string channelToName)
        {
            // first check if extrinsics for requested channel have been cached already
            string extrinsicsKey = $"{channelFromName}_{channelToName}";
            if (_extrinsics.TryGetValue(extrinsicsKey, out RigidBodyTransformation cachedExtrinsics))
            {
                return cachedExtrinsics;
            }

            RealSense2API.RS2StreamProfile from = GetProfileFromSensor(channelFromName);
            RealSense2API.RS2StreamProfile to = GetProfileFromSensor(channelToName);
            if (!from.IsValid())
                from = GetProfileFromCapturedFrames(channelFromName);
            if (!to.IsValid())
                to = GetProfileFromCapturedFrames(channelToName);


            RealSense2API.Extrinsics extrinsics = RealSense2API.GetExtrinsics(from, to);


            Point3f col1 = new Point3f(extrinsics.rotation[0], extrinsics.rotation[1], extrinsics.rotation[2]);
            Point3f col2 = new Point3f(extrinsics.rotation[3], extrinsics.rotation[4], extrinsics.rotation[5]);
            Point3f col3 = new Point3f(extrinsics.rotation[6], extrinsics.rotation[7], extrinsics.rotation[8]);
            RotationMatrix rot = new RotationMatrix(col1, col2, col3);

            Point3f trans = new Point3f(extrinsics.translation[0], extrinsics.translation[1], extrinsics.translation[2]);

            RigidBodyTransformation rbt = new RigidBodyTransformation(rot, trans);
            _extrinsics.Add(extrinsicsKey, rbt);

            return rbt;
        }

        unsafe private FloatCameraImage CalcZImage()
        {
            if (!_currentDepthFrame.IsValid())
            {
                log.Error("Depth frame is not valid...\n");
                return null;
            }

            int height = _currentDepthFrame.Height;
            int width = _currentDepthFrame.Width;

            FloatCameraImage depthData = new FloatCameraImage(width, height);
            short* source = (short*)RealSense2API.GetFrameData(_currentDepthFrame);

            for (int y = 0; y < height; y++)
            {
                short* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    depthData[y, x] = (float)(_depthScale * (*sourceLine++));
                }
            }

            return depthData;
        }

        private FloatCameraImage CalcDistanceImage()
        {
            FloatCameraImage zImage = CalcZImage();
            ProjectiveTransformationZhang projTrans = GetIntrinsics(ChannelNames.ZImage) as ProjectiveTransformationZhang;
            Point3fCameraImage p3fImage = projTrans.ZImageToWorld(zImage);
            return p3fImage.ToFloatCameraImage();
        }

        unsafe private FloatCameraImage CalcIRImage(RealSense2API.RS2Frame frame)
        {
            if (!frame.IsValid())
            {
                log.Error("IR frame is not valid...\n");
                return null;
            }

            int height = frame.Height;
            int width = frame.Width;

            FloatCameraImage IRData = new FloatCameraImage(width, height);
            byte* source = (byte*)RealSense2API.GetFrameData(frame);

            for (int y = 0; y < height; y++)
            {
                byte* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    IRData[y, x] = (float)(*sourceLine++);
                }
            }

            return IRData;
        }

        unsafe private ColorCameraImage CalcColor()
        {
            if (!_currentColorFrame.IsValid())
            {
                log.Error("Color frame is not valid...\n");
                return null;
            }

            int height = _currentColorFrame.Height;
            int width = _currentColorFrame.Width;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle imageRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte* source = (byte*) RealSense2API.GetFrameData(_currentColorFrame);
            byte* target = (byte*) (void*)bmpData.Scan0;
            for (int y = 0; y < height; y++)
            {
                byte* sourceLine = source + y * width * 3;
                for (int x = 0; x < width; x++)
                {
                    target[2] = *sourceLine++;
                    target[1] = *sourceLine++;
                    target[0] = *sourceLine++;
                    target += 3;
                }
            }

            bitmap.UnlockBits(bmpData);
            ColorCameraImage image = new ColorCameraImage(bitmap);

            return image;
        }

        #endregion

        #region Custom public methods
        public void LoadConfigPreset(AdvancedMode.Preset preset)
        {
            LoadCustomConfig(AdvancedMode.GetPreset(preset));
        }
        
        public void LoadCustomConfig(string json)
        {
            RealSense2API.RS2Device dev = RealSense2API.GetActiveDevice(_pipeline);
            RealSense2API.LoadAdvancedConfig(json, dev);
            _depthScale = RealSense2API.GetDepthScale(_pipeline);
        }
        #endregion

        #region private helper methods
        private void CheckOptionSupported(RealSense2API.Option option, string optionName, string sensorName)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException(string.Format("The property '{0}' can only be read or written when the camera is connected!", optionName));

            if (!RealSense2API.IsOptionSupported(_pipeline, sensorName, option))
                throw new NotSupportedException(string.Format("Option '{0}' is not supported by the {1} sensor of this camera.", optionName, sensorName));
        }

        private void CheckRangeValid<T>(RangeParamDesc<T> desc, T value, T adjustedValue, bool adjusted = false)
        {
            if (!desc.IsValid(value))
                if (adjusted)
                    throw new ArgumentOutOfRangeException(string.Format("Value {0} for '{1}' is outside of the range between {2} and {3}", value, desc.Name, desc.Min, desc.Max));
                else
                    throw new ArgumentOutOfRangeException(string.Format("Value {0} (adjusted to {1} to match stepsize) for '{2}' is outside of the range between {3} and {4}", value, adjustedValue, desc.Name, desc.Min, desc.Max));
        }

        private void ThrowIfBusy(string propertyName)
        {
            if (_updatingPipeline)
                throw new InvalidOperationException(string.Format("Can't set {0}. The pipeline is still in the process of updating a parameter.", propertyName));
        }

        private (float min, float max, float step, float def) QueryOption(RealSense2API.Option option, string sensorName)
        {
            RealSense2API.QueryOptionInfo(
                    _pipeline,
                    sensorName,
                    option,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

            return (min, max, step, def);
        }

        private float AdjustValue(float min, float max, float value, float step)
        {
            float adjusted_value = value;
            float rounding = (value - min) % step;
            adjusted_value -= rounding;

            if (rounding > step / 2)
                adjusted_value += step;

            if (adjusted_value > max)
                adjusted_value -= step;
            if (adjusted_value < min)
                adjusted_value += step;

            return adjusted_value;
        }

        private void ValidateRange<T>(Range<T> range, T value, T adjustedValue, bool adjusted = false) where T : IComparable, IConvertible
        {
            if(value.CompareTo(range.Minimum) < 0
            || value.CompareTo(range.Maximum) > 0)
            {
                if (adjusted)
                    throw new Exception(string.Format("Value {0} is outside of the range between {1} and {2}", value, range.Minimum, range.Maximum));
                else
                    throw new Exception(string.Format("Value {0} (adjusted to {1} to match stepsize) is outside of the range between {2} and {3}", value, adjustedValue, range.Minimum, range.Maximum));
            }
        }
        #endregion
    }
}
