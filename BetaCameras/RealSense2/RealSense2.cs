// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
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
        private RealSense2API.RS2Frame _currentColorFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentDepthFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentLeftFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentRightFrame = new RealSense2API.RS2Frame();
        private float _depthScale = 0.0f;
        private bool _disposed = false;

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

        public Point2i ColorResolution { get; set; } = new Point2i(640, 480);

        ParamDesc<Point2i> ColorResolutionDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Pixel";
                res.Description = "Resolution of the color sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        public uint ColorFPS { get; set; } = 30;

        ParamDesc<Point2i> ColorFPSDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Frames per Second";
                res.Description = "FPS of the color sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        public Point2i DepthResolution { get; set; } = new Point2i(640, 480);

        ParamDesc<Point2i> DepthResolutionDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Pixel";
                res.Description = "Resolution of the depth sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        public uint DepthFPS { get; set; } = 30;

        ParamDesc<Point2i> DepthFPSDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Frames per Second";
                res.Description = "FPS of the depth sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION))
                    throw new Exception("Option 'BacklightCompensation' is not supported by the color sensor of this camera.");

                float res = RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION);

                if (res == 1.0f)
                    return true;

                return false;
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION))
                    throw new Exception("Option 'BacklightCompensation' is not supported by the color sensor of this camera.");

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BACKLIGHT_COMPENSATION, value ? 1.0f : 0.0f);
            }
        }

        ParamDesc<bool> BacklightCompensationDesc
        {
            get
            {
                ParamDesc<bool> res = new ParamDesc<bool>();
                res.Unit = "Boolean";
                res.Description = "Enable / disable color backlight compensation";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS))
                    throw new Exception("Option 'Brightness' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS))
                    throw new Exception("Option 'Brightness' is not supported by the color sensor of this camera.");

                if(!BrightnessDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Brightness' is outside of the range between {1} and {2}", value, BrightnessDesc.Min, BrightnessDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.BRIGHTNESS, (float)value);
            }
        }

        RangeParamDesc<int> BrightnessDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline, 
                    RealSense2API.SensorName.COLOR, 
                    RealSense2API.Option.BRIGHTNESS, 
                    out float min, 
                    out float max, 
                    out float step, 
                    out float def, 
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Color image brightness";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST))
                    throw new Exception("Option 'Contrast' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST))
                    throw new Exception("Option 'Contrast' is not supported by the color sensor of this camera.");

                if (!ContrastDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Contrast' is outside of the range between {1} and {2}", value, ContrastDesc.Min, ContrastDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.CONTRAST, (float)value);
            }
        }

        RangeParamDesc<int> ContrastDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.CONTRAST,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Color image contrast";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE))
                    throw new Exception("Option 'Exposure' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE))
                    throw new Exception("Option 'Exposure' is not supported by the color sensor of this camera.");

                if (!ExposureColorDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Exposure' is outside of the range between {1} and {2}", value, ExposureColorDesc.Min, ExposureColorDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.EXPOSURE, (float)value);
            }
        }

        RangeParamDesc<int> ExposureColorDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.EXPOSURE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Controls exposure time of color camera. Setting any value will disable auto exposure";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_EXPOSURE))
                    throw new Exception("Option 'AutoExposure' is not supported by the color sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_EXPOSURE) == 1.0f ? true : false;
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_EXPOSURE))
                    throw new Exception("Option 'AutoExposure' is not supported by the color sensor of this camera.");

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
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.AUTO_EXPOSURE_PRIORITY))
                    throw new Exception("Option 'AutoExposurePriority' is not supported by the color sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.AUTO_EXPOSURE_PRIORITY) == 1.0f ? true : false;
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.AUTO_EXPOSURE_PRIORITY))
                    throw new Exception("Option 'AutoExposurePriority' is not supported by the color sensor of this camera.");

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
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE))
                    throw new Exception("Option 'Exposure' is not supported by the stereo sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE))
                    throw new Exception("Option 'Exposure' is not supported by the stereo sensor of this camera.");

                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.STEREO,
                    RealSense2API.Option.EXPOSURE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);


                // step size for depth exposure is 20
                float adjusted_value = (float)value;
                int rounding = value - (int)min % (int)step;
                adjusted_value -= (float)rounding;

                if (rounding > step / 2)
                {
                    adjusted_value += step;
                }

                if (!ExposureDepthDesc.IsValid(value))
                    throw new Exception(
                        string.Format(
                            "Value {0} (adjusted to {1} to match stepsize) for 'Exposure' is outside of the range between {2} and {3}", 
                            value, 
                            adjusted_value, 
                            ExposureDepthDesc.Min, 
                            ExposureDepthDesc.Min
                        )
                    );

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EXPOSURE, adjusted_value);
            }
        }

        RangeParamDesc<int> ExposureDepthDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.STEREO,
                    RealSense2API.Option.EXPOSURE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Controls exposure time of depth camera. Setting any value will disable auto exposure";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ENABLE_AUTO_EXPOSURE))
                    throw new Exception("Option 'AutoExposure' is not supported by the stereo sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ENABLE_AUTO_EXPOSURE) == 1.0f ? true : false;
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ENABLE_AUTO_EXPOSURE))
                    throw new Exception("Option 'AutoExposure' is not supported by the stereo sensor of this camera.");

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
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN))
                    throw new Exception("Option 'Gain' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN))
                    throw new Exception("Option 'Gain' is not supported by the color sensor of this camera.");

                if (!GainColorDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Gain' is outside of the range between {1} and {2}", value, GainColorDesc.Min, GainColorDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN, (float)value);
            }
        }

        RangeParamDesc<int> GainColorDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.GAIN,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Color image gain";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.GAIN))
                    throw new Exception("Option 'Gain' is not supported by the stereo sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.GAIN);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAIN))
                    throw new Exception("Option 'Gain' is not supported by the stereo sensor of this camera.");

                if (!GainDepthDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Gain' is outside of the range between {1} and {2}", value, GainDepthDesc.Min, GainDepthDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.GAIN, (float)value);
            }
        }

        RangeParamDesc<int> GainDepthDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.STEREO,
                    RealSense2API.Option.GAIN,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Depth image gain";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA))
                    throw new Exception("Option 'Gamma' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA))
                    throw new Exception("Option 'Gamma' is not supported by the color sensor of this camera.");

                if (!GammaDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Gamma' is outside of the range between {1} and {2}", value, GammaDesc.Min, GammaDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.GAMMA, (float)value);
            }
        }

        RangeParamDesc<int> GammaDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.GAMMA,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Color image Gamma";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE))
                    throw new Exception("Option 'Hue' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE))
                    throw new Exception("Option 'Hue' is not supported by the color sensor of this camera.");

                if (!HueDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Hue' is outside of the range between {1} and {2}", value, HueDesc.Min, HueDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.HUE, (float)value);
            }
        }

        RangeParamDesc<int> HueDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.HUE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Color image Hue";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION))
                    throw new Exception("Option 'Saturation' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION))
                    throw new Exception("Option 'Saturation' is not supported by the color sensor of this camera.");

                if (!SaturationDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Saturation' is outside of the range between {1} and {2}", value, SaturationDesc.Min, SaturationDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SATURATION, (float)value);
            }
        }

        RangeParamDesc<int> SaturationDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.SATURATION,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Color image Saturation";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS))
                    throw new Exception("Option 'Sharpness' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS))
                    throw new Exception("Option 'Sharpness' is not supported by the color sensor of this camera.");

                if (!SharpnessDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'Sharpness' is outside of the range between {1} and {2}", value, SharpnessDesc.Min, SharpnessDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.SHARPNESS, (float)value);
            }
        }

        RangeParamDesc<int> SharpnessDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.SHARPNESS,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Color image Sharpness";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE))
                    throw new Exception("Option 'WhiteBalance' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE))
                    throw new Exception("Option 'WhiteBalance' is not supported by the color sensor of this camera.");

                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.WHITE_BALANCE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);


                // step size for depth white balance is 10
                float adjusted_value = (float)value;
                int rounding = value - (int)min % (int)step;
                adjusted_value -= (float)rounding;

                if (rounding > step / 2)
                {
                    adjusted_value += step;
                }

                if (!WhiteBalanceDesc.IsValid(value))
                    throw new Exception(
                        string.Format(
                            "Value {0} (adjusted to {1} to match stepsize) for 'WhiteBalance' is outside of the range between {2} and {3}",
                            value,
                            adjusted_value,
                            WhiteBalanceDesc.Min,
                            WhiteBalanceDesc.Min
                        )
                    );

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.WHITE_BALANCE, adjusted_value);
            }
        }

        RangeParamDesc<int> WhiteBalanceDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.WHITE_BALANCE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Controls white balance of color image.Setting any value will disable auto white balance";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE))
                    throw new Exception("Option 'AutoWhiteBalance' is not supported by the color sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE) == 1.0f ? true : false;
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.ENABLE_AUTO_WHITE_BALANCE))
                    throw new Exception("Option 'AutoWhiteBalance' is not supported by the color sensor of this camera.");

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
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER))
                    throw new Exception("Option 'LaserPower' is not supported by the stereo sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER))
                    throw new Exception("Option 'LaserPower' is not supported by the stereo sensor of this camera.");

                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.STEREO,
                    RealSense2API.Option.LASER_POWER,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);


                // step size for depth laser power is 30
                float adjusted_value = (float)value;
                int rounding = value - (int)min % (int)step;
                adjusted_value -= (float)rounding;

                if (rounding > step / 2)
                {
                    adjusted_value += step;
                }

                if (!LaserPowerDesc.IsValid(value))
                    throw new Exception(
                        string.Format(
                            "Value {0} (adjusted to {1} to match stepsize) for 'LaserPower' is outside of the range between {2} and {3}",
                            value,
                            adjusted_value,
                            LaserPowerDesc.Min,
                            LaserPowerDesc.Min
                        )
                    );

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.LASER_POWER, adjusted_value);
            }
        }

        RangeParamDesc<int> LaserPowerDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.STEREO,
                    RealSense2API.Option.LASER_POWER,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Manual laser power in mw. applicable only when laser power mode is set to Manual";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EMITTER_ENABLED))
                    throw new Exception("Option 'LaserMode' is not supported by the stereo sensor of this camera.");

                return (EmitterMode)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EMITTER_ENABLED);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.EMITTER_ENABLED))
                    throw new Exception("Option 'LaserMode' is not supported by the stereo sensor of this camera.");

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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE))
                    throw new Exception("Option 'FrameQueueSize' is not supported by the color sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE))
                    throw new Exception("Option 'FrameQueueSize' is not supported by the color sensor of this camera.");

                if (!FrameQueueSizeColorDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'FrameQueueSize' is outside of the range between {1} and {2}", value, FrameQueueSizeColorDesc.Min, FrameQueueSizeColorDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.FRAMES_QUEUE_SIZE, (float)value);
            }
        }

        RangeParamDesc<int> FrameQueueSizeColorDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.COLOR,
                    RealSense2API.Option.FRAMES_QUEUE_SIZE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE))
                    throw new Exception("Option 'FrameQueueSize' is not supported by the stereo sensor of this camera.");

                return (int)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE))
                    throw new Exception("Option 'FrameQueueSize' is not supported by the stereo sensor of this camera.");

                if (!FrameQueueSizeDepthDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'FrameQueueSize' is outside of the range between {1} and {2}", value, FrameQueueSizeDepthDesc.Min, FrameQueueSizeDepthDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.FRAMES_QUEUE_SIZE, (float)value);
            }
        }

        RangeParamDesc<int> FrameQueueSizeDepthDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.STEREO,
                    RealSense2API.Option.FRAMES_QUEUE_SIZE,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<int> res = new RangeParamDesc<int>((int)min, (int)max);
                res.Description = "Max number of frames you can hold at a given time. Increasing this number will reduce frame drops but increase latency, and vice versa";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY))
                    throw new Exception("Option 'PowerFrequencyMode' is not supported by the color sensor of this camera.");

                return (PowerLineMode)RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY))
                    throw new Exception("Option 'PowerFrequencyMode' is not supported by the color sensor of this camera.");

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.COLOR, RealSense2API.Option.POWER_LINE_FREQUENCY, (float)value);
            }
        }

        ParamDesc<PowerLineMode> PowerFrequencyModeDesc
        {
            get
            {
                ParamDesc<PowerLineMode> res = new ParamDesc<PowerLineMode>()
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ASIC_TEMPERATURE))
                    throw new Exception("Option 'ASICTemp' is not supported by the stereo sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ASIC_TEMPERATURE);
            }
        }

        ParamDesc<float> ASICTempDesc
        {
            get
            {
                ParamDesc<float> res = new ParamDesc<float>();
                res.Unit = "Degree Celsius °C";
                res.Description = "Current Asic Temperature";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ERROR_POLLING_ENABLED))
                    throw new Exception("Option 'ErrorPolling' is not supported by the stereo sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ERROR_POLLING_ENABLED) == 1.0f ? true : false;
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.ERROR_POLLING_ENABLED))
                    throw new Exception("Option 'ErrorPolling' is not supported by the stereo sensor of this camera.");

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
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.PROJECTOR_TEMPERATURE))
                    throw new Exception("Option 'ProjectorTemp' is not supported by the stereo sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.PROJECTOR_TEMPERATURE);
            }
        }

        ParamDesc<float> ProjectorTempDesc
        {
            get
            {
                ParamDesc<float> res = new ParamDesc<float>();
                res.Unit = "Degree Celsius °C";
                res.Description = "Current Projector Temperature";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.OUTPUT_TRIGGER_ENABLED))
                    throw new Exception("Option 'OutputTrigger' is not supported by the stereo sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.OUTPUT_TRIGGER_ENABLED) == 1.0f ? true : false;
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.OUTPUT_TRIGGER_ENABLED))
                    throw new Exception("Option 'OutputTrigger' is not supported by the stereo sensor of this camera.");

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
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS))
                    throw new Exception("Option 'DepthUnits' is not supported by the stereo sensor of this camera.");

                return RealSense2API.GetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS);
            }

            set
            {
                if (!RealSense2API.IsOptionSupported(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS))
                    throw new Exception("Option 'DepthUnits' is not supported by the stereo sensor of this camera.");

                if (!DepthUnitsDesc.IsValid(value))
                    throw new Exception(string.Format("Value {0} for 'DepthUnits' is outside of the range between {1} and {2}", value, DepthUnitsDesc.Min, DepthUnitsDesc.Min));

                RealSense2API.SetOption(_pipeline, RealSense2API.SensorName.STEREO, RealSense2API.Option.DEPTH_UNITS, value);
            }
        }

        RangeParamDesc<float> DepthUnitsDesc
        {
            get
            {
                RealSense2API.QueryOptionInfo(
                    _pipeline,
                    RealSense2API.SensorName.STEREO,
                    RealSense2API.Option.DEPTH_UNITS,
                    out float min,
                    out float max,
                    out float step,
                    out float def,
                    out string desc);

                RangeParamDesc<float> res = new RangeParamDesc<float>(min, max);
                res.Description = "Number of meters represented by a single depth unit";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
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

            RealSense2API.DeleteConfig(_config);
            RealSense2API.DeletePipeline(_pipeline);
            RealSense2API.DeleteContext(_context);
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
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));

            Channels.Add(cr.RegisterCustomChannel(ChannelNames.Left, typeof(FloatCameraImage)));
            Channels.Add(cr.RegisterCustomChannel(ChannelNames.Right, typeof(FloatCameraImage)));
        }


        protected override void ConnectImpl()
        {
            if (!string.IsNullOrWhiteSpace(SerialNumber))
            {
                RealSense2API.EnableDevice(_config, SerialNumber);
            }

            if (ActiveChannels.Count == 0)
            {
                ActivateChannel(ChannelNames.Color);
                ActivateChannel(ChannelNames.ZImage);
            }

            RealSense2API.PipelineStart(_pipeline, _config);

            RealSense2API.RS2Device dev = RealSense2API.GetActiveDevice(_pipeline);

            if (!RealSense2API.AdvancedModeEnabled(dev))
            {
                RealSense2API.EnabledAdvancedMode(dev, true);
            }

            _depthScale = RealSense2API.GetDepthScale(_pipeline);
        }

        public void RestartPipeline()
        {
            RealSense2API.PipelineStop(_pipeline);
            RealSense2API.PipelineStart(_pipeline, _config);
        }

        protected override void DisconnectImpl()
        {
            RealSense2API.PipelineStop(_pipeline);            
        }

        protected override void UpdateImpl()
        {
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
                RealSense2API.RS2Frame data = RealSense2API.PipelineWaitForFrames(_pipeline, 500);

                if(!data.IsValid() || data.Handle == IntPtr.Zero)
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
                            if(index == 1)
                            {
                                if (getLeft)
                                {
                                    RealSense2API.ReleaseFrame(_currentLeftFrame);
                                    _currentLeftFrame = frame;
                                    haveLeft = true;
                                }
                            }
                            else if(index == 2)
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
            if (channelName == ChannelNames.Color)
            {
                return CalcColor();
            }
            if (channelName == ChannelNames.ZImage)
            {
                return CalcZImage();
            }
            if (channelName == ChannelNames.Left)
            {
                return CalcIRImage(_currentLeftFrame);
            }
            if (channelName == ChannelNames.Right)
            {
                return CalcIRImage(_currentRightFrame);
            }

            log.Error("Unexpected ChannelName in CalcChannel().");
            return null;
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            if(IsChannelActive(channelName))
            {
                log.Debug(string.Format("Channel {0} is already active", channelName));
                return;
            }

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
                fps = (int)ColorFPS;
                index = -1;
            }
            else if (channelName == ChannelNames.ZImage)
            {
                if (IsChannelActive(ChannelNames.Left) || IsChannelActive(ChannelNames.Right))
                {
                    string msg = string.Format("RealSense2: can't have {0}/{1} and {2} active at the same time", ChannelNames.Left, ChannelNames.Right, ChannelNames.ZImage);
                    log.Error(msg);
                    throw new Exception(msg);
                }

                stream = RealSense2API.Stream.DEPTH;
                format = RealSense2API.Format.Z16;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = -1;
            }
            else if (channelName == ChannelNames.Left)
            {
                if(IsChannelActive(ChannelNames.ZImage))
                {
                    string msg = string.Format("RealSense2: can't have {0} and {1} active at the same time", ChannelNames.Left, ChannelNames.ZImage);
                    log.Error(msg);
                    throw new Exception(msg);
                }

                stream = RealSense2API.Stream.INFRARED;
                format = RealSense2API.Format.Y8;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = 1;
            }
            else if (channelName == ChannelNames.Right)
            {
                if (IsChannelActive(ChannelNames.ZImage))
                {
                    string msg = string.Format("RealSense2: can't have {0} and {1} active at the same time", ChannelNames.Right, ChannelNames.ZImage);
                    log.Error(msg);
                    throw new Exception(msg);
                }

                stream = RealSense2API.Stream.INFRARED;
                format = RealSense2API.Format.Y8;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = 2;
            }
            else
            {
                string msg = string.Format("RealSense2: Channel not supported {0}", channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            bool running = RealSense2API.PipelineRunning;

            if(running)
                RealSense2API.PipelineStop(_pipeline);

            RealSense2API.ConfigEnableStream(_config, stream, index, res_x, res_y, format, fps);

            if(running)
                RealSense2API.PipelineStart(_pipeline, _config);
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            RealSense2API.Stream stream = RealSense2API.Stream.ANY;

            if (channelName == ChannelNames.Color)
            {
                stream = RealSense2API.Stream.COLOR;
            }
            else if (channelName == ChannelNames.ZImage)
            {
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

            _currentColorFrame = new RealSense2API.RS2Frame();
            _currentDepthFrame = new RealSense2API.RS2Frame();
            _currentLeftFrame = new RealSense2API.RS2Frame();
            _currentRightFrame = new RealSense2API.RS2Frame();

            bool running = RealSense2API.PipelineRunning;

            if(running)
                RealSense2API.PipelineStop(_pipeline);

            RealSense2API.ConfigDisableStream(_config, stream);

            if (running)
                RealSense2API.PipelineStart(_pipeline, _config);
        }

        unsafe public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            RealSense2API.RS2StreamProfile profile = GetProfileForChannelName(channelName);
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

        private RealSense2API.RS2StreamProfile GetProfileForChannelName(string channelName)
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
                    throw new Exception(msg);
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
            RealSense2API.RS2StreamProfile from = GetProfileForChannelName(channelFromName);
            RealSense2API.RS2StreamProfile to = GetProfileForChannelName(channelToName);

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

            int height = DepthResolution.Y;
            int width = DepthResolution.X;

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

        unsafe private FloatCameraImage CalcIRImage(RealSense2API.RS2Frame frame)
        {
            if (!frame.IsValid())
            {
                log.Error("IR frame is not valid...\n");
                return null;
            }

            int height = DepthResolution.Y;
            int width = DepthResolution.X;

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

            int height = ColorResolution.Y;
            int width = ColorResolution.X;

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
            //RealSense2API.DeleteDevice(dev);
            _depthScale = RealSense2API.GetDepthScale(_pipeline);
        }
    }
}
