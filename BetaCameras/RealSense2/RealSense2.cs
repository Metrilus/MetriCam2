// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Intel.RealSense;
#if NETSTANDARD2_0
#else
using System.Drawing.Imaging;
#endif


namespace MetriCam2.Cameras
{
    public class RealSense2 : Camera, IDisposable
    {
        private Context _context;
        private Pipeline _pipeline;
        private Config _config;
        private Device _dev;
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

        public struct SensorNames
        {
            public const string Color = "RGB Camera";
            public const string Stereo = "Stereo Module";
        }

        private Point2i _colorResolution = new Point2i(640, 480);
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

        ListParamDesc<Point2i> ColorResolutionDesc
        {
            get
            {
                List<Point2i> resolutions = new List<Point2i>();
                resolutions.Add(new Point2i(640, 480));

                if (this.IsConnected)
                {
                    resolutions = GetSupportedResolutions(SensorNames.Color);
                }

                List<string> allowedValues = new List<string>();
                foreach (Point2i resolution in resolutions)
                {
                    allowedValues.Add(string.Format("{0}x{1}", resolution.X, resolution.Y));
                }

                ListParamDesc<Point2i> res = new ListParamDesc<Point2i>(allowedValues);
                res.Unit = "px";
                res.Description = "Resolution of the color sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        private int _colorFPS = 30;
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

        ListParamDesc<int> ColorFPSDesc
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
                
                ListParamDesc<int> res = new ListParamDesc<int>(framerates);
                res.Unit = "fps";
                res.Description = "Frames per Second of the color sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        private Point2i _depthResolution = new Point2i(640, 480);
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

        ListParamDesc<Point2i> DepthResolutionDesc
        {
            get
            {
                List<Point2i> resolutions = new List<Point2i>();
                resolutions.Add(new Point2i(640, 480));

                if(this.IsConnected)
                {
                    resolutions = GetSupportedResolutions(SensorNames.Stereo);
                }

                List<string> allowedValues = new List<string>();
                foreach(Point2i resolution in resolutions)
                {
                    allowedValues.Add(string.Format("{0}x{1}", resolution.X, resolution.Y));
                }

                ListParamDesc<Point2i> res = new ListParamDesc<Point2i>(allowedValues);
                res.Unit = "px";
                res.Description = "Resolution of the depth sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        private int _depthFPS = 30;
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

        ListParamDesc<int> DepthFPSDesc
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

                ListParamDesc<int> res = new ListParamDesc<int>(framerates);
                res.Unit = "fps";
                res.Description = "Frames per Second of the depth sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        
        public string Firmware
        {
            get
            {
                return _dev.Info[CameraInfo.FirmwareVersion];
            }
        }

        ParamDesc<string> FirmwareDesc
        {
            get
            {
                ParamDesc<string> res = new ParamDesc<string>();
                res.Description = "Current Firmware version of the connected camera.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        public override string Vendor { get => "Intel"; }

#if !NETSTANDARD2_0
        public override Icon CameraIcon { get => Properties.Resources.RealSense2Icon; }
#endif

        #region RealSense Options

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

        /// <summary>
        /// Enable / disable color backlight compensation
        /// </summary>
        public bool BacklightCompensation
        {
            get
            {
                CheckOptionSupported(Option.BacklightCompensation, BacklightCompensationDesc.Name, SensorNames.Color);
                return GetOption(SensorNames.Color, Option.BacklightCompensation) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.BacklightCompensation, BacklightCompensationDesc.Name, SensorNames.Color);
                SetOption(SensorNames.Color, Option.BacklightCompensation, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> BacklightCompensationDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "Enable / disable color backlight compensation";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Color image brightness
        /// </summary>
        public int Brightness
        {
            get
            {
                CheckOptionSupported(Option.Brightness, BrightnessDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Brightness);
            }

            set
            {
                CheckOptionSupported(Option.Brightness, BrightnessDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(BrightnessDesc, value, 0);

                SetOption(SensorNames.Color, Option.Brightness, (float)value);
            }
        }

        RangeParamDesc<int> BrightnessDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.Brightness, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Color image brightness";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Color image contrast
        /// </summary>
        public int Contrast
        {
            get
            {
                CheckOptionSupported(Option.Contrast, ContrastDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Contrast);
            }

            set
            {
                CheckOptionSupported(Option.Contrast, ContrastDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(ContrastDesc, value, 0);

                SetOption(SensorNames.Color, Option.Contrast, (float)value);
            }
        }

        RangeParamDesc<int> ContrastDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Contrast, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Color image contrast";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Controls exposure time of color camera. Setting any value will disable auto exposure
        /// </summary>
        public int ExposureColor
        {
            get
            {
                CheckOptionSupported(Option.Exposure, ExposureColorDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Exposure);
            }

            set
            {
                CheckOptionSupported(Option.Exposure, ExposureColorDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(ExposureColorDesc, value, 0);

                SetOption(SensorNames.Color, Option.Exposure, (float)value);
            }
        }

        RangeParamDesc<int> ExposureColorDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Exposure, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Controls exposure time of color camera. Setting any value will disable auto exposure";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Enable / disable color image auto-exposure
        /// </summary>
        public bool AutoExposureColor
        {
            get
            {
                CheckOptionSupported(Option.EnableAutoExposure, AutoExposureColorDesc.Name, SensorNames.Color);
                return GetOption(SensorNames.Color, Option.EnableAutoExposure) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.EnableAutoExposure, AutoExposureColorDesc.Name, SensorNames.Color);
                SetOption(SensorNames.Color, Option.EnableAutoExposure, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> AutoExposureColorDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "Enable / disable color image auto-exposure";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Limit exposure time when auto-exposure is ON to preserve constant fps rate
        /// </summary>
        public bool AutoExposurePriorityColor
        {
            get
            {
                CheckOptionSupported(Option.AutoExposurePriority, AutoExposurePriorityColorDesc.Name, SensorNames.Color);
                return GetOption(SensorNames.Color, Option.AutoExposurePriority) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.AutoExposurePriority, AutoExposurePriorityColorDesc.Name, SensorNames.Color);
                SetOption(SensorNames.Color, Option.AutoExposurePriority, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> AutoExposurePriorityColorDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "Limit exposure time when auto-exposure is ON to preserve constant fps rate";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Controls exposure time of depth camera. Setting any value will disable auto exposure
        /// </summary>
        public int ExposureDepth
        {
            get
            {
                CheckOptionSupported(Option.Exposure, ExposureDepthDesc.Name, SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.Exposure);
            }

            set
            {
                CheckOptionSupported(Option.Exposure, ExposureDepthDesc.Name, SensorNames.Stereo);
                var option = QueryOption(Option.Exposure, SensorNames.Stereo);

                // step size for depth exposure is 20
                float adjusted_value = AdjustValue(option.Min, option.Max, value, option.Step);
                CheckRangeValid<int>(ExposureDepthDesc, value, (int)adjusted_value, true);
                SetOption(SensorNames.Stereo, Option.Exposure, adjusted_value);
            }
        }

        RangeParamDesc<int> ExposureDepthDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Exposure, SensorNames.Stereo);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Controls exposure time of depth camera. Setting any value will disable auto exposure";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Enable / disable depth image auto-exposure
        /// </summary>
        public bool AutoExposureDepth
        {
            get
            {
                CheckOptionSupported(Option.EnableAutoExposure, AutoExposureDepthDesc.Name, SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.EnableAutoExposure) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.EnableAutoExposure, AutoExposureDepthDesc.Name, SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.EnableAutoExposure, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> AutoExposureDepthDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "Enable / disable depth image auto-exposure";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Color image gain
        /// </summary>
        public int GainColor
        {
            get
            {
                CheckOptionSupported(Option.Gain, GainColorDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Gain);
            }

            set
            {
                CheckOptionSupported(Option.Gain, GainColorDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(GainColorDesc, value, 0);
                SetOption(SensorNames.Color, Option.Gain, (float)value);
            }
        }

        RangeParamDesc<int> GainColorDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Gain, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Color image gain";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Depth image gain
        /// </summary>
        public int GainDepth
        {
            get
            {
                CheckOptionSupported(Option.Gain, GainDepthDesc.Name, SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.Gain);
            }

            set
            {
                CheckOptionSupported(Option.Gain, GainDepthDesc.Name, SensorNames.Stereo);
                CheckRangeValid<int>(GainDepthDesc, value, 0);
                SetOption(SensorNames.Stereo, Option.Gain, (float)value);
            }
        }

        RangeParamDesc<int> GainDepthDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Gain, SensorNames.Stereo);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Depth image gain";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Color image Gamma
        /// </summary>
        public int Gamma
        {
            get
            {
                CheckOptionSupported(Option.Gamma, GammaDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Gamma);
            }

            set
            {
                CheckOptionSupported(Option.Gamma, GammaDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(GammaDesc, value, 0);
                SetOption(SensorNames.Color, Option.Gamma, (float)value);
            }
        }

        RangeParamDesc<int> GammaDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Gamma, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Color image Gamma";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Color image Hue
        /// </summary>
        public int Hue
        {
            get
            {
                CheckOptionSupported(Option.Hue, HueDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Hue);
            }

            set
            {
                CheckOptionSupported(Option.Hue, HueDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(HueDesc, value, 0);
                SetOption(SensorNames.Color, Option.Hue, (float)value);
            }
        }

        RangeParamDesc<int> HueDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Hue, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Color image Hue";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Color image Saturation
        /// </summary>
        public int Saturation
        {
            get
            {
                CheckOptionSupported(Option.Saturation, SaturationDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Saturation);
            }

            set
            {
                CheckOptionSupported(Option.Saturation, SaturationDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(SaturationDesc, value, 0);
                SetOption(SensorNames.Color, Option.Saturation, (float)value);
            }
        }

        RangeParamDesc<int> SaturationDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Saturation, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Color image Saturation";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Color image Sharpness
        /// </summary>
        public int Sharpness
        {
            get
            {
                CheckOptionSupported(Option.Sharpness, SharpnessDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.Sharpness);
            }

            set
            {
                CheckOptionSupported(Option.Sharpness, SharpnessDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(SharpnessDesc, value, 0);
                SetOption(SensorNames.Color, Option.Sharpness, (float)value);
            }
        }

        RangeParamDesc<int> SharpnessDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.Sharpness, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Color image Sharpness";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Controls white balance of color image.Setting any value will disable auto white balance
        /// </summary>
        public int WhiteBalance
        {
            get
            {
                CheckOptionSupported(Option.WhiteBalance, WhiteBalanceDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.WhiteBalance);
            }

            set
            {
                CheckOptionSupported(Option.WhiteBalance, WhiteBalanceDesc.Name, SensorNames.Color);
                var option = QueryOption(Option.WhiteBalance, SensorNames.Color);


                // step size for depth white balance is 10
                float adjusted_value = AdjustValue(option.Min, option.Max, value, option.Step);
                CheckRangeValid<int>(WhiteBalanceDesc, value, (int)adjusted_value, true);

                SetOption(SensorNames.Color, Option.WhiteBalance, adjusted_value);
            }
        }

        RangeParamDesc<int> WhiteBalanceDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.WhiteBalance, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Controls white balance of color image.Setting any value will disable auto white balance";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Enable / disable auto-white-balance
        /// </summary>
        public bool AutoWhiteBalance
        {
            get
            {
                CheckOptionSupported(Option.EnableAutoWhiteBalance, AutoWhiteBalanceDesc.Name, SensorNames.Color);
                return GetOption(SensorNames.Color, Option.EnableAutoWhiteBalance) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.EnableAutoWhiteBalance, AutoWhiteBalanceDesc.Name, SensorNames.Color);
                SetOption(SensorNames.Color, Option.EnableAutoWhiteBalance, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> AutoWhiteBalanceDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "Enable / disable auto-white-balance";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Manual laser power in mw. applicable only when laser power mode is set to Manual
        /// </summary>
        public int LaserPower
        {
            get
            {
                CheckOptionSupported(Option.LaserPower, LaserPowerDesc.Name, SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.LaserPower);
            }

            set
            {
                CheckOptionSupported(Option.LaserPower, LaserPowerDesc.Name, SensorNames.Stereo);
                var option = QueryOption(Option.LaserPower, SensorNames.Stereo);

                // step size for depth laser power is 30
                float adjusted_value = AdjustValue(option.Min, option.Max, value, option.Step);
                CheckRangeValid<int>(LaserPowerDesc, value, (int)adjusted_value, true);
                SetOption(SensorNames.Stereo, Option.LaserPower, adjusted_value);
            }
        }

        RangeParamDesc<int> LaserPowerDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.LaserPower, SensorNames.Stereo);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }

                res.Description = "Manual laser power in mw. applicable only when laser power mode is set to Manual";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Power of the DS5 projector
        /// </summary>
        public EmitterMode LaserMode
        {
            get
            {
                CheckOptionSupported(Option.EmitterEnabled, EmmiterModeDesc.Name, SensorNames.Stereo);
                return (EmitterMode)GetOption(SensorNames.Stereo, Option.EmitterEnabled);
            }

            set
            {
                CheckOptionSupported(Option.EmitterEnabled, EmmiterModeDesc.Name, SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.EmitterEnabled, (float)value);
            }
        }

        ParamDesc<EmitterMode> EmmiterModeDesc
        {
            get
            {
                ParamDesc<EmitterMode> res = new ParamDesc<EmitterMode>()
                {
                    Description = "Power of the DS5 projector",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };

                return res;
            }
        }

        /// <summary>
        /// Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa
        /// </summary>
        public int FrameQueueSizeColor
        {
            get
            {
                CheckOptionSupported(Option.FramesQueueSize, FrameQueueSizeColorDesc.Name, SensorNames.Color);
                return (int)GetOption(SensorNames.Color, Option.FramesQueueSize);
            }

            set
            {
                CheckOptionSupported(Option.FramesQueueSize, FrameQueueSizeColorDesc.Name, SensorNames.Color);
                CheckRangeValid<int>(FrameQueueSizeColorDesc, value, 0);
                SetOption(SensorNames.Color, Option.FramesQueueSize, (float)value);
            }
        }

        RangeParamDesc<int> FrameQueueSizeColorDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.FramesQueueSize, SensorNames.Color);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa
        /// </summary>
        public int FrameQueueSizeDepth
        {
            get
            {
                CheckOptionSupported(Option.FramesQueueSize, FrameQueueSizeDepthDesc.Name, SensorNames.Stereo);
                return (int)GetOption(SensorNames.Stereo, Option.FramesQueueSize);
            }

            set
            {
                CheckOptionSupported(Option.FramesQueueSize, FrameQueueSizeDepthDesc.Name, SensorNames.Stereo);
                CheckRangeValid<int>(FrameQueueSizeDepthDesc, value, 0);
                SetOption(SensorNames.Stereo, Option.FramesQueueSize, (float)value);
            }
        }

        RangeParamDesc<int> FrameQueueSizeDepthDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if (this.IsConnected)
                {
                    var option = QueryOption(Option.FramesQueueSize, SensorNames.Stereo);
                    res = new RangeParamDesc<int>((int)option.Min, (int)option.Max);
                }
                else
                {
                    res = new RangeParamDesc<int>(0, 0);
                }
                
                res.Description = "Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        /// <summary>
        /// Power Line Frequency control for anti-flickering Off/50Hz/60Hz/Auto
        /// </summary>
        public PowerLineMode PowerFrequencyMode
        {
            get
            {
                CheckOptionSupported(Option.PowerLineFrequency, PowerFrequencyModeDesc.Name, SensorNames.Color);
                return (PowerLineMode)GetOption(SensorNames.Color, Option.PowerLineFrequency);
            }

            set
            {
                CheckOptionSupported(Option.PowerLineFrequency, PowerFrequencyModeDesc.Name, SensorNames.Color);
                SetOption(SensorNames.Color, Option.PowerLineFrequency, (float)value);
            }
        }

        ListParamDesc<PowerLineMode> PowerFrequencyModeDesc
        {
            get
            {
                ListParamDesc<PowerLineMode> res = new ListParamDesc<PowerLineMode>(typeof(PowerLineMode))
                {
                    Description = "Power Line Frequency control for anti-flickering Off/50Hz/60Hz/Auto",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };

                return res;
            }
        }

        /// <summary>
        /// Current Asic Temperature
        /// </summary>
        public float ASICTemp
        {
            get
            {
                CheckOptionSupported(Option.AsicTemperature, nameof(ASICTemp), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.AsicTemperature);
            }
        }

        //ParamDesc<float> ASICTempDesc
        //{
        //    get
        //    {
        //        ParamDesc<float> res = new ParamDesc<float>();
        //        res.Unit = "°C";
        //        res.Description = "Asic Temperature";
        //        res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
        //        res.WritableWhen = ParamDesc.ConnectionStates.Connected;
        //        return res;
        //    }
        //}


        /// <summary>
        /// Enable / disable polling of camera internal errors
        /// </summary>
        public bool EnableErrorPolling
        {
            get
            {
                CheckOptionSupported(Option.ErrorPollingEnabled, EnableErrorPollingDesc.Name, SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.ErrorPollingEnabled) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.ErrorPollingEnabled, EnableErrorPollingDesc.Name, SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.ErrorPollingEnabled, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> EnableErrorPollingDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "Enable / disable polling of camera internal errors";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }


        /// <summary>
        /// Current Projector Temperature in °C
        /// </summary>
        public float ProjectorTemp
        {
            get
            {
                CheckOptionSupported(Option.ProjectorTemperature, nameof(ProjectorTemp), SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.ProjectorTemperature);
            }
        }

        //ParamDesc<float> ProjectorTempDesc
        //{
        //    get
        //    {
        //        ParamDesc<float> res = new ParamDesc<float>();
        //        res.Unit = "°C";
        //        res.Description = "Projector Temperature";
        //        res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
        //        res.WritableWhen = ParamDesc.ConnectionStates.Connected;
        //        return res;
        //    }
        //}


        /// <summary>
        /// Enable / disable trigger to be outputed from the camera to any external device on every depth frame
        /// </summary>
        public bool OutputTrigger
        {
            get
            {
                CheckOptionSupported(Option.OutputTriggerEnabled, OutputTriggerDesc.Name, SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.OutputTriggerEnabled) == 1.0f;
            }

            set
            {
                CheckOptionSupported(Option.OutputTriggerEnabled, OutputTriggerDesc.Name, SensorNames.Stereo);
                SetOption(SensorNames.Stereo, Option.OutputTriggerEnabled, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> OutputTriggerDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Description = "Enable / disable trigger to be outputed from the camera to any external device on every depth frame";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }


        /// <summary>
        /// Number of meters represented by a single depth unit
        /// </summary>
        public float DepthUnits
        {
            get
            {
                CheckOptionSupported(Option.DepthUnits, DepthUnitsDesc.Name, SensorNames.Stereo);
                return GetOption(SensorNames.Stereo, Option.DepthUnits);
            }

            set
            {
                CheckOptionSupported(Option.DepthUnits, DepthUnitsDesc.Name, SensorNames.Stereo);
                CheckRangeValid<float>(DepthUnitsDesc, value, 0f);
                SetOption(SensorNames.Stereo, Option.DepthUnits, value);
            }
        }

        RangeParamDesc<float> DepthUnitsDesc
        {
            get
            {
                RangeParamDesc<float> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(Option.DepthUnits, SensorNames.Stereo);
                    res = new RangeParamDesc<float>(option.Min, option.Max);
                }
                else
                {
                    res = new RangeParamDesc<float>(0, 0);
                }
                
                res.Description = "Number of meters represented by a single depth unit";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

        #endregion

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

            if(disposing)
            {
                // dispose managed resources
                _config.Dispose();
                _pipeline.Dispose();
                _context.Dispose();
            }
            
            _disposed = true;
        }

        #region Constructor
        public RealSense2()
            : base(modelName: "RealSense2")
        {
            _context = new Context();
            _pipeline = new Pipeline(_context);
            _config = new Config();
            _dev = _context.Devices[0];

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
            _dev = GetDevice();
            Model = _dev.Info[CameraInfo.Name];

            if (!haveSerial)
                this.SerialNumber = _dev.Info[CameraInfo.SerialNumber];

            AdvancedDevice adev = AdvancedDevice.FromDevice(_dev);

            if (!adev.AdvancedModeEnabled)
                adev.AdvancedModeEnabled = true;

            _depthScale = GetDepthScale();
        }

        private void StopPipeline()
        {
            if(_pipelineRunning)
            {
                _pipelineRunning = false;
                _pipeline.Stop();
            }
        }

        private void StartPipeline()
        {
            
            if(!_config.CanResolve(_pipeline))
            {
                string msg = $"{Name}: No camera that supports the current configuration detected";
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            if(!_pipelineRunning)
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
            catch(Exception e)
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
            {
                return;
            }

            throw new Exception($"{Name}: not all requested frames are part of the retrieved FrameSet");
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
                string msg = string.Format("{0}: Channel not supported {1}", Name, channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }


            Action enableStream = () => { _config.EnableStream(stream, index, res_x, res_y, format, fps); };

            if (_pipelineRunning)
            {
                ExecuteWithStoppedPipeline(enableStream);
            }
            else
            {
                enableStream();
            }
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
            {
                ExecuteWithStoppedPipeline(disableStream);
            }
            else
            {
                disableStream();
            }
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
                string msg = string.Format("{0}: intrinsics distrotion model {1} does not match Metrilus.Util", Name, intrinsics.model.ToString());
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

        private VideoStreamProfile GetProfileFromSensor(string channelName)
        {
            string sensorName;
            Point2i refResolution;
            int refFPS;
            Stream streamType;
            int index = 0;

            switch (channelName)
            {
                case ChannelNames.Color:
                    sensorName = SensorNames.Color;
                    refResolution = ColorResolution;
                    refFPS = ColorFPS;
                    streamType = Stream.Color;
                    break;
                case ChannelNames.ZImage:
                case ChannelNames.Distance:
                    sensorName = SensorNames.Stereo;
                    streamType = Stream.Depth;
                    refResolution = DepthResolution;
                    refFPS = DepthFPS;
                    break;
                case ChannelNames.Left:
                    sensorName = SensorNames.Stereo;
                    streamType = Stream.Infrared;
                    refResolution = DepthResolution;
                    refFPS = DepthFPS;
                    index = 1;
                    break;
                case ChannelNames.Right:
                    sensorName = SensorNames.Stereo;
                    streamType = Stream.Infrared;
                    refResolution = DepthResolution;
                    refFPS = DepthFPS;
                    index = 2;
                    break;
                default:
                    string msg = string.Format("{0}: stream profile for channel {1} not available", Name, channelName);
                    log.Error(msg);
                    throw new ArgumentException(msg, nameof(channelName));
            }

            Sensor sensor = GetSensor(sensorName);

            return sensor.VideoStreamProfiles
                .Where(p => p.Stream == streamType)
                .Where(p => p.Framerate == refFPS)
                .Where(p => p.Width == refResolution.X && p.Height == refResolution.Y)
                .Where(p => p.Index == index)
                .First();
        }

        unsafe public override RigidBodyTransformation GetExtrinsics(string channelFromName, string channelToName)
        {
            // first check if extrinsics for requested channel have been cached already
            string extrinsicsKey = $"{channelFromName}_{channelToName}";
            if (_extrinsics.TryGetValue(extrinsicsKey, out RigidBodyTransformation cachedExtrinsics))
            {
                return cachedExtrinsics;
            }

            VideoStreamProfile from = GetProfileFromSensor(channelFromName);
            VideoStreamProfile to = GetProfileFromSensor(channelToName);


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
            image.TimeStamp = _currentFrameTimestamp;

            return image;
        }

        #endregion

        public void LoadConfigPreset(AdvancedMode.Preset preset)
        {
            LoadCustomConfig(AdvancedMode.GetPreset(preset));
        }
        
        public void LoadCustomConfig(string json)
        {
            AdvancedDevice adev = AdvancedDevice.FromDevice(_dev);
            adev.JsonConfiguration = json;
            _depthScale = GetDepthScale();
        }

        private void ExecuteWithStoppedPipeline(Action doStuff)
        {
            bool running = _pipelineRunning;

            StopPipeline();

            doStuff();

            if (running)
            {
                StartPipeline();
            }
        }

        private void CheckOptionSupported(Option option, string optionName, string sensorName)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(string.Format("The property '{0}' can only be read or written when the camera is connected!", optionName));
            }

            if (!GetSensor(sensorName).Options[option].Supported)
            {
                throw new NotSupportedException(string.Format("Option '{0}' is not supported by the {1} sensor of this camera.", optionName, sensorName));
            }
        }

        private float GetOption(string sensorName, Option option)
        {
            return GetSensor(sensorName).Options[option].Value;
        }

        private void SetOption(string sensorName, Option option, float value)
        {
            GetSensor(sensorName).Options[option].Value = value;
        }

        private void CheckRangeValid<T>(RangeParamDesc<T> desc, T value, T adjustedValue, bool adjusted = false)
        {
            if (desc.IsValid(value))
            {
                return;
            }

            if (adjusted)
            {
                throw new ArgumentOutOfRangeException(string.Format("Value {0} for '{1}' is outside of the range between {2} and {3}", value, desc.Name, desc.Min, desc.Max));
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Value {0} (adjusted to {1} to match stepsize) for '{2}' is outside of the range between {3} and {4}", value, adjustedValue, desc.Name, desc.Min, desc.Max));
            }
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
            if (string.IsNullOrEmpty(SerialNumber))
            {
                // No S/N -> return first device
                return _context.Devices[0];
            }

            foreach(Device dev in _context.Devices)
            {
                if (dev.Info[CameraInfo.SerialNumber] == SerialNumber)
                {
                    return dev;
                }
            }

            throw new ArgumentException(string.Format("Device with S/N {0} could not be found", SerialNumber));
        }

        private Sensor GetSensor(string sensorName)
        {
            foreach (Sensor sensor in _dev.Sensors)
            {
                if (sensor.Info[CameraInfo.Name] == sensorName)
                {
                    return sensor;
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
    }
}
