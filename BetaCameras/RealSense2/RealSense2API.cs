using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Metrilus.Util;

namespace MetriCam2.Cameras.RealSense2API
{
    #region Enums
    public enum Format
    {
        /// <summary> When passed to enable stream, librealsense will try to provide best suited format </summary>
        ANY,

        /// <summary> 16-bit linear depth values. The depth is meters is equal to depth scale * pixel value. </summary>
        Z16,

        /// <summary> 16-bit linear disparity values. The depth in meters is equal to depth scale / pixel value. </summary>
        DISPARITY16,

        /// <summary> 32-bit floating point 3D coordinates. </summary>
        XYZ32F,

        /// <summary> Standard YUV pixel format as described in https://en.wikipedia.org/wiki/YUV </summary>
        YUYV,

        /// <summary> 8-bit red, green and blue channels </summary>
        RGB8,

        /// <summary> 8-bit blue, green, and red channels -- suitable for OpenCV </summary>
        BGR8,

        /// <summary> 8-bit red, green and blue channels + constant alpha channel equal to FF </summary>
        RGBA8,

        /// <summary> 8-bit blue, green, and red channels + constant alpha channel equal to FF </summary>
        BGRA8,

        /// <summary> 8-bit per-pixel grayscale image </summary>
        Y8,

        /// <summary> 16-bit per-pixel grayscale image </summary>
        Y16,

        /// <summary> Four 10-bit luminance values encoded into a 5-byte macropixel </summary>
        RAW10,

        /// <summary> 16-bit raw image </summary>
        RAW16,

        /// <summary> 8-bit raw image </summary>
        RAW8,

        /// <summary> Similar to the standard YUYV pixel format, but packed in a different order </summary>
        UYVY,

        /// <summary> Raw data from the motion sensor </summary>
        MOTION_RAW,

        /// <summary> Motion data packed as 3 32-bit float values, for X, Y, and Z axis </summary>
        MOTION_XYZ32F,

        /// <summary> Raw data from the external sensors hooked to one of the GPIO's </summary>
        GPIO_RAW,

        /// <summary> Number of enumeration values. Not a valid input: intended to be used in for-loops. </summary>
        COUNT
    }

    public enum Stream
    {
        ANY,

        /// <summary> Native stream of depth data produced by RealSense device </summary>
        DEPTH,

        /// <summary> Native stream of color data captured by RealSense device </summary>
        COLOR,

        /// <summary> Native stream of infrared data captured by RealSense device </summary>
        INFRARED,

        /// <summary> Native stream of fish-eye (wide) data captured from the dedicate motion camera </summary>
        FISHEYE,

        /// <summary> Native stream of gyroscope motion data produced by RealSense device </summary>
        GYRO,

        /// <summary> Native stream of accelerometer motion data produced by RealSense device </summary>
        ACCEL,

        /// <summary> Signals from external device connected through GPIO </summary>
        GPIO,

        COUNT
    }

    public enum CameraInfo
    {
        /// <summary> Friendly name </summary>
        NAME,

        /// <summary> Device serial number </summary>
        SERIAL_NUMBER,

        /// <summary> Primary firmware version </summary>
        FIRMWARE_VERSION,

        /// <summary> Unique identifier of the port the device is connected to (platform specific) </summary>
        PHYSICAL_PORT,

        /// <summary> If device supports firmware logging, this is the command to send to get logs from firmware </summary>
        DEBUG_OP_CODE,

        /// <summary> True iff the device is in advanced mode </summary>
        ADVANCED_MODE,

        /// <summary> Product ID as reported in the USB descriptor </summary>
        PRODUCT_ID,

        /// <summary> True iff EEPROM is locked </summary>
        CAMERA_LOCKED,

        /// <summary> Number of enumeration values. Not a valid input: intended to be used in for-loops. </summary>
        COUNT
    }


    public enum DistortionModel
    {
        /// <summary> Rectilinear images. No distortion compensation required. </summary>
        NONE,

        /// <summary> Equivalent to Brown-Conrady distortion, except that tangential distortion is applied to radially distorted points </summary>
        MODIFIED_BROWN_CONRADY,

        /// <summary> Equivalent to Brown-Conrady distortion, except undistorts image instead of distorting it </summary>
        INVERSE_BROWN_CONRADY,

        /// <summary> F-Theta fish-eye distortion model </summary>
        FTHETA,

        /// <summary> Unmodified Brown-Conrady distortion model </summary>
        BROWN_CONRADY,

        /// <summary> Number of enumeration values. Not a valid input: intended to be used in for-loops. </summary>
        COUNT
    };

    public enum Option
    {
        /// <summary>
        /// Enable / disable color backlight compensation
        /// </summary>
        BACKLIGHT_COMPENSATION,

        /// <summary>
        /// Color image brightness
        /// </summary>
        BRIGHTNESS,

        /// <summary>
        /// Color image contrast
        /// </summary>
        CONTRAST,

        /// <summary>
        /// Controls exposure time of color camera. Setting any value will disable auto exposure
        /// </summary>
        EXPOSURE,

        /// <summary>
        /// Color image gain
        /// </summary>
        GAIN,

        /// <summary>
        /// Color image gamma setting
        /// </summary>
        GAMMA,

        /// <summary>
        /// Color image hue
        /// </summary>
        HUE,

        /// <summary>
        /// Color image saturation setting
        /// </summary>
        SATURATION,

        /// <summary>
        /// Color image sharpness setting
        /// </summary>
        SHARPNESS,

        /// <summary>
        /// Controls white balance of color image. Setting any value will disable auto white balance
        /// </summary>
        WHITE_BALANCE,

        /// <summary>
        /// Enable / disable color image auto-exposure
        /// </summary>
        ENABLE_AUTO_EXPOSURE,

        /// <summary>
        /// Enable / disable color image auto-white-balance
        /// </summary>
        ENABLE_AUTO_WHITE_BALANCE,

        /// <summary>
        /// Provide access to several recommend sets of option presets for the depth camera
        /// </summary>
        VISUAL_PRESET,

        /// <summary>
        /// Power of the F200 / SR300 projector, with 0 meaning projector off
        /// </summary>
        LASER_POWER,

        /// <summary>
        /// Set the number of patterns projected per frame. The higher the accuracy value the more patterns projected. Increasing the number of patterns help to achieve better accuracy. Note that this control is affecting the Depth FPS
        /// </summary>
        ACCURACY,

        /// <summary>
        /// Motion vs. Range trade-off, with lower values allowing for better motion sensitivity and higher values allowing for better depth range
        /// </summary>
        MOTION_RANGE,

        /// <summary>
        /// Set the filter to apply to each depth frame. Each one of the filter is optimized per the application requirements
        /// </summary>
        FILTER_OPTION,

        /// <summary>
        /// The confidence level threshold used by the Depth algorithm pipe to set whether a pixel will get a valid range or will be marked with invalid range
        /// </summary>
        CONFIDENCE_THRESHOLD,

        /// <summary>
        /// Laser Emitter enabled
        /// </summary>
        EMITTER_ENABLED,

        /// <summary>
        /// Number of frames the user is allowed to keep per stream. Trying to hold-on to more frames will cause frame-drops.
        /// </summary>
        FRAMES_QUEUE_SIZE,

        /// <summary>
        /// Total number of detected frame drops from all streams
        /// </summary>
        TOTAL_FRAME_DROPS,

        /// <summary>
        /// Auto-Exposure modes: Static, Anti-Flicker and Hybrid
        /// </summary>
        AUTO_EXPOSURE_MODE,

        /// <summary>
        /// Power Line Frequency control for anti-flickering Off/50Hz/60Hz/Auto
        /// </summary>
        POWER_LINE_FREQUENCY,

        /// <summary>
        /// Current Asic Temperature
        /// </summary>
        ASIC_TEMPERATURE,

        /// <summary>
        /// disable error handling
        /// </summary>
        ERROR_POLLING_ENABLED,

        /// <summary>
        /// Current Projector Temperature
        /// </summary>
        PROJECTOR_TEMPERATURE,

        /// <summary>
        /// Enable / disable trigger to be outputed from the camera to any external device on every depth frame
        /// </summary>
        OUTPUT_TRIGGER_ENABLED,

        /// <summary>
        /// Current Motion-Module Temperature
        /// </summary>
        MOTION_MODULE_TEMPERATURE,

        /// <summary>
        /// Number of meters represented by a single depth unit
        /// </summary>
        DEPTH_UNITS,

        /// <summary>
        /// Enable/Disable automatic correction of the motion data
        /// </summary>
        ENABLE_MOTION_CORRECTION,

        /// <summary>
        /// Allows sensor to dynamically ajust the frame rate depending on lighting conditions
        /// </summary>
        AUTO_EXPOSURE_PRIORITY,

        /// <summary>
        /// Color scheme for data visualization
        /// </summary>
        COLOR_SCHEME,

        /// <summary>
        /// Perform histogram equalization post-processing on the depth data
        /// </summary>
        HISTOGRAM_EQUALIZATION_ENABLED,

        /// <summary>
        /// Minimal distance to the target
        /// </summary>
        MIN_DISTANCE,

        /// <summary>
        /// Maximum distance to the target
        /// </summary>
        MAX_DISTANCE,

        /// <summary>
        /// Texture mapping stream unique ID
        /// </summary>
        TEXTURE_SOURCE,

        /// <summary>
        /// The 2D-filter effect. The specific interpretation is given within the context of the filter
        /// </summary>
        FILTER_MAGNITUDE,

        /// <summary>
        /// 2D-filter parameter controls the weight/radius for smoothing.
        /// </summary>
        FILTER_SMOOTH_ALPHA,

        /// <summary>
        /// 2D-filter range/validity threshold
        /// </summary>
        FILTER_SMOOTH_DELTA,

        /// <summary>
        /// Number of enumeration values. Not a valid input: intended to be used in for-loops.
        /// </summary>
        COUNT
    };
    #endregion

    #region Structs
    public unsafe struct Intrinsics
    {
        /// <summary> Width of the image in pixels </summary>
        public int width;

        /// <summary> Height of the image in pixels </summary>
        public int height;

        /// <summary> Horizontal coordinate of the principal point of the image, as a pixel offset from the left edge </summary>
        public float ppx;

        /// <summary> Vertical coordinate of the principal point of the image, as a pixel offset from the top edge </summary>
        public float ppy;

        /// <summary> Focal length of the image plane, as a multiple of pixel width </summary>
        public float fx;

        /// <summary> Focal length of the image plane, as a multiple of pixel height </summary>
        public float fy;

        /// <summary> Distortion model of the image </summary>
        public DistortionModel model;

        /// <summary> Distortion coefficients </summary>
        public fixed float coeffs[5];
    };

    public unsafe struct Extrinsics
    {
        /// <summary> Column-major 3x3 rotation matrix </summary>
        public fixed float rotation[9];

        /// <summary> Three-element translation vector, in meters </summary>
        public fixed float translation[3];
    };
    #endregion

    public struct SensorName
    {
        public const string COLOR = "RGB Camera";
        public const string STEREO = "Stereo Module";
    }

    internal class RS2Internals
    {
        #region Constants
        // HINT: update API version to currently used librealsense2
        private const int API_MAJOR_VERSION = 2;
        private const int API_MINOR_VERSION = 8;
        private const int API_PATCH_VERSION = 0;
        private const int API_BUILD_VERSION = 3;

        public const int ApiVersion = API_MAJOR_VERSION * 10000 + API_MINOR_VERSION * 100 + API_PATCH_VERSION;
        #endregion

        #region DLLImport
        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_create_context(int api_version, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_create_pipeline(IntPtr ctx, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_create_config(IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_config_disable_all_streams(IntPtr config, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_config(IntPtr config);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_pipeline(IntPtr pipe);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_context(IntPtr context);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_config_enable_device(IntPtr config, [MarshalAs(UnmanagedType.LPStr)] string serial, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static char* rs2_get_failed_function(IntPtr error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static char* rs2_get_failed_args(IntPtr error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static char* rs2_get_error_message(IntPtr error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_free_error(IntPtr error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_pipeline_start_with_config(IntPtr pipe, IntPtr config, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_pipeline_stop(IntPtr pipe, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_release_frame(IntPtr frame);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_pipeline_wait_for_frames(IntPtr pipe, uint timeout_ms, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_embedded_frames_count(IntPtr composite, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_extract_frame(IntPtr composite, int index, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_frame_add_ref(IntPtr frame, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_get_frame_stream_profile(IntPtr frame, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_get_stream_profile_data(IntPtr profile, ref Stream stream, ref Format format, ref int index, ref int unique_id, ref int framerate, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_config_enable_stream(IntPtr config, Stream stream, int index, int width, int height, Format format, int framerate, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_config_disable_stream(IntPtr config, Stream stream, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_get_frame_data(IntPtr frame, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_pipeline_get_active_profile(IntPtr pipe, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_pipeline_profile_get_device(IntPtr profile, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_query_sensors(IntPtr device, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_get_sensors_count(IntPtr sensor_list, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_create_sensor(IntPtr sensor_list, int index, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static float rs2_get_depth_scale(IntPtr sensor, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static char* rs2_get_sensor_info(IntPtr sensor, CameraInfo info, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_sensor(IntPtr sensor);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_sensor_list(IntPtr info_list);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_device(IntPtr device);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_pipeline_profile(IntPtr profile);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_load_json(IntPtr dev, [MarshalAs(UnmanagedType.LPStr)] string json_content, uint content_size, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_is_enabled(IntPtr dev, ref int enabled, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_toggle_advanced_mode(IntPtr dev, int enable, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_get_video_stream_intrinsics(IntPtr profile_from, Intrinsics* intrinsics, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_get_extrinsics(IntPtr profile_from, IntPtr profile_to, Extrinsics* extrin, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_is_option_read_only(IntPtr sensor, Option option, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_supports_option(IntPtr sensor, Option option, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static float rs2_get_option(IntPtr sensor, Option option, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_set_option(IntPtr sensor, Option option, float value, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_get_option_range(IntPtr sensor, Option option, ref float min, ref float max, ref float step, ref float def, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static char* rs2_get_option_description(IntPtr sensor, Option option, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static char* rs2_get_option_value_description(IntPtr sensor, Option option, float value, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_get_stream_profiles(IntPtr sensor, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_get_stream_profiles_count(IntPtr stream_profile_list, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_stream_profiles_list(IntPtr stream_profile_list);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static IntPtr rs2_get_stream_profile(IntPtr stream_profile_list, int index, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_delete_stream_profile(IntPtr stream_profile);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static void rs2_get_video_stream_resolution(IntPtr stream_profile, ref int width, ref int height, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_get_frame_width(IntPtr frame, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_get_frame_height(IntPtr frame, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static int rs2_config_can_resolve(IntPtr config, IntPtr pipe, IntPtr* error);

        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static char* rs2_get_device_info(IntPtr device, CameraInfo info, IntPtr* error);
        #endregion

        #region Static Methods
        unsafe public static void HandleError(IntPtr e)
        {
            if (e == IntPtr.Zero)
                return;

            char* msg = null;

            msg = rs2_get_failed_function(e);
            string functionName = Marshal.PtrToStringAnsi((IntPtr)msg);

            msg = rs2_get_error_message(e);
            string errorMsg = Marshal.PtrToStringAnsi((IntPtr)msg);

            msg = rs2_get_failed_args(e);
            string arguments = Marshal.PtrToStringAnsi((IntPtr)msg);

            rs2_free_error(e);

            throw new Exception($"Failed function: {functionName} Errormessage: {errorMsg} Arguments: {arguments}");
        }
        #endregion
    }


    #region RS2Objects
    public class RS2Context
    {
        public IntPtr Handle { get; private set; }

        public RS2Context(IntPtr p)
        {
            Handle = p;
        }

        public void Delete()
        {
            RS2Internals.rs2_delete_context(Handle);
        }

        unsafe public static RS2Context Create()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr ctx = RS2Internals.rs2_create_context(RS2Internals.ApiVersion, &error);
                RS2Internals.HandleError(error);

                return new RS2Context(ctx);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public class RS2Pipeline
    {
        public IntPtr Handle { get; private set; }

        public bool Running { get; set; }

        public unsafe float DepthScale
        {
            get
            {
                return this.GetActiveProfile().GetDevice().GetSensor(SensorName.STEREO).DepthScale;
            }
        }

        public RS2Pipeline(IntPtr p)
        {
            Running = false;
            Handle = p;
        }

        public void Delete()
        {
            RS2Internals.rs2_delete_pipeline(Handle);
        }

        unsafe public static RS2Pipeline Create(RS2Context ctx)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr pipe = RS2Internals.rs2_create_pipeline(ctx.Handle, &error);
                RS2Internals.HandleError(error);

                return new RS2Pipeline(pipe);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public bool Check(RS2Config conf)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                int res = RS2Internals.rs2_config_can_resolve(conf.Handle, this.Handle, &error);
                RS2Internals.HandleError(error);

                return (1 == res);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void Start(RS2Config conf)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_pipeline_start_with_config(this.Handle, conf.Handle, &error);
                RS2Internals.HandleError(error);

                Running = true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void Stop()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_pipeline_stop(this.Handle, &error);
                RS2Internals.HandleError(error);

                Running = false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public RS2Frame WaitForFrames(uint timeout)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr frameset = RS2Internals.rs2_pipeline_wait_for_frames(this.Handle, timeout, &error);
                RS2Internals.HandleError(error);
                return new RS2Frame(frameset);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public RS2StreamProfile GetActiveProfile()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr profile = RS2Internals.rs2_pipeline_get_active_profile(this.Handle, &error);
                RS2Internals.HandleError(error);

                return new RS2StreamProfile(profile);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public class RS2Device
    {
        public IntPtr Handle { get; private set; }

        unsafe public bool AdvancedModeEnabled
        {
            get
            {
                try
                {
                    IntPtr error = IntPtr.Zero;
                    int enabled = 0;
                    RS2Internals.rs2_is_enabled(this.Handle, ref enabled, &error);
                    RS2Internals.HandleError(error);

                    return enabled != 0;
                }
                catch (Exception)
                {
                    throw;
                }
            }

            set
            {
                try
                {
                    IntPtr error = IntPtr.Zero;
                    RS2Internals.rs2_toggle_advanced_mode(this.Handle, value ? 1 : 0, &error);
                    RS2Internals.HandleError(error);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public RS2Device(IntPtr p)
        {
            Handle = p;
        }

        unsafe public void Delete()
        {
            RS2Internals.rs2_delete_device(this.Handle);
        }

        unsafe public string GetInfo(CameraInfo info)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                char* res = RS2Internals.rs2_get_device_info(this.Handle, info, &error);
                RS2Internals.HandleError(error);

                return Marshal.PtrToStringAnsi((IntPtr)res);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public RS2Sensor GetSensor(string sensorName)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr sensor = IntPtr.Zero;

                IntPtr list = RS2Internals.rs2_query_sensors(this.Handle, &error);
                RS2Internals.HandleError(error);
                int sensorCount = RS2Internals.rs2_get_sensors_count(list, &error);
                RS2Internals.HandleError(error);

                for (int i = 0; i < sensorCount; i++)
                {
                    sensor = RS2Internals.rs2_create_sensor(list, i, &error);
                    RS2Internals.HandleError(error);

                    string info = this.GetInfo(CameraInfo.NAME);
                    RS2Internals.HandleError(error);

                    if (info == sensorName)
                    {
                        break;
                    }

                    RS2Internals.rs2_delete_sensor(sensor);
                }

                RS2Internals.rs2_delete_sensor_list(list);

                RS2Sensor sensor_obj = new RS2Sensor(sensor);
                if (!sensor_obj.IsValid())
                {
                    throw new Exception(string.Format("No sensor with the name {0} detected", sensorName));
                }

                return sensor_obj;
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void LoadAdvancedConfig(string config)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_load_json(this.Handle, config, (uint)config.ToCharArray().Length, &error);
                RS2Internals.HandleError(error);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public class RS2Sensor
    {
        public IntPtr Handle { get; private set; }

        unsafe public float DepthScale
        {
            get
            {
                try
                {
                    IntPtr error = IntPtr.Zero;
                    float scale = 0.0f;

                    scale = RS2Internals.rs2_get_depth_scale(this.Handle, &error);
                    RS2Internals.HandleError(error);

                    return scale;
                }
                catch (Exception)
                {
                    throw;
                }
            }

        }

        public RS2Sensor(IntPtr p)
        {
            Handle = p;
        }

        public void Delete()
        {
            RS2Internals.rs2_delete_sensor(Handle);
        }

        unsafe public RS2StreamProfilesList GetStreamProfileList()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr list = RS2Internals.rs2_get_stream_profiles(this.Handle, &error);
                RS2Internals.HandleError(error);

                return new RS2StreamProfilesList(list);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public List<Point2i> GetSupportedResolutions()
        {
            List<Point2i> res = new List<Point2i>();
            RS2StreamProfilesList list = this.GetStreamProfileList();
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                RS2StreamProfile profile = list.GetStreamProfile(i);
                Point2i resolution = profile.Resolution;
                if (!res.Contains(resolution))
                    res.Add(resolution);
            }

            return res;
        }

        unsafe public bool IsOptionSupported(Option option)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                int res = 0;

                res = RS2Internals.rs2_supports_option(this.Handle, option, &error);
                RS2Internals.HandleError(error);

                return (1 == res);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public bool IsOptionRealOnly(Option option)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                int res = 0;

                res = RS2Internals.rs2_is_option_read_only(this.Handle, option, &error);
                RS2Internals.HandleError(error);

                return (1 == res);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public float GetOption(Option option)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                float res = 0.0f;

                res = RS2Internals.rs2_get_option(this.Handle, option, &error);
                RS2Internals.HandleError(error);

                return res;
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void SetOption(Option option, float value)
        {
            try
            {
                IntPtr error = IntPtr.Zero;

                RS2Internals.rs2_set_option(this.Handle, option, value, &error);
                RS2Internals.HandleError(error);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void OptionInfo(Option option, out float min, out float max, out float step, out float def, out string desc)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                char* msg = null;

                min = 0;
                max = 0;
                step = 0;
                def = 0;

                RS2Internals.rs2_get_option_range(this.Handle, option, ref min, ref max, ref step, ref def, &error);
                RS2Internals.HandleError(error);

                msg = RS2Internals.rs2_get_option_description(this.Handle, option, &error);
                RS2Internals.HandleError(error);

                desc = Marshal.PtrToStringAnsi((IntPtr)msg);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public string OptionValueInfo(Option option, float value)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                char* msg = null;

                msg = RS2Internals.rs2_get_option_value_description(this.Handle, option, value, &error);
                RS2Internals.HandleError(error);

                return Marshal.PtrToStringAnsi((IntPtr)msg);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public List<int> GetSupportedFrameRates()
        {
            List<int> res = new List<int>();
            RS2StreamProfilesList list = this.GetStreamProfileList();
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                RS2StreamProfile profile = list.GetStreamProfile(i);
                profile.GetData(out Stream stream, out Format format, out int index, out int uid, out int framerate);
                if (!res.Contains(framerate))
                    res.Add(framerate);
            }

            return res;
        }

        public bool IsValid() => (IntPtr.Zero != Handle);
    }

    public class RS2Config
    {
        public IntPtr Handle { get; private set; }

        public RS2Config(IntPtr p)
        {
            Handle = p;
        }

        public void Delete()
        {
            RS2Internals.rs2_delete_config(Handle);
        }

        unsafe public static RS2Config Create()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr conf = RS2Internals.rs2_create_config(&error);
                RS2Internals.HandleError(error);

                return new RS2Config(conf);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void EnableStream(Stream stream, int index, int width, int height, Format format, int framerate)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_config_enable_stream(this.Handle, stream, index, width, height, format, framerate, &error);
                RS2Internals.HandleError(error);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void DisableStream(Stream stream)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_config_disable_stream(this.Handle, stream, &error);
                RS2Internals.HandleError(error);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void DisableAllStreams()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_config_disable_all_streams(this.Handle, &error);
                RS2Internals.HandleError(error);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void EnableDevice(string serial_number)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_config_enable_device(this.Handle, serial_number, &error);
                RS2Internals.HandleError(error);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public class RS2Frame : IDisposable
    {
        private bool _disposed = false;
        public IntPtr Handle { get; private set; }

        unsafe public int Width
        {
            get
            {
                try
                {
                    IntPtr error = IntPtr.Zero;
                    int width = RS2Internals.rs2_get_frame_width(Handle, &error);
                    RS2Internals.HandleError(error);
                    return width;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        unsafe public int Height
        {
            get
            {
                try
                {
                    IntPtr error = IntPtr.Zero;
                    int height = RS2Internals.rs2_get_frame_height(Handle, &error);
                    RS2Internals.HandleError(error);
                    return height;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        unsafe public int EmbeddedFrameCount
        {
            get
            {
                try
                {
                    IntPtr error = IntPtr.Zero;
                    int count = RS2Internals.rs2_embedded_frames_count(this.Handle, &error);
                    RS2Internals.HandleError(error);

                    return count;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            
        }

        public RS2Frame(IntPtr p)
        {
            Handle = p;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if(disposing)
            {
                // Free managed resources
            }


            // free unmanaged resources
            Release();
            _disposed = true;
        }

        unsafe public void Release()
        {
            if(this.Handle != IntPtr.Zero)
                RS2Internals.rs2_release_frame(this.Handle);
        }

        unsafe public RS2Frame Clone()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                RS2Internals.rs2_frame_add_ref(this.Handle, &error);
                RS2Internals.HandleError(error);

                return new RS2Frame(this.Handle);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public IntPtr GetData()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr data = RS2Internals.rs2_get_frame_data(this.Handle, &error);
                RS2Internals.HandleError(error);

                return data;
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public RS2Frame ExtractFrame(int index)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr extractedFramePtr = RS2Internals.rs2_extract_frame(this.Handle, index, &error);
                RS2Internals.HandleError(error);

                return new RS2Frame(extractedFramePtr);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public RS2StreamProfile GetStreamProfile()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr profilePtr = RS2Internals.rs2_get_frame_stream_profile(this.Handle, &error);
                RS2Internals.HandleError(error);

                return new RS2StreamProfile(profilePtr);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool IsValid() => (null != Handle);
    }

    public class RS2StreamProfilesList
    {
        public IntPtr Handle { get; private set; }

        unsafe public int Count
        {
            get
            {
                try
                {
                    IntPtr error = IntPtr.Zero;
                    int count = RS2Internals.rs2_get_stream_profiles_count(this.Handle, &error);
                    RS2Internals.HandleError(error);

                    return count;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            
        }

        public RS2StreamProfilesList(IntPtr p)
        {
            Handle = p;
        }

        public void Delete()
        {
            RS2Internals.rs2_delete_stream_profiles_list(Handle);
        }

        unsafe public RS2StreamProfile GetStreamProfile(int index)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr profilePtr = RS2Internals.rs2_get_stream_profile(this.Handle, index, &error);
                RS2Internals.HandleError(error);

                return new RS2StreamProfile(profilePtr);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public class RS2StreamProfile
    {
        public IntPtr Handle { get; private set; }

        unsafe public Point2i Resolution
        {
            get
            {
                try
                {
                    IntPtr error = IntPtr.Zero;

                    int width = 0;
                    int height = 0;
                    RS2Internals.rs2_get_video_stream_resolution(this.Handle, ref width, ref height, &error);
                    RS2Internals.HandleError(error);

                    return new Point2i(width, height);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public RS2StreamProfile(IntPtr p)
        {
            Handle = p;
        }

        public void Delete()
        {
            RS2Internals.rs2_delete_stream_profile(Handle);
        }

        unsafe public RS2Device GetDevice()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                IntPtr device = RS2Internals.rs2_pipeline_profile_get_device(this.Handle, &error);
                RS2Internals.HandleError(error);

                return new RS2Device(device);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public void GetData(out Stream stream, out Format format, out int index, out int uid, out int framerate)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                stream = Stream.ANY;
                format = Format.ANY;
                index = 0;
                uid = 0;
                framerate = 0;

                RS2Internals.rs2_get_stream_profile_data(this.Handle, ref stream, ref format, ref index, ref uid, ref framerate, &error);
                RS2Internals.HandleError(error);
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public Intrinsics GetIntrinsics()
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                Intrinsics intrinsics = new Intrinsics();
                RS2Internals.rs2_get_video_stream_intrinsics(this.Handle, &intrinsics, &error);
                RS2Internals.HandleError(error);

                return intrinsics;
            }
            catch (Exception)
            {
                throw;
            }
        }

        unsafe public Extrinsics GetExtrinsics(RS2StreamProfile to)
        {
            try
            {
                IntPtr error = IntPtr.Zero;
                Extrinsics extrinsics = new Extrinsics();
                RS2Internals.rs2_get_extrinsics(this.Handle, to.Handle, &extrinsics, &error);
                RS2Internals.HandleError(error);

                return extrinsics;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool IsValid() => (null != Handle);
    }
    #endregion
}
