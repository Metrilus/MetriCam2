// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Intel.RealSense;
using MetriCam2.Attributes;
using MetriCam2.Enums;
#if NETSTANDARD2_0
#else
using System.Drawing.Imaging;
#endif


namespace MetriCam2.Cameras
{
    public class RealSense2 : Camera, IDisposable
    {
        #region private members
        private Context _context;
        private Pipeline _pipeline;
        private Config _config;
        private VideoFrame _currentColorFrame;
        private VideoFrame _currentDepthFrame;
        private VideoFrame _currentLeftFrame;
        private VideoFrame _currentRightFrame;
        private long _currentFrameTimestamp = DateTime.UtcNow.Ticks;
        private bool _disposed = false;
        private Dictionary<string, ProjectiveTransformationZhang> _intrinsics = new Dictionary<string, ProjectiveTransformationZhang>();
        private Dictionary<string, RigidBodyTransformation> _extrinsics = new Dictionary<string, RigidBodyTransformation>();
        private bool _pipelineRunning = false;
        private float _depthScale = 1.0f;
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

        #region structs
        public struct SensorNames
        {
            public const string Color = "RGB Camera";
            public const string Stereo = "Stereo Module";
        }
        #endregion

        #region Device Information
        public override string Vendor { get => "Intel"; }

        public override string Name { get => Model; }

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.RealSense2Icon; }
#endif
        #endregion

        #region properties

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
                {
                    resolutions = GetSupportedResolutions(SensorNames.Color);
                }

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

                ExecuteWithStoppedPipeline(() =>
                {
                    DeactivateChannelImpl(ChannelNames.Color);
                    _colorResolution = value;
                    ActivateChannelImpl(ChannelNames.Color);
                });
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
                    framerates = GetSupportedFramerates(SensorNames.Color);
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

                ExecuteWithStoppedPipeline(() =>
                {
                    DeactivateChannelImpl(ChannelNames.Color);
                    _colorFPS = value;
                    ActivateChannelImpl(ChannelNames.Color);
                });
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
                {
                    resolutions = GetSupportedResolutions(SensorNames.Stereo);
                }

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

                ExecuteWithStoppedPipeline(() =>
                {
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
                });
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
                    framerates = GetSupportedFramerates(SensorNames.Stereo);
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

                ExecuteWithStoppedPipeline(() =>
                {
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
                });
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

                return GetDevice().Info[CameraInfo.FirmwareVersion];
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
                CheckOptionSupported(Option.BacklightCompensation, nameof(BacklightCompensation), SensorNames.Color);
                return GetOption(SensorNames.Color, Option.BacklightCompensation) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.BacklightCompensation, nameof(BacklightCompensation), SensorNames.Color);
                SetOption(SensorNames.Color, Option.BacklightCompensation, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(Option.Brightness, nameof(Brightness), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Brightness);
            }

            set
            {
                CheckOptionSupported(Option.Brightness, nameof(Brightness), SensorNames.Color);
                CheckRangeValid<int>(BrightnessRange, value, 0, nameof(Brightness));

                SetOption(SensorNames.Color, Option.Brightness, (float)value);
            }
        }

        public Range<int> BrightnessRange
        {
            get
            {
                Range<int> range = new Range<int>(0, 1);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Brightness, SensorNames.Color);
                    range = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.Contrast, nameof(Contrast), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Contrast);
            }

            set
            {
                CheckOptionSupported(Option.Contrast, nameof(Contrast), SensorNames.Color);
                CheckRangeValid<int>(ContrastRange, value, 0, nameof(Contrast));

                SetOption(SensorNames.Color, Option.Contrast, (float)value);
            }
        }

        public Range<int> ContrastRange
        {
            get
            {
                Range<int> range = new Range<int>(0, 1);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Contrast, SensorNames.Color);
                    range = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.Exposure, nameof(ExposureColor), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Exposure);
            }

            set
            {
                CheckOptionSupported(Option.Exposure, nameof(ExposureColor), SensorNames.Color);
                CheckRangeValid<int>(ExposureColorRange, value, 0, nameof(ExposureColor));

                SetOption(SensorNames.Color, Option.Exposure, (float)value);
            }
        }

        public Range<int> ExposureColorRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Exposure, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.EnableAutoExposure, nameof(AutoExposureColor), SensorNames.Color);
                return GetOption(SensorNames.Color, Option.EnableAutoExposure) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.EnableAutoExposure, nameof(AutoExposureColor), SensorNames.Color);
                SetOption(SensorNames.Color, Option.EnableAutoExposure, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(Option.AutoExposurePriority, nameof(AutoExposurePriorityColor), SensorNames.Color);
                return GetOption(SensorNames.Color, Option.AutoExposurePriority) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.AutoExposurePriority, nameof(AutoExposurePriorityColor), SensorNames.Color);
                SetOption(SensorNames.Color, Option.AutoExposurePriority, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(Option.Exposure, nameof(ExposureDepth), SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.Exposure);
            }

            set
            {
                CheckOptionSupported(Option.Exposure, nameof(ExposureDepth), SensorNames.Stereo);
                var option = QueryOption(Option.Exposure, SensorNames.Stereo);

                // step size for depth exposure is 20
                float adjusted_value = AdjustValue(option.Min, option.Max, value, option.Step);
                CheckRangeValid<int>(ExposureDepthRange, value, (int)adjusted_value, nameof(ExposureDepth), true);
                SetOption(SensorNames.Stereo, Option.Exposure, adjusted_value);
            }
        }

        public Range<int> ExposureDepthRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Exposure, SensorNames.Stereo);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.EnableAutoExposure, nameof(AutoExposureDepth), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.EnableAutoExposure) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.EnableAutoExposure, nameof(AutoExposureDepth), SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.EnableAutoExposure, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(Option.Gain, nameof(GainColor), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Gain);
            }

            set
            {
                CheckOptionSupported(Option.Gain, nameof(GainColor), SensorNames.Color);
                CheckRangeValid<int>(GainColorRange, value, 0, nameof(GainColor));
                SetOption(SensorNames.Color, Option.Gain, (float)value);
            }
        }

        public Range<int> GainColorRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Gain, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.Gain, nameof(GainDepth), SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.Gain);
            }

            set
            {
                CheckOptionSupported(Option.Gain, nameof(GainDepth), SensorNames.Stereo);
                CheckRangeValid<int>(GainDepthRange, value, 0, nameof(GainDepth));
                SetOption(SensorNames.Stereo, Option.Gain, (float)value);
            }
        }

        public Range<int> GainDepthRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Gain, SensorNames.Stereo);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.Gamma, nameof(Gamma), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Gamma);
            }

            set
            {
                CheckOptionSupported(Option.Gamma, nameof(Gamma), SensorNames.Color);
                CheckRangeValid<int>(GammaRange, value, 0, nameof(Gamma));
                SetOption(SensorNames.Color, Option.Gamma, (float)value);
            }
        }

        public Range<int> GammaRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Gamma, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.Hue, nameof(Hue), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Hue);
            }

            set
            {
                CheckOptionSupported(Option.Hue, nameof(Hue), SensorNames.Color);
                CheckRangeValid<int>(HueRange, value, 0, nameof(Hue));
                SetOption(SensorNames.Color, Option.Hue, (float)value);
            }
        }

        public Range<int> HueRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Hue, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.Saturation, nameof(Saturation), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Saturation);
            }

            set
            {
                CheckOptionSupported(Option.Saturation, nameof(Saturation), SensorNames.Color);
                CheckRangeValid<int>(SaturationRange, value, 0, nameof(Saturation));
                SetOption(SensorNames.Color, Option.Saturation, (float)value);
            }
        }

        public Range<int> SaturationRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Saturation, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.Sharpness, nameof(Sharpness), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Sharpness);
            }

            set
            {
                CheckOptionSupported(Option.Sharpness, nameof(Sharpness), SensorNames.Color);
                CheckRangeValid<int>(SharpnessRange, value, 0, nameof(Sharpness));
                SetOption(SensorNames.Color, Option.Sharpness, (float)value);
            }
        }

        public Range<int> SharpnessRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Sharpness, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.WhiteBalance, nameof(WhiteBalance), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.WhiteBalance);
            }

            set
            {
                CheckOptionSupported(Option.WhiteBalance, nameof(WhiteBalance), SensorNames.Color);
                var option = QueryOption(Option.WhiteBalance, SensorNames.Color);


                // step size for depth white balance is 10
                float adjusted_value = AdjustValue(option.Min, option.Max, value, option.Step);
                CheckRangeValid<int>(WhiteBalanceRange, value, (int)adjusted_value, nameof(WhiteBalance), true);

                SetOption(SensorNames.Color, Option.WhiteBalance, adjusted_value);
            }
        }

        public Range<int> WhiteBalanceRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.WhiteBalance, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.EnableAutoWhiteBalance, nameof(AutoWhiteBalance), SensorNames.Color);
                return GetOption(SensorNames.Color, Option.EnableAutoWhiteBalance) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.EnableAutoWhiteBalance, nameof(AutoWhiteBalance), SensorNames.Color);
                SetOption(SensorNames.Color, Option.EnableAutoWhiteBalance, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(Option.LaserPower, nameof(LaserPower), SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.LaserPower);
            }

            set
            {
                CheckOptionSupported(Option.LaserPower, nameof(LaserPower), SensorNames.Stereo);
                var option = QueryOption(Option.LaserPower, SensorNames.Stereo);

                // step size for depth laser power is 30
                float adjusted_value = AdjustValue(option.Min, option.Max, value, option.Step);
                CheckRangeValid<int>(LaserPowerRange, value, (int)adjusted_value, nameof(LaserPower), true);
                SetOption(SensorNames.Stereo, Option.LaserPower, adjusted_value);
            }
        }

        public Range<int> LaserPowerRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.LaserPower, SensorNames.Stereo);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.EmitterEnabled, nameof(LaserMode), SensorNames.Stereo);
                return (EmitterMode)GetOption(SensorNames.Stereo, Option.EmitterEnabled);
            }

            set
            {
                CheckOptionSupported(Option.EmitterEnabled, nameof(LaserMode), SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.EmitterEnabled, (float)value);
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
                CheckOptionSupported(Option.FramesQueueSize, nameof(FrameQueueSizeColor), SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.FramesQueueSize);
            }

            set
            {
                CheckOptionSupported(Option.FramesQueueSize, nameof(FrameQueueSizeColor), SensorNames.Color);
                CheckRangeValid<int>(FrameQueueSizeColorRange, value, 0, nameof(FrameQueueSizeColor));
                SetOption(SensorNames.Color, Option.FramesQueueSize, (float)value);
            }
        }

        public Range<int> FrameQueueSizeColorRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.FramesQueueSize, SensorNames.Color);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.FramesQueueSize, nameof(FrameQueueSizeDepth), SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.FramesQueueSize);
            }

            set
            {
                CheckOptionSupported(Option.FramesQueueSize, nameof(FrameQueueSizeDepth), SensorNames.Stereo);
                CheckRangeValid<int>(FrameQueueSizeDepthRange, value, 0, nameof(FrameQueueSizeDepth));
                SetOption(SensorNames.Stereo, Option.FramesQueueSize, (float)value);
            }
        }

        public Range<int> FrameQueueSizeDepthRange
        {
            get
            {
                Range<int> res = new Range<int>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.FramesQueueSize, SensorNames.Stereo);
                    res = new Range<int>((int)option.Min, (int)option.Max);
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
                CheckOptionSupported(Option.PowerLineFrequency, nameof(PowerFrequencyMode), SensorNames.Color);
                return (PowerLineMode)GetOption(SensorNames.Color, Option.PowerLineFrequency);
            }

            set
            {
                CheckOptionSupported(Option.PowerLineFrequency, nameof(PowerFrequencyMode), SensorNames.Color);
                SetOption(SensorNames.Color, Option.PowerLineFrequency, (float)value);
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
                CheckOptionSupported(Option.AsicTemperature, nameof(ASICTemp), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.AsicTemperature);
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
                CheckOptionSupported(Option.ErrorPollingEnabled, nameof(EnableErrorPolling), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.ErrorPollingEnabled) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.ErrorPollingEnabled, nameof(EnableErrorPolling), SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.ErrorPollingEnabled, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(Option.ProjectorTemperature, nameof(ProjectorTemp), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.ProjectorTemperature);
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
                CheckOptionSupported(Option.OutputTriggerEnabled, nameof(OutputTrigger), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.OutputTriggerEnabled) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.OutputTriggerEnabled, nameof(OutputTrigger), SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.OutputTriggerEnabled, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(Option.DepthUnits, nameof(DepthUnits), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.DepthUnits);
            }

            set
            {
                CheckOptionSupported(Option.DepthUnits, nameof(DepthUnits), SensorNames.Stereo);
                CheckRangeValid<float>(DepthUnitsRange, value, 0f, nameof(DepthUnits));
                SetOption(SensorNames.Stereo, Option.DepthUnits, value);
            }
        }

        public Range<float> DepthUnitsRange
        {
            get
            {
                Range<float> res = new Range<float>(0, 0);

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.DepthUnits, SensorNames.Stereo);
                    res = new Range<float>(option.Min, option.Max);
                }
                return res;
            }
        }
        #endregion

        #endregion

        #region IDisposable
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
                _config.Dispose();
                _pipeline.Dispose();
                _context.Dispose();
            }

            _disposed = true;
        }
        #endregion

        #region Constructor
        public RealSense2()
            : base(modelName: "RealSense2")
        {
            _context = new Context();
            _pipeline = new Pipeline(_context);
            _config = new Config();

            _config.DisableAllStreams();
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
                _config.EnableDevice(SerialNumber);

            if (ActiveChannels.Count == 0)
            {
                ActivateChannel(ChannelNames.Color);
                ActivateChannel(ChannelNames.ZImage);
            }

            StartPipeline();
            Device dev = GetDevice();
            Model = dev.Info[CameraInfo.Name];

            if (!haveSerial)
                this.SerialNumber = dev.Info[CameraInfo.SerialNumber];

            AdvancedDevice adev = AdvancedDevice.FromDevice(dev);

            if (!adev.AdvancedModeEnabled)
                adev.AdvancedModeEnabled = true;

            _depthScale = GetDepthScale();
        }

        private void StopPipeline()
        {
            if (_pipelineRunning)
            {
                _pipelineRunning = false;
                _pipeline.Stop();
            }
        }

        private void StartPipeline()
        {

            if (!_config.CanResolve(_pipeline))
            {
                string msg = "RealSense2: No camera that supports the current configuration detected";
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            if (!_pipelineRunning)
            {
                _pipeline.Start(_config);
                _pipelineRunning = true;
            }
        }

        protected override void DisconnectImpl()
        {
            try
            {
                _intrinsics.Clear();
                _extrinsics.Clear();
                StopPipeline();
            }
            catch (Exception e)
            {
                log.Warn(e.Message);
            }
        }

        protected override void UpdateImpl()
        {
            bool getColor = IsChannelActive(ChannelNames.Color);
            bool getDepth = IsChannelActive(ChannelNames.ZImage);
            bool getLeft = IsChannelActive(ChannelNames.Left);
            bool getRight = IsChannelActive(ChannelNames.Right);
            bool haveColor = false;
            bool haveDepth = false;
            bool haveLeft = false;
            bool haveRight = false;

            DisposeCurrentFrames();

            // check if there is a undelivered frameset available for grabs
            if (_pipeline.PollForFrames(out FrameSet data))
            {
                ExtractFrameSetData(data, getColor, getDepth, getLeft, getRight, ref haveColor, ref haveDepth, ref haveLeft, ref haveRight);
                data.Dispose();
                CheckFrameSetComplete(getColor, getDepth, getLeft, getRight, haveColor, haveDepth, haveLeft, haveRight);
                return;
            }

            // otherwise wait until a new frameset is available
            data = _pipeline.WaitForFrames(5000);
            ExtractFrameSetData(data, getColor, getDepth, getLeft, getRight, ref haveColor, ref haveDepth, ref haveLeft, ref haveRight);
            data.Dispose();

            CheckFrameSetComplete(getColor, getDepth, getLeft, getRight, haveColor, haveDepth, haveLeft, haveRight);
        }

        private void DisposeCurrentFrames()
        {
            if (null != _currentColorFrame)
                _currentColorFrame.Dispose();
            if (null != _currentDepthFrame)
                _currentDepthFrame.Dispose();
            if (null != _currentLeftFrame)
                _currentLeftFrame.Dispose();
            if (null != _currentRightFrame)
                _currentRightFrame.Dispose();
        }

        private void CheckFrameSetComplete(bool getColor, bool getDepth, bool getLeft, bool getRight,
            bool haveColor, bool haveDepth, bool haveLeft, bool haveRight)
        {
            if (((getColor && haveColor) || !getColor)
            && ((getDepth && haveDepth) || !getDepth)
            && ((getLeft && haveLeft) || !getLeft)
            && ((getRight && haveRight) || !getRight))
                return;

            throw new Exception("RealSense2: not all requested frames are part of the retrieved FrameSet");
        }

        private void ExtractFrameSetData(FrameSet data, bool getColor, bool getDepth, bool getLeft, bool getRight,
            ref bool haveColor, ref bool haveDepth, ref bool haveLeft, ref bool haveRight)
        {
            _currentFrameTimestamp = DateTime.UtcNow.Ticks;

            // extract all frames
            foreach (VideoFrame vframe in data)
            {
                // what kind of frame did we get?
                StreamProfile profile = vframe.Profile;
                Stream stream = profile.Stream;


                switch (stream)
                {
                    case Stream.Color:
                        if (getColor)
                        {
                            _currentColorFrame = vframe;
                            haveColor = true;
                        }
                        break;
                    case Stream.Depth:
                        if (getDepth)
                        {
                            _currentDepthFrame = vframe;
                            haveDepth = true;
                        }
                        break;
                    case Stream.Infrared:
                        int index = profile.Index;
                        if (index == 1)
                        {
                            if (getLeft)
                            {
                                _currentLeftFrame = vframe;
                                haveLeft = true;
                            }
                        }
                        else if (index == 2)
                        {
                            if (getRight)
                            {
                                _currentRightFrame = vframe;
                                haveRight = true;
                            }
                        }
                        break;
                }
            }
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
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
            Stream stream = Stream.Any;
            Format format = Format.Any;
            int res_x = 640;
            int res_y = 480;
            int fps = 30;
            int index = -1;

            if (channelName == ChannelNames.Color)
            {
                stream = Stream.Color;
                format = Format.Rgb8;

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

                stream = Stream.Depth;
                format = Format.Z16;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = DepthFPS;
                index = -1;
            }
            else if (channelName == ChannelNames.Left)
            {
                stream = Stream.Infrared;
                format = Format.Y8;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = 1;
            }
            else if (channelName == ChannelNames.Right)
            {
                stream = Stream.Infrared;
                format = Format.Y8;

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


            Action enableStream = () => { _config.EnableStream(stream, index, res_x, res_y, format, fps); };

            if (_pipelineRunning)
                ExecuteWithStoppedPipeline(enableStream);
            else
                enableStream();

        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            Stream stream = Stream.Any;
            int index = -1;

            if (channelName == ChannelNames.Color)
            {
                stream = Stream.Color;
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

                stream = Stream.Depth;
            }
            else if (channelName == ChannelNames.Left)
            {
                stream = Stream.Infrared;
                index = 1;
            }
            else if (channelName == ChannelNames.Right)
            {
                stream = Stream.Infrared;
                index = 2;
            }


            Action disableStream = () => { _config.DisableStream(stream, index); };

            if (_pipelineRunning)
                ExecuteWithStoppedPipeline(disableStream);
            else
                disableStream();
        }

        unsafe public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            // first check if intrinsics for requested channel have been cached already
            if (_intrinsics.TryGetValue(channelName, out ProjectiveTransformationZhang cachedIntrinsics))
            {
                return cachedIntrinsics;
            }

            VideoStreamProfile profile = GetProfileFromSensor(channelName) as VideoStreamProfile;
            Intrinsics intrinsics = profile.GetIntrinsics();

            if (intrinsics.model != Distortion.BrownConrady)
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

        private StreamProfile GetProfileFromSensor(string channelName)
        {
            string sensorName;
            Point2i refResolution;

            switch (channelName)
            {
                case ChannelNames.Color:
                    sensorName = SensorNames.Color;
                    refResolution = ColorResolution;
                    break;
                case ChannelNames.ZImage:
                case ChannelNames.Left:
                case ChannelNames.Right:
                case ChannelNames.Distance:
                default:
                    sensorName = SensorNames.Stereo;
                    refResolution = DepthResolution;
                    break;
            }

            StreamProfileList list = GetSensor(sensorName).StreamProfiles;

            foreach (VideoStreamProfile profile in list)
            {
                Point2i res = new Point2i(profile.Width, profile.Height);
                if (res == refResolution)
                    return profile;
            }

            throw new ArgumentException(string.Format("StreamProfile for channel '{0}' with resolution {1}x{2} notavailable", channelName, refResolution.X, refResolution.Y));
        }

        private StreamProfile GetProfileFromCapturedFrames(string channelName)
        {
            Frame frame;

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

            return frame.Profile;
        }

        unsafe public override RigidBodyTransformation GetExtrinsics(string channelFromName, string channelToName)
        {
            // first check if extrinsics for requested channel have been cached already
            string extrinsicsKey = $"{channelFromName}_{channelToName}";
            if (_extrinsics.TryGetValue(extrinsicsKey, out RigidBodyTransformation cachedExtrinsics))
            {
                return cachedExtrinsics;
            }

            StreamProfile from = GetProfileFromSensor(channelFromName);
            StreamProfile to = GetProfileFromSensor(channelToName);


            Extrinsics extrinsics = from.GetExtrinsicsTo(to);


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
            int height = _currentDepthFrame.Height;
            int width = _currentDepthFrame.Width;

            FloatCameraImage depthData = new FloatCameraImage(width, height);
            depthData.TimeStamp = _currentFrameTimestamp;
            short* source = (short*)_currentDepthFrame.Data;

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

        unsafe private FloatCameraImage CalcIRImage(VideoFrame frame)
        {
            int height = frame.Height;
            int width = frame.Width;

            FloatCameraImage IRData = new FloatCameraImage(width, height);
            IRData.TimeStamp = _currentFrameTimestamp;
            byte* source = (byte*)frame.Data;

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
            int height = _currentColorFrame.Height;
            int width = _currentColorFrame.Width;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle imageRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte* source = (byte*)_currentColorFrame.Data;
            byte* target = (byte*)(void*)bmpData.Scan0;
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
            image.TimeStamp = _currentFrameTimestamp;

            return image;
        }

        #endregion

        #region advanced config
        public void LoadConfigPreset(AdvancedMode.Preset preset)
        {
            LoadCustomConfig(AdvancedMode.GetPreset(preset));
        }

        public void LoadCustomConfig(string json)
        {
            AdvancedDevice adev = AdvancedDevice.FromDevice(GetDevice());
            adev.JsonConfiguration = json;
            _depthScale = GetDepthScale();
        }
        #endregion

        #region private helpers
        private void ExecuteWithStoppedPipeline(Action doStuff)
        {
            bool running = _pipelineRunning;

            StopPipeline();

            doStuff();

            if (running)
                StartPipeline();
        }

        private void CheckOptionSupported(Option option, string optionName, string sensorName)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException(string.Format("The property '{0}' can only be read or written when the camera is connected!", optionName));


            if (!GetSensor(sensorName).Options[option].Supported)
                throw new NotSupportedException(string.Format("Option '{0}' is not supported by the {1} sensor of this camera.", optionName, sensorName));
        }

        private float GetOption(string sensorName, Option option)
        {
            return GetSensor(sensorName).Options[option].Value;
        }

        private void SetOption(string sensorName, Option option, float value)
        {
            GetSensor(sensorName).Options[option].Value = value;
        }

        private void CheckRangeValid<T>(Range<T> range, T value, T adjustedValue, string optionName, bool adjusted = false) where T : IComparable, IConvertible
        {
            if (!range.IsValid(value))
                if (adjusted)
                    throw new ArgumentOutOfRangeException(
                        string.Format(
                            "Value {0} for '{1}' is outside of the range between {2} and {3}",
                            value, optionName, range.Minimum, range.Maximum
                        )
                    );
                else
                    throw new ArgumentOutOfRangeException(
                        string.Format(
                            "Value {0} (adjusted to {1} to match stepsize) for '{2}' is outside of the range between {3} and {4}",
                            value, adjustedValue, optionName, range.Minimum, range.Maximum
                        )
                   );
        }

        private Sensor.CameraOption QueryOption(Option option, string sensorName)
        {
            Sensor.CameraOption cop = GetSensor(sensorName).Options[option];
            return cop;
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

        private Device GetDevice()
        {
            Device device = _context.Devices[0];

            if (string.IsNullOrEmpty(this.SerialNumber))
                return device;

            foreach (Device dev in _context.Devices)
            {
                if (dev.Info[CameraInfo.SerialNumber] == this.SerialNumber)
                {
                    return dev;
                }
            }

            throw new ArgumentException(string.Format("Device with S/N {0} could not be found", this.SerialNumber));
        }

        private Sensor GetSensor(string sensorName)
        {
            Device dev = GetDevice();

            foreach (Sensor sen in dev.Sensors)
            {
                if (sen.Info[CameraInfo.Name] == sensorName)
                {
                    return sen;
                }
            }

            throw new ArgumentException(string.Format("Sensor with name '{0}' could not be found", sensorName));
        }

        private List<Point2i> GetSupportedResolutions(string sensorName)
        {
            return GetSensor(sensorName).VideoStreamProfiles.Select(p => new Point2i(p.Width, p.Height)).ToList();
        }

        private List<int> GetSupportedFramerates(string sensorName)
        {
            return GetSensor(sensorName).StreamProfiles.Select(p => p.Framerate).ToList();
        }

        private float GetDepthScale()
        {
            Sensor depthSensor = GetSensor(SensorNames.Stereo);
            depthSensor.Open();
            float depthScale = depthSensor.DepthScale;
            depthSensor.Close();

            return depthScale;
        }
        #endregion
    }
}
