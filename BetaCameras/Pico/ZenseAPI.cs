﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MetriCam2.Cameras.Pico.ZenseAPI
{
    public class Constants
    {
        public const int OFFSETLEN = 49;
        public const int AUDIOSIZE = 2048 * 4;
    }


    public enum DepthRange
    {
        NearRange = 0,
        MidRange = 1,
        FarRange = 2,
    }

    public enum PropertyType
    {
        DepthRange_Int32 = 0,
        SN_Str = 5,
        FWVer_Str = 6,
        HWVer_Str = 7,
        DataMode_UInt8 = 8
    }

    public enum FrameType
    {
        DepthFrame = 0,
        IRFrame = 1,
        GrayFrame = 2,
        RGBFrame = 3,
        DepthRGBFFrame = 4,
        RGBDepthFrame = 5
    }

    public enum SensorType
    {
        DepthSensor = 0x01,
        RgbSensor = 0x02,
    }

    public enum PixelFormat
    {
        DepthMM16,
        Gray16,
        Gray8,
        RGB888,
        BGR888
    }

    public enum ReturnStatus
    {
        OK = 0,
        NoDeviceConnected = -1,
        InvalidDeviceIndex = -2,
        DevicePointerIsNull = -3,
        InvalidFrameType = -4,
        FramePointerIsNull = -5,
        NoPropertyValueGet = -6,
        NoPropertyValueSet = -7,
        PropertyPointerIsNull = -8,
        PropertySizeNotEnough = -9,
        InvalidDepthRange = -10,
        ReadNextFrameError = -11,
        InputPointerIsNull = -12,
        CameraNotOpened = -13,
        InvalidCameraType = -14,
        InvalidParams = -15,
        Unknown = -255,
    }

    public enum DataMode
    {
        Depth_30 = 0,
        IR_30 = 1,
        DepthAndIR_30 = 2,
        Raw_30 = 3,
        NoCCD_30 = 4,
        Depth_60 = 5,
        IR_60 = 6,
        DepthAndIR_60 = 7,
        Raw_60 = 8,
        NoCCD_60 = 9,
        DepthAndIR_15 = 10,
    }

    public enum CameraParamsEnum
    {
        DepthIntrinsic = 0,
        DepthDistortion,
        RGBIntrinsic,
        RGBDistortion,
        Rotation,
        Transfer,
        E,
        F
    }

    public enum DepthMode
    {
        Mode0 = 0,
        Mode1 = 1,
        Mode2 = 2,
    }

    public enum WorkMode
    {
        User = 1,
        Factory = 2,
    }

    public enum FactoryPropertyType
    {
        PsFProperty = 0,
        HTP = 1,
        OFFSET = 2,
        MAXDEPTH = 3,
        CameraParams = 4,
        DataMode_UInt8 = 5,
        LDEnable_UInt8 = 6,
        ISAEnable_UInt8 = 7,
        TALEnable_UInt8 = 8,
        DepthMode_UInt8 = 9,
        PCBA_Str = 10,
        WorkMode_UInt8 = 11,
        IRS2Enable_UInt8 = 12,
        GMMGain_UInt16 = 13,
        GMMParams = 14,
        ACCSensitivity = 15,
        GyroSensitivity = 16,
        IMUDataMode = 17,
        RgbExpoTime_float = 18, //ms
        LensConfig = 19,
        PulseCountConfig = 20,
        ACCOffset = 21,
        GyroOffset = 22,
        ACCOffsetTemp = 23,
        GyroOffsetTemp = 24,
    }

    public enum IMUDataMode
    {
        RawData = 0,
        StandardData,
        CorrectData,
        CorrectFilterData
    }

    public enum FilterType
    {
        ComputeRealDepthFilter = 0,
        SmoothingGDFilter,
        SmoothingExpFilter,
    }

    public enum EncodeType
    {
        NV12 = 0x01,
        H264 = 0x02,
    }

    // Color image pixel type in 24-bit RGB format
    public struct RGB888Pixel
    {
        byte r;
        byte g;
        byte b;
    }

    // Color image pixel type in 24-bit BGR format
    public struct BGR888Pixel
    {
        byte b;
        byte g;
        byte r;
    }

    public struct FrameMode
    {
        PixelFormat pixelFormat;
        int resolutionWidth;
        int resolutionHeight;
        int fps;
    }

    public struct Vector3f
    {
        float x;
        float y;
        float z;
    }

    public struct DepthVector3
    {
        int depthX;
        int depthY;
        ushort depthZ;
    }

    public struct Imu
    {
        Vector3f acc;     //m/s^2
        Vector3f gyro;    //rad/s
        byte frameNo;
    }

    public struct ImuWithParams
    {
        Vector3f acc;     //m/s^2
        Vector3f gyro;    //rad/s
        float temp;       //Celsius temperature scale
        byte frameNo;
    }

    //Camera Intrinsic and distortion coefficient
    public struct CameraParameters
    {
        double fx;  // Focal length x (pixel)
        double fy;  // Focal length y (pixel)
        double cx;  // Principal point x (pixel)
        double cy;  // Principal point y (pixel)
        double k1;  // Radial distortion coefficient, 1st-order
        double k2;  // Radial distortion coefficient, 2nd-order
        double k3;  // Radial distortion coefficient, 3rd-order
        double p1;  // Tangential distortion coefficient
        double p2;	// Tangential distortion coefficient
    }

    public struct Frame
    {
        FrameType frameType;
        PixelFormat pixelFormat;
        byte imuFrameNo;     //Used to synchronize with imu
        IntPtr pFrameData;
        uint dataLen;
        float exposureTime;	//ms
    }

    public struct AudioFrame
    {
        byte audioFormat;    //0:pcm
        byte numChannels;    //1:mono; 2:stereo
        byte bitsPerSample;  //16bit
        uint sampleRate;        //16Khz	
        IntPtr pData;
        uint dataLen;
    };

    public unsafe struct Distortion
    {
        fixed double d[8];
        double reserved;
    }

    public unsafe struct Transfer
    {
        fixed double t[3];
        fixed double reserved[6];
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct Parameters
    {
        //| fx | 0  | cx |
        //| 0  | fy | cy |
        //| 0  | 0  | 1  |
        [FieldOffset(0)]
        fixed double intrinsic[9];
        [FieldOffset(0)]
        fixed double rotation[9];
        [FieldOffset(0)]
        fixed double e[9];
        [FieldOffset(0)]
        fixed double f[9];

        [FieldOffset(0)]
        Distortion distortion;
        [FieldOffset(0)]
        Transfer transfer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraParams
    {
        CameraParamsEnum type;
        Parameters param;
    }

    public struct HTP
    {
        byte depthMode;
        //Near
        ushort LD0_Near;
        ushort LD1_Near;
        ushort LD2_Near;
        ushort SUB0_Near;
        ushort SUB1_Near;
        ushort SUB2_Near;
        //Mid
        ushort LD0_Mid;
        ushort LD1_Mid;
        ushort LD2_Mid;
        ushort SUB0_Mid;
        ushort SUB1_Mid;
        ushort SUB2_Mid;
        //Far
        ushort LD0_Far;
        ushort LD1_Far;
        ushort LD2_Far;
        ushort SUB0_Far;
        ushort SUB1_Far;
        ushort SUB2_Far;
    }

    public unsafe struct OFFSET
    {
        byte depthMode;
        byte depthRange;
        fixed short offset[Constants.OFFSETLEN];
    }

    public struct MaxDepth
    {
        byte depthMode;
        ushort maxNear;
        ushort maxMid;
        ushort maxFar;
    }

    public unsafe struct GMMParams
    {
        fixed ushort param[12];
    }

    public unsafe struct IMUSensitivity
    {
        fixed float matrix[9];
    }

    public struct IMUOffset
    {
        byte sum; //[1, 10]
        byte index; //[1, 10]
        SByte temp;
        float x;
        float y;
        float z;
    }

    public struct IMUOffsetTemp
    {
        byte sum; //[1, 10]
        SByte temp1;
        SByte temp2;
        SByte temp3;
        SByte temp4;
        SByte temp5;
        SByte temp6;
        SByte temp7;
        SByte temp8;
        SByte temp9;
        SByte temp10;
    }

    public struct LensConfig
    {
        byte type;
        byte state;//0:OFF; 1:ON
        ushort width;
        ushort height;
        byte fps;
        byte encodeType;
    }

    public struct PulseCountConfig
    {
        ushort pulseCount;
        byte mode;
        byte range;
        byte option; // 0:To users, Effective immediately; 1:To factory calib
    }

    public struct PulseCountGet
    {
        ushort Current; //current mode,current range,
        ushort M0_Near;
        ushort M0_Mid;
        ushort M0_Far;
        ushort M1_Near;
        ushort M1_Mid;
        ushort M1_Far;
        ushort M2_Near;
        ushort M2_Mid;
        ushort M2_Far;
    }

    public class Methods
    {
        /*
        *  Initialize PicoZense SDK, should be called first before calling any other api
        *  @Parameters: None
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsInitialize", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus Initialize();


        /*
        *  Shutdown PicoZense SDK, release all resources the SDK created, it is forbiden to call any other api after PsShutdown is called
        *  @Parameters: None
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsShutdown", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus Shutdown();


        /*
        *  Get the count of all supported devices connected
        *  @Parameters: 
        *	pDeviceCount[Out]: pointer to the variable that used to store returned device count, you need to create an int32_t variable and pass its pointer to this api
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetDeviceCount", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetDeviceCount(out int deviceCount);


        /*
        *  Open the corresponding device indicated by the deviceIndex 
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsOpenDevice", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus OpenDevice(int deviceIndex);


        /*
        *  Close the corresponding device indicated by the deviceIndex
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsCloseDevice", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus CloseDevice(int deviceIndex);


        /*
        *  Start to capture the image frame indicated by frameType
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	frameType[In]: frame type, refer to PsFrameType
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsStartFrame", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus StartFrame(int deviceIndex, FrameType frameType);


        /*
        *  Stop to capture the image frame indicated by frameType
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	frameType[In]: frame type, refer to PsFrameType
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsStopFrame", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus StopFrame(int deviceIndex, FrameType frameType);


        /*
        *  Read frame from the corresponding device by this api, it should be called first before you want to get one frame data through PsGetFrame
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsReadNextFrame", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus ReadNextFrame(int deviceIndex);


        /*
        *  Get one frame data by this api, it should be called after PsReadNextFrame
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	frameType[In]: frame type, refer to PsFrameType
        *	ppFrame[Out]: pointer of pointer to frame buffer, you need to create a frame pointer variable and pass its pointer to this api 
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetFrame", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetFrame(int deviceIndex, FrameType frameType, out Frame pPsFrame);


        /*
        *  Get the depth range that the corresponding device currently used
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	pDepthRange[Out]: pointer to the variable that used to store returned depth range, you need to create a PsDepthRange variable and pass its pointer to this api
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetDepthRange", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetDepthRange(int deviceIndex, out DepthRange pDepthRange);


        /*
        *  Set the depth range to the corresponding device
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	depthRange[In]: the depth range mode, refer to PsDepthRange
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetDepthRange", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetDepthRange(int deviceIndex, DepthRange pDepthRange);


        /*
        *  Get the frame mode
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	frameType[In]: frame type, refer to PsFrameType
        *	pFrameMode[Out]: pointer to the variable that used to store returned frame mode, you need to create a PsFrameMode variable and pass its pointer to this api
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetFrameMode", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetFrameMode(int deviceIndex, FrameType frameType, out FrameMode pFrameMode);


        /*
        *  Set the frame mode
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	frameType[In]: frame type, refer to PsFrameType
        *	pFrameMode[In]: pointer to the variable that used to store frame mode, you need to create a PsFrameMode variable and pass its pointer to this api
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetFrameMode", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetFrameMode(int deviceIndex, FrameType frameType, FrameMode* pFrameMode);


        /*
        *  Get the corresponding property value
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	propertyType[In]: property type, refer to PsPropertyType
        *	pData[Out]: pointer to the buffer that used to store returned property value, you need to create a buffer and pass its pointer to this api
        *	pDataSize[Out]: pointer to the variable that used to store returned size in byte of returned property value, you need to create an int32_t variable and pass its pointer to this api
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetProperty", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetProperty(int deviceIndex, PropertyType propertyType, IntPtr pData, int* pDataSize);


        /*
        *  Set the corresponding property value
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	propertyType[In]: property type, refer to PsPropertyType
        *	pData[In]: pointer to the buffer which stored the property value to be set, you need to create a buffer and set the property value to this buffer, then pass its pointer to this api
        *	dataSize[In]: the property value size in byte
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetProperty", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetProperty(int deviceIndex, PropertyType propertyType, IntPtr pData, int pDataSize);


        /*
        *  Converts the input points from the World coordinate system to the Depth coordinate system
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	pWorldVector[In]: pointer to the buffer which stored the x,y,z value of world coordinate of the input points to be converted, measured in millimeters
        *	pDepthVect[Out]: pointer to the buffer to store the output x,y,z value of depth coordinate
        *	                 (x,y) is measured in pixels with (0,0) at the top left of the image
        *	                 z is measured in millimeters, same as the reference of PsPixelFormat of depth frame
        *	pointCount[In]: the point count to be converted
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsConvertWorldToDepth", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus ConvertWorldToDepth(int deviceIndex, Vector3f[] pWorldVector, DepthVector3[] pDepthVector, int pointCount);


        /*
        *  Converts the input points from the Depth coordinate system to the World coordinate system
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	pDepthVector[In]: pointer to the buffer which stored the x,y,z value of depth coordinate of the input points to be converted
        *	                  (x,y) is measured in pixels with (0,0) at the top left of the image
        *	                  z is measured in millimeters, same as the reference of PsPixelFormat of depth frame
        *	pWorldVector[Out]: pointer to the buffer to store the output x,y,z value of world coordinate, measured in millimeters
        *	pointCount[In]: the point count to be converted
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsConvertWorldToDepth", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus ConvertWorldToDepth(int deviceIndex, DepthVector3[] pDepthVector, Vector3f[] pWorldVector, int pointCount);


        /*
        *  Set to enable or disable the correction of depth feature
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	bEnabled [In]: true to enable the feature, false to disable the feature
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetRealDepthCorrectionEnabled", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetRealDepthCorrectionEnabled(int deviceIndex, bool bEnabled);


        /*  Set the threshold value
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	threshold[In]: the threshold value
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetThreshold", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetThreshold(int deviceIndex, ushort threshold);


        /*  Get the threshold value
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	pThreshold[Out]: the threshold value
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetThreshold", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetThreshold(int deviceIndex, out ushort threshold);


        /*
        *  Get the device pulse count
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	pPulseCount[out]: pointer to the variable that used to store returned pulse count
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetPulseCount", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetPulseCount(int deviceIndex, out ushort pPulseCount);


        /*
        *  Get imu data Sync (update rate 1000hz), 
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	imuV[out]: reference to the variable that used to store imu data
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetImu", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetImu(int deviceIndex, out Imu imu);


        [DllImport("picozense_api", EntryPoint = "PsGetImuWithParams", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetImuWithParams(int deviceIndex, out ImuWithParams imu);


        [DllImport("picozense_api", EntryPoint = "PsSetColorPixelFormat", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetColorPixelFormat(int deviceIndex, PixelFormat pixelFormat);


        /*
        *  Get camera intrinsic and distortion coefficient parameters
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	sensorType[In]: sensor type, indicate which sensor (depth or rgb) parameters to get, refer to PsSensorType
        *	pCameraParameters[out]: pointer to the PsCameraParameters structure variable that used to store returned camera parameters
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetCameraParameters", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetCameraParameters(int deviceIndex, SensorType sensorType, out CameraParameters pCameraParameters);


        /*
        *  write register value
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	addr[In]: register address
        *	val[Out]:register value
        *	regNum[In]: register number
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetRegVal", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetRegVal(int deviceIndex, ushort[] addr, ushort[] val, byte regNum);


        /*
        *  read register value
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	addr[In]: register address
        *	val[Out]:register value
        *	regNum[In]: register number
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsGetRegVal", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetRegVal(int deviceIndex, ushort[] addr, ushort[] val, byte regNum);


        /*
        *  Set to enable or disable the Filter feature
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	filterType[In]: filter type, refer to PsFilterType
        *	bEnabled [In]: true to enable the feature, false to disable the feature
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetFilter", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetFilter(int deviceIndex, FilterType filterType, bool bEnabled);


        /*
        *  Set to enable or disable the Audio Record feature
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	bEnabled [In]: true to enable the feature, false to disable the feature
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetAudioRecordEnabled", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetAudioRecordEnabled(int deviceIndex, bool bEnabled);


        [DllImport("picozense_api", EntryPoint = "PsGetAudio", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus GetAudio(int deviceIndex, out AudioFrame audio);


        /*
        *  Set to enable or disable the DepthDistortion feature
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	bEnabled [In]: true to enable the feature, false to disable the feature
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetDepthDistortionEnabled", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetDepthDistortionEnabled(int deviceIndex, bool bEnabled);


        /*
        *  Set to enable or disable the IrDistortion feature
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	bEnabled [In]: true to enable the feature, false to disable the feature
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsPsSetIrDistortionEnabled", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus PsSetIrDistortionEnabled(int deviceIndex, bool bEnabled);


        /*
        *  Set to enable or disable the RGBDistortion feature
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	bEnabled [In]: true to enable the feature, false to disable the feature
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetRGBDistortionEnabled", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetRGBDistortionEnabled(int deviceIndex, bool bEnabled);


        /*
        *  Set to enable or disable the Mapper feature
        *  @Parameters:
        *	deviceIndex[In]: the device index, its range is 0 to deviceCount-1
        *	bEnabled [In]: true to enable the feature, false to disable the feature
        *  @Return: PsReturnStatus value, PsRetOK: Succeed, Others: Failed
        */
        [DllImport("picozense_api", EntryPoint = "PsSetMapperEnabled", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public unsafe extern static ReturnStatus SetMapperEnabled(int deviceIndex, bool bEnabled);
    }
    
}
