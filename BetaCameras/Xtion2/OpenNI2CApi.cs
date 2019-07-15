using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Metrilus.Util;
using System.Drawing;
using System.Drawing.Imaging;

namespace MetriCam2.Cameras
{
    // Basic C# wrapper around the OpenNI2 C-API using PInvoke
    public class OpenNI2CApi
    {
        #region constants
        private const int ONI_VERSION_MAJOR = 2;
        private const int ONI_VERSION_MINOR = 2;
        private const int ONI_API_VERSION = ONI_VERSION_MAJOR * 1000 + ONI_VERSION_MINOR;

        private const int ONI_MAX_STR = 256;
        private const int ONI_MAX_SENSORS = 10;
        private const int TRUE = 1;
        private const int FALSE = 0;
        private static IntPtr ANY_DEVICE = IntPtr.Zero;
        #endregion constants

        #region enums
        public enum OniStatus
        {
            OK = 0,
            ERROR = 1,
            NOT_IMPLEMENTED = 2,
            NOT_SUPPORTED = 3,
            BAD_PARAMETER = 4,
            OUT_OF_FLOW = 5,
            NO_DEVICE = 6,
            TIME_OUT = 102,
        };

        public enum OniSensorType
        {
            IR = 1,
            COLOR = 2,
            DEPTH = 3,
        };

        public enum OniPixelFormat
        {
            // Depth
            DEPTH_1_MM = 100,
            DEPTH_100_UM = 101,
            SHIFT_9_2 = 102,
            SHIFT_9_3 = 103,

            // Color
            RGB888 = 200,
            YUV422 = 201,
            GRAY8 = 202,
            GRAY16 = 203,
            JPEG = 204,
            YUYV = 205,
        };

        public enum OniDeviceState
        {
            OK = 0,
            ERROR = 1,
            NOT_READY = 2,
            EOF = 3
        };

        public enum OniImageRegistrationMode
        {
            OFF = 0,
            DEPTH_TO_COLOR = 1,
        };
        #endregion enums

        #region structs
        public struct OniVersion
        {
            public int major;
            public int minor;
            public int maintenance;
            public int build;
        };

        public struct OniVideoMode
        {
            public OniPixelFormat pixelFormat;
            public int resolutionX;
            public int resolutionY;
            public int fps;
        };

        public unsafe struct OniSensorInfo
        {
            public OniSensorType sensorType;
            public int numSupportedVideoModes;
            public OniVideoMode* pSupportedVideoModes;
        };

        public unsafe struct OniDeviceInfo
        {
            public fixed char uri[ONI_MAX_STR];
            public fixed char vendor[ONI_MAX_STR];
            public fixed char name[ONI_MAX_STR];
            public ushort usbVendorId;
            public ushort usbProductId;
        };

        public unsafe struct OniFrame
        {
            public int dataSize;
            public void* data;

            public OniSensorType sensorType;
            public ulong timestamp;
            public int frameIndex;

            public int width;
            public int height;

            public OniVideoMode videoMode;
            public int croppingEnabled;
            public int cropOriginX;
            public int cropOriginY;

            public int stride;
        };
        #endregion

        #region DLLImport
        [DllImport("OpenNI2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniInitialize(int apiVersion);


        [DllImport("OpenNI2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void oniShutdown();


        [DllImport("OpenNI2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniGetDeviceList(OniDeviceInfo** pDevices, ref int pNumDevices);


        [DllImport("OpenNI2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniReleaseDeviceList(IntPtr pDevices);


        [DllImport("OpenNI2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniWaitForAnyStream(IntPtr pStreams, int numStreams, ref int pStreamIndex, int timeout);


        [DllImport("OpenNI2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniVersion oniGetVersion();


        [DllImport("OpenNI2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static char* oniGetExtendedError();


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceOpen([MarshalAs(UnmanagedType.LPStr)] string uri, IntPtr* pDevice);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceClose(IntPtr device);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniSensorInfo* oniDeviceGetSensorInfo(IntPtr device, OniSensorType sensorType);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceGetInfo(IntPtr device, OniDeviceInfo* pInfo);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceCreateStream(IntPtr device, OniSensorType sensorType, IntPtr* pStreamHandle);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceEnableDepthColorSync(IntPtr device);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void oniDeviceDisableDepthColorSync(IntPtr device);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int oniDeviceGetDepthColorSyncEnabled(IntPtr device);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceSetProperty(IntPtr device, int propertyId, void* data, int dataSize);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceGetProperty(IntPtr device, int propertyId, void* data, ref int pDataSize);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int oniDeviceIsPropertySupported(IntPtr device, int propertyId);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniDeviceInvoke(IntPtr device, int commandId, void* data, int dataSize);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int oniDeviceIsCommandSupported(IntPtr device, int commandId);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int oniDeviceIsImageRegistrationModeSupported(IntPtr device, OniImageRegistrationMode mode);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void oniStreamDestroy(IntPtr stream);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniSensorInfo* oniStreamGetSensorInfo(IntPtr stream);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniStreamStart(IntPtr streamHandle);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void oniStreamStop(IntPtr streamHandle);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniStreamReadFrame(IntPtr streamHandle, OniFrame** pFrame);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniStreamSetProperty(IntPtr streamHandle, int propertyId, void* data, int dataSize);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniStreamGetProperty(IntPtr streamHandle, int propertyId, void* data, int* pDataSize);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int oniStreamIsPropertySupported(IntPtr streamHandle, int propertyId);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniStreamInvoke(IntPtr streamHandle, int commandId, void* data, int dataSize);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int oniStreamIsCommandSupported(IntPtr streamHandle, int commandId);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void oniFrameAddRef(OniFrame* pFrame);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static void oniFrameRelease(OniFrame* pFrame);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniSetLogOutputFolder([MarshalAs(UnmanagedType.LPStr)] string strOutputFolder);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniSetLogConsoleOutput(int bConsoleOutput);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static OniStatus oniSetLogFileOutput(int bFileOutput);


        [DllImport("OpenNI2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe extern static int oniFormatBytesPerPixel(OniPixelFormat format);
        #endregion

        #region public functions

        public static void Init()
        {
            OniStatus status = oniInitialize(ONI_API_VERSION);
            HandleError(status);
        }

        public static void Shutdown()
        {
            oniShutdown();
        }

        unsafe public static List<DeviceInfo> GetDeviceList()
        {
            int deviceCount = -1;
            OniDeviceInfo* deviceList = (OniDeviceInfo*)IntPtr.Zero;
            OniStatus status = oniGetDeviceList(&deviceList, ref deviceCount);
            HandleError(status);

            List<DeviceInfo> list = new List<DeviceInfo>();
            for (int i = 0; i < deviceCount; i++)
            {
                DeviceInfo tmpInfo = new DeviceInfo();
                OniDeviceInfo deviceInfo = deviceList[i];
                tmpInfo.uri = Marshal.PtrToStringAnsi((IntPtr)deviceInfo.uri);
                tmpInfo.name = Marshal.PtrToStringAnsi((IntPtr)deviceInfo.name);
                tmpInfo.vendor = Marshal.PtrToStringAnsi((IntPtr)deviceInfo.vendor);
                tmpInfo.usbProductId = deviceInfo.usbProductId;
                tmpInfo.usbVendorId = deviceInfo.usbVendorId;
                list.Add(tmpInfo);
            }

            oniReleaseDeviceList((IntPtr)deviceList);

            return list;
        }

        unsafe public static Device OpenDevice(string URI)
        {
            IntPtr deviceHandle = IntPtr.Zero;
            OniStatus status = oniDeviceOpen(URI, &deviceHandle);
            HandleError(status);

            return new Device(deviceHandle);
        }

        unsafe public static void CloseDevice(Device dev)
        {
            oniDeviceClose(dev.Handle);
        }

        unsafe public static Stream CreateStream(Device dev, OniSensorType type)
        {
            IntPtr streamHandle = IntPtr.Zero;
            OniStatus status = oniDeviceCreateStream(dev.Handle, type, &streamHandle);
            HandleError(status);

            return new Stream(streamHandle);
        }

        unsafe public static void DestroyStream(Stream stream)
        {
            oniStreamDestroy(stream.Handle);
        }

        unsafe public static void StartStream(Stream stream)
        {
            OniStatus status = oniStreamStart(stream.Handle);
            HandleError(status);
        }

        unsafe public static void StopStream(Stream stream)
        {
            oniStreamStop(stream.Handle);
        }

        unsafe public static Frame ReadFrame(Stream stream)
        {
            OniFrame* frameHandle = (OniFrame*)IntPtr.Zero;
            OniStatus status = oniStreamReadFrame(stream.Handle, &frameHandle);
            HandleError(status);

            return new Frame((IntPtr)frameHandle);
        }

        unsafe public static void ReleaseFrame(Frame frame)
        {
            if(frame.IsValid())
                oniFrameRelease((OniFrame*)frame.Handle);
        }

        unsafe public static FloatImage FrameToFloatImage(Frame frame)
        {
            OniFrame* frameHandle = (OniFrame*)frame.Handle;
            int width = (*frameHandle).width;
            int height = (*frameHandle).height;
            FloatImage img = new FloatImage(width, height);

            short* source = (short*)(*frameHandle).data;
            for (int y = 0; y < height; y++)
            {
                short* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    img[y, x] = (float)(*sourceLine++);
                }
            }

            return img;
        }

        unsafe public static ColorImage FrameToColorImage(Frame frame)
        {
            OniFrame* frameHandle = (OniFrame*)frame.Handle;
            int width = (*frameHandle).width;
            int height = (*frameHandle).height;


            int bytePerPixel = oniFormatBytesPerPixel((*frameHandle).videoMode.pixelFormat);

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle imageRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte* source = (byte*)(*frameHandle).data;
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
            ColorImage image = new ColorImage(bitmap);

            return image;
        }
        #endregion

        public struct DeviceInfo
        {
            public string uri;
            public string vendor;
            public string name;
            public ushort usbVendorId;
            public ushort usbProductId;
        }

        public struct Device
        {
            public IntPtr Handle { get; private set; }

            public Device(IntPtr handle)
            {
                Handle = handle;
            }
        }

        public struct Stream
        {
            public IntPtr Handle { get; private set; }

            public Stream(IntPtr handle)
            {
                Handle = handle;
            }
        }

        public struct Frame
        {
            public IntPtr Handle { get; private set; }

            public Frame(IntPtr handle)
            {
                Handle = handle;
            }

            public bool IsValid() => (IntPtr.Zero != Handle);
        }

        unsafe private static void HandleError(OniStatus status)
        {
            if (status == OniStatus.OK)
                return;

            char* msg = oniGetExtendedError();
            string errorMsg = Marshal.PtrToStringAnsi((IntPtr)msg);

            throw new Exception(errorMsg);
        }

    }
}
