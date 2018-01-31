// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Reflection;
#if NETSTANDARD2_0
#else
using System.Drawing.Imaging;
#endif


namespace MetriCam2.Cameras
{
    public class RealSense2 : Camera, IDisposable
    {
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

        private Point2i _colorResolution = new Point2i(640, 480);
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

        ListParamDesc<Point2i> ColorResolutionDesc
        {
            get
            {
                List<Point2i> resolutions = new List<Point2i>();
                resolutions.Add(new Point2i(640, 480));

                if (this.IsConnected)
                {
                    resolutions = RealSense2API.GetSupportedResolutions(_pipeline, RealSense2API.SensorName.COLOR);
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

        ListParamDesc<int> ColorFPSDesc
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

        ListParamDesc<Point2i> DepthResolutionDesc
        {
            get
            {
                List<Point2i> resolutions = new List<Point2i>();
                resolutions.Add(new Point2i(640, 480));

                if(this.IsConnected)
                {
                    resolutions = RealSense2API.GetSupportedResolutions(_pipeline, RealSense2API.SensorName.STEREO);
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

        ListParamDesc<int> DepthFPSDesc
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
                return RealSense2API.GetFirmwareVersion(_pipeline);
            }
        }

        ParamDesc<Point2i> FirmwareDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Description = "Current Firmware version of the connected camera.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }

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
                CheckOptionSupported(RealSense2API.Option.BACKLIGHT_COMPENSATION, BacklightCompensationDesc.Name, RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.BACKLIGHT_COMPENSATION, BacklightCompensationDesc.Name, RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(RealSense2API.Option.BRIGHTNESS, BrightnessDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.BRIGHTNESS, BrightnessDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(BrightnessDesc, value, 0);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS, (float)value);
            }
        }

        RangeParamDesc<int> BrightnessDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.BRIGHTNESS, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.CONTRAST, ContrastDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.CONTRAST, ContrastDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(ContrastDesc, value, 0);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST, (float)value);
            }
        }

        RangeParamDesc<int> ContrastDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.CONTRAST, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, ExposureColorDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, ExposureColorDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(ExposureColorDesc, value, 0);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE, (float)value);
            }
        }

        RangeParamDesc<int> ExposureColorDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.EXPOSURE, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, AutoExposureColorDesc.Name, RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_EXPOSURE) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, AutoExposureColorDesc.Name, RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_EXPOSURE, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(RealSense2API.Option.AUTO_EXPOSURE_PRIORITY, AutoExposurePriorityColorDesc.Name, RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.AUTO_EXPOSURE_PRIORITY) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.AUTO_EXPOSURE_PRIORITY, AutoExposurePriorityColorDesc.Name, RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.AUTO_EXPOSURE_PRIORITY, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, ExposureDepthDesc.Name, RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.EXPOSURE, ExposureDepthDesc.Name, RealSense2API.SensorName.STEREO);
                var option = QueryOption(RealSense2API.Option.EXPOSURE, RealSense2API.SensorName.STEREO);

                // step size for depth exposure is 20
                float adjusted_value = AdjustValue(option.min, option.max, value, option.step);
                CheckRangeValid<int>(ExposureDepthDesc, value, (int)adjusted_value, true);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE, adjusted_value);
            }
        }

        RangeParamDesc<int> ExposureDepthDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.EXPOSURE, RealSense2API.SensorName.STEREO);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, AutoExposureDepthDesc.Name, RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ENABLE_AUTO_EXPOSURE) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_EXPOSURE, AutoExposureDepthDesc.Name, RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ENABLE_AUTO_EXPOSURE, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(RealSense2API.Option.GAIN, GainColorDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.GAIN, GainColorDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(GainColorDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN, (float)value);
            }
        }

        RangeParamDesc<int> GainColorDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.GAIN, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.GAIN, GainDepthDesc.Name, RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.GAIN);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.GAIN, GainDepthDesc.Name, RealSense2API.SensorName.STEREO);
                CheckRangeValid<int>(GainDepthDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.GAIN, (float)value);
            }
        }

        RangeParamDesc<int> GainDepthDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.GAIN, RealSense2API.SensorName.STEREO);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.GAMMA, GammaDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.GAMMA, GammaDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(GammaDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA, (float)value);
            }
        }

        RangeParamDesc<int> GammaDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.GAMMA, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.HUE, HueDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.HUE, HueDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(HueDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE, (float)value);
            }
        }

        RangeParamDesc<int> HueDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.HUE, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.SATURATION, SaturationDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.SATURATION, SaturationDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(SaturationDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION, (float)value);
            }
        }

        RangeParamDesc<int> SaturationDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.SATURATION, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.SHARPNESS, SharpnessDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.SHARPNESS, SharpnessDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(SharpnessDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS, (float)value);
            }
        }

        RangeParamDesc<int> SharpnessDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.SHARPNESS, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.WHITE_BALANCE, WhiteBalanceDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.WHITE_BALANCE, WhiteBalanceDesc.Name, RealSense2API.SensorName.COLOR);
                var option = QueryOption(RealSense2API.Option.WHITE_BALANCE, RealSense2API.SensorName.COLOR);


                // step size for depth white balance is 10
                float adjusted_value = AdjustValue(option.min, option.max, value, option.step);
                CheckRangeValid<int>(WhiteBalanceDesc, value, (int)adjusted_value, true);

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE, adjusted_value);
            }
        }

        RangeParamDesc<int> WhiteBalanceDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.WHITE_BALANCE, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE, AutoWhiteBalanceDesc.Name, RealSense2API.SensorName.COLOR);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE, AutoWhiteBalanceDesc.Name, RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(RealSense2API.Option.LASER_POWER, LaserPowerDesc.Name, RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.LASER_POWER, LaserPowerDesc.Name, RealSense2API.SensorName.STEREO);
                var option = QueryOption(RealSense2API.Option.LASER_POWER, RealSense2API.SensorName.STEREO);

                // step size for depth laser power is 30
                float adjusted_value = AdjustValue(option.min, option.max, value, option.step);
                CheckRangeValid<int>(LaserPowerDesc, value, (int)adjusted_value, true);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER, adjusted_value);
            }
        }

        RangeParamDesc<int> LaserPowerDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.LASER_POWER, RealSense2API.SensorName.STEREO);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.EMITTER_ENABLED, EmmiterModeDesc.Name, RealSense2API.SensorName.STEREO);
                return (EmitterMode)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EMITTER_ENABLED);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.EMITTER_ENABLED, EmmiterModeDesc.Name, RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER, (float)value);
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
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, FrameQueueSizeColorDesc.Name, RealSense2API.SensorName.COLOR);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, FrameQueueSizeColorDesc.Name, RealSense2API.SensorName.COLOR);
                CheckRangeValid<int>(FrameQueueSizeColorDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE, (float)value);
            }
        }

        RangeParamDesc<int> FrameQueueSizeColorDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.FRAMES_QUEUE_SIZE, RealSense2API.SensorName.COLOR);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, FrameQueueSizeDepthDesc.Name, RealSense2API.SensorName.STEREO);
                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.FRAMES_QUEUE_SIZE, FrameQueueSizeDepthDesc.Name, RealSense2API.SensorName.STEREO);
                CheckRangeValid<int>(FrameQueueSizeDepthDesc, value, 0);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE, (float)value);
            }
        }

        RangeParamDesc<int> FrameQueueSizeDepthDesc
        {
            get
            {
                RangeParamDesc<int> res;

                if (this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.FRAMES_QUEUE_SIZE, RealSense2API.SensorName.STEREO);
                    res = new RangeParamDesc<int>((int)option.min, (int)option.max);
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
                CheckOptionSupported(RealSense2API.Option.POWER_LINE_FREQUENCY, PowerFrequencyModeDesc.Name, RealSense2API.SensorName.COLOR);
                return (PowerLineMode)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.POWER_LINE_FREQUENCY, PowerFrequencyModeDesc.Name, RealSense2API.SensorName.COLOR);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY, (float)value);
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
                CheckOptionSupported(RealSense2API.Option.ASIC_TEMPERATURE, ASICTempDesc.Name, RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ASIC_TEMPERATURE);
            }
        }

        ParamDesc<float> ASICTempDesc
        {
            get
            {
                ParamDesc<float> res = new ParamDesc<float>();
                res.Unit = "°C";
                res.Description = "Asic Temperature";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }


        /// <summary>
        /// Enable / disable polling of camera internal errors
        /// </summary>
        public bool EnableErrorPolling
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.ERROR_POLLING_ENABLED, EnableErrorPollingDesc.Name, RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ERROR_POLLING_ENABLED) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.ERROR_POLLING_ENABLED, EnableErrorPollingDesc.Name, RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ERROR_POLLING_ENABLED, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(RealSense2API.Option.PROJECTOR_TEMPERATURE, ProjectorTempDesc.Name, RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.PROJECTOR_TEMPERATURE);
            }
        }

        ParamDesc<float> ProjectorTempDesc
        {
            get
            {
                ParamDesc<float> res = new ParamDesc<float>();
                res.Unit = "°C";
                res.Description = "Projector Temperature";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Connected;
                return res;
            }
        }


        /// <summary>
        /// Enable / disable trigger to be outputed from the camera to any external device on every depth frame
        /// </summary>
        public bool OutputTrigger
        {
            get
            {
                CheckOptionSupported(RealSense2API.Option.OUTPUT_TRIGGER_ENABLED, OutputTriggerDesc.Name, RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.OUTPUT_TRIGGER_ENABLED) == 1.0f ? true : false;
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.OUTPUT_TRIGGER_ENABLED, OutputTriggerDesc.Name, RealSense2API.SensorName.STEREO);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.OUTPUT_TRIGGER_ENABLED, value ? 1.0f : 0.0f);
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
                CheckOptionSupported(RealSense2API.Option.DEPTH_UNITS, DepthUnitsDesc.Name, RealSense2API.SensorName.STEREO);
                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS);
            }

            set
            {
                CheckOptionSupported(RealSense2API.Option.DEPTH_UNITS, DepthUnitsDesc.Name, RealSense2API.SensorName.STEREO);
                CheckRangeValid<float>(DepthUnitsDesc, value, 0f);
                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS, value);
            }
        }

        RangeParamDesc<float> DepthUnitsDesc
        {
            get
            {
                RangeParamDesc<float> res;

                if(this.IsConnected)
                {
                    var option = QueryOption(RealSense2API.Option.DEPTH_UNITS, RealSense2API.SensorName.STEREO);
                    res = new RangeParamDesc<float>(option.min, option.max);
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

            _config.Delete();
            _pipeline.Delete();
            _context.Delete();
            _disposed = true;
        }

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
            StopPipeline();
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

            return new ProjectiveTransformationZhang(
                intrinsics.width,
                intrinsics.height,
                intrinsics.fx,
                intrinsics.fy,
                intrinsics.ppx,
                intrinsics.ppy,
                intrinsics.coeffs[0],
                intrinsics.coeffs[1],
                intrinsics.coeffs[2],
                intrinsics.coeffs[3],
                intrinsics.coeffs[4]);
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

            return new RigidBodyTransformation(rot, trans);
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
    }
}
