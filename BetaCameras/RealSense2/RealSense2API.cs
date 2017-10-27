using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MetriCam2.Cameras
{
    class RealSense2API
    {
        // HINT: update API version to currently used librealsense2
        private const int API_MAJOR_VERSION = 2;
        private const int API_MINOR_VERSION = 8;
        private const int API_PATCH_VERSION = 0;
        private const int API_BUILD_VERSION = 0;

        private const int ApiVersion = API_MAJOR_VERSION * 10000 + API_MINOR_VERSION * 100 + API_PATCH_VERSION;

        public static bool PipelineRunning { get; private set; } = false;

        public enum Format
        {
            ANY,             /* When passed to enable stream, librealsense will try to provide best suited format */
            Z16,             /* 16-bit linear depth values. The depth is meters is equal to depth scale * pixel value. */
            DISPARITY16,     /* 16-bit linear disparity values. The depth in meters is equal to depth scale / pixel value. */
            XYZ32F,          /* 32-bit floating point 3D coordinates. */
            YUYV,            /* Standard YUV pixel format as described in https://en.wikipedia.org/wiki/YUV */
            RGB8,            /* 8-bit red, green and blue channels */
            BGR8,            /* 8-bit blue, green, and red channels -- suitable for OpenCV */
            RGBA8,           /* 8-bit red, green and blue channels + constant alpha channel equal to FF */
            BGRA8,           /* 8-bit blue, green, and red channels + constant alpha channel equal to FF */
            Y8,              /* 8-bit per-pixel grayscale image */
            Y16,             /* < 16-bit per-pixel grayscale image */
            RAW10,           /* Four 10-bit luminance values encoded into a 5-byte macropixel */
            RAW16,           /* 16-bit raw image */
            RAW8,            /* 8-bit raw image */
            UYVY,            /* Similar to the standard YUYV pixel format, but packed in a different order */
            MOTION_RAW,      /* Raw data from the motion sensor */
            MOTION_XYZ32F,   /* Motion data packed as 3 32-bit float values, for X, Y, and Z axis */
            GPIO_RAW,        /* Raw data from the external sensors hooked to one of the GPIO's */
            COUNT            /* Number of enumeration values. Not a valid input: intended to be used in for-loops. */
        }

        public enum Stream
        {
            ANY,
            DEPTH,       /* Native stream of depth data produced by RealSense device */
            COLOR,       /* Native stream of color data captured by RealSense device */
            INFRARED,    /* Native stream of infrared data captured by RealSense device */
            FISHEYE,     /* Native stream of fish-eye (wide) data captured from the dedicate motion camera */
            GYRO,        /* Native stream of gyroscope motion data produced by RealSense device */
            ACCEL,       /* Native stream of accelerometer motion data produced by RealSense device */
            GPIO,        /* Signals from external device connected through GPIO */
            COUNT
        }

        public enum CameraInfo
        {
            NAME,               /* Friendly name */
            SERIAL_NUMBER,      /* Device serial number */
            FIRMWARE_VERSION,   /* Primary firmware version */
            PHYSICAL_PORT,      /* Unique identifier of the port the device is connected to (platform specific) */
            DEBUG_OP_CODE,      /* If device supports firmware logging, this is the command to send to get logs from firmware */
            ADVANCED_MODE,      /* True iff the device is in advanced mode */
            PRODUCT_ID,         /* Product ID as reported in the USB descriptor */
            CAMERA_LOCKED,      /* True iff EEPROM is locked */
            COUNT               /* Number of enumeration values. Not a valid input: intended to be used in for-loops. */
        }

        public enum DistortionModel
        {
            NONE,                   /* Rectilinear images. No distortion compensation required. */
            MODIFIED_BROWN_CONRADY, /* Equivalent to Brown-Conrady distortion, except that tangential distortion is applied to radially distorted points */
            INVERSE_BROWN_CONRADY,  /* Equivalent to Brown-Conrady distortion, except undistorts image instead of distorting it */
            FTHETA,                 /* F-Theta fish-eye distortion model */
            BROWN_CONRADY,          /* Unmodified Brown-Conrady distortion model */
            COUNT,                  /* Number of enumeration values. Not a valid input: intended to be used in for-loops. */
        };

        public unsafe struct Intrinsics
        {
            public int width;              /* Width of the image in pixels */
            public int height;             /* Height of the image in pixels */
            public float ppx;              /* Horizontal coordinate of the principal point of the image, as a pixel offset from the left edge */
            public float ppy;              /* Vertical coordinate of the principal point of the image, as a pixel offset from the top edge */
            public float fx;               /* Focal length of the image plane, as a multiple of pixel width */
            public float fy;               /* Focal length of the image plane, as a multiple of pixel height */
            public DistortionModel model;  /* Distortion model of the image */
            public fixed float coeffs[5];  /* Distortion coefficients */
        };

        public unsafe struct Extrinsics
        {
            public fixed float rotation[9];    /* Column-major 3x3 rotation matrix */
            public fixed float translation[3]; /* Three-element translation vector, in meters */
        };

        #region DLLImport
        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_create_context(int api_version, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_create_pipeline(IntPtr ctx, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_create_config(IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_config_disable_all_streams(IntPtr config, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_delete_config(IntPtr config);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_delete_pipeline(IntPtr pipe);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_delete_context(IntPtr context);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_config_enable_device(IntPtr config, [MarshalAs(UnmanagedType.LPStr)] string serial, IntPtr* error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static char* rs2_get_failed_function(IntPtr error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static char* rs2_get_failed_args(IntPtr error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static char* rs2_get_error_message(IntPtr error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_free_error(IntPtr error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_pipeline_start_with_config(IntPtr pipe, IntPtr config, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_pipeline_stop(IntPtr pipe, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_release_frame(IntPtr frame);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_pipeline_wait_for_frames(IntPtr pipe, uint timeout_ms, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int rs2_embedded_frames_count(IntPtr composite, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_extract_frame(IntPtr composite, int index, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_frame_add_ref(IntPtr frame, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_get_frame_stream_profile(IntPtr frame, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_get_stream_profile_data(IntPtr profile, ref Stream stream, ref Format format, ref int index, ref int unique_id, ref int framerate, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_config_enable_stream(IntPtr config, Stream stream, int index, int width, int height, Format format, int framerate, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_config_disable_stream(IntPtr config, Stream stream, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_get_frame_data(IntPtr frame, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_pipeline_get_active_profile(IntPtr pipe, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_pipeline_profile_get_device(IntPtr profile, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_query_sensors(IntPtr device, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int rs2_get_sensors_count(IntPtr sensor_list, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static IntPtr rs2_create_sensor(IntPtr sensor_list, int index, IntPtr* error);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static float rs2_get_depth_scale(IntPtr sensor, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static char* rs2_get_sensor_info(IntPtr sensor, CameraInfo info, IntPtr* error);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_delete_sensor(IntPtr sensor);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_delete_sensor_list(IntPtr info_list);


        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_delete_device(IntPtr device);

        [DllImport("realsense2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_delete_pipeline_profile(IntPtr profile);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_load_json(IntPtr dev, [MarshalAs(UnmanagedType.LPStr)] string json_content, uint content_size, IntPtr* error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_is_enabled(IntPtr dev, ref int enabled, IntPtr* error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_toggle_advanced_mode(IntPtr dev, int enable, IntPtr* error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_get_video_stream_intrinsics(IntPtr profile_from, Intrinsics* intrinsics, IntPtr* error);


        [DllImport("realsense2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void rs2_get_extrinsics(IntPtr profile_from, IntPtr profile_to, Extrinsics* extrin, IntPtr* error);
        #endregion

        public struct RS2Context
        {
            public IntPtr Handle { get; private set; }

            public RS2Context(IntPtr p)
            {
                Handle = p;
            }
        }

        public struct RS2Pipeline
        {
            public IntPtr Handle { get; private set; }

            public RS2Pipeline(IntPtr p)
            {
                Handle = p;
            }
        }

        public struct RS2Device
        {
            public IntPtr Handle { get; private set; }

            public RS2Device(IntPtr p)
            {
                Handle = p;
            }
        }

        public struct RS2Config
        {
            public IntPtr Handle { get; private set; }

            public RS2Config(IntPtr p)
            {
                Handle = p;
            }
        }

        public struct RS2Frame
        {
            public IntPtr Handle { get; private set; }

            public RS2Frame(IntPtr p)
            {
                Handle = p;
            }

            public bool IsValid() => (null != Handle);
        }

        public struct RS2StreamProfile
        {
            public IntPtr Handle { get; private set; }

            public RS2StreamProfile(IntPtr p)
            {
                Handle = p;
            }
        }

        unsafe public static RS2Context CreateContext()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ctx = rs2_create_context(ApiVersion, &error);
            HandleError(error);

            return new RS2Context(ctx);
        }

        unsafe public static RS2Pipeline CreatePipeline(RS2Context ctx)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr pipe = rs2_create_pipeline(ctx.Handle, &error);
            HandleError(error);

            return new RS2Pipeline(pipe);
        }

        unsafe public static RS2Config CreateConfig()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr conf = rs2_create_config(&error);
            HandleError(error);

            return new RS2Config(conf);
        }

        unsafe public static void DisableAllStreams(RS2Config conf)
        {
            IntPtr error = IntPtr.Zero;
            rs2_config_disable_all_streams(conf.Handle, &error);
            HandleError(error);
        }

        unsafe public static void DeleteConfig(RS2Config conf)
        {
            rs2_delete_config(conf.Handle);
        }

        unsafe public static void DeletePipeline(RS2Pipeline pipe)
        {
            rs2_delete_pipeline(pipe.Handle);
        }

        unsafe public static void DeleteContext(RS2Context ctx)
        {
            rs2_delete_context(ctx.Handle);
        }

        unsafe public static void EnableDevice(RS2Config conf, string serial_number)
        {
            IntPtr error = IntPtr.Zero;
            rs2_config_enable_device(conf.Handle, serial_number, &error);
            HandleError(error);
        }

        unsafe public static void PipelineStart(RS2Pipeline pipe, RS2Config conf)
        {
            IntPtr error = IntPtr.Zero;
            rs2_pipeline_start_with_config(pipe.Handle, conf.Handle, &error);
            HandleError(error);

            PipelineRunning = true;
        }

        unsafe public static void PipelineStop(RS2Pipeline pipe)
        {
            IntPtr error = IntPtr.Zero;
            rs2_pipeline_stop(pipe.Handle, &error);
            HandleError(error);

            PipelineRunning = false;
        }

        unsafe public static void ReleaseFrame(RS2Frame frame)
        {
            rs2_release_frame(frame.Handle);
        }

        unsafe public static RS2Frame PipelineWaitForFrames(RS2Pipeline pipe, uint timeout)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr frameset = rs2_pipeline_wait_for_frames(pipe.Handle, timeout, &error);
            HandleError(error);

            return new RS2Frame(frameset);
        }

        unsafe public static void FrameAddRef(RS2Frame frame)
        {
            IntPtr error = IntPtr.Zero;
            rs2_frame_add_ref(frame.Handle, &error);
            HandleError(error);
        }

        unsafe public static int FrameEmbeddedCount(RS2Frame frame)
        {
            IntPtr error = IntPtr.Zero;
            int count = rs2_embedded_frames_count(frame.Handle, &error);
            HandleError(error);

            return count;
        }

        unsafe public static RS2Frame FrameExtract(RS2Frame frame, int index)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr extractedFramePtr = rs2_extract_frame(frame.Handle, index, &error);
            HandleError(error);

            return new RS2Frame(extractedFramePtr);
        }

        unsafe public static RS2StreamProfile GetStreamProfile(RS2Frame frame)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr profilePtr = rs2_get_frame_stream_profile(frame.Handle, &error);
            HandleError(error);

            return new RS2StreamProfile(profilePtr);
        }

        unsafe public static void GetStreamProfileData(RS2StreamProfile profile, out Stream stream, out Format format, out int index, out int uid, out int framerate)
        {
            IntPtr error = IntPtr.Zero;
            stream = Stream.ANY;
            format = Format.ANY;
            index = 0;
            uid = 0;
            framerate = 0;

            rs2_get_stream_profile_data(profile.Handle, ref stream, ref format, ref index, ref uid, ref framerate, &error);
            HandleError(error);
        }

        unsafe public static void ConfigEnableStream(RS2Config conf, Stream stream, int index, int width, int height, Format format, int framerate)
        {
            IntPtr error = IntPtr.Zero;
            rs2_config_enable_stream(conf.Handle, stream, index, width, height, format, framerate, &error);
            HandleError(error);
        }

        unsafe public static void ConfigDisableStream(RS2Config conf, Stream stream)
        {
            IntPtr error = IntPtr.Zero;
            rs2_config_disable_stream(conf.Handle, stream, &error);
            HandleError(error);
        }

        unsafe public static IntPtr GetFrameData(RS2Frame frame)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr data = rs2_get_frame_data(frame.Handle, &error);
            HandleError(error);

            return data;
        }

        unsafe public static RS2Device GetActiveDevice(RS2Pipeline pipe)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr profile = rs2_pipeline_get_active_profile(pipe.Handle, &error);
            HandleError(error);
            IntPtr device = rs2_pipeline_profile_get_device(profile, &error);
            HandleError(error);

            rs2_delete_pipeline_profile(profile);
            return new RS2Device(device);
        }

        unsafe public static void DeleteDevice(RS2Device device)
        {
            rs2_delete_device(device.Handle);
        }

        unsafe public static bool AdvancedModeEnabled(RS2Device device)
        {
            IntPtr error = IntPtr.Zero;
            int enabled = 0;
            rs2_is_enabled(device.Handle, ref enabled, &error);
            HandleError(error);

            return enabled != 0;
        }

        unsafe public static void EnabledAdvancedMode(RS2Device device, bool enable)
        {
            IntPtr error = IntPtr.Zero;
            rs2_toggle_advanced_mode(device.Handle, enable ? 1 : 0, &error);
            HandleError(error);
        }

        unsafe public static void LoadAdvancedConfig(string config, RS2Device device)
        {
            IntPtr error = IntPtr.Zero;
            rs2_load_json(device.Handle, config, (uint)config.ToCharArray().Length, &error);
            HandleError(error);
        }

        unsafe public static Intrinsics GetIntrinsics(RS2StreamProfile profile)
        {
            IntPtr error = IntPtr.Zero;
            Intrinsics intrinsics = new Intrinsics();
            rs2_get_video_stream_intrinsics(profile.Handle, &intrinsics, &error);
            HandleError(error);

            return intrinsics;
        }

        unsafe public static Extrinsics GetExtrinsics(RS2StreamProfile from, RS2StreamProfile to)
        {
            IntPtr error = IntPtr.Zero;
            Extrinsics extrinsics = new Extrinsics();
            rs2_get_extrinsics(from.Handle, to.Handle, &extrinsics, &error);
            HandleError(error);

            return extrinsics;
        }

        unsafe public static float GetDepthScale(RS2Pipeline pipe)
        {
            IntPtr error = IntPtr.Zero;
            float res = 0.0f;

            RS2Device dev = GetActiveDevice(pipe);
            IntPtr list = rs2_query_sensors(dev.Handle, &error);
            HandleError(error);
            int sensorCount = rs2_get_sensors_count(list, &error);
            HandleError(error);

            for (int i = 0; i < sensorCount; i++)
            {
                IntPtr sensor = rs2_create_sensor(list, i, &error);
                HandleError(error);

                float scale = rs2_get_depth_scale(sensor, &error);
                HandleError(error);

                char* info = rs2_get_sensor_info(sensor, CameraInfo.NAME, &error);
                string infoString = Marshal.PtrToStringAnsi((IntPtr)info);
                HandleError(error);

                rs2_delete_sensor(sensor);

                if (infoString == "Stereo Module")
                {
                    res = scale;
                    break;
                }
            }

            rs2_delete_sensor_list(list);
            DeleteDevice(dev);

            return res;
        }

        unsafe public static string GetFirmwareVersion(RS2Pipeline pipe)
        {
            IntPtr error = IntPtr.Zero;

            RS2Device dev = GetActiveDevice(pipe);
            IntPtr list = rs2_query_sensors(dev.Handle, &error);
            HandleError(error);
            int sensorCount = rs2_get_sensors_count(list, &error);
            HandleError(error);

            if (sensorCount == 0)
                throw new InvalidOperationException("No sensor detected to get firmware version from");


            IntPtr sensor = rs2_create_sensor(list, 0, &error);
            HandleError(error);

            char* info = rs2_get_sensor_info(sensor, CameraInfo.FIRMWARE_VERSION, &error);
            string infoString = Marshal.PtrToStringAnsi((IntPtr)info);
            HandleError(error);

            rs2_delete_sensor_list(list);
            DeleteDevice(dev);

            return infoString;
        }

        unsafe private static void HandleError(IntPtr e)
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

            throw new Exception($"Failed function: {functionName}\nErrormessage: {errorMsg}\nArguments: {arguments}");
        }
    }
}
