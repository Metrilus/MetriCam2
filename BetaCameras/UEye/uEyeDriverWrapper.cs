#pragma warning disable 0169
#pragma warning disable 0649
//==========================================================================//
//                                                                          //
//	C# - Interfaceclass for uEye Camera family			    //
//                                                                          //
//  Copyright (C) 2005 - 2011                                               //
//	IDS - Imaging Development Systems GmbH				    //
//  Dimbacher Str. 6-8                                                      //
//  D-74182 Obersulm-Willsbach                                              //
//                                                                          //
//  The information in this document is subject to change without           //
//  notice and should not be construed as a commitment by IDS Imaging GmbH. //
//  IDS Imaging GmbH does not assume any responsibility for any errors      //
//  that may appear in this document.                                       //
//                                                                          //
//  This document, or source code, is provided solely as an example         //
//  of how to utilize IDS software libraries in a sample application.       //
//  IDS Imaging GmbH does not assume any responsibility for the use or      //
//  reliability of any portion of this document or the described software.  //
//                                                                          //
//  General permission to copy or modify, but not for profit, is hereby     //
//  granted,  provided that the above copyright notice is included and      //
//  included and reference made to the fact that reproduction privileges    //
//  were granted by IDS Imaging GmbH.                                       //
//                                                                          //
//  IDS cannot assume any responsibility for the use, or misuse, of any     //
//  portion or misuse, of any portion of this software for other than its   //
//  intended diagnostic purpose in calibrating and testing IDS manufactured //
//  image processing boards and software.                                   //
//                                                                          //
//==========================================================================//


using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MetriCam2.Cameras.Internal.uEye
{
    /// <summary>
    /// Internal wrapper for the IDS uEye driver API.
    /// </summary>
    internal class uEyeDriverWrapper
    {
        #region Driver name
        public const string DRIVER_DLL_NAME = "ueye_api_64.dll";
        #endregion

        #region API Constants, Structures, Enumeration and Exports
        #region Constants
        #region Color modes
        public const int IS_COLORMODE_INVALID = 0;
        public const int IS_COLORMODE_MONOCHROME = 1;
        public const int IS_COLORMODE_BAYER = 2;
        public const int IS_COLORMODE_CBYCRY = 4;
        #endregion

        #region Sensor Types
        public const int IS_SENSOR_INVALID = 0x0;

        // CMOS Sensors
        public const int IS_SENSOR_UI141X_M = 0x1;          // VGA rolling shutter - VGA monochrome
        public const int IS_SENSOR_UI141X_C = 0x2;          // VGA rolling shutter - VGA color
        public const int IS_SENSOR_UI144X_M = 0x3;          // SXGA rolling shutter - SXGA monochrome
        public const int IS_SENSOR_UI144X_C = 0x4;          // SXGA rolling shutter - SXGA color

        public const int IS_SENSOR_UI145X_C = 0x8;          // UXGA rolling shutter - UXGA color
        public const int IS_SENSOR_UI146X_C = 0xA;          // QXGA rolling shutter - QXGA color
        public const int IS_SENSOR_UI148X_M = 0xB;          // 5MP rolling shutter, monochrome
        public const int IS_SENSOR_UI148X_C = 0xC;          // 5MP rolling shutter, color
        public const int IS_SENSOR_UI149X_M = 0x3E;         // 10MP rolling shutter, monochrome
        public const int IS_SENSOR_UI149X_C = 0x3F;         // 10MP rolling shutter, color

        public const int IS_SENSOR_UI121X_M = 0x10;         // VGA global shutter - VGA monochrome
        public const int IS_SENSOR_UI121X_C = 0x11;         // VGA global shutter - VGA color
        public const int IS_SENSOR_UI122X_M = 0x12;         // VGA global shutter - VGA monochrome
        public const int IS_SENSOR_UI122X_C = 0x13;         // VGA global shutter - VGA color

        public const int IS_SENSOR_UI164X_C = 0x15;         // SXGA rolling shutter - SXGA color
        public const int IS_SENSOR_UI155X_C = 0x17;         // UXGA rolling shutter - UXGA color

        public const int IS_SENSOR_UI1225_M = 0x22;         // WVGA global shutter - WVGA monochrome, LE model
        public const int IS_SENSOR_UI1225_C = 0x23;         // WVGA global shutter - WVGA color, LE model

        public const int IS_SENSOR_UI1645_C = 0x25;         // SXGA rolling shutter - SXGA color, LE model
        public const int IS_SENSOR_UI1555_C = 0x27;         // UXGA rolling shutter - SXGA color, LE model
        public const int IS_SENSOR_UI1545_M = 0x28;         // SXGA rolling shutter, monochrome, LE model
        public const int IS_SENSOR_UI1545_C = 0x29;         // SXGA rolling shutter, color, LE model
        public const int IS_SENSOR_UI1455_C = 0x2B;         // UXGA rolling shutter, color, LE model
        public const int IS_SENSOR_UI1465_C = 0x2D;         // QXGA rolling shutter, color, LE model
        public const int IS_SENSOR_UI1485_M = 0x2E;         // 5MP rolling shutter, monochrome, LE model
        public const int IS_SENSOR_UI1485_C = 0x2F;         // 5MP rolling shutter, color, LE model
        public const int IS_SENSOR_UI1495_M = 0x40;         // 10MP rolling shutter, monochrome, LE model
        public const int IS_SENSOR_UI1495_C = 0x41;         // 10MP rolling shutter, color, LE model

        public const int IS_SENSOR_UI112X_M = 0x4A;         // 0768x576, HDR sensor, monochrome
        public const int IS_SENSOR_UI112X_C = 0x4B;         // 0768x576, HDR sensor, color

        public const int IS_SENSOR_UI1008_M = 0x4C;
        public const int IS_SENSOR_UI1008_C = 0x4D;

        public const int IS_SENSOR_UI1240_M = 0x50;         // SXGA global shutter, monochrome
        public const int IS_SENSOR_UI1240_C = 0x51;         // SXGA global shutter, color

        public const int IS_SENSOR_UI154X_M = 0x30;         // SXGA rolling shutter - SXGA monochrome
        public const int IS_SENSOR_UI154X_C = 0x31;         // SXGA rolling shutter - SXGA color
        public const int IS_SENSOR_UI1543_M = 0x32;         // SXGA rolling shutter - SXGA monochrome
        public const int IS_SENSOR_UI1543_C = 0x33;         // SXGA rolling shutter - SXGA color

        public const int IS_SENSOR_UI1453_C = 0x35;         // UXGA rolling shutter - UXGA color
        public const int IS_SENSOR_UI1463_C = 0x37;         // QXGA rolling shutter - QXGA monochrome
        public const int IS_SENSOR_UI1483_M = 0x38;         // 5MP rolling shutter, monochrome, single board
        public const int IS_SENSOR_UI1483_C = 0x39;         // 5MP rolling shutter, color, single board
        public const int IS_SENSOR_UI1493_M = 0x4E;         // 10MP rolling shutter, monochrome, single board
        public const int IS_SENSOR_UI1493_C = 0x4F;         // 10MP rolling shutter, color, single board
        public const int IS_SENSOR_UI1544_C = 0x3B;		    // SXGA rolling shutter, color, single board

        public const int IS_SENSOR_UI1543_M_WO = 0x3C;      // SXGA rolling shutter, color, single board
        public const int IS_SENSOR_UI1543_C_WO = 0x3D;      // SXGA rolling shutter, color, single board
        public const int IS_SENSOR_UI1463_M_WO = 0x44;      // QXGA rolling shutter, monochrome, single board
        public const int IS_SENSOR_UI1463_C_WO = 0x45;      // QXGA rolling shutter, color, single board

        public const int IS_SENSOR_UI1553_C_WN = 0x47;      // UXGA rolling shutter, color, single board
        public const int IS_SENSOR_UI1483_M_WO = 0x48;      // QSXGA rolling shutter, monochrome, single board
        public const int IS_SENSOR_UI1483_C_WO = 0x49;      // QSXGA rolling shutter, color, single board


        // CCD Sensors
        public const int IS_SENSOR_UI223X_M = 0x80;         // Sony CCD sensor - XGA monochrome
        public const int IS_SENSOR_UI223X_C = 0x81;         // Sony CCD sensor - XGA color

        public const int IS_SENSOR_UI241X_M = 0x82;         // Sony CCD sensor - VGA monochrome 
        public const int IS_SENSOR_UI241X_C = 0x83;         // Sony CCD sensor - VGA color 

        public const int IS_SENSOR_UI234X_M = 0x84;         // Sony CCD sensor - SXGA monochrome
        public const int IS_SENSOR_UI234X_C = 0x85;         // Sony CCD sensor - SXGA color

        public const int IS_SENSOR_UI233X_M = 0x86;         // Kodak CCD sensor - 1MP mono
        public const int IS_SENSOR_UI233X_C = 0x87;         // Kodak CCD sensor - 1MP color

        public const int IS_SENSOR_UI221X_M = 0x88;         // Sony CCD sensor - VGA monochrome
        public const int IS_SENSOR_UI221X_C = 0x89;         // Sony CCD sensor - VGA color

        public const int IS_SENSOR_UI231X_M = 0x90;         // Sony CCD sensor - VGA monochrome
        public const int IS_SENSOR_UI231X_C = 0x91;         // Sony CCD sensor - VGA color

        public const int IS_SENSOR_UI222X_M = 0x92;         // Sony CCD sensor - CCIR / PAL monochrome
        public const int IS_SENSOR_UI222X_C = 0x93;         // Sony CCD sensor - CCIR / PAL color

        public const int IS_SENSOR_UI224X_M = 0x96;         // Sony CCD sensor - SXGA monochrome	
        public const int IS_SENSOR_UI224X_C = 0x97;         // Sony CCD sensor - SXGA color

        public const int IS_SENSOR_UI225X_M = 0x98;         // Sony CCD sensor - UXGA monochrome
        public const int IS_SENSOR_UI225X_C = 0x99;         // Sony CCD sensor - UXGA color

        public const int IS_SENSOR_UI214X_M = 0x9A;         // Sony CCD sensor - SXGA monochrome
        public const int IS_SENSOR_UI214X_C = 0x9B;         // Sony CCD sensor - SXGA color

        public const int IS_SENSOR_UI228X_M = 0x9C;         // Sony CCD sensor - QXGA monochrome
        public const int IS_SENSOR_UI228X_C = 0x9D;         // Sony CCD sensor - QXGA color
        #endregion

        #region Return values / Error codes
        public const int IS_NO_SUCCESS = -1;
        public const int IS_SUCCESS = 0;
        public const int IS_INVALID_CAMERA_HANDLE = 1;
        public const int IS_INVALID_HANDLE = 1;

        public const int IS_IO_REQUEST_FAILED = 2;
        public const int IS_CANT_OPEN_DEVICE = 3;
        public const int IS_CANT_CLOSE_DEVICE = 4;
        public const int IS_CANT_SETUP_MEMORY = 5;
        public const int IS_NO_HWND_FOR_ERROR_REPORT = 6;
        public const int IS_ERROR_MESSAGE_NOT_CREATED = 7;
        public const int IS_ERROR_STRING_NOT_FOUND = 8;
        public const int IS_HOOK_NOT_CREATED = 9;
        public const int IS_TIMER_NOT_CREATED = 10;
        public const int IS_CANT_OPEN_REGISTRY = 11;
        public const int IS_CANT_READ_REGISTRY = 12;
        public const int IS_CANT_VALIDATE_BOARD = 13;
        public const int IS_CANT_GIVE_BOARD_ACCESS = 14;
        public const int IS_NO_IMAGE_MEM_ALLOCATED = 15;
        public const int IS_CANT_CLEANUP_MEMORY = 16;
        public const int IS_CANT_COMMUNICATE_WITH_DRIVER = 17;
        public const int IS_FUNCTION_NOT_SUPPORTED_YET = 18;
        public const int IS_OPERATING_SYSTEM_NOT_SUPPORTED = 19;

        public const int IS_INVALID_VIDEO_IN = 20;
        public const int IS_INVALID_IMG_SIZE = 21;
        public const int IS_INVALID_ADDRESS = 22;
        public const int IS_INVALID_VIDEO_MODE = 23;
        public const int IS_INVALID_AGC_MODE = 24;
        public const int IS_INVALID_GAMMA_MODE = 25;
        public const int IS_INVALID_SYNC_LEVEL = 26;
        public const int IS_INVALID_CBARS_MODE = 27;
        public const int IS_INVALID_COLOR_MODE = 28;
        public const int IS_INVALID_SCALE_FACTOR = 29;
        public const int IS_INVALID_IMAGE_SIZE = 30;
        public const int IS_INVALID_IMAGE_POS = 31;
        public const int IS_INVALID_CAPTURE_MODE = 32;
        public const int IS_INVALID_RISC_PROGRAM = 33;
        public const int IS_INVALID_BRIGHTNESS = 34;
        public const int IS_INVALID_CONTRAST = 35;
        public const int IS_INVALID_SATURATION_U = 36;
        public const int IS_INVALID_SATURATION_V = 37;
        public const int IS_INVALID_HUE = 38;
        public const int IS_INVALID_HOR_FILTER_STEP = 39;
        public const int IS_INVALID_VERT_FILTER_STEP = 40;
        public const int IS_INVALID_EEPROM_READ_ADDRESS = 41;
        public const int IS_INVALID_EEPROM_WRITE_ADDRESS = 42;
        public const int IS_INVALID_EEPROM_READ_LENGTH = 43;
        public const int IS_INVALID_EEPROM_WRITE_LENGTH = 44;
        public const int IS_INVALID_BOARD_INFO_POINTER = 45;
        public const int IS_INVALID_DISPLAY_MODE = 46;
        public const int IS_INVALID_ERR_REP_MODE = 47;
        public const int IS_INVALID_BITS_PIXEL = 48;
        public const int IS_INVALID_MEMORY_POINTER = 49;

        public const int IS_FILE_WRITE_OPEN_ERROR = 50;
        public const int IS_FILE_READ_OPEN_ERROR = 51;
        public const int IS_FILE_READ_INVALID_BMP_ID = 52;
        public const int IS_FILE_READ_INVALID_BMP_SIZE = 53;
        public const int IS_FILE_READ_INVALID_BIT_COUNT = 54;
        public const int IS_WRONG_KERNEL_VERSION = 55;

        public const int IS_RISC_INVALID_XLENGTH = 60;
        public const int IS_RISC_INVALID_YLENGTH = 61;
        public const int IS_RISC_EXCEED_IMG_SIZE = 62;

        public const int IS_DD_MAIN_FAILED = 70;
        public const int IS_DD_PRIMSURFACE_FAILED = 71;
        public const int IS_DD_SCRN_SIZE_NOT_SUPPORTED = 72;
        public const int IS_DD_CLIPPER_FAILED = 73;
        public const int IS_DD_CLIPPER_HWND_FAILED = 74;
        public const int IS_DD_CLIPPER_CONNECT_FAILED = 75;
        public const int IS_DD_BACKSURFACE_FAILED = 76;
        public const int IS_DD_BACKSURFACE_IN_SYSMEM = 77;
        public const int IS_DD_MDL_MALLOC_ERR = 78;
        public const int IS_DD_MDL_SIZE_ERR = 79;
        public const int IS_DD_CLIP_NO_CHANGE = 80;
        public const int IS_DD_PRIMMEM_NULL = 81;
        public const int IS_DD_BACKMEM_NULL = 82;
        public const int IS_DD_BACKOVLMEM_NULL = 83;
        public const int IS_DD_OVERLAYSURFACE_FAILED = 84;
        public const int IS_DD_OVERLAYSURFACE_IN_SYSMEM = 85;
        public const int IS_DD_OVERLAY_NOT_ALLOWED = 86;
        public const int IS_DD_OVERLAY_COLKEY_ERR = 87;
        public const int IS_DD_OVERLAY_NOT_ENABLED = 88;
        public const int IS_DD_GET_DC_ERROR = 89;
        public const int IS_DD_DDRAW_DLL_NOT_LOADED = 90;
        public const int IS_DD_THREAD_NOT_CREATED = 91;
        public const int IS_DD_CANT_GET_CAPS = 92;
        public const int IS_DD_NO_OVERLAYSURFACE = 93;
        public const int IS_DD_NO_OVERLAYSTRETCH = 94;
        public const int IS_DD_CANT_CREATE_OVERLAYSURFACE = 95;
        public const int IS_DD_CANT_UPDATE_OVERLAYSURFACE = 96;
        public const int IS_DD_INVALID_STRETCH = 97;

        public const int IS_EV_INVALID_EVENT_NUMBER = 100;
        public const int IS_INVALID_MODE = 101;
        public const int IS_CANT_FIND_FALCHOOK = 102;
        public const int IS_CANT_FIND_HOOK = 102;
        public const int IS_CANT_GET_HOOK_PROC_ADDR = 103;
        public const int IS_CANT_CHAIN_HOOK_PROC = 104;
        public const int IS_CANT_SETUP_WND_PROC = 105;
        public const int IS_HWND_NULL = 106;
        public const int IS_INVALID_UPDATE_MODE = 107;
        public const int IS_NO_ACTIVE_IMG_MEM = 108;
        public const int IS_CANT_INIT_EVENT = 109;
        public const int IS_FUNC_NOT_AVAIL_IN_OS = 110;
        public const int IS_CAMERA_NOT_CONNECTED = 111;
        public const int IS_SEQUENCE_LIST_EMPTY = 112;
        public const int IS_CANT_ADD_TO_SEQUENCE = 113;
        public const int IS_LOW_OF_SEQUENCE_RISC_MEM = 114;
        public const int IS_IMGMEM2FREE_USED_IN_SEQ = 115;
        public const int IS_IMGMEM_NOT_IN_SEQUENCE_LIST = 116;
        public const int IS_SEQUENCE_BUF_ALREADY_LOCKED = 117;
        public const int IS_INVALID_DEVICE_ID = 118;
        public const int IS_INVALID_BOARD_ID = 119;
        public const int IS_ALL_DEVICES_BUSY = 120;
        public const int IS_HOOK_BUSY = 121;
        public const int IS_TIMED_OUT = 122;
        public const int IS_NULL_POINTER = 123;
        public const int IS_WRONG_HOOK_VERSION = 124;
        public const int IS_INVALID_PARAMETER = 125;
        public const int IS_NOT_ALLOWED = 126;
        public const int IS_OUT_OF_MEMORY = 127;
        public const int IS_INVALID_WHILE_LIVE = 128;
        public const int IS_ACCESS_VIOLATION = 129;
        public const int IS_UNKNOWN_ROP_EFFECT = 130;
        public const int IS_INVALID_RENDER_MODE = 131;
        public const int IS_INVALID_THREAD_CONTEXT = 132;
        public const int IS_NO_HARDWARE_INSTALLED = 133;
        public const int IS_INVALID_WATCHDOG_TIME = 134;
        public const int IS_INVALID_WATCHDOG_MODE = 135;
        public const int IS_INVALID_PASSTHROUGH_IN = 136;
        public const int IS_ERROR_SETTING_PASSTHROUGH_IN = 137;
        public const int IS_FAILURE_ON_SETTING_WATCHDOG = 138;
        public const int IS_NO_USB20 = 139;
        public const int IS_CAPTURE_RUNNING = 140;

        public const int IS_MEMORY_BOARD_ACTIVATED = 141;
        public const int IS_MEMORY_BOARD_DEACTIVATED = 142;
        public const int IS_NO_MEMORY_BOARD_CONNECTED = 143;
        public const int IS_TOO_LESS_MEMORY = 144;
        public const int IS_IMAGE_NOT_PRESENT = 145;
        public const int IS_MEMORY_MODE_RUNNING = 146;
        public const int IS_MEMORYBOARD_DISABLED = 147;

        public const int IS_TRIGGER_ACTIVATED = 148;
        public const int IS_WRONG_KEY = 150;
        public const int IS_CRC_ERROR = 151;
        public const int IS_NOT_YET_RELEASED = 152;   // this feature is not available yet
        public const int IS_NOT_CALIBRATED = 153;   // the camera is not calibrated
        public const int IS_WAITING_FOR_KERNEL = 154;   // a request to the kernel exceeded
        public const int IS_NOT_SUPPORTED = 155;   // operation mode is not supported
        public const int IS_TRIGGER_NOT_ACTIVATED = 156;   // operation could not execute while trigger is disabled
        public const int IS_OPERATION_ABORTED = 157;
        public const int IS_BAD_STRUCTURE_SIZE = 158;
        public const int IS_INVALID_BUFFER_SIZE = 159;
        public const int IS_INVALID_PIXEL_CLOCK = 160;
        public const int IS_INVALID_EXPOSURE_TIME = 161;
        public const int IS_AUTO_EXPOSURE_RUNNING = 162;

        public const int IS_CANNOT_CREATE_BB_SURF = 163; // error creating backbuffer surface  
        public const int IS_CANNOT_CREATE_BB_MIX = 164; // backbuffer mixer surfaces can not be created
        public const int IS_BB_OVLMEM_NULL = 165; // backbuffer overlay mem could not be locked  
        public const int IS_CANNOT_CREATE_BB_OVL = 166; // backbuffer overlay mem could not be created  
        public const int IS_NOT_SUPP_IN_OVL_SURF_MODE = 167; // function not supported in overlay surface mode  
        public const int IS_INVALID_SURFACE = 168; // surface invalid
        public const int IS_SURFACE_LOST = 169; // surface has been lost  
        public const int IS_RELEASE_BB_OVL_DC = 170; // error releasing backbuffer overlay DC  
        public const int IS_BB_TIMER_NOT_CREATED = 171; // backbuffer timer could not be created  
        public const int IS_BB_OVL_NOT_EN = 172; // backbuffer overlay has not been enabled  
        public const int IS_ONLY_IN_BB_MODE = 173; // only possible in backbuffer mode 
        public const int IS_INVALID_COLOR_FORMAT = 174; // invalid color format
        public const int IS_INVALID_WB_BINNING_MODE = 175;
        public const int IS_INVALID_I2C_DEVICE_ADDRESS = 176;
        public const int IS_COULD_NOT_CONVERT = 177; // current image couldn't be converted
        public const int IS_TRANSFER_ERROR = 178; // transfer failed
        public const int IS_PARAMETER_SET_NOT_PRESENT = 179; // the parameter set is not present
        public const int IS_INVALID_CAMERA_TYPE = 180; // the camera type in the ini file doesn't match
        public const int IS_INVALID_HOST_IP_HIBYTE = 181; // HIBYTE of host address is invalid 
        public const int IS_CM_NOT_SUPP_IN_CURR_DISPLAYMODE = 182;   // color mode is not supported in the current display mode
        public const int IS_NO_IR_FILTER = 183;
        public const int IS_STARTER_FW_UPLOAD_NEEDED = 184;
        public const int IS_DR_LIBRARY_NOT_FOUND = 185;	// the DirectRender library could not be found
        public const int IS_DR_DEVICE_OUT_OF_MEMORY = 186;	// insufficient graphics adapter video memory
        public const int IS_DR_CANNOT_CREATE_SURFACE = 187;	// the image or overlay surface could not be created
        public const int IS_DR_CANNOT_CREATE_VERTEX_BUFFER = 188;	// the vertex buffer could not be created
        public const int IS_DR_CANNOT_CREATE_TEXTURE = 189;	// the texture could not be created  
        public const int IS_DR_CANNOT_LOCK_OVERLAY_SURFACE = 190;	// the overlay surface could not be locked
        public const int IS_DR_CANNOT_UNLOCK_OVERLAY_SURFACE = 191;	// the overlay surface could not be unlocked
        public const int IS_DR_CANNOT_GET_OVERLAY_DC = 192;	// cannot get the overlay surface DC 
        public const int IS_DR_CANNOT_RELEASE_OVERLAY_DC = 193;	// cannot release the overlay surface DC
        public const int IS_DR_DEVICE_CAPS_INSUFFICIENT = 194;	// insufficient graphics adapter capabilities
        public const int IS_INCOMPATIBLE_SETTING = 195;  // Operation is not possible because of another incompatible setting
        public const int IS_DR_NOT_ALLOWED_WHILE_DC_IS_ACTIVE = 196;  // user App still has DC handle.
        public const int IS_DEVICE_ALREADY_PAIRED = 197;   // The device is already paired
        public const int IS_SUBNETMASK_MISMATCH = 198;   // The subnetmasks of the device and the adapter differ
        public const int IS_SUBNET_MISMATCH = 199;   // The subnets of the device and the adapter differ
        public const int IS_INVALID_IP_CONFIGURATION = 200;   // The IP configuation of the device is invalid
        public const int IS_DEVICE_NOT_COMPATIBLE = 201;   // The device is incompatible to the driver
        public const int IS_NETWORK_FRAME_SIZE_INCOMPATIBLE = 202;   // The frame size settings of the device and the network adapter are incompatible
        public const int IS_NETWORK_CONFIGURATION_INVALID = 203;   // The network adapter configuration is invalid
        public const int IS_ERROR_CPU_IDLE_STATES_CONFIGURATION = 204;   // The setting of the CPU idle state configuration failed
        #endregion

        #region Image files types
        public const int IS_IMG_BMP = 0;
        public const int IS_IMG_JPG = 1;
        public const int IS_IMG_PNG = 2;
        public const int IS_IMG_RAW = 4;
        public const int IS_IMG_TIF = 8;
        #endregion

        #region I2C defines
        // nRegisterAddr | IS_I2C_16_BIT_REGISTER
        // *********************************************** 
        public const int IS_I2C_16_BIT_REGISTER = 0x10000000;
        #endregion

        #region common definitions
        public const int IS_OFF = 0;
        public const int IS_ON = 1;
        public const int IS_IGNORE_PARAMETER = -1;
        #endregion

        #region device enumeration
        public const int IS_USE_DEVICE_ID = 0x8000;
        public const int IS_ALLOW_STARTER_FW_UPLOAD = 0x10000;
        #endregion

        #region autoExit enable/disable
        public const int IS_DISABLE_AUTO_EXIT = 0;
        public const int IS_ENABLE_AUTO_EXIT = 1;
        public const int IS_GET_AUTO_EXIT_ENABLED = 0x8000;
        #endregion

        #region live/freeze parameters
        public const int IS_GET_LIVE = 0x8000;

        public const int IS_WAIT = 1;
        public const int IS_DONT_WAIT = 0;
        public const int IS_FORCE_VIDEO_STOP = 0x4000;
        public const int IS_FORCE_VIDEO_START = 0x4000;
        #endregion

        #region video finish constants
        public const int IS_VIDEO_NOT_FINISH = 0;
        public const int IS_VIDEO_FINISH = 1;
        #endregion

        #region bitmap render modes
        public const int IS_GET_RENDER_MODE = 0x8000;

        public const int IS_RENDER_DISABLED = 0;
        public const int IS_RENDER_NORMAL = 1;
        public const int IS_RENDER_FIT_TO_WINDOW = 2;
        public const int IS_RENDER_DOWNSCALE_1_2 = 4;
        public const int IS_RENDER_MIRROR_UPDOWN = 16;
        public const int IS_RENDER_DOUBLE_HEIGHT = 32;
        public const int IS_RENDER_HALF_HEIGHT = 64;
        public const int IS_USE_AS_DC_HANDLE = 0x8000;
        public const int IS_USE_AS_DC_STRUCTURE = 0x4000;
        #endregion

        #region external trigger mode constants
        public const int IS_GET_EXTERNALTRIGGER = 0x8000;
        public const int IS_GET_TRIGGER_STATUS = 0x8001;
        public const int IS_GET_TRIGGER_MASK = 0x8002;
        public const int IS_GET_TRIGGER_INPUTS = 0x8003;
        public const int IS_GET_SUPPORTED_TRIGGER_MODE = 0x8004;
        public const int IS_GET_TRIGGER_COUNTER = 0x8000;

        // Old defines for compatibility
        public const int IS_SET_TRIG_OFF = 0x0;
        public const int IS_SET_TRIG_HI_LO = 0x1;
        public const int IS_SET_TRIG_LO_HI = 0x2;
        public const int IS_SET_TRIG_SOFTWARE = 0x8;
        public const int IS_SET_TRIG_HI_LO_SYNC = 0x10;
        public const int IS_SET_TRIG_LO_HI_SYNC = 0x20;

        public const int IS_SET_TRIG_MASK = 0x100;

        // New defines
        public const int IS_SET_TRIGGER_CONTINUOUS = 0x1000;
        public const int IS_SET_TRIGGER_OFF = IS_SET_TRIG_OFF;
        public const int IS_SET_TRIGGER_HI_LO = (IS_SET_TRIGGER_CONTINUOUS | IS_SET_TRIG_HI_LO);
        public const int IS_SET_TRIGGER_LO_HI = (IS_SET_TRIGGER_CONTINUOUS | IS_SET_TRIG_LO_HI);
        public const int IS_SET_TRIGGER_SOFTWARE = (IS_SET_TRIGGER_CONTINUOUS | IS_SET_TRIG_SOFTWARE);
        public const int IS_SET_TRIGGER_HI_LO_SYNC = IS_SET_TRIG_HI_LO_SYNC;
        public const int IS_SET_TRIGGER_LO_HI_SYNC = IS_SET_TRIG_LO_HI_SYNC;

        public const int IS_GET_TRIGGER_DELAY = 0x8000;
        public const int IS_GET_MIN_TRIGGER_DELAY = 0x8001;
        public const int IS_GET_MAX_TRIGGER_DELAY = 0x8002;
        public const int IS_GET_TRIGGER_DELAY_GRANULARITY = 0x8003;
        #endregion

        #region  timing
        // pixelclock
        public const int IS_GET_PIXEL_CLOCK = 0x8000;
        public const int IS_GET_DEFAULT_PIXEL_CLK = 0x8001;
        public const int IS_GET_PIXEL_CLOCK_INC = 0x8005;

        // framerate
        public const int IS_GET_FRAMERATE = 0x8000;
        public const int IS_GET_DEFAULT_FRAMERATE = 0x8001;

        // exposure
        public const int IS_GET_EXPOSURE_TIME = 0x8000;
        public const int IS_GET_DEFAULT_EXPOSURE = 0x8001;
        public const int IS_GET_EXPOSURE_MIN_VALUE = 0x8002;
        public const int IS_GET_EXPOSURE_MAX_VALUE = 0x8003;
        public const int IS_GET_EXPOSURE_INCREMENT = 0x8004;
        public const int IS_GET_EXPOSURE_FINE_INCREMENT = 0x8005;
        #endregion

        #region gain definitions
        public const int IS_GET_MASTER_GAIN = 0x8000;
        public const int IS_GET_RED_GAIN = 0x8001;
        public const int IS_GET_GREEN_GAIN = 0x8002;
        public const int IS_GET_BLUE_GAIN = 0x8003;
        public const int IS_GET_DEFAULT_MASTER = 0x8004;
        public const int IS_GET_DEFAULT_RED = 0x8005;
        public const int IS_GET_DEFAULT_GREEN = 0x8006;
        public const int IS_GET_DEFAULT_BLUE = 0x8007;
        public const int IS_GET_GAINBOOST = 0x8008;
        public const int IS_SET_GAINBOOST_ON = 0x0001;
        public const int IS_SET_GAINBOOST_OFF = 0x0000;
        public const int IS_GET_SUPPORTED_GAINBOOST = 0x0002;
        #endregion

        #region gain factor definitions
        public const int IS_GET_MASTER_GAIN_FACTOR = 0x8000;
        public const int IS_GET_RED_GAIN_FACTOR = 0x8001;
        public const int IS_GET_GREEN_GAIN_FACTOR = 0x8002;
        public const int IS_GET_BLUE_GAIN_FACTOR = 0x8003;
        public const int IS_SET_MASTER_GAIN_FACTOR = 0x8004;
        public const int IS_SET_RED_GAIN_FACTOR = 0x8005;
        public const int IS_SET_GREEN_GAIN_FACTOR = 0x8006;
        public const int IS_SET_BLUE_GAIN_FACTOR = 0x8007;
        public const int IS_GET_DEFAULT_MASTER_GAIN_FACTOR = 0x8008;
        public const int IS_GET_DEFAULT_RED_GAIN_FACTOR = 0x8009;
        public const int IS_GET_DEFAULT_GREEN_GAIN_FACTOR = 0x800a;
        public const int IS_GET_DEFAULT_BLUE_GAIN_FACTOR = 0x800b;
        public const int IS_INQUIRE_MASTER_GAIN_FACTOR = 0x800c;
        public const int IS_INQUIRE_RED_GAIN_FACTOR = 0x800d;
        public const int IS_INQUIRE_GREEN_GAIN_FACTOR = 0x800e;
        public const int IS_INQUIRE_BLUE_GAIN_FACTOR = 0x800f;
        #endregion

        #region blacklevel compensation
        public const int IS_GET_BL_COMPENSATION = 0x8000;
        public const int IS_GET_BL_OFFSET = 0x8001;
        public const int IS_GET_BL_DEFAULT_MODE = 0x8002;
        public const int IS_GET_BL_DEFAULT_OFFSET = 0x8003;
        public const int IS_GET_BL_SUPPORTED_MODE = 0x8004;

        public const int IS_BL_COMPENSATION_DISABLE = 0;
        public const int IS_BL_COMPENSATION_ENABLE = 1;
        public const int IS_BL_COMPENSATION_OFFSET = 32;

        public const int IS_MIN_BL_OFFSET = 0;
        public const int IS_MAX_BL_OFFSET = 255;
        #endregion

        #region hardware gamma definitions
        public const int IS_GET_HW_GAMMA = 0x8000;
        public const int IS_GET_HW_SUPPORTED_GAMMA = 0x8001;

        public const int IS_SET_HW_GAMMA_OFF = 0x0;
        public const int IS_SET_HW_GAMMA_ON = 0x1;
        #endregion

        #region Camera LUT
        public const int IS_ENABLE_CAMERA_LUT = 0x0001;
        public const int IS_SET_CAMERA_LUT_VALUES = 0x0002;
        public const int IS_ENABLE_RGB_GRAYSCALE = 0x0004;
        public const int IS_GET_CAMERA_LUT_USER = 0x0008;
        public const int IS_GET_CAMERA_LUT_COMPLETE = 0x0010;
        public const int IS_GET_CAMERA_LUT_SUPPORTED_CHANNELS = 0x0020;
        #endregion

        #region camera LUT presets
        public const int IS_CAMERA_LUT_IDENTITY = 0x00000100;
        public const int IS_CAMERA_LUT_NEGATIV = 0x00000200;
        public const int IS_CAMERA_LUT_GLOW1 = 0x00000400;
        public const int IS_CAMERA_LUT_GLOW2 = 0x00000800;
        public const int IS_CAMERA_LUT_ASTRO1 = 0x00001000;
        public const int IS_CAMERA_LUT_RAINBOW1 = 0x00002000;
        public const int IS_CAMERA_LUT_MAP1 = 0x00004000;
        public const int IS_CAMERA_LUT_COLD_HOT = 0x00008000;
        public const int IS_CAMERA_LUT_SEPIC = 0x00010000;
        public const int IS_CAMERA_LUT_ONLY_RED = 0x00020000;
        public const int IS_CAMERA_LUT_ONLY_GREEN = 0x00040000;
        public const int IS_CAMERA_LUT_ONLY_BLUE = 0x00080000;

        public const int IS_CAMERA_LUT_64 = 64;
        public const int IS_CAMERA_LUT_128 = 128;
        #endregion

        #region Image parameters
        // brightness
        public const int IS_GET_BRIGHTNESS = 0x8000;
        public const int IS_MIN_BRIGHTNESS = 0;
        public const int IS_MAX_BRIGHTNESS = 255;
        public const int IS_DEFAULT_BRIGHTNESS = -1;
        //contrast    
        public const int IS_GET_CONTRAST = 0x8000;
        public const int IS_MIN_CONTRAST = 0;
        public const int IS_MAX_CONTRAST = 511;
        public const int IS_DEFAULT_CONTRAST = -1;
        // gamma    
        public const int IS_GET_GAMMA = 0x8000;
        public const int IS_MIN_GAMMA = 1;
        public const int IS_MAX_GAMMA = 1000;
        public const int IS_DEFAULT_GAMMA = -1;
        public const int IS_GET_GAMMA_MODE = 0x8000;
        public const int IS_SET_GAMMA_OFF = 0;
        public const int IS_SET_GAMMA_ON = 1;
        #endregion

        #region image position + size
        // Deprectated defines
        public const int IS_GET_IMAGE_SIZE_X = 0x8000;
        public const int IS_GET_IMAGE_SIZE_Y = 0x8001;
        public const int IS_GET_IMAGE_SIZE_X_INC = 0x8002;
        public const int IS_GET_IMAGE_SIZE_Y_INC = 0x8003;
        public const int IS_GET_IMAGE_SIZE_X_MIN = 0x8004;
        public const int IS_GET_IMAGE_SIZE_Y_MIN = 0x8005;
        public const int IS_GET_IMAGE_SIZE_X_MAX = 0x8006;
        public const int IS_GET_IMAGE_SIZE_Y_MAX = 0x8007;

        public const int IS_GET_IMAGE_POS_X = 0x8001;
        public const int IS_GET_IMAGE_POS_Y = 0x8002;
        public const int IS_GET_IMAGE_POS_X_ABS = 0xC001;
        public const int IS_GET_IMAGE_POS_Y_ABS = 0xC002;
        public const int IS_GET_IMAGE_POS_X_INC = 0xC003;
        public const int IS_GET_IMAGE_POS_Y_INC = 0xC004;
        public const int IS_GET_IMAGE_POS_X_MIN = 0xC005;
        public const int IS_GET_IMAGE_POS_Y_MIN = 0xC006;
        public const int IS_GET_IMAGE_POS_X_MAX = 0xC007;
        public const int IS_GET_IMAGE_POS_Y_MAX = 0xC008;

        public const int IS_SET_IMAGE_POS_X_ABS = 0x00010000;
        public const int IS_SET_IMAGE_POS_Y_ABS = 0x00010000;
        public const int IS_SET_IMAGEPOS_X_ABS = 0x8000;
        public const int IS_SET_IMAGEPOS_Y_ABS = 0x8000;


        // Valid defines

        // Image
        public const int IS_AOI_IMAGE_SET_AOI = 0x0001;
        public const int IS_AOI_IMAGE_GET_AOI = 0x0002;
        public const int IS_AOI_IMAGE_SET_POS = 0x0003;
        public const int IS_AOI_IMAGE_GET_POS = 0x0004;
        public const int IS_AOI_IMAGE_SET_SIZE = 0x0005;
        public const int IS_AOI_IMAGE_GET_SIZE = 0x0006;
        public const int IS_AOI_IMAGE_GET_POS_MIN = 0x0007;
        public const int IS_AOI_IMAGE_GET_SIZE_MIN = 0x0008;
        public const int IS_AOI_IMAGE_GET_POS_MAX = 0x0009;
        public const int IS_AOI_IMAGE_GET_SIZE_MAX = 0x0010;
        public const int IS_AOI_IMAGE_GET_POS_INC = 0x0011;
        public const int IS_AOI_IMAGE_GET_SIZE_INC = 0x0012;
        public const int IS_AOI_IMAGE_GET_POS_X_ABS = 0x0013;
        public const int IS_AOI_IMAGE_GET_POS_Y_ABS = 0x0014;
        public const int IS_AOI_IMAGE_GET_ORIGINAL_AOI = 0x0015;

        public const int IS_AOI_IMAGE_POS_ABSOLUTE = 0x10000000;

        /* Fast move */
        public const int IS_AOI_IMAGE_SET_POS_FAST = 0x0020;
        public const int IS_AOI_IMAGE_SET_POS_FAST_SUPPORTED = 0x0021;

        /* Auto features */
        public const int IS_AOI_AUTO_BRIGHTNESS_SET_AOI = 0x0030;
        public const int IS_AOI_AUTO_BRIGHTNESS_GET_AOI = 0x0031;
        public const int IS_AOI_AUTO_WHITEBALANCE_SET_AOI = 0x0032;
        public const int IS_AOI_AUTO_WHITEBALANCE_GET_AOI = 0x0033;

        /* Multi AOI */
        public const int IS_AOI_MULTI_GET_SUPPORTED_MODES = 0x0100;
        public const int IS_AOI_MULTI_SET_AOI = 0x0200;
        public const int IS_AOI_MULTI_GET_AOI = 0x0400;
        public const int IS_AOI_MULTI_DISABLE_AOI = 0x0800;
        public const int IS_AOI_MULTI_MODE_AXES = 0x0001;

        /* AOI sequence */
        public const int IS_AOI_SEQUENCE_GET_SUPPORTED = 0x0050;
        public const int IS_AOI_SEQUENCE_SET_PARAMS = 0x0051;
        public const int IS_AOI_SEQUENCE_GET_PARAMS = 0x0052;
        public const int IS_AOI_SEQUENCE_SET_ENABLE = 0x0053;
        public const int IS_AOI_SEQUENCE_GET_ENABLE = 0x0054;

        public const int IS_AOI_SEQUENCE_INDEX_AOI_1 = 0;
        public const int IS_AOI_SEQUENCE_INDEX_AOI_2 = 1;
        public const int IS_AOI_SEQUENCE_INDEX_AOI_3 = 2;
        public const int IS_AOI_SEQUENCE_INDEX_AOI_4 = 4;
        #endregion

        #region rop effect constants
        public const int IS_GET_ROP_EFFECT = 0x8000;
        public const int IS_GET_SUPPORTED_ROP_EFFECT = 0x8001;

        public const int IS_SET_ROP_MIRROR_NONE = 0;
        public const int IS_SET_ROP_MIRROR_UPDOWN = 8;
        public const int IS_SET_ROP_MIRROR_UPDOWN_ODD = 16;
        public const int IS_SET_ROP_MIRROR_UPDOWN_EVEN = 32;
        public const int IS_SET_ROP_MIRROR_LEFTRIGHT = 64;
        #endregion

        #region subsampling
        public const int IS_GET_SUBSAMPLING = 0x8000;
        public const int IS_GET_SUBSAMPLING_MODE = 0x8001;
        public const int IS_GET_SUPPORTED_SUBSAMPLING = 0x8001;
        public const int IS_GET_SUBSAMPLING_TYPE = 0x8002;
        public const int IS_GET_SUBSAMPLING_FACTOR_HORIZONTAL = 0x8004;
        public const int IS_GET_SUBSAMPLING_FACTOR_VERTICAL = 0x8008;

        public const int IS_SUBSAMPLING_DISABLE = 0x0;

        public const int IS_SUBSAMPLING_2X_VERTICAL = 0x0001;
        public const int IS_SUBSAMPLING_2X_HORIZONTAL = 0x0002;
        public const int IS_SUBSAMPLING_4X_VERTICAL = 0x0004;
        public const int IS_SUBSAMPLING_4X_HORIZONTAL = 0x0008;
        public const int IS_SUBSAMPLING_3X_VERTICAL = 0x0010;
        public const int IS_SUBSAMPLING_3X_HORIZONTAL = 0x0020;
        public const int IS_SUBSAMPLING_5X_VERTICAL = 0x0040;
        public const int IS_SUBSAMPLING_5X_HORIZONTAL = 0x0080;
        public const int IS_SUBSAMPLING_6X_VERTICAL = 0x0100;
        public const int IS_SUBSAMPLING_6X_HORIZONTAL = 0x0200;
        public const int IS_SUBSAMPLING_8X_VERTICAL = 0x0400;
        public const int IS_SUBSAMPLING_8X_HORIZONTAL = 0x0800;
        public const int IS_SUBSAMPLING_16X_VERTICAL = 0x1000;
        public const int IS_SUBSAMPLING_16X_HORIZONTAL = 0x2000;

        public const int IS_SUBSAMPLING_COLOR = 0x01;
        public const int IS_SUBSAMPLING_MONO = 0x02;

        public const int IS_SUBSAMPLING_MASK_VERTICAL = (IS_SUBSAMPLING_2X_VERTICAL | IS_SUBSAMPLING_4X_VERTICAL | IS_SUBSAMPLING_3X_VERTICAL | IS_SUBSAMPLING_5X_VERTICAL | IS_SUBSAMPLING_6X_VERTICAL | IS_SUBSAMPLING_8X_VERTICAL | IS_SUBSAMPLING_16X_VERTICAL);
        public const int IS_SUBSAMPLING_MASK_HORIZONTAL = (IS_SUBSAMPLING_2X_HORIZONTAL | IS_SUBSAMPLING_4X_HORIZONTAL | IS_SUBSAMPLING_3X_HORIZONTAL | IS_SUBSAMPLING_5X_HORIZONTAL | IS_SUBSAMPLING_6X_HORIZONTAL | IS_SUBSAMPLING_8X_HORIZONTAL | IS_SUBSAMPLING_16X_HORIZONTAL);

        // Compatibility
        public const int IS_SUBSAMPLING_VERT = IS_SUBSAMPLING_2X_VERTICAL;
        public const int IS_SUBSAMPLING_HOR = IS_SUBSAMPLING_2X_HORIZONTAL;
        #endregion

        #region binning
        public const int IS_GET_BINNING = 0x8000;
        public const int IS_GET_BINNING_MODE = 0x8001;
        public const int IS_GET_SUPPORTED_BINNING = 0x8001;
        public const int IS_GET_BINNING_TYPE = 0x8002;
        public const int IS_GET_BINNING_FACTOR_HORIZONTAL = 0x8004;
        public const int IS_GET_BINNING_FACTOR_VERTICAL = 0x8008;

        public const int IS_BINNING_DISABLE = 0x0;

        public const int IS_BINNING_2X_VERTICAL = 0x0001;
        public const int IS_BINNING_2X_HORIZONTAL = 0x0002;
        public const int IS_BINNING_4X_VERTICAL = 0x0004;
        public const int IS_BINNING_4X_HORIZONTAL = 0x0008;
        public const int IS_BINNING_3X_VERTICAL = 0x0010;
        public const int IS_BINNING_3X_HORIZONTAL = 0x0020;
        public const int IS_BINNING_5X_VERTICAL = 0x0040;
        public const int IS_BINNING_5X_HORIZONTAL = 0x0080;
        public const int IS_BINNING_6X_VERTICAL = 0x0100;
        public const int IS_BINNING_6X_HORIZONTAL = 0x0200;
        public const int IS_BINNING_8X_VERTICAL = 0x0400;
        public const int IS_BINNING_8X_HORIZONTAL = 0x0800;
        public const int IS_BINNING_16X_VERTICAL = 0x1000;
        public const int IS_BINNING_16X_HORIZONTAL = 0x2000;

        public const int IS_BINNING_COLOR = 0x01;
        public const int IS_BINNING_MONO = 0x02;

        public const int IS_BINNING_MASK_VERTICAL = (IS_BINNING_2X_VERTICAL | IS_BINNING_3X_VERTICAL | IS_BINNING_4X_VERTICAL | IS_BINNING_5X_VERTICAL | IS_BINNING_6X_VERTICAL | IS_BINNING_8X_VERTICAL | IS_BINNING_16X_VERTICAL);
        public const int IS_BINNING_MASK_HORIZONTAL = (IS_BINNING_2X_HORIZONTAL | IS_BINNING_3X_HORIZONTAL | IS_BINNING_4X_HORIZONTAL | IS_BINNING_5X_HORIZONTAL | IS_BINNING_6X_HORIZONTAL | IS_BINNING_8X_HORIZONTAL | IS_BINNING_16X_HORIZONTAL);

        // Compatibility
        public const int IS_BINNING_VERT = IS_BINNING_2X_VERTICAL;
        public const int IS_BINNING_HOR = IS_BINNING_2X_HORIZONTAL;
        #endregion

        #region Auto Control Parameter
        public const int IS_SET_ENABLE_AUTO_GAIN = 0x8800;
        public const int IS_GET_ENABLE_AUTO_GAIN = 0x8801;
        public const int IS_SET_ENABLE_AUTO_SHUTTER = 0x8802;
        public const int IS_GET_ENABLE_AUTO_SHUTTER = 0x8803;
        public const int IS_SET_ENABLE_AUTO_WHITEBALANCE = 0x8804;
        public const int IS_GET_ENABLE_AUTO_WHITEBALANCE = 0x8805;
        public const int IS_SET_ENABLE_AUTO_FRAMERATE = 0x8806;
        public const int IS_GET_ENABLE_AUTO_FRAMERATE = 0x8807;
        public const int IS_SET_ENABLE_AUTO_SENSOR_GAIN = 0x8808;
        public const int IS_GET_ENABLE_AUTO_SENSOR_GAIN = 0x8809;
        public const int IS_SET_ENABLE_AUTO_SENSOR_SHUTTER = 0x8810;
        public const int IS_GET_ENABLE_AUTO_SENSOR_SHUTTER = 0x8811;
        public const int IS_SET_ENABLE_AUTO_SENSOR_GAIN_SHUTTER = 0x8812;
        public const int IS_GET_ENABLE_AUTO_SENSOR_GAIN_SHUTTER = 0x8813;
        public const int IS_SET_ENABLE_AUTO_SENSOR_FRAMERATE = 0x8814;
        public const int IS_GET_ENABLE_AUTO_SENSOR_FRAMERATE = 0x8815;
        public const int IS_SET_ENABLE_AUTO_SENSOR_WHITEBALANCE = 0x8816;
        public const int IS_GET_ENABLE_AUTO_SENSOR_WHITEBALANCE = 0x8817;

        public const int IS_SET_AUTO_REFERENCE = 0x8000;
        public const int IS_GET_AUTO_REFERENCE = 0x8001;
        public const int IS_SET_AUTO_GAIN_MAX = 0x8002;
        public const int IS_GET_AUTO_GAIN_MAX = 0x8003;
        public const int IS_SET_AUTO_SHUTTER_MAX = 0x8004;
        public const int IS_GET_AUTO_SHUTTER_MAX = 0x8005;
        public const int IS_SET_AUTO_SPEED = 0x8006;
        public const int IS_GET_AUTO_SPEED = 0x8007;
        public const int IS_SET_AUTO_WB_OFFSET = 0x8008;
        public const int IS_GET_AUTO_WB_OFFSET = 0x8009;
        public const int IS_SET_AUTO_WB_GAIN_RANGE = 0x800A;
        public const int IS_GET_AUTO_WB_GAIN_RANGE = 0x800B;
        public const int IS_SET_AUTO_WB_SPEED = 0x800C;
        public const int IS_GET_AUTO_WB_SPEED = 0x800D;
        public const int IS_SET_AUTO_WB_ONCE = 0x800E;
        public const int IS_GET_AUTO_WB_ONCE = 0x800F;
        public const int IS_SET_AUTO_BRIGHTNESS_ONCE = 0x8010;
        public const int IS_GET_AUTO_BRIGHTNESS_ONCE = 0x8011;
        public const int IS_SET_AUTO_HYSTERESIS = 0x8012;
        public const int IS_GET_AUTO_HYSTERESIS = 0x8013;
        public const int IS_GET_AUTO_HYSTERESIS_RANGE = 0x8014;
        public const int IS_SET_AUTO_WB_HYSTERESIS = 0x8015;
        public const int IS_GET_AUTO_WB_HYSTERESIS = 0x8016;
        public const int IS_GET_AUTO_WB_HYSTERESIS_RANGE = 0x8017;
        public const int IS_SET_AUTO_SKIPFRAMES = 0x8018;
        public const int IS_GET_AUTO_SKIPFRAMES = 0x8019;
        public const int IS_GET_AUTO_SKIPFRAMES_RANGE = 0x801A;
        public const int IS_SET_AUTO_WB_SKIPFRAMES = 0x801B;
        public const int IS_GET_AUTO_WB_SKIPFRAMES = 0x801C;
        public const int IS_GET_AUTO_WB_SKIPFRAMES_RANGE = 0x801D;
        public const int IS_SET_SENS_AUTO_SHUTTER_PHOTOM = 0x801E;
        public const int IS_SET_SENS_AUTO_GAIN_PHOTOM = 0x801F;
        public const int IS_GET_SENS_AUTO_SHUTTER_PHOTOM = 0x8020;
        public const int IS_GET_SENS_AUTO_GAIN_PHOTOM = 0x8021;
        public const int IS_GET_SENS_AUTO_SHUTTER_PHOTOM_DEF = 0x8022;
        public const int IS_GET_SENS_AUTO_GAIN_PHOTOM_DEF = 0x8023;
        public const int IS_SET_SENS_AUTO_CONTRAST_CORRECTION = 0x8024;
        public const int IS_GET_SENS_AUTO_CONTRAST_CORRECTION = 0x8025;
        public const int IS_GET_SENS_AUTO_CONTRAST_CORRECTION_RANGE = 0x8026;
        public const int IS_GET_SENS_AUTO_CONTRAST_CORRECTION_INC = 0x8027;
        public const int IS_GET_SENS_AUTO_CONTRAST_CORRECTION_DEF = 0x8028;
        public const int IS_SET_SENS_AUTO_CONTRAST_FDT_AOI_ENABLE = 0x8029;
        public const int IS_GET_SENS_AUTO_CONTRAST_FDT_AOI_ENABLE = 0x8030;
        public const int IS_SET_SENS_AUTO_BACKLIGHT_COMP = 0x8031;
        public const int IS_GET_SENS_AUTO_BACKLIGHT_COMP = 0x8032;
        public const int IS_GET_SENS_AUTO_BACKLIGHT_COMP_RANGE = 0x8033;
        public const int IS_GET_SENS_AUTO_BACKLIGHT_COMP_INC = 0x8034;
        public const int IS_GET_SENS_AUTO_BACKLIGHT_COMP_DEF = 0x8035;
        public const int IS_SET_ANTI_FLICKER_MODE = 0x8036;
        public const int IS_GET_ANTI_FLICKER_MODE = 0x8037;
        public const int IS_GET_ANTI_FLICKER_MODE_DEF = 0x8038;
        #endregion

        #region Auto Control definitions
        public const int IS_MIN_AUTO_BRIGHT_REFERENCE = 0;
        public const int IS_MAX_AUTO_BRIGHT_REFERENCE = 255;
        public const int IS_DEFAULT_AUTO_BRIGHT_REFERENCE = 128;
        public const int IS_MIN_AUTO_SPEED = 0;
        public const int IS_MAX_AUTO_SPEED = 100;
        public const int IS_DEFAULT_AUTO_SPEED = 50;
        public const int IS_DEFAULT_AUTO_WB_OFFSET = 0;
        public const int IS_MIN_AUTO_WB_OFFSET = -50;
        public const int IS_MAX_AUTO_WB_OFFSET = 50;
        public const int IS_DEFAULT_AUTO_WB_SPEED = 50;
        public const int IS_MIN_AUTO_WB_SPEED = 0;
        public const int IS_MAX_AUTO_WB_SPEED = 100;
        public const int IS_MIN_AUTO_WB_REFERENCE = 0;
        public const int IS_MAX_AUTO_WB_REFERENCE = 255;
        #endregion

        #region AOI types to set/get
        public const int IS_SET_AUTO_BRIGHT_AOI = 0x8000;
        public const int IS_GET_AUTO_BRIGHT_AOI = 0x8001;
        public const int IS_SET_IMAGE_AOI = 0x8002;
        public const int IS_GET_IMAGE_AOI = 0x8003;
        public const int IS_SET_AUTO_WB_AOI = 0x8004;
        public const int IS_GET_AUTO_WB_AOI = 0x8005;
        #endregion

        #region color modes
        public const int IS_GET_COLOR_MODE = 0x8000;

        public const int IS_SET_CM_RGB32 = 0;
        public const int IS_SET_CM_RGB24 = 1;
        public const int IS_SET_CM_RGB16 = 2;
        public const int IS_SET_CM_RGB15 = 3;
        public const int IS_SET_CM_Y8 = 6;
        public const int IS_SET_CM_RGB8 = 7;
        public const int IS_SET_CM_BAYER = 11;
        public const int IS_SET_CM_UYVY = 12;
        public const int IS_SET_CM_UYVY_MONO = 13;
        public const int IS_SET_CM_UYVY_BAYER = 14;
        public const int IS_SET_CM_CBYCRY = 23;
        public const int IS_SET_CM_RGBY = 24;
        public const int IS_SET_CM_RGB30 = 25;
        public const int IS_SET_CM_Y12 = 26;
        public const int IS_SET_CM_BAYER12 = 27;
        public const int IS_SET_CM_Y16 = 28;
        public const int IS_SET_CM_BAYER16 = 29;

        public const int IS_CM_MODE_MASK = 0x007F;

        // planar vs packed format
        public const int IS_CM_FORMAT_PACKED = 0x0000;
        public const int IS_CM_FORMAT_PLANAR = 0x2000;
        public const int IS_CM_FORMAT_MASK = 0x2000;

        // BGR vs. RGB order
        public const int IS_CM_ORDER_BGR = 0x0000;
        public const int IS_CM_ORDER_RGB = 0x0080;
        public const int IS_CM_ORDER_MASK = 0x0080;

        // define compliant color format names
        public const int IS_CM_MONO8 = IS_SET_CM_Y8;			    // occupies 8 Bit
        public const int IS_CM_MONO12 = IS_SET_CM_Y12;		        // occupies 16 Bit
        public const int IS_CM_MONO16 = IS_SET_CM_Y16;		        // occupies 16 Bit

        public const int IS_CM_BAYER_RG8 = IS_SET_CM_BAYER;		    // occupies 8 Bit
        public const int IS_CM_BAYER_RG12 = IS_SET_CM_BAYER12;	    // occupies 16 Bit
        public const int IS_CM_BAYER_RG16 = IS_SET_CM_BAYER16;      // occupies 16 Bit

        public const int IS_CM_SENSOR_RAW8 = IS_SET_CM_BAYER;       // occupies 8 Bit
        public const int IS_CM_SENSOR_RAW12 = IS_SET_CM_BAYER12;    // occupies 16 Bit
        public const int IS_CM_SENSOR_RAW16 = IS_SET_CM_BAYER16;    // occupies 16 Bit

        public const int IS_CM_BGR555_PACKED = (IS_SET_CM_RGB15 | IS_CM_ORDER_BGR | IS_CM_FORMAT_PACKED);   // occupies 16 Bit
        public const int IS_CM_BGR565_PACKED = (IS_SET_CM_RGB16 | IS_CM_ORDER_BGR | IS_CM_FORMAT_PACKED);   // occupies 16 Bit 

        public const int IS_CM_RGB8_PACKED = (IS_SET_CM_RGB24 | IS_CM_ORDER_RGB | IS_CM_FORMAT_PACKED);     // occupies 24 Bit
        public const int IS_CM_BGR8_PACKED = (IS_SET_CM_RGB24 | IS_CM_ORDER_BGR | IS_CM_FORMAT_PACKED);     // occupies 24 Bit  
        public const int IS_CM_RGBA8_PACKED = (IS_SET_CM_RGB32 | IS_CM_ORDER_RGB | IS_CM_FORMAT_PACKED);    // occupies 32 Bit
        public const int IS_CM_BGRA8_PACKED = (IS_SET_CM_RGB32 | IS_CM_ORDER_BGR | IS_CM_FORMAT_PACKED);    // occupies 32 Bit
        public const int IS_CM_RGBY8_PACKED = (IS_SET_CM_RGBY | IS_CM_ORDER_RGB | IS_CM_FORMAT_PACKED);     // occupies 32 Bit
        public const int IS_CM_BGRY8_PACKED = (IS_SET_CM_RGBY | IS_CM_ORDER_BGR | IS_CM_FORMAT_PACKED);     // occupies 32 Bit
        public const int IS_CM_RGB10V2_PACKED = (IS_SET_CM_RGB30 | IS_CM_ORDER_RGB | IS_CM_FORMAT_PACKED);  // occupies 32 Bit
        public const int IS_CM_BGR10V2_PACKED = (IS_SET_CM_RGB30 | IS_CM_ORDER_BGR | IS_CM_FORMAT_PACKED);  // occupies 32 Bit

        //public const int IS_CM_YUV422_PACKED;		//no compliant version      
        public const int IS_CM_UYVY_PACKED = (IS_SET_CM_UYVY | IS_CM_FORMAT_PACKED);			            // occupies 16 Bit
        public const int IS_CM_UYVY_MONO_PACKED = (IS_SET_CM_UYVY_MONO | IS_CM_FORMAT_PACKED);
        public const int IS_CM_UYVY_BAYER_PACKED = (IS_SET_CM_UYVY_BAYER | IS_CM_FORMAT_PACKED);
        public const int IS_CM_CBYCRY_PACKED = (IS_SET_CM_CBYCRY | IS_CM_FORMAT_PACKED);                    // occupies 16 Bit

        //public const int IS_CM_RGB8_PLANAR        //no compliant version
        //public const int IS_CM_RGB12_PLANAR       //no compliant version
        //public const int IS_CM_RGB16_PLANAR       //no compliant version

        public const int IS_CM_ALL_POSSIBLE = 0xFFFF;
        #endregion

        #region Hotpixel correction
        // Deprecated defines 
        public const int IS_GET_BPC_MODE = 0x8000;
        public const int IS_GET_BPC_THRESHOLD = 0x8001;
        public const int IS_GET_BPC_SUPPORTED_MODE = 0x8002;

        public const int IS_BPC_DISABLE = 0;
        public const int IS_BPC_ENABLE_LEVEL_1 = 1;
        public const int IS_BPC_ENABLE_LEVEL_2 = 2;
        public const int IS_BPC_ENABLE_USER = 4;
        public const int IS_BPC_ENABLE_SOFTWARE = IS_BPC_ENABLE_LEVEL_2;
        public const int IS_BPC_ENABLE_HARDWARE = IS_BPC_ENABLE_LEVEL_1;

        public const int IS_SET_BADPIXEL_LIST = 0x01;
        public const int IS_GET_BADPIXEL_LIST = 0x02;
        public const int IS_GET_LIST_SIZE = 0x03;

        // Valid defines
        public const int IS_HOTPIXEL_DISABLE_CORRECTION = 0x0000;
        public const int IS_HOTPIXEL_ENABLE_SENSOR_CORRECTION = 0x0001;
        public const int IS_HOTPIXEL_ENABLE_CAMERA_CORRECTION = 0x0002;
        public const int IS_HOTPIXEL_ENABLE_SOFTWARE_USER_CORRECTION = 0x0004;

        public const int IS_HOTPIXEL_GET_CORRECTION_MODE = 0x8000;
        public const int IS_HOTPIXEL_GET_SUPPORTED_CORRECTION_MODES = 0x8001;

        public const int IS_HOTPIXEL_GET_SOFTWARE_USER_LIST_EXISTS = 0x8100;
        public const int IS_HOTPIXEL_GET_SOFTWARE_USER_LIST_NUMBER = 0x8101;
        public const int IS_HOTPIXEL_GET_SOFTWARE_USER_LIST = 0x8102;
        public const int IS_HOTPIXEL_SET_SOFTWARE_USER_LIST = 0x8103;
        public const int IS_HOTPIXEL_SAVE_SOFTWARE_USER_LIST = 0x8104;
        public const int IS_HOTPIXEL_LOAD_SOFTWARE_USER_LIST = 0x8105;

        public const int IS_HOTPIXEL_GET_CAMERA_FACTORY_LIST_EXISTS = 0x8106;
        public const int IS_HOTPIXEL_GET_CAMERA_FACTORY_LIST_NUMBER = 0x8107;
        public const int IS_HOTPIXEL_GET_CAMERA_FACTORY_LIST = 0x8108;

        public const int IS_HOTPIXEL_GET_CAMERA_USER_LIST_EXISTS = 0x8109;
        public const int IS_HOTPIXEL_GET_CAMERA_USER_LIST_NUMBER = 0x810A;
        public const int IS_HOTPIXEL_GET_CAMERA_USER_LIST = 0x810B;
        public const int IS_HOTPIXEL_SET_CAMERA_USER_LIST = 0x810C;
        public const int IS_HOTPIXEL_GET_CAMERA_USER_LIST_MAX_NUMBER = 0x810D;
        public const int IS_HOTPIXEL_DELETE_CAMERA_USER_LIST = 0x810E;

        public const int IS_HOTPIXEL_GET_MERGED_CAMERA_LIST_NUMBER = 0x810F;
        public const int IS_HOTPIXEL_GET_MERGED_CAMERA_LIST = 0x8110;
        #endregion

        #region color correction definitions
        public const int IS_GET_CCOR_MODE = 0x8000;

        public const int IS_GET_SUPPORTED_CCOR_MODE = 0x8001;
        public const int IS_GET_DEFAULT_CCOR_MODE = 0x8002;
        public const int IS_GET_CCOR_FACTOR = 0x8003;
        public const int IS_GET_CCOR_FACTOR_MIN = 0x8004;
        public const int IS_GET_CCOR_FACTOR_MAX = 0x8005;
        public const int IS_GET_CCOR_FACTOR_DEFAULT = 0x8006;

        public const int IS_CCOR_DISABLE = 0x0;
        public const int IS_CCOR_ENABLE = 0x1;

        public const int IS_CCOR_ENABLE_NORMAL = IS_CCOR_ENABLE;
        public const int IS_CCOR_ENABLE_BG40_ENHANCED = 0x0002;
        public const int IS_CCOR_ENABLE_HQ_ENHANCED = 0x0004;
        public const int IS_CCOR_SET_IR_AUTOMATIC = 0x0080;
        public const int IS_CCOR_FACTOR = 0x0100;

        public const int IS_CCOR_ENABLE_MASK = (IS_CCOR_ENABLE_NORMAL | IS_CCOR_ENABLE_BG40_ENHANCED | IS_CCOR_ENABLE_HQ_ENHANCED);
        #endregion

        #region bayer algorithm modes
        public const int IS_GET_BAYER_CV_MODE = 0x8000;

        public const int IS_SET_BAYER_CV_NORMAL = 0x0000;
        public const int IS_SET_BAYER_CV_BETTER = 0x0001;
        public const int IS_SET_BAYER_CV_BEST = 0x0002;
        #endregion

        #region color converter modes
        public const int IS_CONV_MODE_NONE = 0x0000;
        public const int IS_CONV_MODE_SOFTWARE = 0x0001;
        public const int IS_CONV_MODE_SOFTWARE_3X3 = 0x0002;
        public const int IS_CONV_MODE_SOFTWARE_5X5 = 0x0004;
        public const int IS_CONV_MODE_HARDWARE_3X3 = 0x0008;
        public const int IS_CONV_MODE_OPENCL_3X3 = 0x0020;
        public const int IS_CONV_MODE_OPENCL_5X5 = 0x0040;
        #endregion

        #region Edge enhancement
        public const int IS_GET_EDGE_ENHANCEMENT = 0x8000;

        public const int IS_EDGE_EN_DISABLE = 0;
        public const int IS_EDGE_EN_STRONG = 1;
        public const int IS_EDGE_EN_WEAK = 2;
        #endregion

        #region white balance modes
        public const int IS_GET_WB_MODE = 0x8000;

        public const int IS_SET_WB_DISABLE = 0x0;
        public const int IS_SET_WB_USER = 0x1;
        public const int IS_SET_WB_AUTO_ENABLE = 0x2;
        public const int IS_SET_WB_AUTO_ENABLE_ONCE = 0x4;

        public const int IS_SET_WB_DAYLIGHT_65 = 0x101;
        public const int IS_SET_WB_COOL_WHITE = 0x102;
        public const int IS_SET_WB_U30 = 0x103;
        public const int IS_SET_WB_ILLUMINANT_A = 0x104;
        public const int IS_SET_WB_HORIZON = 0x105;
        #endregion

        #region flash strobe constants
        public const int IS_GET_FLASHSTROBE_MODE = 0x8000;
        public const int IS_GET_FLASHSTROBE_LINE = 0x8001;
        public const int IS_GET_SUPPORTED_FLASH_IO_PORTS = 0x8002;

        public const int IS_SET_FLASH_OFF = 0;
        public const int IS_SET_FLASH_ON = 1;
        public const int IS_SET_FLASH_LO_ACTIVE = IS_SET_FLASH_ON;
        public const int IS_SET_FLASH_HI_ACTIVE = 2;
        public const int IS_SET_FLASH_HIGH = 3;
        public const int IS_SET_FLASH_LOW = 4;
        public const int IS_SET_FLASH_LO_ACTIVE_FREERUN = 5;
        public const int IS_SET_FLASH_HI_ACTIVE_FREERUN = 6;
        public const int IS_SET_FLASH_IO_1 = 0x0010;
        public const int IS_SET_FLASH_IO_2 = 0x0020;
        public const int IS_SET_FLASH_IO_3 = 0x0040;
        public const int IS_SET_FLASH_IO_4 = 0x0080;
        public const int IS_FLASH_IO_PORT_MASK = (IS_SET_FLASH_IO_1 | IS_SET_FLASH_IO_2 | IS_SET_FLASH_IO_3 | IS_SET_FLASH_IO_4);


        public const int IS_GET_FLASH_DELAY = -1;
        public const int IS_GET_FLASH_DURATION = -2;
        public const int IS_GET_MAX_FLASH_DELAY = -3;
        public const int IS_GET_MAX_FLASH_DURATION = -4;
        public const int IS_GET_MIN_FLASH_DELAY = -5;
        public const int IS_GET_MIN_FLASH_DURATION = -6;
        public const int IS_GET_FLASH_DELAY_GRANULARITY = -7;
        public const int IS_GET_FLASH_DURATION_GRANULARITY = -8;
        #endregion

        #region Digital IO constants
        public const int IS_GET_IO = 0x8000;
        public const int IS_GET_IO_MASK = 0x8000;
        public const int IS_GET_SUPPORTED_IO_PORTS = 0x8004;
        #endregion

        #region EEPROM defines
        public const int IS_EEPROM_MIN_USER_ADDRESS = 0;
        public const int IS_EEPROM_MAX_USER_ADDRESS = 63;
        public const int IS_EEPROM_MAX_USER_SPACE = 64;
        #endregion

        #region error report modes
        public const int IS_GET_ERR_REP_MODE = 0x8000;
        public const int IS_DISABLE_ERR_REP = 0;
        public const int IS_ENABLE_ERR_REP = 1;
        #endregion

        #region display mode slectors
        public const int IS_GET_DISPLAY_MODE = 0x8000;
        public const int IS_GET_DISPLAY_SIZE_X = 0x8000;
        public const int IS_GET_DISPLAY_SIZE_Y = 0x8001;
        public const int IS_GET_DISPLAY_POS_X = 0x8000;
        public const int IS_GET_DISPLAY_POS_Y = 0x8001;

        public const int IS_SET_DM_DIB = 0x1;
        public const int IS_SET_DM_DIRECTDRAW = 0x2;
        public const int IS_SET_DM_DIRECT3D = 0x4;
        public const int IS_SET_DM_ALLOW_SYSMEM = 0x40;
        public const int IS_SET_DM_ALLOW_PRIMARY = 0x80;

        // -- overlay display mode ---
        public const int IS_GET_DD_OVERLAY_SCALE = 0x8000;

        public const int IS_SET_DM_ALLOW_OVERLAY = 0x100;
        public const int IS_SET_DM_ALLOW_SCALING = 0x200;
        public const int IS_SET_DM_MONO = 0x800;
        public const int IS_SET_DM_BAYER = 0x1000;
        public const int IS_SET_DM_YCBCR = 0x4000;

        // -- backbuffer display mode ---
        public const int IS_SET_DM_BACKBUFFER = 0x2000;
        #endregion

        #region DirectRenderer commands
        public const int DR_GET_OVERLAY_DC = 1;
        public const int DR_GET_MAX_OVERLAY_SIZE = 2;
        public const int DR_GET_OVERLAY_KEY_COLOR = 3;
        public const int DR_RELEASE_OVERLAY_DC = 4;
        public const int DR_SHOW_OVERLAY = 5;
        public const int DR_HIDE_OVERLAY = 6;
        public const int DR_SET_OVERLAY_SIZE = 7;
        public const int DR_SET_OVERLAY_POSITION = 8;
        public const int DR_SET_OVERLAY_KEY_COLOR = 9;
        public const int DR_SET_HWND = 10;
        public const int DR_ENABLE_SCALING = 11;
        public const int DR_DISABLE_SCALING = 12;
        public const int DR_CLEAR_OVERLAY = 13;
        public const int DR_ENABLE_SEMI_TRANSPARENT_OVERLAY = 14;
        public const int DR_DISABLE_SEMI_TRANSPARENT_OVERLAY = 15;
        public const int DR_CHECK_COMPATIBILITY = 16;
        public const int DR_SET_VSYNC_OFF = 17;
        public const int DR_SET_VSYNC_AUTO = 18;
        public const int DR_SET_USER_SYNC = 19;
        public const int DR_GET_USER_SYNC_POSITION_RANGE = 20;
        public const int DR_LOAD_OVERLAY_FROM_FILE = 21;
        public const int DR_STEAL_NEXT_FRAME = 22;
        public const int DR_SET_STEAL_FORMAT = 23;
        public const int DR_GET_STEAL_FORMAT = 24;
        public const int DR_ENABLE_IMAGE_SCALING = 25;
        public const int DR_GET_OVERLAY_SIZE = 26;
        public const int DR_CHECK_COLOR_MODE_SUPPORT = 27;
        #endregion

        #region DirectDraw keying color constants
        public const int IS_GET_KC_RED = 0x8000;
        public const int IS_GET_KC_GREEN = 0x8001;
        public const int IS_GET_KC_BLUE = 0x8002;
        public const int IS_GET_KC_RGB = 0x8003;
        public const int IS_GET_KC_INDEX = 0x8004;

        public const int IS_SET_KC_DEFAULT = 0xFF00FF;
        public const int IS_SET_KC_DEFAULT_8 = 253;
        #endregion

        #region memoryboard
        public const int IS_MEMORY_GET_COUNT = 0x8000;
        public const int IS_MEMORY_GET_DELAY = 0x8001;
        public const int IS_MEMORY_MODE_DISABLE = 0x0;
        public const int IS_MEMORY_USE_TRIGGER = 0xFFFF;
        #endregion

        #region Testimage modes
        public const int IS_GET_TEST_IMAGE = 0x8000;

        public const int IS_SET_TEST_IMAGE_DISABLED = 0x0000;
        public const int IS_SET_TEST_IMAGE_MEMORY_1 = 0x0001;
        public const int IS_SET_TEST_IMAGE_MEMORY_2 = 0x0002;
        public const int IS_SET_TEST_IMAGE_MEMORY_3 = 0x0003;
        #endregion

        #region Led settings
        public const int IS_SET_LED_OFF = 0x0000;
        public const int IS_SET_LED_ON = 0x0001;
        public const int IS_SET_LED_TOGGLE = 0x0002;
        public const int IS_GET_LED = 0x8000;
        #endregion

        #region save options
        public const int IS_SAVE_USE_ACTUAL_IMAGE_SIZE = 0x00010000;
        #endregion

        #region renumeration modes
        public const int IS_RENUM_BY_CAMERA = 0;
        public const int IS_RENUM_BY_HOST = 1;
        #endregion

        #region event constants
        public const int IS_SET_EVENT_FRAME = 2;
        public const int IS_SET_EVENT_EXTTRIG = 3;
        public const int IS_SET_EVENT_VSYNC = 4;
        public const int IS_SET_EVENT_SEQ = 5;
        public const int IS_SET_EVENT_STEAL = 6;
        public const int IS_SET_EVENT_TRANSFER_FAILED = 8;
        public const int IS_SET_EVENT_CAPTURE_STATUS = 8;
        public const int IS_SET_EVENT_DEVICE_RECONNECTED = 9;
        public const int IS_SET_EVENT_MEMORY_MODE_FINISH = 10;
        public const int IS_SET_EVENT_FRAME_RECEIVED = 11;
        public const int IS_SET_EVENT_WB_FINISHED = 12;
        public const int IS_SET_EVENT_AUTOBRIGHTNESS_FINISHED = 13;
        public const int IS_SET_EVENT_OVERLAY_DATA_LOST = 16;

        public const int IS_SET_EVENT_REMOVE = 128;
        public const int IS_SET_EVENT_REMOVAL = 129;
        public const int IS_SET_EVENT_NEW_DEVICE = 130;
        public const int IS_SET_EVENT_STATUS_CHANGED = 131;
        #endregion

        #region Window message defines
        public const int IS_UEYE_MESSAGE = 1280;    // WM_USER = 0x400, WM_USER + 0x100 = 0x500 (1280)
        public const int IS_FRAME = 0x0;
        public const int IS_SEQUENCE = 0x1;
        public const int IS_TRIGGER = 0x2;
        public const int IS_TRANSFER_FAILED = 0x3;
        public const int IS_CAPTURE_STATUS = 0x3;
        public const int IS_DEVICE_RECONNECTED = 0x4;
        public const int IS_MEMORY_MODE_FINISH = 0x5;
        public const int IS_FRAME_RECEIVED = 0x6;
        public const int IS_GENERIC_ERROR = 0x7;
        public const int IS_STEAL_VIDEO = 0x8;
        public const int IS_WB_FINISHED = 0x9;
        public const int IS_AUTOBRIGHTNESS_FINISHED = 0xA;
        public const int IS_OVERLAY_DATA_LOST = 0xB;

        public const int IS_DEVICE_REMOVED = 0x1000;
        public const int IS_DEVICE_REMOVAL = 0x1001;
        public const int IS_NEW_DEVICE = 0x1002;
        public const int IS_DEVICE_STATUS_CHANGED = 0x1003;
        #endregion

        #region camera id constants
        public const int IS_GET_CAMERA_ID = 0x8000;
        #endregion

        #region camera info constants
        public const int IS_GET_STATUS = 0x8000;

        public const int IS_EXT_TRIGGER_EVENT_CNT = 0;
        public const int IS_FIFO_OVR_CNT = 1;
        public const int IS_SEQUENCE_CNT = 2;
        public const int IS_LAST_FRAME_FIFO_OVR = 3;
        public const int IS_SEQUENCE_SIZE = 4;
        public const int IS_STEAL_FINISHED = 6;
        public const int IS_BOARD_REVISION = 9;
        public const int IS_MIRROR_BITMAP_UPDOWN = 10;
        public const int IS_BUS_OVR_CNT = 11;
        public const int IS_STEAL_ERROR_CNT = 12;
        public const int IS_WAIT_TIMEOUT = 19;
        public const int IS_TRIGGER_MISSED = 20;
        public const int IS_LAST_CAPTURE_ERROR = 21;
        public const int IS_PARAMETER_SET_1 = 22;
        public const int IS_PARAMETER_SET_2 = 23;
        public const int IS_STANDBY = 24;
        public const int IS_STANDBY_SUPPORTED = 25;
        public const int IS_QUEUED_IMAGE_EVENT_CNT = 26;
        #endregion

        #region interface type defines
        public const int IS_INTERFACE_TYPE_USB = 0x40;
        public const int IS_INTERFACE_TYPE_ETH = 0x80;
        #endregion

        #region board type defines
        public const int IS_BOARD_TYPE_FALCON = 1;
        public const int IS_BOARD_TYPE_EAGLE = 2;
        public const int IS_BOARD_TYPE_FALCON2 = 3;
        public const int IS_BOARD_TYPE_FALCON_PLUS = 7;
        public const int IS_BOARD_TYPE_FALCON_QUATTRO = 9;
        public const int IS_BOARD_TYPE_FALCON_DUO = 10;
        public const int IS_BOARD_TYPE_EAGLE_QUATTRO = 11;
        public const int IS_BOARD_TYPE_EAGLE_DUO = 12;
        public const int IS_BOARD_TYPE_UEYE_USB = (IS_INTERFACE_TYPE_USB + 0);	    // 0x40
        public const int IS_BOARD_TYPE_UEYE_USB_SE = IS_BOARD_TYPE_UEYE_USB;		// 0x40
        public const int IS_BOARD_TYPE_UEYE_USB_RE = IS_BOARD_TYPE_UEYE_USB;		// 0x40
        public const int IS_BOARD_TYPE_UEYE_USB_ME = (IS_INTERFACE_TYPE_USB + 1);	// 0x41
        public const int IS_BOARD_TYPE_UEYE_USB_LE = (IS_INTERFACE_TYPE_USB + 2);   // 0x42
        public const int IS_BOARD_TYPE_UEYE_USB_XS = (IS_INTERFACE_TYPE_USB + 3);   // 0x43
        public const int IS_BOARD_TYPE_UEYE_ETH = IS_INTERFACE_TYPE_ETH;            // 0x80
        public const int IS_BOARD_TYPE_UEYE_ETH_HE = IS_BOARD_TYPE_UEYE_ETH;        // 0x80
        public const int IS_BOARD_TYPE_UEYE_ETH_SE = (IS_INTERFACE_TYPE_ETH + 1);   // 0x81
        public const int IS_BOARD_TYPE_UEYE_ETH_RE = IS_BOARD_TYPE_UEYE_ETH_SE;	    // 0x81
        public const int IS_BOARD_TYPE_UEYE_ETH_CP = IS_BOARD_TYPE_UEYE_ETH + 4;    // 0x84
        #endregion

        #region camera type defines
        public const int IS_CAMERA_TYPE_UEYE_USB = IS_BOARD_TYPE_UEYE_USB_SE;
        public const int IS_CAMERA_TYPE_UEYE_USB_SE = IS_BOARD_TYPE_UEYE_USB_SE;
        public const int IS_CAMERA_TYPE_UEYE_USB_RE = IS_BOARD_TYPE_UEYE_USB_RE;
        public const int IS_CAMERA_TYPE_UEYE_USB_ME = IS_BOARD_TYPE_UEYE_USB_ME;
        public const int IS_CAMERA_TYPE_UEYE_USB_LE = IS_BOARD_TYPE_UEYE_USB_LE;
        public const int IS_CAMERA_TYPE_UEYE_ETH = IS_BOARD_TYPE_UEYE_ETH_HE;
        public const int IS_CAMERA_TYPE_UEYE_ETH_HE = IS_BOARD_TYPE_UEYE_ETH_HE;
        public const int IS_CAMERA_TYPE_UEYE_ETH_SE = IS_BOARD_TYPE_UEYE_ETH_SE;
        public const int IS_CAMERA_TYPE_UEYE_ETH_RE = IS_BOARD_TYPE_UEYE_ETH_RE;
        public const int IS_CAMERA_TYPE_UEYE_ETH_CP = IS_BOARD_TYPE_UEYE_ETH_CP;
        #endregion

        #region readable operation system defines
        public const int IS_OS_UNDETERMINED = 0;
        public const int IS_OS_WIN95 = 1;
        public const int IS_OS_WINNT40 = 2;
        public const int IS_OS_WIN98 = 3;
        public const int IS_OS_WIN2000 = 4;
        public const int IS_OS_WINXP = 5;
        public const int IS_OS_WINME = 6;
        public const int IS_OS_WINNET = 7;
        public const int IS_OS_WINSERVER2003 = 8;
        public const int IS_OS_WINVISTA = 9;
        public const int IS_OS_LINUX24 = 10;
        public const int IS_OS_LINUX26 = 11;
        public const int IS_OS_WIN7 = 12;
        #endregion

        #region usb bus speed
        public const int IS_USB_10 = 0x0001;
        public const int IS_USB_11 = 0x0002;
        public const int IS_USB_20 = 0x0004;
        public const int IS_USB_30 = 0x0008;
        public const int IS_ETHERNET_10 = 0x0080;
        public const int IS_ETHERNET_100 = 0x0100;
        public const int IS_ETHERNET_1000 = 0x0200;
        public const int IS_ETHERNET_10000 = 0x0400;

        public const int IS_USB_LOW_SPEED = 1;
        public const int IS_USB_FULL_SPEED = 12;
        public const int IS_USB_HIGH_SPEED = 480;
        public const int IS_USB_SUPER_SPEED = 5000;
        public const int IS_ETHERNET_10Base = 10;
        public const int IS_ETHERNET_100Base = 100;
        public const int IS_ETHERNET_1000Base = 1000;
        public const int IS_ETHERNET_10GBase = 10000;
        #endregion

        #region HDR
        public const int IS_HDR_NOT_SUPPORTED = 0;
        public const int IS_HDR_KNEEPOINTS = 1;
        public const int IS_DISABLE_HDR = 0;
        public const int IS_ENABLE_HDR = 1;
        #endregion

        #region Test images
        public const int IS_TEST_IMAGE_NONE = 0x00000000;
        public const int IS_TEST_IMAGE_WHITE = 0x00000001;
        public const int IS_TEST_IMAGE_BLACK = 0x00000002;
        public const int IS_TEST_IMAGE_HORIZONTAL_GREYSCALE = 0x00000004;
        public const int IS_TEST_IMAGE_VERTICAL_GREYSCALE = 0x00000008;
        public const int IS_TEST_IMAGE_DIAGONAL_GREYSCALE = 0x00000010;
        public const int IS_TEST_IMAGE_WEDGE_GRAY = 0x00000020;
        public const int IS_TEST_IMAGE_WEDGE_COLOR = 0x00000040;
        public const int IS_TEST_IMAGE_ANIMATED_WEDGE_GRAY = 0x00000080;

        public const int IS_TEST_IMAGE_ANIMATED_WEDGE_COLOR = 0x00000100;
        public const int IS_TEST_IMAGE_MONO_BARS = 0x00000200;
        public const int IS_TEST_IMAGE_COLOR_BARS1 = 0x00000400;
        public const int IS_TEST_IMAGE_COLOR_BARS2 = 0x00000800;
        public const int IS_TEST_IMAGE_GREYSCALE1 = 0x00001000;
        public const int IS_TEST_IMAGE_GREY_AND_COLOR_BARS = 0x00002000;
        public const int IS_TEST_IMAGE_MOVING_GREY_AND_COLOR_BARS = 0x00004000;
        public const int IS_TEST_IMAGE_ANIMATED_LINE = 0x00008000;

        public const int IS_TEST_IMAGE_ALTERNATE_PATTERN = 0x00010000;
        public const int IS_TEST_IMAGE_VARIABLE_GREY = 0x00020000;
        public const int IS_TEST_IMAGE_MONOCHROME_HORIZONTAL_BARS = 0x00040000;
        public const int IS_TEST_IMAGE_MONOCHROME_VERTICAL_BARS = 0x00080000;
        public const int IS_TEST_IMAGE_CURSOR_H = 0x00100000;
        public const int IS_TEST_IMAGE_CURSOR_V = 0x00200000;
        public const int IS_TEST_IMAGE_COLDPIXEL_GRID = 0x00400000;
        public const int IS_TEST_IMAGE_HOTPIXEL_GRID = 0x00800000;

        public const int IS_TEST_IMAGE_VARIABLE_RED_PART = 0x01000000;
        public const int IS_TEST_IMAGE_VARIABLE_GREEN_PART = 0x02000000;
        public const int IS_TEST_IMAGE_VARIABLE_BLUE_PART = 0x04000000;
        public const int IS_TEST_IMAGE_SHADING_IMAGE = 0x08000000;
        public const int IS_TEST_IMAGE_WEDGE_GRAY_SENSOR = 0x10000000;
        public const int IS_TEST_IMAGE_ANIMATED_WEDGE_GRAY_SENSOR = 0x20000000;
        public const int IS_TEST_IMAGE_RAMPING_PATTERN = 0x40000000;
        //															= 0x80000000;
        #endregion

        #region Sensor scaler
        public const int IS_ENABLE_SENSOR_SCALER = 1;
        public const int IS_ENABLE_ANTI_ALIASING = 2;
        #endregion

        #region Timeouts
        public const int IS_TRIGGER_TIMEOUT = 0;
        #endregion

        #region SetOptimalCameraTiming
        public const int IS_BEST_PCLK_RUN_ONCE = 0;
        #endregion

        #region sequence flags
        public const int IS_LOCK_LAST_BUFFER = 0x8002;
        public const int IS_GET_ALLOC_ID_OF_THIS_BUF = 0x8004;
        public const int IS_GET_ALLOC_ID_OF_LAST_BUF = 0x8008;
        public const int IS_USE_ALLOC_ID = 0x8000;
        public const int IS_USE_CURRENT_IMG_SIZE = 0xC000;
        #endregion

        #region Memory information flags
        public const int IS_GET_D3D_MEM = 0x8000;
        #endregion

        #region Capture status
        public const int IS_CAPTURE_STATUS_INFO_CMD_RESET = 1;
        public const int IS_CAPTURE_STATUS_INFO_CMD_GET = 2;
        #endregion

        #region auto feature structs and definitions
        public const int AC_SHUTTER = 0x00000001;
        public const int AC_GAIN = 0x00000002;
        public const int AC_WHITEBAL = 0x00000004;
        public const int AC_WB_RED_CHANNEL = 0x00000008;
        public const int AC_WB_GREEN_CHANNEL = 0x00000010;
        public const int AC_WB_BLUE_CHANNEL = 0x00000020;
        public const int AC_FRAMERATE = 0x00000040;
        public const int AC_SENSOR_SHUTTER = 0x00000080;
        public const int AC_SENSOR_GAIN = 0x00000100;
        public const int AC_SENSOR_GAIN_SHUTTER = 0x00000200;
        public const int AC_SENSOR_FRAMERATE = 0x00000400;
        public const int AC_SENSOR_WB = 0x00000800;
        public const int AC_SENSOR_AUTO_REFERENCE = 0x00001000;
        public const int AC_SENSOR_AUTO_SPEED = 0x00002000;
        public const int AC_SENSOR_AUTO_HYSTERESIS = 0x00004000;
        public const int AC_SENSOR_AUTO_SKIPFRAMES = 0x00008000;
        public const int AC_SENSOR_AUTO_CONTRAST_CORRECTION = 0x00010000;
        public const int AC_SENSOR_AUTO_CONTRAST_FDT_AOI = 0x00020000;
        public const int AC_SENSOR_AUTO_BACKLIGHT_COMP = 0x00040000;

        public const int ACS_ADJUSTING = 0x00000001;
        public const int ACS_FINISHED = 0x00000002;
        public const int ACS_DISABLED = 0x00000004;
        #endregion

        #region Global Shutter definitions
        public const int IS_SET_GLOBAL_SHUTTER_ON = 0x0001;
        public const int IS_SET_GLOBAL_SHUTTER_OFF = 0x0000;
        public const int IS_GET_GLOBAL_SHUTTER = 0x0010;
        public const int IS_GET_SUPPORTED_GLOBAL_SHUTTER = 0x0020;
        #endregion

        #region auto shutter photometry capabilities
        public const int AS_PM_NONE = 0;
        public const int AS_PM_SENS_CENTER_WEIGHTED = 0x00000001; // sensor auto shutter: center weighted 
        public const int AS_PM_SENS_CENTER_SPOT = 0x00000002; // sensor auto shutter: center spot
        public const int AS_PM_SENS_PORTRAIT = 0x00000004; // sensor auto shutter: portrait
        public const int AS_PM_SENS_LANDSCAPE = 0x00000008; // sensor auto shutter: landscape
        #endregion

        #region auto gain photometry capabilities
        public const int AG_PM_NONE = 0;
        public const int AG_PM_SENS_CENTER_WEIGHTED = 0x00000001; // sensor auto gain: center weighted 
        public const int AG_PM_SENS_CENTER_SPOT = 0x00000002; // sensor auto gain: center spot
        public const int AG_PM_SENS_PORTRAIT = 0x00000004; // sensor auto gain: portrait
        public const int AG_PM_SENS_LANDSCAPE = 0x00000008; // sensor auto gain: landscape
        #endregion

        #region anti flicker modes
        public const int ANTIFLCK_MODE_OFF = 0;
        public const int ANTIFLCK_MODE_SENS_AUTO = 0x00000001;
        public const int ANTIFLCK_MODE_SENS_50_FIXED = 0x00000002;
        public const int ANTIFLCK_MODE_SENS_60_FIXED = 0x00000004;
        #endregion

        #region whitebalance modes
        public const int WB_MODE_DISABLE = 0;
        public const int WB_MODE_AUTO = 0x00000001;
        public const int WB_MODE_ALL_PULLIN = 0x00000002;
        public const int WB_MODE_INCANDESCENT_LAMP = 0x00000004;
        public const int WB_MODE_FLUORESCENT_DL = 0x00000008;
        public const int WB_MODE_OUTDOOR_CLEAR_SKY = 0x00000010;
        public const int WB_MODE_OUTDOOR_CLOUDY = 0x00000020;
        #endregion
        #endregion

        #region Structures
        #region BOARDINFO structure
        public struct CAMINFO
        {
            public string SerNo;		    //12
            public string id;			    //20
            public string Version;		    //10
            public string Date;			    //12
            public byte Select;
            public byte Type;
            public string Reserverd;        //8
        }
        #endregion

        public struct SENSORINFO
        {
            public int SensorID;
            public string strSensorName;    //32
            public byte nColorMode;
            public int nMaxWidth;
            public int nMaxHeight;
            public bool bMasterGain;
            public bool bRGain;
            public bool bGGain;
            public bool bBGain;
            public bool bGlobShutter;
            public ushort wPixelSize;
            public string reserved;         //14
        }

        public struct REVISIONINFO
        {
            public int size;                // 2
            public int Sensor;              // 2
            public int Cypress;             // 2
            public long Blackfin;           // 4
            public int DspFirmware;         // 2
            // --12
            public int USB_Board;           // 2
            public int Sensor_Board;        // 2
            public int Processing_Board;    // 2
            public int Memory_Board;        // 2
            public int Housing;             // 2
            public int Filter;              // 2
            public int Timing_Board;        // 2
            public int Product;             // 2
            // --24
            public string reserved;         // --128
        }

        public struct UEYE_CAPTURE_ERROR_INFO
        {
            public long dwCapErrCnt_Total;
            public byte[] reserved;
            public long[] adwCapErrCnt_Detail; // access via UEYE_CAPTURE_ERROR	
        }

        public struct UEYE_CAPTURE_STATUS_INFO
        {
            public long dwCapStatusCnt_Total;
            public byte[] reserved;
            public long[] adwCapStatusCnt_Detail; // access via UEYE_CAPTURE_STATUS
        }

        // ***********************************************
        // UEYE_CAMERA_INFO structure
        // ***********************************************
        public struct UEYE_CAMERA_INFO
        {
            public long dwCameraID;         // this is the user defineable camera ID
            public long dwDeviceID;         // this is the systems enumeration ID
            public long dwSensorID;         // this is the sensor ID e.g. IS_SENSOR_UI141X_M
            public long dwInUse;            // flag, whether the camera is in use or not
            public string SerNo;            // serial numer of the camera 16
            public string Model;            // model name of the camera 16
            public long dwStatus;			// various flags with camera status
            public string dwReserved;
        }

        // usage of the list:
        // 1. call the DLL with .dwCount = 0
        // 2. DLL returns .dwCount = N  (N = number of available cameras)
        // 3. call DLL with .dwCount = N and a pointer to UEYE_CAMERA_LIST with
        //    and array of UEYE_CAMERA_INFO[N]
        // 4. DLL will fill in the array with the camera infos and
        //    will update the .dwCount member with the actual number of cameras
        //    because there may be a change in number of cameras between step 2 and 3
        // 5. check if there's a difference in actual .dwCount and formerly
        //    reported value of N and call DLL again with an updated array size
        // ***********************************************
        // UEYE_CAMERA_LIST structure
        // ***********************************************
        public struct UEYE_CAMERA_LIST
        {
            public long dwCount;
            public UEYE_CAMERA_INFO[] uci;
        }

        // ***********************************************
        // AUTO_BRIGHT_STATUS structure
        // ***********************************************
        public struct AUTO_BRIGHT_STATUS
        {
            public long curValue;             // current average greylevel
            public long curError;             // current auto brightness error
            public long curController;        // current active brightness controller -> AC_x
            public long curCtrlStatus;        // current control status -> ACS_x
        }

        // ***********************************************
        // AUTO_WB_STATUS structure
        // ***********************************************
        public struct AUTO_WB_CHANNEL_STATUS
        {
            public long curValue;             // current average greylevel
            public long curError;             // current auto wb error
            public long curCtrlStatus;        // current control status -> ACS_x
        }

        // ***********************************************
        // AUTO_WB_STATUS structure
        // ***********************************************
        public struct AUTO_WB_STATUS
        {
            public AUTO_WB_CHANNEL_STATUS RedChannel;
            public AUTO_WB_CHANNEL_STATUS GreenChannel;
            public AUTO_WB_CHANNEL_STATUS BlueChannel;
            public long curController;        // current active wb controller -> AC_x
        }

        // ***********************************************
        // UEYE_AUTO_INFO structure
        // ***********************************************
        public struct UEYE_AUTO_INFO
        {
            public long AutoAbility;                        // autocontrol ability
            public AUTO_BRIGHT_STATUS sBrightCtrlStatus;   // brightness autocontrol status
            public AUTO_WB_STATUS sWBCtrlStatus;           // white balance autocontrol status
            public long AShutterPhotomCaps;                 // auto shutter photometry capabilities(AUTO_SHUTTER_PHOTOM)
            public long AGainPhotomCaps;                    // auto gain photometry capabilities (AUTO_GAIN_PHOTOM)
            public long AAntiFlickerCaps;                   // auto anti-flicker capabilities
            public long SensorWBModeCaps;                   // white balance mode capabilities      
            public string reserved;
        }

        public struct RANGE_OF_VALUES_U32
        {
            public ulong u32Minimum;     /*!< \brief Minimum value. */
            public ulong u32Maximum;     /*!< \brief Maximum value, not considered Infinite. */
            public ulong u32Increment;   /*!< \brief Increment value. */
            public ulong u32Default;     /*!< \brief Default value. */
            public ulong u32Infinite;    /*!< \brief Infinite code, where applicable. */
        }
        #endregion

        #region Enumerations
        // ************************************************************************/
        // Capture errors                                                         */
        // ************************************************************************/
        enum UEYE_CAPTURE_ERROR
        {
            IS_CAPERR_API_NO_DEST_MEM = 0xa2,
            IS_CAPERR_API_CONVERSION_FAILED = 0xa3,
            IS_CAPERR_API_IMAGE_LOCKED = 0xa5,

            IS_CAPERR_DRV_OUT_OF_BUFFERS = 0xb2,
            IS_CAPERR_DRV_DEVICE_NOT_READY = 0xb4,

            IS_CAPERR_USB_TRANSFER_FAILED = 0xc7,

            IS_CAPERR_DEV_TIMEOUT = 0xd6,

            IS_CAPERR_ETH_BUFFER_OVERRUN = 0xe4,
            IS_CAPERR_ETH_MISSED_IMAGES = 0xe5
        }

        enum UEYE_CAPTURE_STATUS
        {
            IS_CAP_STATUS_API_NO_DEST_MEM = 0xa2,
            IS_CAP_STATUS_API_CONVERSION_FAILED = 0xa3,
            IS_CAP_STATUS_API_IMAGE_LOCKED = 0xa5,

            IS_CAP_STATUS_DRV_OUT_OF_BUFFERS = 0xb2,
            IS_CAP_STATUS_DRV_DEVICE_NOT_READY = 0xb4,

            IS_CAP_STATUS_USB_TRANSFER_FAILED = 0xc7,

            IS_CAP_STATUS_DEV_TIMEOUT = 0xd6,

            IS_CAP_STATUS_ETH_BUFFER_OVERRUN = 0xe4,
            IS_CAP_STATUS_ETH_MISSED_IMAGES = 0xe5
        }
        #endregion

        #region new datatypes only valid for uEye ETH - BEGIN
        // ***********************************************
        // IP V4 address
        // ***********************************************
        //using System.Runtime.InteropServices;
        [StructLayout(LayoutKind.Explicit)]
        public struct UEYE_ETH_ADDR_IPV4
        {
            [FieldOffset(0)]
            public byte by1;
            [FieldOffset(1)]
            public byte by2;
            [FieldOffset(2)]
            public byte by3;
            [FieldOffset(3)]
            public byte by4;

            [FieldOffset(0)]
            public uint dwAddr;
        }

        // ***********************************************
        // Ethernet address
        // ***********************************************
        public struct UEYE_ETH_ADDR_MAC
        {
            public string abyOctet;	// [6]
        }

        // ***********************************************
        // IP configuration
        // ***********************************************
        public struct UEYE_ETH_IP_CONFIGURATION
        {
            public UEYE_ETH_ADDR_IPV4 ipAddress;		// IP address
            public UEYE_ETH_ADDR_IPV4 ipSubnetmask;	// IP subnetmask
        }

        // ***********************************************
        // values for UEYE_ETH_DEVICE_INFO_HEARTBEAT::dwStatus
        // ***********************************************
        public const uint IS_ETH_DEVSTATUS_READY_TO_OPERATE = 0x00000001;	// device is ready to operate
        public const uint IS_ETH_DEVSTATUS_TESTING_IP_CURRENT = 0x00000002;	// device is (arp-)probing its current ip
        public const uint IS_ETH_DEVSTATUS_TESTING_IP_PERSISTENT = 0x00000004;	// device is (arp-)probing its persistent ip
        public const uint IS_ETH_DEVSTATUS_TESTING_IP_RANGE = 0x00000008;	// device is (arp-)probing the autocfg ip range

        public const uint IS_ETH_DEVSTATUS_INAPPLICABLE_IP_CURRENT = 0x00000010;	// current ip is inapplicable 
        public const uint IS_ETH_DEVSTATUS_INAPPLICABLE_IP_PERSISTENT = 0x00000020;	// persistent ip is inapplicable 
        public const uint IS_ETH_DEVSTATUS_INAPPLICABLE_IP_RANGE = 0x00000040;	// autocfg ip range is inapplicable 

        public const uint IS_ETH_DEVSTATUS_UNPAIRED = 0x00000100;	// device is unpaired
        public const uint IS_ETH_DEVSTATUS_PAIRING_IN_PROGRESS = 0x00000200;	// device is being paired
        public const uint IS_ETH_DEVSTATUS_PAIRED = 0x00000400;	// device is paired 

        public const uint IS_ETH_DEVSTATUS_FORCE_100MBPS = 0x00001000; // device phy is configured to 100 Mbps */
        public const uint IS_ETH_DEVSTATUS_NO_COMPORT = 0x00002000; // device does not support ueye eth comport */

        public const uint IS_ETH_DEVSTATUS_RECEIVING_FW_STARTER = 0x00010000;	// device is receiving the starter firmware
        public const uint IS_ETH_DEVSTATUS_RECEIVING_FW_RUNTIME = 0x00020000;	// device is receiving the runtime firmware
        public const uint IS_ETH_DEVSTATUS_INAPPLICABLE_FW_RUNTIME = 0x00040000;	// runtime firmware is inapplicable
        public const uint IS_ETH_DEVSTATUS_INAPPLICABLE_FW_STARTER = 0x00080000;	// starter firmware is inapplicable

        public const uint IS_ETH_DEVSTATUS_REBOOTING_FW_RUNTIME = 0x00100000;	// device is rebooting to runtime firmware
        public const uint IS_ETH_DEVSTATUS_REBOOTING_FW_STARTER = 0x00200000;	// device is rebooting to starter firmware
        public const uint IS_ETH_DEVSTATUS_REBOOTING_FW_FAILSAFE = 0x00400000;	// device is rebooting to failsafe firmware

        public const uint IS_ETH_DEVSTATUS_RUNTIME_FW_ERR0 = 0x80000000;	// checksum error runtime firmware

        // ***********************************************
        // heartbeat info transmitted periodically by a device
        // contained in UEYE_ETH_DEVICE_INFO	
        // ***********************************************
        public struct UEYE_ETH_DEVICE_INFO_HEARTBEAT
        {
            public string abySerialNumber;		// camera's serial number (string)	// [12]
            public byte byDeviceType;			// device type / board type, 0x80 for ETH
            public byte byCameraID;				// camera id
            public ushort wSensorID;				// size of camera's image memory in MB
            public ushort wSizeImgMem_MB;			// size of camera's image memory in MB
            public uint dwVerStarterFirmware;	// starter firmware version
            public uint dwVerRuntimeFirmware;	// runtime firmware version
            public uint dwStatus;				// camera status flags
            public ushort wTemperature;			// camera temperature
            public ushort wLinkSpeed_Mb;			// link speed in Mb
            public UEYE_ETH_ADDR_MAC macDevice;				// camera's MAC address
            public ushort wComportOffset;			// comport offset from 100, valid range -99 to +156
            public UEYE_ETH_IP_CONFIGURATION ipcfgPersistentIpCfg;	// persistent IP configuration
            public UEYE_ETH_IP_CONFIGURATION ipcfgCurrentIpCfg;		// current IP configuration
            public UEYE_ETH_ADDR_MAC macPairedHost;			// paired host's MAC address
            public UEYE_ETH_ADDR_IPV4 ipPairedHostIp;			// paired host's IP address
            public UEYE_ETH_ADDR_IPV4 ipAutoCfgIpRangeBegin;	// begin of IP address range
            public UEYE_ETH_ADDR_IPV4 ipAutoCfgIpRangeEnd;	// end of IP address range
            public string abyUserSpace;			// user space data (first 8 bytes)	// [8]
        }

        // ***********************************************
        // values for UEYE_ETH_DEVICE_INFO_CONTROL::dwControlStatus
        // ***********************************************
        public const uint IS_ETH_CTRLSTATUS_AVAILABLE = 0x00000001;	// device is available TO US
        public const uint IS_ETH_CTRLSTATUS_ACCESSIBLE1 = 0x00000002;	// device is accessible BY US, i.e. directly 'unicastable'
        public const uint IS_ETH_CTRLSTATUS_ACCESSIBLE2 = 0x00000004;	// device is accessible BY US, i.e. not on persistent ip and adapters ip autocfg range is valid
        public const uint IS_ETH_CTRLSTATUS_PERSISTENT_IP_USED = 0x00000010;	// device is running on persistent ip configuration
        public const uint IS_ETH_CTRLSTATUS_COMPATIBLE = 0x00000020;	// device is compatible TO US
        public const uint IS_ETH_CTRLSTATUS_ADAPTER_ON_DHCP = 0x00000040;	// adapter is configured to use dhcp
        public const uint IS_ETH_CTRLSTATUS_ADAPTER_SETUP_OK = 0x00000080; // adapter's setup is ok with respect to uEye needs
        public const uint IS_ETH_CTRLSTATUS_UNPAIRING_IN_PROGRESS = 0x00000100;	// device is being unpaired FROM US
        public const uint IS_ETH_CTRLSTATUS_PAIRING_IN_PROGRESS = 0x00000200;	// device is being paired TO US
        public const uint IS_ETH_CTRLSTATUS_PAIRED = 0x00001000;	// device is paired TO US
        public const uint IS_ETH_CTRLSTATUS_OPENED = 0x00004000;    // device is opened BY SELF
        public const uint IS_ETH_CTRLSTATUS_FW_UPLOAD_STARTER = 0x00010000;	// device is receiving the starter firmware
        public const uint IS_ETH_CTRLSTATUS_FW_UPLOAD_RUNTIME = 0x00020000;	// device is receiving the runtime firmware
        public const uint IS_ETH_CTRLSTATUS_REBOOTING = 0x00100000;	// device is rebooting
        public const uint IS_ETH_CTRLSTATUS_BOOTBOOST_ENABLED = 0x01000000; // boot-boosting is enabled for this device
        public const uint IS_ETH_CTRLSTATUS_BOOTBOOST_ACTIVE = 0x02000000; // boot-boosting is active for this device
        public const uint IS_ETH_CTRLSTATUS_INITIALIZED = 0x08000000;	// device object is initialized
        public const uint IS_ETH_CTRLSTATUS_TO_BE_DELETED = 0x40000000;	// device object is being deleted
        public const uint IS_ETH_CTRLSTATUS_TO_BE_REMOVED = 0x80000000;	// device object is being removed

        // ***********************************************
        // control info for a listed device
        // contained in UEYE_ETH_DEVICE_INFO
        // ***********************************************
        public struct UEYE_ETH_DEVICE_INFO_CONTROL
        {
            public uint dwDeviceID;			// device's unique id
            public uint dwControlStatus;	// device control status
        }

        // ***********************************************
        // Ethernet configuration
        // ***********************************************
        public struct UEYE_ETH_ETHERNET_CONFIGURATION
        {
            public UEYE_ETH_IP_CONFIGURATION ipcfg;
            public UEYE_ETH_ADDR_MAC mac;
        }

        // ***********************************************
        // autocfg ip setup
        // ***********************************************
        public struct UEYE_ETH_AUTOCFG_IP_SETUP
        {
            public UEYE_ETH_ADDR_IPV4 ipAutoCfgIpRangeBegin;	// begin of ip address range for devices
            public UEYE_ETH_ADDR_IPV4 ipAutoCfgIpRangeEnd;	// end of ip address range for devices
        }

        // ***********************************************
        // values for incoming packets filter setup
        // notice: arp and icmp (ping) packets are always passed!
        // ***********************************************
        public const int IS_ETH_PCKTFLT_PASSALL = 0;	// pass all packets to OS
        public const int IS_ETH_PCKTFLT_BLOCKUEGET = 1;	// block UEGET packets to the OS
        public const int IS_ETH_PCKTFLT_BLOCKALL = 2;	// block all packets to the OS

        // ***********************************************
        // control info for a device's network adapter
        // contained in UEYE_ETH_DEVICE_INFO
        // ***********************************************
        public struct UEYE_ETH_ADAPTER_INFO
        {
            public uint dwAdapterID;			// adapter's unique id
            public UEYE_ETH_ETHERNET_CONFIGURATION ethcfg;					// adapter's eth configuration
            public bool bIsEnabledDHCP;			// adapter's dhcp enabled flag
            public UEYE_ETH_AUTOCFG_IP_SETUP autoCfgIp;
            public bool bIsValidAutoCfgIpRange;	// the given range is valid when: 
            // - begin and end are valid ip addresses
            // - begin and end are in the subnet of the adapter
            public uint dwCntDevicesKnown;		// count of listed Known devices
            public uint dwCntDevicesPaired;		// count of listed Paired devices
            public ushort wPacketFilter;			// Setting for the Incoming Packets Filter. see UEYE_ETH_PACKETFILTER_SETUP enum above.
        }

        // ***********************************************
        // driver info
        // contained in UEYE_ETH_DEVICE_INFO
        // ***********************************************
        public struct UEYE_ETH_DRIVER_INFO
        {
            public uint dwMinVerStarterFirmware;	// minimum version compatible starter firmware
            public uint dwMaxVerStarterFirmware;	//maximum version compatible starter firmware
        }

        // ***********************************************
        // use GetEthDeviceInfo() to obtain this data.
        // ***********************************************
        public struct UEYE_ETH_DEVICE_INFO
        {
            public UEYE_ETH_DEVICE_INFO_HEARTBEAT infoDevHeartbeat;
            public UEYE_ETH_DEVICE_INFO_CONTROL infoDevControl;
            public UEYE_ETH_ADAPTER_INFO infoAdapter;
            public UEYE_ETH_DRIVER_INFO infoDriver;
        }


        // ***********************************************
        // ***********************************************
        public struct UEYE_COMPORT_CONFIGURATION
        {
            public int wComportNumber;
        }

        // --------------------------------------------------------------------
        // new datatypes only valid for uEye ETH - END
        // --------------------------------------------------------------------
        #endregion

        #region exports from uEye_api.dll
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_CaptureStatus")]
        private static extern int is_CaptureStatus(int hCam, uint nCommand, IntPtr pParam, uint SizeOfParam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_StopLiveVideo")]			// is_StopLiveVideo
        private static extern int is_StopLiveVideo(int hCam, int mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_FreezeVideo")]				// is_FreezeVideo
        private static extern int is_FreezeVideo(int hCam, int wait);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_CaptureVideo")]				// is_CaptureVideo
        private static extern int is_CaptureVideo(int hCam, int Wait);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_IsVideoFinish")]				// is_IsVideoFinish
        private static extern int is_IsVideoFinish(int hCam, ref int pValue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_HasVideoStarted")]			// is_HasVideoStarted
        private static extern int is_HasVideoStarted(int hCam, ref int pBool);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetBrightness")]				// is_SetBrightness
        private static extern int is_SetBrightness(int hCam, int bright);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetContrast")]				// is_SetContrast
        private static extern int is_SetContrast(int hCam, int Cont);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetGamma")]					// is_SetGamma
        private static extern int is_SetGamma(int hCam, int mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_AllocImageMem")]				// is_AllocImageMem
        private static extern int is_AllocImageMem(int hCam, int width, int height, int bits, ref IntPtr ppcImg, ref int pid);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_FreeImageMem")]				// is_FreeImageMem
        private static extern int is_FreeImageMem(int hCam, IntPtr pImgMem, int id);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetImageMem")]				// is_SetImageMem
        private static extern int is_SetImageMem(int hCam, IntPtr pcImg, int id);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetImageMem")]				// is_GetImageMem
        private static extern int is_GetImageMem(int hCam, ref IntPtr ppMem);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetActiveImageMem")]			// is_GetActiveImageMem
        private static extern int is_GetActiveImageMem(int hCam, ref IntPtr ppcMem, ref int pnID);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_InquireImageMem")]			// is_InquireImageMem
        private static extern int is_InquireImageMem(int hCam, IntPtr pcMem, int nID, ref int pnX, ref int pnY, ref int pnBits, ref int pnPitch);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetImageMemPitch")]			// is_GetImageMemPitch
        private static extern int is_GetImageMemPitch(int hCam, ref int pPitch);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetAllocatedImageMem")]		// is_SetAllocatedImageMem
        private static extern int is_SetAllocatedImageMem(int hCam, int width, int height, int bpp, IntPtr pImgMem, ref int id);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SaveImageMem")]				// is_SaveImageMem
        private static extern int is_SaveImageMem(int hCam, byte[] strFile, IntPtr pMem, int nId);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_CopyImageMem")]				// is_CopyImageMem
        private static extern int is_CopyImageMem(int hCam, IntPtr pcSource, int nID, IntPtr pcDest);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_CopyImageMemLines")]			// is_CopyImageMemLines
        private static extern int is_CopyImageMemLines(int hCam, IntPtr pcSource, int nID, int nLines, IntPtr pcDest);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_AddToSequence")]				// is_AddToSequence
        private static extern int is_AddToSequence(int hCam, IntPtr pMem, int nId);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ClearSequence")]				// is_ClearSequence
        private static extern int is_ClearSequence(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetActSeqBuf")]				// is_GetActSeqBuf
        private static extern int is_GetActSeqBuf(int hCam, ref int pnNum, ref IntPtr ppcMem, ref IntPtr ppcMemLast);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_LockSeqBuf")]				// is_LockSeqBuf
        private static extern int is_LockSeqBuf(int hCam, int nNum, IntPtr pcMem);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_UnlockSeqBuf")]				// is_UnlockSeqBuf
        private static extern int is_UnlockSeqBuf(int hCam, int nNum, IntPtr pcMem);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetImageSize")]				// is_SetImageSize
        private static extern int is_SetImageSize(int hCam, int X, int Y);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetImagePos")]				// is_SetImagePos
        private static extern int is_SetImagePos(int hCam, int X, int Y);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetError")]					// is_GetError
        private static extern int is_GetError(int hCam, ref int pErr, IntPtr ppcErr);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetErrorReport")]			// is_SetErrorReport
        private static extern int is_SetErrorReport(int hCam, int mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ReadEEPROM")]				// is_ReadEEPROM
        private static extern int is_ReadEEPROM(int hCam, int Adr, byte[] pcString, int count);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_WriteEEPROM")]				// is_WriteEEPROM
        private static extern int is_WriteEEPROM(int hCam, int Adr, byte[] pcString, int count);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SaveImage")]					// is_SaveImage
        private static extern int is_SaveImage(int hCam, byte[] pcString);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetColorMode")]				// is_SetColorMode
        private static extern int is_SetColorMode(int hCam, int ColorMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetColorDepth")]             // is_GetColorDepth
        private static extern int is_GetColorDepth(int hCam, ref int pnCol, ref int pnColMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_RenderBitmap")]				// is_RenderBitmap
        private static extern int is_RenderBitmap(int hCam, int MemID, int hWnd, int mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetDisplayMode")]			// is_SetDisplayMode
        private static extern int is_SetDisplayMode(int hCam, int mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetDC")]						// is_GetDC
        private static extern int is_GetDC(int hCam, ref int phDC);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ReleaseDC")]					// is_ReleaseDC
        private static extern int is_ReleaseDC(int hCam, int hDC);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_UpdateDisplay")]				// is_UpdateDisplay
        private static extern int is_UpdateDisplay(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetDisplayPos")]				// is_SetDisplayPos
        private static extern int is_SetDisplayPos(int hCam, int X, int Y);

        // Direct Draw
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_LockDDOverlayMem")]			// is_LockDDOverlayMem
        private static extern int is_LockDDOverlayMem(int hCam, ref IntPtr ppMem, ref int pPitch);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_UnlockDDOverlayMem")]		// is_UnlockDDOverlayMem
        private static extern int is_UnlockDDOverlayMem(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_LockDDMem")]					// is_LockDDMem
        private static extern int is_LockDDMem(int hCam, ref IntPtr ppMem, ref int pPitch);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_UnlockDDMem")]				// is_UnlockDDMem
        private static extern int is_UnlockDDMem(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetDDUpdateTime")]				// is_SetDDUpdateTime
        private static extern int is_SetDDUpdateTime(int hCam, int ms);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_EnableDDOverlay")]				// is_EnableDDOverlay
        private static extern int is_EnableDDOverlay(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_DisableDDOverlay")]				// is_DisableDDOverlay
        private static extern int is_DisableDDOverlay(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ShowDDOverlay")]				// is_ShowDDOverlay
        private static extern int is_ShowDDOverlay(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_HideDDOverlay")]				// is_HideDDOverlay
        private static extern int is_HideDDOverlay(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetDDOvlSurface")]			// is_GetDDOvlSurface
        private static extern int is_GetDDOvlSurface(int hCam, ref IntPtr ppDDSurf);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetKeyColor")]				// is_SetKeyColor
        private static extern int is_SetKeyColor(int hCam, int r, int g, int b);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_StealVideo")]				// is_StealVideo
        private static extern int is_StealVideo(int hCam, int Wait);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetHwnd")]					// is_SetHwnd
        private static extern int is_SetHwnd(int hCam, int hWnd);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetVsyncCount")]             // is_GetVsyncCount
        private static extern int is_GetVsyncCount(int hCam, ref long pIntr, ref long pActIntr);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetOsVersion")]				// is_GetOsVersion
        private static extern int is_GetOsVersion();

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetDLLVersion")]			    // is_GetDLLVersion
        private static extern int is_GetDLLVersion();

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_InitEvent")]					// is_InitEvent
        private static extern int is_InitEvent(int hCam, int hEv, int which);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ExitEvent")]					// is_ExitEvent
        private static extern int is_ExitEvent(int hCam, int which);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_EnableEvent")]				// is_EnableEvent
        private static extern int is_EnableEvent(int hCam, int which);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_DisableEvent")]				// is_DisableEvent
        private static extern int is_DisableEvent(int hCam, int which);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetIO")]			            // is_SetIO
        private static extern int is_SetIO(ref int phCam, int nIO);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetFlashStrobe")]			// is_SetFlashStrobe
        private static extern int is_SetFlashStrobe(int hCam, int nMode, int nField);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetExternalTrigger")]		// is_SetExternalTrigger
        private static extern int is_SetExternalTrigger(int hCam, int nTriggerMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetRopEffect")]				// is_SetRopEffect
        private static extern int is_SetRopEffect(int hCam, int effect, int param, int reserved);

        // Camera functions
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_InitCamera")]				// is_InitCamera
        private static extern int is_InitCamera(ref int phCam, int hwnd);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ExitCamera")]				// is_ExitCamera
        private static extern int is_ExitCamera(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetCameraInfo")]				// is_GetCameraInfo
        private static extern int is_GetCameraInfo(int hCam, byte[] pInfo);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_CameraStatus")]				// is_CameraStatus
        private static extern int is_CameraStatus(int hCam, int nInfo, int ulValue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetCameraType")]				// is_GetCameraType
        private static extern int is_GetCameraType(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetNumberOfCameras")]		// is_GetNumberOfCameras
        private static extern int is_GetNumberOfCameras(ref int pnNumCams);

        // Pixelclock
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetPixelClockRange")]		// is_GetPixelClockRange
        private static extern int is_GetPixelClockRange(int hCam, ref int pnMin, ref int pnMax);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetPixelClock")]				// is_SetPixelClock
        private static extern int is_SetPixelClock(int hCam, int Clock);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetUsedBandwidth")]			// is_GetUsedBandwidth
        private static extern int is_GetUsedBandwidth(int hCam);

        // Framerate
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetFrameTimeRange")]			// is_GetFrameTimeRange
        private static extern int is_GetFrameTimeRange(int hCam, ref double min, ref double max, ref double intervall);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetFrameRate")]				// is_SetFrameRate
        private static extern int is_SetFrameRate(int hCam, double FPS, ref double newFPS);

        // Exposure
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetExposureRange")]			// is_GetExposureRange
        private static extern int is_GetExposureRange(int hCam, ref double min, ref double max, ref double intervall);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetExposureTime")]			// is_SetExposureTime
        private static extern int is_SetExposureTime(int hCam, double EXP, ref double newEXP);

        // Get Frames per Second
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetFramesPerSecond")]		// is_GetFramesPerSecond
        private static extern int is_GetFramesPerSecond(int hCam, ref double dblFPS);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetIOMask")]				    // is_SetIOMask
        private static extern int is_SetIOMask(int hCam, int nMask);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetSensorInfo")]				// is_GetSensorInfo
        private static extern int is_GetSensorInfo(int hCam, byte[] pSensorInfo);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetRevisionInfo")]			// is_GetRevisionInfo
        private static extern int is_GetRevisionInfo(int hCam, byte[] pRevisionInfo);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_EnableAutoExit")]			// is_EnableAutoExit
        private static extern int is_EnableAutoExit(int hCam, int nMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_EnableMessage")]				// is_EnableMessage
        private static extern int is_EnableMessage(int hCam, int which, int hWnd);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetHardwareGain")]			// is_SetHardwareGain
        private static extern int is_SetHardwareGain(int hCam, int nMaster, int nRed, int nGreen, int nBlue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetRenderMode")]			    // is_SetRenderMode
        private static extern int is_SetRenderMode(int hCam, int nMode);

        // enable/disable WhiteBalance
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetWhiteBalance")]			// is_SetWhiteBalance
        private static extern int is_SetWhiteBalance(int hCam, int nMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetWhiteBalanceMultipliers")]// is_SetWhiteBalanceMultipliers
        private static extern int is_SetWhiteBalanceMultipliers(int hCam, double dblRed, double dblGreen, double dblBlue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetWhiteBalanceMultipliers")]// is_GetWhiteBalanceMultipliers
        private static extern int is_GetWhiteBalanceMultipliers(int hCam, ref double pdblRed, ref double pdblGreen, ref double pdblBlue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetEdgeEnhancement")]		// is_SetEdgeEnhancement
        private static extern int is_SetEdgeEnhancement(int hCam, int nEnable);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetColorCorrection")]		// is_SetColorCorrection
        private static extern int is_SetColorCorrection(int hCam, int nEnable, double[] factors);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetBlCompensation")]			// is_SetBlCompensation
        private static extern int is_SetBlCompensation(int hCam, int nEnable, int offset, int reserved);

        // Hot Pixel Correction
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetBadPixelCorrection")]		// is_SetBadPixelCorrection
        private static extern int is_SetBadPixelCorrection(int hCam, int nEnable, int threshold);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_LoadBadPixelCorrectionTable")]// is_LoadBadPixelCorrectionTable
        private static extern int is_LoadBadPixelCorrectionTable(int hCam, byte[] File);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SaveBadPixelCorrectionTable")]// is_SaveBadPixelCorrectionTable
        private static extern int is_SaveBadPixelCorrectionTable(int hCam, byte[] File);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetBadPixelCorrectionTable")]// is_SetBadPixelCorrectionTable
        private static extern int is_SetBadPixelCorrectionTable(int hCam, int nMode, byte[] pList);

        // Memoryboard
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetMemoryMode")]				// is_SetMemoryMode
        private static extern int is_SetMemoryMode(int hCam, int nCount, int nDelay);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_TransferImage")]				// is_TransferImage
        private static extern int is_TransferImage(int hCam, int nMemID, int seqID, int imageNr, int reserved);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_TransferMemorySequence")]	// is_TransferMemorySequence
        private static extern int is_TransferMemorySequence(int hCam, int seqID, int StartNr, int nCount, int nSeqPos);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_MemoryFreezeVideo")]			// is_MemoryFreezeVideo
        private static extern int is_MemoryFreezeVideo(int hCam, int nMemID, int Wait);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetLastMemorySequence")]		// is_GetLastMemorySequence
        private static extern int is_GetLastMemorySequence(int hCam, ref int pID);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetNumberOfMemoryImages")]	// is_GetNumberOfMemoryImages
        private static extern int is_GetNumberOfMemoryImages(int hCam, ref int nID, ref int pnCount);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetMemorySequenceWindow")]	// is_GetMemorySequenceWindow
        private static extern int is_GetMemorySequenceWindow(int hCam, int nID, ref int left, ref int top, ref int right, ref int bottom);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_IsMemoryBoardConnected")]	// is_IsMemoryBoardConnected
        private static extern int is_IsMemoryBoardConnected(int hCam, ref byte pConnected);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ResetMemory")]				// is_ResetMemory
        private static extern int is_ResetMemory(int hCam, int nReserved);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetSubSampling")]			// is_SetSubSampling
        private static extern int is_SetSubSampling(int hCam, int nMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ForceTrigger")]				// is_ForceTrigger
        private static extern int is_ForceTrigger(int hCam);

        // new with driver version 1.12.0006
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetBusSpeed")]				// is_GetBusSpeed
        private static extern int is_GetBusSpeed(int hCam);

        // new with driver version 1.12.0015
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetBinning")]			    // is_SetBinning
        private static extern int is_SetBinning(int hCam, int nMode);

        // new with driver version 1.12.0017
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ResetToDefault")]		    // is_ResetToDefault
        private static extern int is_ResetToDefault(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_LoadParameters")]			// is_LoadParameters
        private static extern int is_LoadParameters(int hCam, byte[] pFilename);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SaveParameters")]			// is_SaveParameters
        private static extern int is_SaveParameters(int hCam, byte[] pFilename);

        // new with driver version 1.14.0001
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetGlobalFlashDelays")]	    // is_GetGlobalFlashDelays
        private static extern int is_GetGlobalFlashDelays(int hCam, ref long pulDelay, ref long pulDuration);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetFlashDelay")]	            // is_SetFlashDelay
        private static extern int is_SetFlashDelay(int hCam, int ulDelay, int ulDuration);

        // new with driver version 1.14.0002
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_LoadImage")]			        // is_LoadImage
        private static extern int is_LoadImage(int hCam, byte[] pFilename);

        // new with driver version 1.14.0008
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetImageAOI")]	            // is_SetImageAOI
        private static extern int is_SetImageAOI(int hCam, int xPos, int yPos, int width, int height);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetCameraID")]			    // is_SetCameraID
        private static extern int is_SetCameraID(int hCam, int nID);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetBayerConversion")]        // is_SetBayerConversion
        private static extern int is_SetBayerConversion(int hCam, int nMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetTestImage")]			    // is_SetTestImage
        private static extern int is_SetTestImage(int hCam, int nMode);

        // new with driver version 1.14.0009
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetHardwareGamma")]			// is_SetHardwareGamma
        private static extern int is_SetHardwareGamma(int hCam, int nMode);

        // new with driver version 2.00.0001
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetCameraList")]			    // is_GetCameraList
        private static extern int is_GetCameraList(byte[] pucl);

        // new with driver version 2.00.0011
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetAOI")]			        // is_SetAOI
        private static extern int is_SetAOI(int hCam, int type, ref int pXPos, ref int pYPos, ref int pWidth, ref int pHeight);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetAutoParameter")]          // is_SetAutoParameter
        private static extern int is_SetAutoParameter(int hCam, int param, ref double pval1, ref double pval2);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetAutoInfo")]				// is_GetAutoInfo
        private static extern int is_GetAutoInfo(int hCam, byte[] pInfo);

        // new with driver version 2.20.0001
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ConvertImage")]				// is_ConvertImage
        private static extern int is_ConvertImage(int hCam, IntPtr pcSource, int nIDSource, ref IntPtr pcDest, ref int nIDDest, ref int reserved);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetConvertParam")]			// is_SetConvertParam
        private static extern int is_SetConvertParam(int hCam, int ColorCorrection, int BayerConversionMode, int ColorMode, int Gamma, double[] WhiteBalanceMultipliers);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SaveImageEx")]			    // is_SaveImageEx
        private static extern int is_SaveImageEx(int hCam, byte[] File, int fileFormat, int Param);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SaveImageMemEx")]			// is_SaveImageMemEx
        private static extern int is_SaveImageMemEx(int hCam, byte[] File, IntPtr pcMem, int nID, int FileFormat, int Param);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_LoadImageMem")]			    // is_LoadImageMem
        private static extern int is_LoadImageMem(int hCam, byte[] File, ref IntPtr ppcImgMem, ref int pid);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetImageHistogram")]			// is_GetImageHistogram
        private static extern int is_GetImageHistogram(int hCam, int nID, int ColorMode, IntPtr pHistoMem);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetTriggerDelay")]			// is_SetTriggerDelay
        private static extern int is_SetTriggerDelay(int hCam, int us);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetGainBoost")]			// is_SetGainBoost
        private static extern int is_SetGainBoost(int hCam, int mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetLED")]			// is_SetLED
        private static extern int is_SetLED(int hCam, int nValue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetGlobalShutter")]			// is_SetGlobalShutter
        private static extern int is_SetGlobalShutter(int hf, int mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetExtendedRegister")]			// is_SetExtendedRegister
        private static extern int is_SetExtendedRegister(int hf, int index, ushort val);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetHWGainFactor")]			// is_SetHWGainFactor
        private static extern int is_SetHWGainFactor(int hCam, int nMode, int nFactor);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_PrepareStealVideo")]				// is_PrepareStealVideo
        private static extern int is_PrepareStealVideo(int hCam, int nMode, int StealColorMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetNumberOfDevices")]				// is_GetNumberOfDevices
        private static extern int is_GetNumberOfDevices();

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Renumerate")]				// is_Renumerate
        private static extern int is_Renumerate(int hCam, int nMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetExtendedRegister")]		// is_GetExtendedRegister
        private static extern int is_GetExtendedRegister(int hCam, int index, ushort[] val);
        #endregion

        public struct KNEEPOINT
        {
            public double x;
            public double y;
        };

        public struct KNEEPOINTARRAY
        {
            public int NumberOfUsedKneepoints;
            public KNEEPOINT[] Kneepoint;
        };

        public struct KNEEPOINTINFO
        {
            public int NumberOfSupportedKneepoints;
            public int NumberOfUsedKneepoints;
            public double MinValueX;
            public double MaxValueX;
            public double MinValueY;
            public double MaxValueY;
            public KNEEPOINT[] DefaultKneepoint;
            public int[] Reserved;
        };

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetHdrMode")]			// is_GetHdrMode
        private static extern int is_GetHdrMode(int hCam, ref int Mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_EnableHdr")]				// is_EnableHdr
        private static extern int is_EnableHdr(int hCam, int Enable);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetHdrKneepoints")]		// is_SetHdrKneepoints
        private static extern int is_SetHdrKneepoints(int hCam, byte[] KneepointArray, int KneepointArraySize);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetHdrKneepoints")]		// is_GetHdrKneepoints
        private static extern int is_GetHdrKneepoints(int hCam, byte[] KneepointArray, int KneepointArraySize);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetHdrKneepointInfo")]		// is_GetHdrKneepointInfo
        private static extern int is_GetHdrKneepointInfo(int hCam, byte[] KneepointInfo, int KneepointInfoSize);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_WriteI2C")]		// is_WriteI2C
        private static extern int is_WriteI2C(int hCam, int nDeviceAddr, int nRegisterAddr, byte[] pbData, int nLen);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ReadI2C")]		// is_WriteI2C
        private static extern int is_ReadI2C(int hCam, int nDeviceAddr, int nRegisterAddr, byte[] pbData, int nLen);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetOptimalCameraTiming")] // is_SetOptimalCameraTiming
        private static extern int is_SetOptimalCameraTiming(int hCam, int Mode, int Timeout, ref int pMaxPxlClk, ref double pMaxFrameRate);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetComportNumber")] // is_GetComportNumber
        private static extern int is_GetComportNumber(int hCam, ref uint pComportNumber);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetSupportedTestImages")] // is_GetSupportedTestImages
        private static extern int is_GetSupportedTestImages(int hCam, ref int SupportedTestImages);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetTestImageValueRange")] // is_GetTestImageValueRange
        private static extern int is_GetTestImageValueRange(int hCam, int TestImage, ref int TestImageValueMin, ref int TestImageValueMax);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetSensorTestImage")] // is_SetSensorTestImage
        private static extern int is_SetSensorTestImage(int hCam, int Param1, int Param2);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetCameraLUT")] // is_SetCameraLUT
        private static extern int is_SetCameraLUT(int hCam, uint Mode, uint NumberOfEntries, double[] pRed_grey, double[] pGreen, double[] pBlue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetCameraLUT")] // is_GetCameraLUT
        private static extern int is_GetCameraLUT(int hCam, uint Mode, uint NumberOfEntries, double[] pRed_grey, double[] pGreen, double[] pBlue);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetColorConverter")] // is_SetColorConverter
        private static extern int is_SetColorConverter(int hCam, int ColorMode, int ConvertMode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetColorConverter")] // is_GetColorConverter
        private static extern int is_GetColorConverter(int hCam, int ColorMode, ref int pCurrentConvertMode, ref int pDefaultConvertMode, ref int pSupportedConvertModes);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetCaptureErrorInfo")] // is_GetCaptureErrorInfo
        private static extern int is_GetCaptureErrorInfo(int hCam, byte[] pCaptureErrorInfo, int SizeCaptureErrorInfo);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ResetCaptureErrorInfo")] // is_ResetCaptureErrorInfo
        private static extern int is_ResetCaptureErrorInfo(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_WaitForNextImage")]	    // is_WaitForNextImage
        private static extern int is_WaitForNextImage(int hCam, uint timeout, ref IntPtr ppcImg, ref int pid);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_InitImageQueue")]        // is_InitImageQueue
        private static extern int is_InitImageQueue(int hCam, int Mode);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ExitImageQueue")]        // is_ExitImageQueue
        private static extern int is_ExitImageQueue(int hCam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetTimeout")]			// is_SetTimeout
        private static extern int is_SetTimeout(int hCam, uint Mode, uint Timeout);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetTimeout")]			// is_GetTimeout
        private static extern int is_GetTimeout(int hCam, uint Mode, ref uint pTimeout);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetDuration")]			// is_GetDuration
        private static extern int is_GetDuration(int hCam, uint nMode, ref uint pnTime);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetSensorScalerInfo")] // is_GetSensorScalerInfo
        private static extern int is_GetSensorScalerInfo(int hCam, ref SENSORSCALERINFO pSensorScalerInfo, int nSensorScalerInfoSize);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetSensorScaler")] // is_SetSensorScaler
        private static extern int is_SetSensorScaler(int hCam, uint nMode, double dblFactor);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetImageInfo")] // is_GetImageInfo
        private static extern int is_GetImageInfo(int hCam, int nImageBufferID, ref UEYEIMAGEINFO pImageInfo, int nImageInfoSize);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_DirectRenderer")] // is_DirectRenderer
        private static extern int is_DirectRenderer(int hCam, uint nMode, IntPtr pParam, uint SizeOfParam);


        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ImageFormat")] // is_ImageFormat
        private static extern int is_ImageFormat(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_FaceDetection")] // is_FaceDetection
        private static extern int is_FaceDetection(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Focus")] // is_Focus
        private static extern int is_Focus(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        /*!< get estimated duration of GigE SE/CP starter firmware upload in milliseconds */
        public const int IS_SE_STARTER_FW_UPLOAD = 0x00000001;
        public const int IS_CP_STARTER_FW_UPLOAD = 0x00000002;
        public const int IS_STARTER_FW_UPLOAD = 0x00000004;



        // new with driver version 3.40.0000
        public struct SENSORSCALERINFO
        {
            public int nCurrMode;
            public int nNumberOfSteps;
            public double dblFactorIncrement;
            public double dblMinFactor;
            public double dblMaxFactor;
            public double dblCurrFactor;
            public byte[] bReserved;
        };

        unsafe public struct UEYETIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
            public fixed byte byReserved[10];
        };


        public struct UEYEIMAGEINFO
        {
            public int dwFlags;
            public byte[] byReserved1;
            public ulong u64TimestampDevice;
            public UEYETIME TimestampSystem;
            public int dwIoStatus;
            public short wAOIIndex;
            public short wAOICycle;
            public ulong u64FrameNumber;
            public int dwImageBuffers;
            public int dwImageBuffersInUse;
            public int dwReserved3;
            public int dwImageHeight;
            public int dwImageWidth;
        };

        public const uint IMGFRMT_CMD_GET_NUM_ENTRIES = 1;  /* Get the number of supported image formats.
                                                                         pParam hast to be a Pointer to IS_U32. If  -1 is reported, the device
                                                                         supports continuous AOI settings (maybe with fixed increments)         */
        public const uint IMGFRMT_CMD_GET_LIST = 2;  /* Get a array of IMAGE_FORMAT_ELEMENTs.                                  */
        public const uint IMGFRMT_CMD_SET_FORMAT = 3;  /* Select a image format                                                  */
        public const uint IMGFRMT_CMD_GET_ARBITRARY_AOI_SUPPORTED = 4;  /* Does the device supports the setting of an arbitrary AOI.              */
        public const uint IMGFRMT_CMD_GET_FORMAT_INFO = 5;  /* Get IMAGE_FORMAT_INFO for a given formatID                             */

        // no trigger
        public const uint CAPTMODE_FREERUN = 0x00000001;
        public const uint CAPTMODE_SINGLE = 0x00000002;

        // software trigger modes
        public const uint CAPTMODE_TRIGGER_SOFT_SINGLE = 0x00000010;
        public const uint CAPTMODE_TRIGGER_SOFT_CONTINUOUS = 0x00000020;

        // hardware trigger modes
        public const uint CAPTMODE_TRIGGER_HW_SINGLE = 0x00000100;
        public const uint CAPTMODE_TRIGGER_HW_CONTINUOUS = 0x00000200;


        unsafe public struct IMAGE_FORMAT_INFO
        {
            public int nFormatID;                      /* Image format ID.                                                            */
            public uint nWidth;                        /* Width.                                                                      */
            public uint nHeight;                       /* Height.                                                                     */
            public int nX0;                            /* Start position x.                                                           */
            public int nY0;                            /* Start position Y.                                                           */
            public uint nSupportedCaptureModes;        /* Flag field with supported capture modes for this resolution (Bitmask of 
                                                      of CAPTUREMODE enumeration).                                                */
            public uint nBinningMode;                  /* Binning mode.                                                               */
            public uint nSubsamplingMode;              /* Subsampling mode.                                                           */
            public fixed byte strFormatName[64];       /* Format name. Currently not used                                             */
            public double dSensorScalerFactor;         /* Sensor scaler factor                                                        */
            public fixed uint nReserved[22];
        };


        unsafe public struct IMAGE_FORMAT_LIST
        {
            public uint nSizeOfListEntry;              /* Size of one list entry in byte. */
            public uint nNumListElements;              /* Number of list entries.         */
            public fixed uint nReserved[4];            /* Reserved for future use.        */
            public IMAGE_FORMAT_INFO FormatInfo;       /* First format entry.             */
        };


        public const uint FDT_CAP_INVALID = 0;
        public const uint FDT_CAP_SUPPORTED = 0x00000001; /* Face detection supported.                                      */
        public const uint FDT_CAP_SEARCH_ANGLE = 0x00000002; /* Search angle.                                                  */
        public const uint FDT_CAP_SEARCH_AOI = 0x00000004; /* Search AOI.                                                    */
        public const uint FDT_CAP_INFO_POSX = 0x00000010; /* Query horizontal position (center) of detected face.           */
        public const uint FDT_CAP_INFO_POSY = 0x00000020; /* Query vertical position(center) of detected face.              */
        public const uint FDT_CAP_INFO_WIDTH = 0x00000040; /* Query width of detected face.                                  */
        public const uint FDT_CAP_INFO_HEIGHT = 0x00000080; /* Query height of detected face.                                 */
        public const uint FDT_CAP_INFO_ANGLE = 0x00000100; /* Query angle of detected face.                                  */
        public const uint FDT_CAP_INFO_POSTURE = 0x00000200; /* Query posture of detected face.                                */
        public const uint FDT_CAP_INFO_FACENUMBER = 0x00000400; /* Query number of detected faces.                                */
        public const uint FDT_CAP_INFO_OVL = 0x00000800; /* Overlay: Mark the detected face in the image.                  */
        public const uint FDT_CAP_INFO_NUM_OVL = 0x00001000; /* Overlay: Limit the maximum number of overlays in one image.    */
        public const uint FDT_CAP_INFO_OVL_LINEWIDTH = 0x00002000; /* Overlay line width.                                            */


        unsafe public struct FDT_INFO_EL
        {
            public int nFacePosX;              /* Start X position.                                                                */
            public int nFacePosY;              /* Start Y position.                                                                */
            public int nFaceWidth;             /* Face width.                                                                      */
            public int nFaceHeight;            /* Face height.                                                                     */
            public int nAngle;                 /* Face Angle (0...360° clockwise, 0° at twelve o'clock position. -1: undefined ).  */
            public uint nPosture;              /* Face posture.                                                                    */
            public UEYETIME TimestampSystem;   /* System time stamp (device query time) .                                          */
            public ulong nReserved;            /* Reserved for future use.                                                         */
            public fixed uint nReserved2[4];   /* Reserved for future use.                                                         */
        };


        unsafe public struct FDT_INFO_LIST
        {
            public uint nSizeOfListEntry;      /* Size of one list entry in byte(in).  */
            public uint nNumDetectedFaces;     /* Number of detected faces(out).       */
            public uint nNumListElements;      /* Number of list elements(in).         */
            public fixed uint nReserved[4];    /* reserved for future use(out).        */
            public FDT_INFO_EL FaceEntry;      /* First face entry.                    */
        };


        public const uint FDT_CMD_GET_CAPABILITIES = 0;    /* Get the capabilities for face detection.                     */
        public const uint FDT_CMD_SET_DISABLE = 1;    /* Disable face detection.                                      */
        public const uint FDT_CMD_SET_ENABLE = 2;    /* Enable face detection.                                       */
        public const uint FDT_CMD_SET_SEARCH_ANGLE = 3;    /* Set the search angle.                                        */
        public const uint FDT_CMD_GET_SEARCH_ANGLE = 4;    /* Get the search angle parameter.                              */
        public const uint FDT_CMD_SET_SEARCH_ANGLE_ENABLE = 5;    /* Enable search angle.                                         */
        public const uint FDT_CMD_SET_SEARCH_ANGLE_DISABLE = 6;    /* Enable search angle.                                         */
        public const uint FDT_CMD_GET_SEARCH_ANGLE_ENABLE = 7;    /* Get the current setting of search angle enable.              */
        public const uint FDT_CMD_SET_SEARCH_AOI = 8;    /* Set the search AOI.                                          */
        public const uint FDT_CMD_GET_SEARCH_AOI = 9;    /* Get the search AOI.                                          */
        public const uint FDT_CMD_GET_FACE_LIST = 10;   /* Get a list with detected faces.                              */
        public const uint FDT_CMD_GET_NUMBER_FACES = 11;   /* Get the number of detected faces.                            */
        public const uint FDT_CMD_SET_SUSPEND = 12;   /* Keep the face detection result of that moment.               */
        public const uint FDT_CMD_SET_RESUME = 13;   /* Continue with the face detection.                            */
        public const uint FDT_CMD_GET_MAX_NUM_FACES = 14;   /* Get the maximum number of faces that can be detected once.   */
        public const uint FDT_CMD_SET_INFO_MAX_NUM_OVL = 15;   /* Set the maximum number of overlays displayed.                */
        public const uint FDT_CMD_GET_INFO_MAX_NUM_OVL = 16;   /* Get the setting 'maximum number of overlays displayed'.      */
        public const uint FDT_CMD_SET_INFO_OVL_LINE_WIDTH = 17;   /* Set the overlay line width.                                  */
        public const uint FDT_CMD_GET_INFO_OVL_LINE_WIDTH = 18;   /* Get the overlay line width.                                  */
        public const uint FDT_CMD_GET_ENABLE = 19;   /* Face detection enabled?.                                     */
        public const uint FDT_CMD_GET_SUSPEND = 20;   /* Face detection suspended?.                                   */
        public const uint FDT_CMD_GET_HORIZONTAL_RESOLUTION = 21;   /* Horizontal resolution of face detection.                     */
        public const uint FDT_CMD_GET_VERTICAL_RESOLUTION = 22;   /* Vertical resolution of face detection.                       */


        public const uint FOC_CAP_INVALID = 0;
        public const uint FOC_CAP_AUTOFOCUS_SUPPORTED = 0x00000001;   /* Auto focus supported.                                    */
        public const uint FOC_CAP_MANUAL_SUPPORTED = 0x00000002;   /* Manual focus supported.                                  */
        public const uint FOC_CAP_GET_DISTANCE = 0x00000004;   /* Support for query the distance of the focused object.    */
        public const uint FOC_CAP_SET_AUTOFOCUS_RANGE = 0x00000008;   /* Support for setting focus ranges.                        */
        public const uint FOC_CAP_AUTOFOCUS_FDT_AOI = 0x00000010;   /* Use of face detection AOI for autofocus supported.       */

        public const uint FOC_RANGE_NORMAL = 0x00000001;   /* Normal focus range(without Macro).   */
        public const uint FOC_RANGE_ALLRANGE = 0x00000002;   /* Allrange (macro to Infinity).        */
        public const uint FOC_RANGE_MACRO = 0x00000004;   /* Macro (only macro).                  */


        public const uint FOC_CMD_GET_CAPABILITIES = 0;    /* Get focus capabilities.                      */
        public const uint FOC_CMD_SET_DISABLE_AUTOFOCUS = 1;    /* Disable autofocus.                           */
        public const uint FOC_CMD_SET_ENABLE_AUTOFOCUS = 2;    /* Enable autofocus.                            */
        public const uint FOC_CMD_GET_AUTOFOCUS_ENABLE = 3;    /* Autofocus enabled?.                          */
        public const uint FOC_CMD_SET_AUTOFOCUS_RANGE = 4;    /* Preset autofocus range.                      */
        public const uint FOC_CMD_GET_AUTOFOCUS_RANGE = 5;    /* Get preset of autofocus range.               */
        public const uint FOC_CMD_GET_DISTANCE = 6;    /* Get distance to focused object.              */
        public const uint FOC_CMD_SET_MANUAL_FOCUS = 7;    /* Set manual focus.                            */
        public const uint FOC_CMD_GET_MANUAL_FOCUS = 8;    /* Get the value for manual focus.              */
        public const uint FOC_CMD_GET_MANUAL_FOCUS_MIN = 9;    /* Get the minimum manual focus value.          */
        public const uint FOC_CMD_GET_MANUAL_FOCUS_MAX = 10;   /* Get the maximum manual focus value.          */
        public const uint FOC_CMD_GET_MANUAL_FOCUS_INC = 11;   /* Get the increment of the manual focus value. */
        public const uint FOC_CMD_SET_ENABLE_AF_FDT_AOI = 12;   /* Enable face detection AOI use for autofocus. */
        public const uint FOC_CMD_SET_DISABLE_AF_FDT_AOI = 13;   /* Disable face detection AOI use for autofocus */
        public const uint FOC_CMD_GET_AF_FDT_AOI_ENABLE = 14;   /* Use autofocus FDT AOI?                       */


        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ImageStabilization")] // is_ImageStabilization
        private static extern int is_ImageStabilization(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        // image stabilization capability flags
        public const uint IMGSTAB_CAP_INVALID = 0;
        public const uint IMGSTAB_CAP_IMAGE_STABILIZATION_SUPPORTED = 0x00000001;    /* Image stabilization supported. */

        public const uint IMGSTAB_CMD_GET_CAPABILITIES = 0;    /* Get the capabilities for image stabilization.    */
        public const uint IMGSTAB_CMD_SET_DISABLE = 1;    /* Disable image stabilization.                     */
        public const uint IMGSTAB_CMD_SET_ENABLE = 2;    /* Enable image stabilization.                      */
        public const uint IMGSTAB_CMD_GET_ENABLE = 3;    /* Image stabilization enabled?                     */


        // New functions and defines for 3.61 (uEye XS)
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ScenePreset")] // is_ScenePreset
        private static extern int is_ScenePreset(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        public const uint SCENE_CMD_GET_SUPPORTED_PRESETS = 1;      /* Get the supported scene presets      */
        public const uint SCENE_CMD_SET_PRESET = 2;      /* Set the scene preset                 */
        public const uint SCENE_CMD_GET_PRESET = 3;      /* Get the current sensor scene preset  */
        public const uint SCENE_CMD_GET_DEFAULT_PRESET = 4;      /* Get the default sensor scene preset  */

        public const uint SCENE_INVALID = 0;
        public const uint SCENE_SENSOR_AUTOMATIC = 0x00000001;
        public const uint SCENE_SENSOR_PORTRAIT = 0x00000002;
        public const uint SCENE_SENSOR_SUNNY = 0x00000004;
        public const uint SCENE_SENSOR_ENTERTAINMENT = 0x00000008;
        public const uint SCENE_SENSOR_NIGHT = 0x00000010;
        public const uint SCENE_SENSOR_SPORTS = 0x00000040;
        public const uint SCENE_SENSOR_LANDSCAPE = 0x00000080;


        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Zoom")] // is_Zoom
        private static extern int is_Zoom(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        public const uint ZOOM_CMD_GET_CAPABILITIES = 0;  /* Get the zoom capabilities. */
        public const uint ZOOM_CMD_DIGITAL_GET_NUM_LIST_ENTRIES = 1;  /* Get the number of list entries. */
        public const uint ZOOM_CMD_DIGITAL_GET_LIST = 2;  /* Get a list of supported zoom factors. */
        public const uint ZOOM_CMD_DIGITAL_SET_VALUE = 3;  /* Set the digital zoom factor zoom factors. */
        public const uint ZOOM_CMD_DIGITAL_GET_VALUE = 4;  /* Get a current digital zoom factor. */

        public const uint ZOOM_CAP_INVALID = 0;
        public const uint ZOOM_CAP_DIGITAL_ZOOM = 0x00001;


        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Sharpness")] // is_Sharpness
        private static extern int is_Sharpness(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        public const uint SHARPNESS_CMD_GET_CAPABILITIES = 0;  /* Get the sharpness capabilities */
        public const uint SHARPNESS_CMD_GET_VALUE = 1;  /* Get the current sharpness value */
        public const uint SHARPNESS_CMD_GET_MIN_VALUE = 2;  /* Get the minimum sharpness value */
        public const uint SHARPNESS_CMD_GET_MAX_VALUE = 3;  /* Get the maximum sharpness value */
        public const uint SHARPNESS_CMD_GET_INCREMENT = 4;  /* Get the sharpness increment */
        public const uint SHARPNESS_CMD_GET_DEFAULT_VALUE = 5;  /* Get the default sharpness value */
        public const uint SHARPNESS_CMD_SET_VALUE = 6;  /* Set the sharpness value */

        public const uint SHARPNESS_CAP_INVALID = 0x0000;
        public const uint SHARPNESS_CAP_SHARPNESS_SUPPORTED = 0x0001;


        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Saturation")] // is_Saturation
        private static extern int is_Saturation(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        public const uint SATURATION_CMD_GET_CAPABILITIES = 0;  /* Get the saturation capabilities */
        public const uint SATURATION_CMD_GET_VALUE = 1;  /* Get the current saturation value */
        public const uint SATURATION_CMD_GET_MIN_VALUE = 2;  /* Get the minimum saturation value */
        public const uint SATURATION_CMD_GET_MAX_VALUE = 3;  /* Get the maximum saturation value */
        public const uint SATURATION_CMD_GET_INCREMENT = 4;  /* Get the saturation increment */
        public const uint SATURATION_CMD_GET_DEFAULT_VALUE = 5;  /* Get the default saturation value */
        public const uint SATURATION_CMD_SET_VALUE = 6;  /* Set the saturation value */

        public const uint SATURATION_CAP_INVALID = 0x0000;
        public const uint SATURATION_CAP_SATURATION_SUPPORTED = 0x0001;


        // Trigger debounce modes
        public const uint TRIGGER_DEBOUNCE_MODE_NONE = 0x0000;
        public const uint TRIGGER_DEBOUNCE_MODE_FALLING_EDGE = 0x0001;
        public const uint TRIGGER_DEBOUNCE_MODE_RISING_EDGE = 0x0002;
        public const uint TRIGGER_DEBOUNCE_MODE_BOTH_EDGES = 0x0004;
        public const uint TRIGGER_DEBOUNCE_MODE_AUTOMATIC = 0x0008;

        // Trigger debounce commands
        public const uint TRIGGER_DEBOUNCE_CMD_SET_MODE = 0; /* Set a new trigger debounce mode */
        public const uint TRIGGER_DEBOUNCE_CMD_SET_DELAY_TIME = 1; /* Set a new trigger debounce delay time */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_SUPPORTED_MODES = 2; /* Get the supported modes */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_MODE = 3; /* Get the current trigger debounce mode */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_DELAY_TIME = 4; /* Get the current trigger debounce delay time */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_DELAY_TIME_MIN = 5; /* Get the minimum value for the trigger debounce delay time */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_DELAY_TIME_MAX = 6; /* Get the maximum value for the trigger debounce delay time */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_DELAY_TIME_INC = 7; /* Get the increment of the trigger debounce delay time */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_MODE_DEFAULT = 8; /* Get the default trigger debounce mode */
        public const uint TRIGGER_DEBOUNCE_CMD_GET_DELAY_TIME_DEFAULT = 9; /* Get the default trigger debounce delay time */

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_TriggerDebounce")] // is_TriggerDebounce
        private static extern int is_TriggerDebounce(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        // RGB color models
        public const uint RGB_COLOR_MODEL_SRGB_D50 = 0x0001;
        public const uint RGB_COLOR_MODEL_SRGB_D65 = 0x0002;
        public const uint RGB_COLOR_MODEL_CIE_RGB_E = 0x0004;
        public const uint RGB_COLOR_MODEL_ECI_RGB_D50 = 0x0008;
        public const uint RGB_COLOR_MODEL_ADOBE_RGB_D65 = 0x0010;

        // Color temperature commands
        public const uint COLOR_TEMPERATURE_CMD_SET_TEMPERATURE = 0; /* Set a new color temperature */
        public const uint COLOR_TEMPERATURE_CMD_SET_RGB_COLOR_MODEL = 1; /* Set a new RGB color model */
        public const uint COLOR_TEMPERATURE_CMD_GET_SUPPORTED_RGB_COLOR_MODELS = 2; /* Get the supported RGB color models */
        public const uint COLOR_TEMPERATURE_CMD_GET_TEMPERATURE = 3; /* Get the current color temperature */
        public const uint COLOR_TEMPERATURE_CMD_GET_RGB_COLOR_MODEL = 4; /* Get the current RGB color model */
        public const uint COLOR_TEMPERATURE_CMD_GET_TEMPERATURE_MIN = 5; /* Get the minimum value for the color temperature */
        public const uint COLOR_TEMPERATURE_CMD_GET_TEMPERATURE_MAX = 6; /* Get the maximum value for the color temperature */
        public const uint COLOR_TEMPERATURE_CMD_GET_TEMPERATURE_INC = 7; /* Get the increment of the color temperature */
        public const uint COLOR_TEMPERATURE_CMD_GET_TEMPERATURE_DEFAULT = 8; /* Get the default color temperature */
        public const uint COLOR_TEMPERATURE_CMD_GET_RGB_COLOR_MODEL_DEFAULT = 9; /* Get the default RGB color model */

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_ColorTemperature")] // is_ColorTemperature
        private static extern int is_ColorTemperature(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_HotPixel")] // is_HotPixel
        private static extern int is_HotPixel(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        public struct IS_POINT_2D
        {
            public int s32X;
            public int s32Y;
        };

        public struct IS_SIZE_2D
        {
            public int s32Width;
            public int s32Height;
        };

        public struct IS_RECT
        {
            public int s32X;
            public int s32Y;
            public int s32Width;
            public int s32Height;
        };

        public struct AOI_SEQUENCE_PARAMS
        {
            public int s32AOIIndex;
            public int s32NumberOfCycleRepetitions;
            public int s32X;
            public int s32Y;
            public double dblExposure;
            public int s32Gain;
            public int s32BinningMode;
            public int s32SubsamplingMode;
            public int s32DetachImageParameters;
            public double dblScalerFactor;
            public byte[] byReserved;
        };

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_AOI")] // is_AOI
        private static extern int is_AOI(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        // Device feature commands
        public const uint IS_DEVICE_FEATURE_CMD_GET_SUPPORTED_FEATURES = 1;
        public const uint IS_DEVICE_FEATURE_CMD_SET_LINESCAN_MODE = 2;
        public const uint IS_DEVICE_FEATURE_CMD_GET_LINESCAN_MODE = 3;
        public const uint IS_DEVICE_FEATURE_CMD_SET_LINESCAN_NUMBER = 4;
        public const uint IS_DEVICE_FEATURE_CMD_GET_LINESCAN_NUMBER = 5;
        public const uint IS_DEVICE_FEATURE_CMD_SET_SHUTTER_MODE = 6;
        public const uint IS_DEVICE_FEATURE_CMD_GET_SHUTTER_MODE = 7;
        public const uint IS_DEVICE_FEATURE_CMD_SET_PREFER_XS_HS_MODE = 8;
        public const uint IS_DEVICE_FEATURE_CMD_GET_PREFER_XS_HS_MODE = 9;
        public const uint IS_DEVICE_FEATURE_CMD_GET_DEFAULT_PREFER_XS_HS_MODE = 10;

        // Device feature caps
        public const uint IS_DEVICE_FEATURE_CAP_SHUTTER_MODE_ROLLING = 0x00000001;
        public const uint IS_DEVICE_FEATURE_CAP_SHUTTER_MODE_GLOBAL = 0x00000002;
        public const uint IS_DEVICE_FEATURE_CAP_LINESCAN_MODE_FAST = 0x00000004;
        public const uint IS_DEVICE_FEATURE_CAP_LINESCAN_NUMBER = 0x00000008;
        public const uint IS_DEVICE_FEATURE_CAP_PREFER_XS_HS_MODE = 0x00000010;

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_DeviceFeature")] // is_DeviceFeature
        private static extern int is_DeviceFeature(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        public const uint IS_CONFIG_CPU_IDLE_STATES_BIT_AC_VALUE = 0x01; /*!< Mains power */
        public const uint IS_CONFIG_CPU_IDLE_STATES_BIT_DC_VALUE = 0x02; /*!< Battery power */
        public const uint IS_CONFIG_OPEN_MP_DISABLE = 0;
        public const uint IS_CONFIG_OPEN_MP_ENABLE = 1;
        public const uint IS_CONFIG_INITIAL_PARAMETERSET_NONE = 0;
        public const uint IS_CONFIG_INITIAL_PARAMETERSET_1 = 1;
        public const uint IS_CONFIG_INITIAL_PARAMETERSET_2 = 2;

        public const uint IS_CONFIG_CMD_GET_CAPABILITIES = 1; /*!< Get supported configuration capabilities (bitmask of CONFIGURATION_CAPS)*/
        public const uint IS_CONFIG_CPU_IDLE_STATES_CMD_GET_ENABLE = 2; /*!< Get the current CPU idle states enable state (bitmask of CONFIGURATION_SEL)*/
        public const uint IS_CONFIG_CPU_IDLE_STATES_CMD_SET_DISABLE_ON_OPEN = 4; /*!< Disable migration to other CPU idle states (other than C0) if the first USB camera is being opened*/
        public const uint IS_CONFIG_CPU_IDLE_STATES_CMD_GET_DISABLE_ON_OPEN = 5; /*!< Get the current setting of the command IS_CPU_IDLE_STATES_CMD_SET_DISABLE_ON_OPEN*/
        public const uint IS_CONFIG_OPEN_MP_CMD_GET_ENABLE = 6;
        public const uint IS_CONFIG_OPEN_MP_CMD_SET_ENABLE = 7;
        public const uint IS_CONFIG_OPEN_MP_CMD_GET_ENABLE_DEFAULT = 8;
        public const uint IS_CONFIG_INITIAL_PARAMETERSET_CMD_SET = 9;
        public const uint IS_CONFIG_INITIAL_PARAMETERSET_CMD_GET = 10;


        public const uint IS_CONFIG_CPU_IDLE_STATES_CAP_SUPPORTED = 0x00000001; /*!< CPU idle state commands are supported by the SDK */
        public const uint IS_CONFIG_OPEN_MP_CAP_SUPPORTED = 0x00000002; /*!< Open MP commands are supported by the SDK */
        public const uint IS_CONFIG_INITIAL_PARAMETERSET_CAP_SUPPORTED = 0x00000004; /*!< Initial parameter set commands are supported by the SDK */

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Configuration")] // is_Configuration
        private static extern int is_Configuration(uint nCommand, IntPtr pParam, uint nSizeOfParam);


        public const uint IS_EXPOSURE_CMD_GET_CAPS = 1;
        public const uint IS_EXPOSURE_CMD_GET_EXPOSURE_DEFAULT = 2;
        public const uint IS_EXPOSURE_CMD_GET_EXPOSURE_RANGE_MIN = 3;
        public const uint IS_EXPOSURE_CMD_GET_EXPOSURE_RANGE_MAX = 4;
        public const uint IS_EXPOSURE_CMD_GET_EXPOSURE_RANGE_INC = 5;
        public const uint IS_EXPOSURE_CMD_GET_EXPOSURE_RANGE = 6;
        public const uint IS_EXPOSURE_CMD_GET_EXPOSURE = 7;
        public const uint IS_EXPOSURE_CMD_GET_FINE_INCREMENT_RANGE_MIN = 8;
        public const uint IS_EXPOSURE_CMD_GET_FINE_INCREMENT_RANGE_MAX = 9;
        public const uint IS_EXPOSURE_CMD_GET_FINE_INCREMENT_RANGE_INC = 10;
        public const uint IS_EXPOSURE_CMD_GET_FINE_INCREMENT_RANGE = 11;
        public const uint IS_EXPOSURE_CMD_SET_EXPOSURE = 12;
        public const uint IS_EXPOSURE_CMD_GET_LONG_EXPOSURE_RANGE_MIN = 13;
        public const uint IS_EXPOSURE_CMD_GET_LONG_EXPOSURE_RANGE_MAX = 14;
        public const uint IS_EXPOSURE_CMD_GET_LONG_EXPOSURE_RANGE_INC = 15;
        public const uint IS_EXPOSURE_CMD_GET_LONG_EXPOSURE_RANGE = 16;
        public const uint IS_EXPOSURE_CMD_GET_LONG_EXPOSURE_ENABLE = 17;
        public const uint IS_EXPOSURE_CMD_SET_LONG_EXPOSURE_ENABLE = 18;

        public const uint IS_EXPOSURE_CAP_EXPOSURE = 0x00000001;
        public const uint IS_EXPOSURE_CAP_FINE_INCREMENT = 0x00000002;
        public const uint IS_EXPOSURE_CAP_LONG_EXPOSURE = 0x00000004;


        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Exposure")]
        private static extern int is_Exposure(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        public const uint IS_TRIGGER_CMD_GET_BURST_SIZE_SUPPORTED = 1;
        public const uint IS_TRIGGER_CMD_GET_BURST_SIZE_RANGE = 2;
        public const uint IS_TRIGGER_CMD_GET_BURST_SIZE = 3;
        public const uint IS_TRIGGER_CMD_SET_BURST_SIZE = 4;

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Trigger")]
        private static extern int is_Trigger(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        // --------------------------------------------------------------------
        // new exports only valid for uEye ETH - BEGIN
        // --------------------------------------------------------------------

        // is_GetEthDeviceInfo
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_GetEthDeviceInfo")]
        private static extern int is_GetEthDeviceInfo(int hCam, byte[] pDeviceInfo, uint uStructSize);

        // is_SetPersistentIpCfg
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetPersistentIpCfg")]
        private static extern int is_SetPersistentIpCfg(int hCam, byte[] pIpCfg, uint uStructSize);

        // is_SetStarterFirmware
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetStarterFirmware")]
        private static extern int is_SetStarterFirmware(int hCam, byte[] pcFilepath, uint uFilepathLen);

        // is_SetAutoCfgIpSetup
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetAutoCfgIpSetup")]
        private static extern int is_SetAutoCfgIpSetup(int iAdapterID, byte[] pSetup, uint uStructSize);

        // is_SetAutoCfgIpSetup
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_SetPacketFilter")]
        private static extern int is_SetPacketFilter(int iAdapterID, uint uFilterSetting);


        // IpConfig capability flags
        public const uint IPCONFIG_CAP_PERSISTENT_IP_SUPPORTED = 0x01;
        public const uint IPCONFIG_CAP_AUTOCONFIG_IP_SUPPORTED = 0x04;

        // Commands supported by the IP configuration access function
        public const uint IPCONFIG_CMD_QUERY_CAPABILITIES = 0;
        public const uint IPCONFIG_CMD_SET_PERSISTENT_IP = 0x01010000;
        public const uint IPCONFIG_CMD_SET_AUTOCONFIG_IP = 0x01040000;
        public const uint IPCONFIG_CMD_GET_PERSISTENT_IP = 0x02010000;
        public const uint IPCONFIG_CMD_GET_AUTOCONFIG_IP = 0x02040000;

        // Generic interface to the device's IP configuration 

        // is_IpConfig
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_IpConfig")]
        private static extern int is_IpConfig(int iID, UEYE_ETH_ADDR_MAC mac, uint nCommand, IntPtr pParam, uint cbSizeOfParam);


        // Capabilities of the transfer engine access function
        public const uint TRANSFER_CAP_IMAGEDELAY = 0x01;
        public const uint TRANSFER_CAP_PACKETINTERVAL = 0x20;

        // Commands supported by the transfer engine access function
        public const uint TRANSFER_CMD_QUERY_CAPABILITIES = 0;
        public const uint TRANSFER_CMD_SET_IMAGEDELAY_US = 1000;
        public const uint TRANSFER_CMD_SET_PACKETINTERVAL_US = 1005;
        public const uint TRANSFER_CMD_GET_IMAGEDELAY_US = 2000;
        public const uint TRANSFER_CMD_GET_PACKETINTERVAL_US = 2005;
        public const uint TRANSFER_CMD_GETRANGE_IMAGEDELAY_US = 3000;
        public const uint TRANSFER_CMD_GETRANGE_PACKETINTERVAL_US = 3005;

        // Generic interface to the transfer engine setup

        // is_Transfer
        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_Transfer")]
        private static extern int is_Transfer(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        public const uint IS_BOOTBOOST_ID_MIN = 1;
        public const uint IS_BOOTBOOST_ID_MAX = 254;
        public const uint IS_BOOTBOOST_ID_NONE = 0;
        public const uint IS_BOOTBOOST_ID_ALL = 255;

        public struct IS_BOOTBOOST_IDLIST
        {
            /*! \brief the number of listed elements. */
            public int u32NumberOfEntries;

            /*! \brief the list. */
            public byte[] aList;
        };

        public const uint IS_BOOTBOOST_CMD_ENABLE = 0x00010001;
        public const uint IS_BOOTBOOST_CMD_DISABLE = 0x00010011;
        public const uint IS_BOOTBOOST_CMD_GET_ENABLED = 0x20010021;
        public const uint IS_BOOTBOOST_CMD_ADD_ID = 0x10100001;
        public const uint IS_BOOTBOOST_CMD_SET_IDLIST = 0x10100005;
        public const uint IS_BOOTBOOST_CMD_REMOVE_ID = 0x10100011;
        public const uint IS_BOOTBOOST_CMD_CLEAR_IDLIST = 0x00100015;
        public const uint IS_BOOTBOOST_CMD_GET_IDLIST = 0x30100021;
        public const uint IS_BOOTBOOST_CMD_GET_IDLIST_SIZE = 0x20100022;

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_BootBoost")]
        private static extern int is_BootBoost(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        /*!
        * \brief Structure for flash delay and duration \see is_IO.
        */
        public struct IO_FLASH_PARAMS
        {
            public uint u32Delay;
            public uint u32Duration;
        };

        /*!
        * \brief Structure for the PWM params \see is_IO.
        */
        public struct IO_PWM_PARAMS
        {
            public double dblFrequency_Hz;
            public double dblDutyCycle;
        };

        /*!
        * \brief Defines used by is_IO(), \see is_IO.
        */
        public const uint IO_LED_STATE_1 = 0;
        public const uint IO_LED_STATE_2 = 1;

        public const uint IO_FLASH_MODE_OFF = 0;
        public const uint IO_FLASH_MODE_TRIGGER_LO_ACTIVE = 1;
        public const uint IO_FLASH_MODE_TRIGGER_HI_ACTIVE = 2;
        public const uint IO_FLASH_MODE_CONSTANT_HIGH = 3;
        public const uint IO_FLASH_MODE_CONSTANT_LOW = 4;
        public const uint IO_FLASH_MODE_FREERUN_LO_ACTIVE = 5;
        public const uint IO_FLASH_MODE_FREERUN_HI_ACTIVE = 6;

        public const uint IS_FLASH_MODE_PWM = 0x8000;
        public const uint IO_FLASH_MODE_GPIO_1 = 0x0010;
        public const uint IO_FLASH_MODE_GPIO_2 = 0x0020;
        public const uint IO_FLASH_MODE_GPIO_3 = 0x0040;
        public const uint IO_FLASH_MODE_GPIO_4 = 0x0080;
        public const uint IO_FLASH_GPIO_PORT_MASK = (IO_FLASH_MODE_GPIO_1 | IO_FLASH_MODE_GPIO_2 | IO_FLASH_MODE_GPIO_3 | IO_FLASH_MODE_GPIO_4);

        public const uint IO_GPIO_1 = 0x0001;
        public const uint IO_GPIO_2 = 0x0002;
        public const uint IO_GPIO_3 = 0x0004;
        public const uint IO_GPIO_4 = 0x0008;

        public const uint IS_IO_CMD_GPIOS_GET_SUPPORTED = 1;
        public const uint IS_IO_CMD_GPIOS_GET_SUPPORTED_INPUTS = 2;
        public const uint IS_IO_CMD_GPIOS_GET_SUPPORTED_OUTPUTS = 3;
        public const uint IS_IO_CMD_GPIOS_GET_DIRECTION = 4;
        public const uint IS_IO_CMD_GPIOS_SET_DIRECTION = 5;
        public const uint IS_IO_CMD_GPIOS_GET_STATE = 6;
        public const uint IS_IO_CMD_GPIOS_SET_STATE = 7;
        public const uint IS_IO_CMD_LED_GET_STATE = 8;
        public const uint IS_IO_CMD_LED_SET_STATE = 9;
        public const uint IS_IO_CMD_LED_TOGGLE_STATE = 10;
        public const uint IS_IO_CMD_FLASH_GET_GLOBAL_PARAMS = 11;
        public const uint IS_IO_CMD_FLASH_APPLY_GLOBAL_PARAMS = 12;
        public const uint IS_IO_CMD_FLASH_GET_SUPPORTED_GPIOS = 13;
        public const uint IS_IO_CMD_FLASH_GET_PARAMS_MIN = 14;
        public const uint IS_IO_CMD_FLASH_GET_PARAMS_MAX = 15;
        public const uint IS_IO_CMD_FLASH_GET_PARAMS_INC = 16;
        public const uint IS_IO_CMD_FLASH_GET_PARAMS = 17;
        public const uint IS_IO_CMD_FLASH_SET_PARAMS = 18;
        public const uint IS_IO_CMD_FLASH_GET_MODE = 19;
        public const uint IS_IO_CMD_FLASH_SET_MODE = 20;
        public const uint IS_IO_CMD_PWM_GET_SUPPORTED_GPIOS = 21;
        public const uint IS_IO_CMD_PWM_GET_PARAMS_MIN = 22;
        public const uint IS_IO_CMD_PWM_GET_PARAMS_MAX = 23;
        public const uint IS_IO_CMD_PWM_GET_PARAMS_INC = 24;
        public const uint IS_IO_CMD_PWM_GET_PARAMS = 25;
        public const uint IS_IO_CMD_PWM_SET_PARAMS = 26;
        public const uint IS_IO_CMD_PWM_GET_MODE = 27;
        public const uint IS_IO_CMD_PWM_SET_MODE = 28;

        [DllImport(DRIVER_DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "is_IO")]
        private static extern int is_IO(int hCam, uint nCommand, IntPtr pParam, uint nSizeOfParam);


        // --------------------------------------------------------------------
        // new exports only valid for uEye ETH - END
        // --------------------------------------------------------------------
        #endregion

        // internal variables
        private int m_hCam;		// internal camera handle

        // constructor
        public uEyeDriverWrapper()
        {
            m_hCam = 0;
        }

        //********************************************************************************************
        // Helper functions
        //********************************************************************************************

        // ------------------------------ GetStringFromByte  --------------------------
        //
        private string GetStringFromByte(byte[] pByte, int nStart, int nLength)
        {
            int i = 0;
            char[] pChars = new char[nLength];
            for (i = 0; i < nLength; i++)
            {
                pChars[i] = Convert.ToChar(pByte[nStart + i]);
            }
            string strResult = new string(pChars);
            return strResult;
        }


        // ------------------------------ GetLongFromByte  --------------------------
        //
        private long GetLongFromByte(byte[] pByte, int nStart)
        {
            long b1 = (long)(pByte[nStart]);
            long b2 = (long)(pByte[nStart + 1] << 8);
            long b3 = (long)(pByte[nStart + 2] << 16);
            long b4 = (long)(pByte[nStart + 3] << 24);

            long result = b1 + b2 + b3 + b4;
            return result;
        }


        // ------------------------------ SaveLongInByte  --------------------------
        //
        private void SaveLongInByte(byte[] pByte, int nStart, long Number)
        {
            pByte[nStart] = (byte)((Number & 0x000000FF));
            pByte[nStart + 1] = (byte)((Number & 0x0000FF00) >> 8);
            pByte[nStart + 2] = (byte)((Number & 0x00FF0000) >> 16);
            pByte[nStart + 3] = (byte)((Number & 0xFF000000) >> 24);
        }


        // -------------------------  IsuEyeOpen  -----------------
        //
        public bool IsOpen()
        {
            if (m_hCam == 0)
                return false;
            else
                return true;
        }


        //*************************************************************************
        // Function wrappers
        //*************************************************************************

        // ---------------------  init camera  ---------------------
        //
        public int InitCamera(int hCam, int hWnd)
        {
            int ret = 0;
            if (m_hCam != 0)
                return IS_INVALID_CAMERA_HANDLE;

            ret = is_InitCamera(ref hCam, hWnd);
            if (ret == IS_SUCCESS)
                m_hCam = hCam;

            return ret;
        }

        // ---------------------  ExitCamera  -----------------------
        //
        public int ExitCamera()
        {
            int ret = 0;
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            ret = is_ExitCamera(m_hCam);
            if (ret == IS_SUCCESS)
                m_hCam = 0;

            return ret;
        }


        // ---------------------  StopLiveVideo  -----------------------
        //
        public int StopLiveVideo(int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_StopLiveVideo(m_hCam, mode);
        }

        // ---------------------  FreezeVideo  -----------------------
        //
        public int FreezeVideo(int wait)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_FreezeVideo(m_hCam, wait);
        }

        // ---------------------  CaptureVideo  -----------------------
        //
        public int CaptureVideo(int wait)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_CaptureVideo(m_hCam, wait);
        }

        // ---------------------  SetDisplayMode  -----------------------
        //
        public int SetDisplayMode(int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetDisplayMode(m_hCam, mode);
        }

        // ---------------------  SetKeyColor  -----------------------
        //
        public int SetKeyColor(int r, int g, int b)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetKeyColor(m_hCam, r, g, b);
        }

        // ---------------------  AllocImageMem  -----------------------
        //
        public int AllocImageMem(int width, int height, int bits, ref IntPtr ppcImg, ref int pid)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_AllocImageMem(m_hCam, width, height, bits, ref ppcImg, ref pid);
        }


        // ---------------------  SetImageMem  -----------------------
        //
        public int SetImageMem(IntPtr pcImg, int id)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetImageMem(m_hCam, pcImg, id);
        }

        // ---------------------  GetImageMem  -----------------------
        //
        public int GetImageMem(ref IntPtr ppMem)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetImageMem(m_hCam, ref ppMem);
        }

        // ---------------------  SetBrightness  -----------------------
        //
        public int SetBrightness(int bright)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetBrightness(m_hCam, bright);
        }

        // ---------------------  SetContrast  -----------------------
        //
        public int SetContrast(int cont)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetContrast(m_hCam, cont);
        }

        // ---------------------  SetGamma  -----------------------
        //
        public int SetGamma(int gamma)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetGamma(m_hCam, gamma);
        }

        // ---------------------  SetColorMode  -----------------------
        //
        public int SetColorMode(int ColorMode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetColorMode(m_hCam, ColorMode);
        }

        // ---------------------  GetImageMemPitch  -----------------------
        //
        public int GetImageMemPitch(ref int pPitch)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetImageMemPitch(m_hCam, ref pPitch);
        }

        // ---------------------  SetImageSize  -----------------------
        //
        public int SetImageSize(int width, int height)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetImageSize(m_hCam, width, height);
        }
        public int GetDisplayWidth()
        {
            return SetImageSize(IS_GET_IMAGE_SIZE_X, 0);
        }
        public int GetDisplayHeight()
        {
            return SetImageSize(IS_GET_IMAGE_SIZE_Y, 0);
        }


        // ---------------------  SetImagePos  -----------------------
        //
        public int SetImagePos(int x, int y)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetImagePos(m_hCam, x, y);
        }
        public int GetDisplayPos(ref int x, ref int y)
        {
            x = SetImagePos(IS_GET_IMAGE_POS_X, 0);
            y = SetImagePos(IS_GET_IMAGE_POS_Y, 0);
            return IS_SUCCESS;
        }


        // ---------------------  GetError  -----------------------
        //
        public int GetError(ref int pErr, IntPtr ppcErr)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetError(m_hCam, ref pErr, ppcErr);
        }

        // ---------------------  SetErrorReport  -----------------------
        //
        public int SetErrorReport(int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetErrorReport(m_hCam, mode);
        }

        // ---------------------  GetCameraInfo  -----------------------
        //
        public int GetCameraInfo(ref CAMINFO pBoardinfo)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            // read board info
            byte[] pTemp = new byte[64];
            int ret = is_GetCameraInfo(m_hCam, pTemp);

            // copy structure if success
            if (ret == IS_SUCCESS)
            {
                pBoardinfo.SerNo = GetStringFromByte(pTemp, 0, 12);
                pBoardinfo.id = GetStringFromByte(pTemp, 12, 20);
                pBoardinfo.Version = GetStringFromByte(pTemp, 32, 10);
                pBoardinfo.Date = GetStringFromByte(pTemp, 42, 12);
                pBoardinfo.Select = pTemp[54];
                pBoardinfo.Type = pTemp[55];
            }
            return ret;
        }

        // ---------------------  GetCameraList  -----------------------
        //
        public int GetCameraList(ref UEYE_CAMERA_LIST pucl)
        {
            int nRet;
            long SizeOfStructure;
            long NumberOfCameras = pucl.dwCount;

            if (NumberOfCameras == 0)
            {
                SizeOfStructure = 4 + 112;
                byte[] pTemp = new byte[SizeOfStructure];

                nRet = is_GetCameraList(pTemp);
                if (nRet == IS_SUCCESS)
                {
                    pucl.dwCount = GetLongFromByte(pTemp, 0);
                }
            }
            else
            {
                SizeOfStructure = 4 + NumberOfCameras * 112;
                byte[] pTemp = new byte[SizeOfStructure];
                SaveLongInByte(pTemp, 0, NumberOfCameras);

                nRet = is_GetCameraList(pTemp);
                if (nRet == IS_SUCCESS)
                {
                    pucl.dwCount = GetLongFromByte(pTemp, 0);

                    int i = 0;
                    for (i = 0; i < NumberOfCameras; i++)
                    {
                        int Offset = i * 112;

                        pucl.uci[i].dwCameraID = GetLongFromByte(pTemp, 4 + Offset);
                        pucl.uci[i].dwDeviceID = GetLongFromByte(pTemp, 8 + Offset);
                        pucl.uci[i].dwSensorID = GetLongFromByte(pTemp, 12 + Offset);
                        pucl.uci[i].dwInUse = GetLongFromByte(pTemp, 16 + Offset);
                        pucl.uci[i].SerNo = GetStringFromByte(pTemp, 20 + Offset, 16);
                        pucl.uci[i].Model = GetStringFromByte(pTemp, 36 + Offset, 16);
                        pucl.uci[i].dwStatus = GetLongFromByte(pTemp, 52 + Offset);
                    }
                }
            }

            return nRet;
        }


        // ---------------------  ReadEEPROM  -----------------------
        //
        public int ReadEEPROM(int Adr, byte[] pcString, int count)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ReadEEPROM(m_hCam, Adr, pcString, count);
        }

        // ---------------------  WriteEEPROM  -----------------------
        //
        public int WriteEEPROM(int Adr, byte[] pcString, int count)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_WriteEEPROM(m_hCam, Adr, pcString, count);
        }

        // ---------------------  SaveImage  -----------------------
        //
        public int SaveImage(byte[] pcString)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SaveImage(m_hCam, pcString);
        }


        // ---------------------  SetDisplayPos  -----------------------
        //
        public int SetDisplayPos(int Adr, int X, int Y)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetDisplayPos(m_hCam, X, Y);
        }

        // ---------------------  FreeImageMem  -----------------------
        //
        public int FreeImageMem(IntPtr pImgMem, int id)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_FreeImageMem(m_hCam, pImgMem, id);
        }


        // ---------------------  IsVideoFinish  -----------------------
        //
        public int IsVideoFinish(ref int pValue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_IsVideoFinish(m_hCam, ref pValue);
        }


        // ---------------------  HasVideoStarted  -----------------------
        //
        public int HasVideoStarted(ref int pBool)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_HasVideoStarted(m_hCam, ref pBool);
        }


        // ---------------------  GetDC  -----------------------
        //
        public int GetDC(ref int phDC)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetDC(m_hCam, ref phDC);
        }


        // ---------------------  ReleaseDC  -----------------------
        //
        public int ReleaseDC(int hDC)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ReleaseDC(m_hCam, hDC);
        }


        // ---------------------  LockDDOverlayMem  -----------------------
        //
        public int LockDDOverlayMem(ref IntPtr ppMem, ref int pPitch)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_LockDDOverlayMem(m_hCam, ref ppMem, ref pPitch);
        }


        // ---------------------  UnlockDDOverlayMem  -----------------------
        //
        public int UnlockDDOverlayMem()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_UnlockDDOverlayMem(m_hCam);
        }

        // ---------------------  LockDDMem  -----------------------
        //
        public int LockDDMem(ref IntPtr ppMem, ref int pPitch)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_LockDDMem(m_hCam, ref ppMem, ref pPitch);
        }

        // ---------------------  UnlockDDMem  -----------------------
        //
        public int UnlockDDMem()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_UnlockDDMem(m_hCam);
        }


        // --------------------  GetDDOvlSurface  ---------------------
        //
        public int GetDDOvlSurface(ref IntPtr ppDDSurf)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetDDOvlSurface(m_hCam, ref ppDDSurf);
        }


        // ---------------------  InitEvent  -----------------------
        //
        public int InitEvent(int hEv, int which)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_InitEvent(m_hCam, hEv, which);
        }


        // ---------------------  ExitEvent  -----------------------
        //
        public int ExitEvent(int which)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ExitEvent(m_hCam, which);
        }

        // ---------------------  EnableEvent  -----------------------
        //
        public int EnableEvent(int which)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_EnableEvent(m_hCam, which);
        }

        // ---------------------  DisableEvent  -----------------------
        //
        public int DisableEvent(int which)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_DisableEvent(m_hCam, which);
        }


        // ---------------------  SetHwnd  -----------------------
        //
        public int SetHwnd(int hWnd)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetHwnd(m_hCam, hWnd);
        }

        // ---------------------  GetActiveImageMem  -----------------------
        //
        public int GetActiveImageMem(ref IntPtr ppcMem, ref int pnID)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetActiveImageMem(m_hCam, ref ppcMem, ref pnID);
        }

        // ---------------------  InquireImageMem  -----------------------
        //
        public int InquireImageMem(IntPtr pcMem, int nID, ref int pnX, ref int pnY, ref int pnBits, ref int pnPitch)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_InquireImageMem(m_hCam, pcMem, nID, ref pnX, ref pnY, ref pnBits, ref pnPitch);
        }


        // ---------------------  AddToSequence  -----------------------
        //
        public int AddToSequence(IntPtr pMem, int nId)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_AddToSequence(m_hCam, pMem, nId);
        }


        // ---------------------  ClearSequence  -----------------------
        //
        public int ClearSequence()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ClearSequence(m_hCam);
        }


        // ---------------------  ClearSequence  -----------------------
        //
        public int GetActSeqBuf(ref int pnNum, ref IntPtr ppcMem, ref IntPtr ppcMemLast)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetActSeqBuf(m_hCam, ref pnNum, ref ppcMem, ref ppcMemLast);
        }

        // ---------------------  LockSeqBuf  -----------------------
        //
        public int LockSeqBuf(int nNum, IntPtr pcMem)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_LockSeqBuf(m_hCam, nNum, pcMem);
        }

        // ---------------------  UnlockSeqBuf  -----------------------
        //
        public int UnlockSeqBuf(int nNum, IntPtr pcMem)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_UnlockSeqBuf(m_hCam, nNum, pcMem);
        }

        // ---------------------  GetColorDepth  -----------------------
        //
        public int GetColorDepth(ref int pnCol, ref int pnColMode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetColorDepth(m_hCam, ref pnCol, ref pnColMode);
        }

        // ---------------------  SaveImageMem  -----------------------
        //
        public int SaveImageMem(byte[] strFile, IntPtr pMem, int nId)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SaveImageMem(m_hCam, strFile, pMem, nId);
        }


        // ---------------------  CopyImageMem  -----------------------
        //
        public int CopyImageMem(IntPtr pcSource, int nID, IntPtr pcDest)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_CopyImageMem(m_hCam, pcSource, nID, pcDest);
        }

        // ---------------------  CopyImageMemLines  -----------------------
        //
        public int CopyImageMemLines(int hCam, IntPtr pcSource, int nID, int nLines, IntPtr pcDest)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_CopyImageMemLines(m_hCam, pcSource, nID, nLines, pcDest);
        }

        // ---------------------  GetOsVersion  -----------------------
        //
        static public int GetOsVersion()
        {
            return is_GetOsVersion();
        }

        // ---------------------  SetAllocatedImageMem  --------------
        //
        public int SetAllocatedImageMem(int width, int height, int bpp, IntPtr pImgMem, ref int id)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetAllocatedImageMem(m_hCam, width, height, bpp, pImgMem, ref id);
        }

        // ---------------------  SetRopEffect  -----------------------
        //
        public int SetRopEffect(int effect, int param, int reserved)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetRopEffect(m_hCam, effect, param, reserved);
        }

        // ---------------------  SetFlashStrobe  -----------------------
        //
        public int SetFlashStrobe(int nMode, int nField)
        {
            return is_SetFlashStrobe(m_hCam, nMode, nField);
        }

        // ---------------------  SetExternalTrigger  -----------------------
        //
        public int SetExternalTrigger(int nTriggerMode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetExternalTrigger(m_hCam, nTriggerMode);
        }

        // ---------------------  RenderBitmap  -----------------------
        //
        public int RenderBitmap(int MemID, int hWnd, int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_RenderBitmap(m_hCam, MemID, hWnd, mode);
        }

        // ---------------------  GetDLLVersion  -----------------------
        //
        static public int GetDLLVersion()
        {
            return is_GetDLLVersion();
        }

        // ---------------------  GetPixelClockRange  -----------------------
        //
        public int GetPixelClockRange(ref int pnMin, ref int pnMax)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetPixelClockRange(m_hCam, ref  pnMin, ref  pnMax);
        }

        // ---------------------  SetPixelClock  -----------------------
        //
        public int SetPixelClock(int Clock)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetPixelClock(m_hCam, Clock);
        }

        // ---------------------  GetSensorInfo  -----------------------
        //
        public int GetSensorInfo(ref SENSORINFO pSensorInfo)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            byte[] pTemp = new byte[66];
            int ret = is_GetSensorInfo(m_hCam, pTemp);

            if (ret == IS_SUCCESS)
            {
                pSensorInfo.SensorID = (pTemp[1] << 8) + (pTemp[0]);
                pSensorInfo.strSensorName = GetStringFromByte(pTemp, 2, 32);
                pSensorInfo.nColorMode = pTemp[34];
                pSensorInfo.nMaxWidth = (pTemp[39] << 24) + (pTemp[38] << 16) + (pTemp[37] << 8) + (pTemp[36]);
                pSensorInfo.nMaxHeight = (pTemp[43] << 24) + (pTemp[42] << 16) + (pTemp[41] << 8) + (pTemp[40]);
                pSensorInfo.bMasterGain = Convert.ToBoolean(pTemp[44]);
                pSensorInfo.bRGain = Convert.ToBoolean(pTemp[48]);
                pSensorInfo.bGGain = Convert.ToBoolean(pTemp[52]);
                pSensorInfo.bBGain = Convert.ToBoolean(pTemp[56]);
                pSensorInfo.bGlobShutter = Convert.ToBoolean(pTemp[60]);
                pSensorInfo.wPixelSize = Convert.ToUInt16((pTemp[65] << 8) + pTemp[64]);
            }

            return ret;
        }

        // ---------------------  EnableMessage  -----------------------
        //
        public int EnableMessage(int which, int hWnd)
        {
            return is_EnableMessage(m_hCam, which, hWnd);
        }

        // ---------------------  SetHardwareGain  -----------------------
        //
        public int SetHardwareGain(int nMaster, int nRed, int nGreen, int nBlue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetHardwareGain(m_hCam, nMaster, nRed, nGreen, nBlue);
        }

        // ---------------------  GetFramesPerSecond  -----------------------
        //
        public int GetFramesPerSecond(ref double dblFPS)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetFramesPerSecond(m_hCam, ref dblFPS);
        }

        // ---------------------  EnableAutoExit  -----------------------
        //
        public int EnableAutoExit(int nMode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_EnableAutoExit(m_hCam, nMode);
        }

        // ---------------------  SetWhiteBalance  -----------------------
        //
        public int SetWhiteBalance(int nMode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetWhiteBalance(m_hCam, nMode);
        }

        // ---------------------  SetWhiteBalanceMultipliers  -----------------------
        //
        public int SetWhiteBalanceMultipliers(double dblRed, double dblGreen, double dblBlue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetWhiteBalanceMultipliers(m_hCam, dblRed, dblGreen, dblBlue);
        }

        // ---------------------  GetWhiteBalanceMultipliers  -----------------------
        //
        public int GetWhiteBalanceMultipliers(ref double pdblRed, ref double pdblGreen, ref double pdblBlue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetWhiteBalanceMultipliers(m_hCam, ref pdblRed, ref pdblGreen, ref pdblBlue);
        }

        // ---------------------  SetFrameRate  -----------------------
        //
        public int SetFrameRate(double FPS, ref double newFPS)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetFrameRate(m_hCam, FPS, ref newFPS);
        }

        // ---------------------  SetExposureTime  -----------------------
        //
        public int SetExposureTime(double EXP, ref double newEXP)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetExposureTime(m_hCam, EXP, ref newEXP);
        }

        // ---------------------  SetEdgeEnhancement  -----------------------
        //
        public int SetEdgeEnhancement(int nEnable)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetEdgeEnhancement(m_hCam, nEnable);
        }


        // ---------------------  CameraStatus  -----------------------
        //
        public long CameraStatus(int nInfo, int ulValue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_CameraStatus(m_hCam, nInfo, ulValue);
        }

        // ---------------------  GetCameraType  -----------------------
        //
        public int GetCameraType()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetCameraType(m_hCam);
        }

        // ---------------------  GetNumberOfCameras  -----------------------
        //
        static public int GetNumberOfCameras(ref int pnNumCams)
        {
            return is_GetNumberOfCameras(ref pnNumCams);
        }

        // ---------------------  SetColorCorrection  -----------------------
        //
        public int SetColorCorrection(int nEnable, double[] factors)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetColorCorrection(m_hCam, nEnable, factors);
        }

        // ---------------------  SetBlCompensation  -----------------------
        //
        public int SetBlCompensation(int nEnable, int offset, int reserved)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetBlCompensation(m_hCam, nEnable, offset, reserved);
        }


        // ---------------------  SetBadPixelCorrection  -----------------------
        //
        public int SetBadPixelCorrection(int nEnable, int threshold)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetBadPixelCorrection(m_hCam, nEnable, threshold);
        }

        // ---------------------  LoadBadPixelCorrectionTable  -----------------------
        //
        public int LoadBadPixelCorrectionTable(byte[] File)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_LoadBadPixelCorrectionTable(m_hCam, File);
        }

        // ---------------------  SaveBadPixelCorrectionTable  -----------------------
        //
        public int SaveBadPixelCorrectionTable(byte[] File)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SaveBadPixelCorrectionTable(m_hCam, File);
        }

        // ---------------------  SetBadPixelCorrectionTable  -----------------------
        //
        public int SetBadPixelCorrectionTable(int nMode, byte[] pList)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetBadPixelCorrectionTable(m_hCam, nMode, pList);
        }


        // ---------------------  GetUsedBandwidth  -----------------------
        //
        public int GetUsedBandwidth()
        {
            return is_GetUsedBandwidth(0);
        }


        // ---------------------  GetFrameTimeRange  -----------------------
        //
        public int GetFrameTimeRange(ref double min, ref double max, ref double intervall)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetFrameTimeRange(m_hCam, ref  min, ref max, ref intervall);
        }


        // ---------------------  GetExposureRange  -----------------------
        //
        public int GetExposureRange(ref double min, ref double max, ref double intervall)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            int rv;
            unsafe
            {
                double* ret = stackalloc double[3];
                rv = is_Exposure(m_hCam, IS_EXPOSURE_CMD_GET_EXPOSURE_RANGE, new IntPtr(ret), 3 * sizeof(double));
                min = ret[0];
                max = ret[1];
                intervall = ret[2];
            }
            return rv;
        }


        // ---------------------  SetMemoryMode  -----------------------
        //
        public int SetMemoryMode(int nCount, int nDelay)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetMemoryMode(m_hCam, nCount, nDelay);
        }

        // ---------------------  TransferImage  -----------------------
        //
        public int TransferImage(int nMemID, int seqID, int imageNr, int reserved)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_TransferImage(m_hCam, nMemID, seqID, imageNr, reserved);
        }


        // ---------------------  TransferMemorySequence  -----------------------
        //
        public int TransferMemorySequence(int seqID, int StartNr, int nCount, int nSeqPos)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_TransferMemorySequence(m_hCam, seqID, StartNr, nCount, nSeqPos);
        }

        // ---------------------  MemoryFreezeVideo  -----------------------
        //
        public int MemoryFreezeVideo(int nMemID, int Wait)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_MemoryFreezeVideo(m_hCam, nMemID, Wait);
        }


        // ---------------------  GetLastMemorySequence  -----------------------
        //
        public int GetLastMemorySequence(ref int pID)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetLastMemorySequence(m_hCam, ref pID);
        }


        // ---------------------  GetNumberOfMemoryImages  -----------------------
        //
        public int GetNumberOfMemoryImages(ref int nID, ref int pnCount)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetNumberOfMemoryImages(m_hCam, ref nID, ref pnCount);
        }


        // ---------------------  GetMemorySequenceWindow  -----------------------
        //
        public int GetMemorySequenceWindow(int nID, ref int left, ref int top, ref int right, ref int bottom)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetMemorySequenceWindow(m_hCam, nID, ref left, ref top, ref right, ref bottom);
        }


        // ---------------------  IsMemoryBoardConnected  -----------------------
        //
        public bool IsMemoryBoardConnected()
        {
            if (m_hCam == 0)
                return false;

            byte pConnected = 0;
            if (is_IsMemoryBoardConnected(m_hCam, ref pConnected) != IS_SUCCESS)
                return false;

            if (pConnected == 0)
                return false;

            return true;
        }


        // ---------------------  ResetMemory  -----------------------
        //
        public int ResetMemory()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ResetMemory(m_hCam, 0);
        }


        // ---------------------  SetSubSampling  -----------------------
        //
        public int SetSubSampling(int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetSubSampling(m_hCam, mode);
        }

        // ---------------------  SetBinning  -----------------------
        //
        public int SetBinning(int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetBinning(m_hCam, mode);
        }

        // ---------------------  ForceTrigger  -----------------------
        //
        public int ForceTrigger()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ForceTrigger(m_hCam);
        }


        // ---------------------  GetBusSpeed  -----------------------
        //
        public int GetBusSpeed()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetBusSpeed(m_hCam);
        }

        public int SetDDUpdateTime(int ms)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetDDUpdateTime(m_hCam, ms);
        }


        public int EnableDDOverlay()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_EnableDDOverlay(m_hCam);
        }

        public int DisableDDOverlay()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_DisableDDOverlay(m_hCam);
        }


        public int ShowDDOverlay()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_ShowDDOverlay(m_hCam);
        }

        public int HideDDOverlay()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_HideDDOverlay(m_hCam);
        }

        public int ConvertImage(IntPtr pcSource, int nIDSource, ref IntPtr pcDest, ref int nIDDest, ref int reserved)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_ConvertImage(m_hCam, pcSource, nIDSource, ref pcDest, ref nIDDest, ref reserved);
        }

        public int SetConvertParam(int ColorCorrection, int BayerConversionMode, int ColorMode, int Gamma, double[] WhiteBalanceMultipliers)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SetConvertParam(m_hCam, ColorCorrection, BayerConversionMode, ColorMode, Gamma, WhiteBalanceMultipliers);
        }

        public int SaveImageEx(byte[] File, int fileFormat, int Param)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SaveImageEx(m_hCam, File, fileFormat, Param);
        }

        public int SaveImageMemEx(byte[] File, IntPtr pcMem, int nID, int FileFormat, int Param)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SaveImageMemEx(m_hCam, File, pcMem, nID, FileFormat, Param);
        }

        public int LoadImage(byte[] pFilename)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_LoadImage(m_hCam, pFilename);
        }

        public int LoadImageMem(byte[] File, ref IntPtr ppcImgMem, ref int pid)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_LoadImageMem(m_hCam, File, ref ppcImgMem, ref pid);
        }

        public int GetImageHistogram(int nID, int ColorMode, IntPtr pHistoMem)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_GetImageHistogram(m_hCam, nID, ColorMode, pHistoMem);
        }


        public int SetTriggerDelay(int us)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SetTriggerDelay(m_hCam, us);
        }

        public int SetGainBoost(int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SetGainBoost(m_hCam, mode);
        }

        public int SetLED(int nValue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SetLED(m_hCam, nValue);
        }

        public int SetGlobalShutter(int mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SetGlobalShutter(m_hCam, mode);
        }

        public int SetExtendedRegister(int index, ushort val)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SetExtendedRegister(m_hCam, index, val);
        }

        public int SetHWGainFactor(int nMode, int nFactor)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;
            return is_SetHWGainFactor(m_hCam, nMode, nFactor);
        }

        public int SetFlashDelay(int ulDelay, int ulDuration)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetFlashDelay(m_hCam, ulDelay, ulDuration);

        }

        public int GetGlobalFlashDelay(ref long pulDelay, ref long pulDuration)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetGlobalFlashDelays(m_hCam, ref pulDelay, ref pulDuration);

        }


        public int PrepareStealVideo(int nMode, int StealColorMode)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_PrepareStealVideo(m_hCam, nMode, StealColorMode);

        }

        public int GetNumberOfDevices()
        {
            return is_GetNumberOfDevices();
        }

        public int StealVideo(int Wait)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_StealVideo(m_hCam, Wait);

        }

        public int LoadParameters(byte[] pFilename)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_LoadParameters(m_hCam, pFilename);

        }

        public int SaveParameters(byte[] pFilename)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SaveParameters(m_hCam, pFilename);

        }

        public int SetAutoParameter(int param, ref double pval1, ref double pval2)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetAutoParameter(m_hCam, param, ref pval1, ref pval2);

        }


        public int Renumerate(int nMode)
        {

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_Renumerate(m_hCam, nMode);

        }

        public int GetHdrMode(ref int Mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetHdrMode(m_hCam, ref Mode);
        }

        public int EnableHdr(int Enable)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_EnableHdr(m_hCam, Enable);
        }


        public int SetHdrKneepoints(ref KNEEPOINTARRAY KneepointArray, int KneepointArraySize)
        {
            int i, j;

            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            // Temporary byte array
            byte[] pTemp = new byte[KneepointArraySize];
            byte[] temp = new byte[4];
            //Byte [] temp2 = new byte[8];

            // Get 4 bytes (NumberOfUsedKneepoints) and copy them to temp
            temp = System.BitConverter.GetBytes(KneepointArray.NumberOfUsedKneepoints);
            for (i = 0; i < 4; i++)
                pTemp[i] = temp[i];

            // The nest 4 bytes are not used (alignment)

            // copy the kneepoint values to temp
            for (i = 0; i < 10; i++)
            {
                temp = System.BitConverter.GetBytes(KneepointArray.Kneepoint[i].x);
                for (j = 0; j < 8; j++)
                    pTemp[8 + 16 * i + j] = temp[j];

                temp = System.BitConverter.GetBytes(KneepointArray.Kneepoint[i].y);
                for (j = 0; j < 8; j++)
                    pTemp[16 + 16 * i + j] = temp[j];
            }

            int ret = is_SetHdrKneepoints(m_hCam, pTemp, KneepointArraySize);
            return ret;
        }


        public int GetHdrKneepoints(ref KNEEPOINTARRAY KneepointArray, int KneepointArraySize)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            // Temporary byte array
            byte[] pTemp = new byte[KneepointArraySize];

            int ret = is_GetHdrKneepoints(m_hCam, pTemp, KneepointArraySize);
            if (ret == IS_SUCCESS)
            {
                KneepointArray.NumberOfUsedKneepoints = System.BitConverter.ToInt32(pTemp, 0);

                for (int i = 0; i < 10; i++)
                {
                    KneepointArray.Kneepoint[i].x = System.BitConverter.ToDouble(pTemp, 8 + i * 16);
                    KneepointArray.Kneepoint[i].y = System.BitConverter.ToDouble(pTemp, 16 + i * 16);
                }
            }

            return ret;
        }

        public int GetHdrKneepointInfo(ref KNEEPOINTINFO KneepointInfo, int KneepointInfoSize)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            // Temporary byte array
            byte[] pTemp = new byte[KneepointInfoSize];

            // Get Hdr info from API
            int ret = is_GetHdrKneepointInfo(m_hCam, pTemp, KneepointInfoSize);

            if (ret == IS_SUCCESS)
            {
                // Convert the bytes fromthe array to the correct data types
                KneepointInfo.NumberOfSupportedKneepoints = System.BitConverter.ToInt32(pTemp, 0);
                KneepointInfo.NumberOfSupportedKneepoints = System.BitConverter.ToInt32(pTemp, 4);
                KneepointInfo.MinValueX = System.BitConverter.ToDouble(pTemp, 8);
                KneepointInfo.MaxValueX = System.BitConverter.ToDouble(pTemp, 16);
                KneepointInfo.MinValueY = System.BitConverter.ToDouble(pTemp, 24);
                KneepointInfo.MaxValueY = System.BitConverter.ToDouble(pTemp, 32);

                for (int i = 0; i < 10; i++)
                {
                    KneepointInfo.DefaultKneepoint[i].x = System.BitConverter.ToDouble(pTemp, 40 + i * 16);
                    KneepointInfo.DefaultKneepoint[i].y = System.BitConverter.ToDouble(pTemp, 48 + i * 16);
                }
            }

            return ret;
        }

        public int SetOptimalCameraTiming(int Mode, int Timeout, ref int pMaxPxlClk, ref double pMaxFrameRate)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetOptimalCameraTiming(m_hCam, Mode, Timeout, ref pMaxPxlClk, ref pMaxFrameRate);
        }

        public int WaitForNextImage(uint timeout, ref IntPtr ppcImg, ref int pid)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_WaitForNextImage(m_hCam, timeout, ref ppcImg, ref pid);
        }

        public int InitImageQueue(int Mode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_InitImageQueue(m_hCam, Mode);
        }

        public int ExitImageQueue()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ExitImageQueue(m_hCam);
        }

        public int SetTimeout(uint Mode, uint Timeout)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetTimeout(m_hCam, Mode, Timeout);
        }

        public int GetTimeout(uint Mode, ref uint pTimeout)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetTimeout(m_hCam, Mode, ref pTimeout);
        }


        public int ImageFormat(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_ImageFormat(m_hCam, nCommand, pParam, nSizeOfParam);
        }


        public int FaceDetection(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_FaceDetection(m_hCam, nCommand, pParam, nSizeOfParam);
        }


        public int ImageStabilization(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_ImageStabilization(m_hCam, nCommand, pParam, nSizeOfParam);
        }


        public int Focus(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_Focus(m_hCam, nCommand, pParam, nSizeOfParam);
        }

        public int ScenePreset(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_ScenePreset(m_hCam, nCommand, pParam, nSizeOfParam);
        }

        public int Zoom(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_Zoom(m_hCam, nCommand, pParam, nSizeOfParam);
        }

        public int Sharpness(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_Sharpness(m_hCam, nCommand, pParam, nSizeOfParam);
        }

        public int Saturation(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_Saturation(m_hCam, nCommand, pParam, nSizeOfParam);
        }

        public int TriggerDebounce(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_TriggerDebounce(m_hCam, nCommand, pParam, nSizeOfParam);
        }

        public int ColorTemperature(uint nCommand, IntPtr pParam, uint nSizeOfParam)
        {
            if (m_hCam == 0)
            {
                return IS_INVALID_CAMERA_HANDLE;
            }

            return is_ColorTemperature(m_hCam, nCommand, pParam, nSizeOfParam);
        }

        // --------------------------------------------------------------------
        // new wrappers only valid for uEye ETH - BEGIN
        // --------------------------------------------------------------------

        // ---------------------  GetEthDeviceInfo  -----------------------
        //
        public int GetEthDeviceInfo(int iDeviceID, ref UEYE_ETH_DEVICE_INFO pDeviceInfo)
        {
            const int k_iDllSize_DeviceInfo = 640;
            const int k_iDllSize_DeviceInfo_Hrtbt = 248;
            const int k_iDllSize_DeviceInfo_Ctrl = 152;
            const int k_iDllSize_AdapterInfo = 160;
            //const int k_iDllSize_DriverInfo=		80;
            //const int k_iDllSize_EthCfg=			18;
            //const int k_iDllSize_AddrIpV4=		4;
            //const int k_iDllSize_AddrMAC=			6;
            //const int k_iDllSize_IpCfg=			12;
            //const int k_iDllSize_AutocfgIp_Stp=	12;

            int off = 0;

            byte[] pTemp = new byte[k_iDllSize_DeviceInfo];
            int ret = is_GetEthDeviceInfo(iDeviceID, pTemp, k_iDllSize_DeviceInfo);

            if (ret == IS_SUCCESS)
            {
                // copy UEYE_ETH_DEVICE_INFO_HEARTBEAT data
                off = 0;
                pDeviceInfo.infoDevHeartbeat.abySerialNumber = GetStringFromByte(pTemp, off, 12);
                off += 12;
                pDeviceInfo.infoDevHeartbeat.byDeviceType = pTemp[off];
                off++;
                pDeviceInfo.infoDevHeartbeat.byCameraID = pTemp[off];
                off++;
                pDeviceInfo.infoDevHeartbeat.wSensorID = (ushort)((pTemp[off + 1] << 8) + (pTemp[off]));
                off += 2;
                pDeviceInfo.infoDevHeartbeat.wSizeImgMem_MB = (ushort)((pTemp[off + 1] << 8) + (pTemp[off]));
                off += 2;
                off += 2;
                pDeviceInfo.infoDevHeartbeat.dwVerStarterFirmware = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevHeartbeat.dwVerRuntimeFirmware = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevHeartbeat.dwStatus = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                off += 4;
                pDeviceInfo.infoDevHeartbeat.wTemperature = (ushort)((pTemp[off + 1] << 8) + (pTemp[off]));
                off += 2;
                pDeviceInfo.infoDevHeartbeat.wLinkSpeed_Mb = (ushort)((pTemp[off + 1] << 8) + (pTemp[off]));
                off += 2;
                pDeviceInfo.infoDevHeartbeat.macDevice.abyOctet = GetStringFromByte(pTemp, off, 6);
                off += 6;
                pDeviceInfo.infoDevHeartbeat.wComportOffset = (ushort)((pTemp[off + 1] << 8) + (pTemp[off]));
                off += 2;
                pDeviceInfo.infoDevHeartbeat.ipcfgPersistentIpCfg.ipAddress.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevHeartbeat.ipcfgPersistentIpCfg.ipSubnetmask.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                off += 4;
                pDeviceInfo.infoDevHeartbeat.ipcfgCurrentIpCfg.ipAddress.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevHeartbeat.ipcfgCurrentIpCfg.ipSubnetmask.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                off += 4;
                pDeviceInfo.infoDevHeartbeat.macPairedHost.abyOctet = GetStringFromByte(pTemp, off, 6);
                off += 6;
                off += 2;
                pDeviceInfo.infoDevHeartbeat.ipPairedHostIp.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevHeartbeat.ipAutoCfgIpRangeBegin.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevHeartbeat.ipAutoCfgIpRangeEnd.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevHeartbeat.abyUserSpace = GetStringFromByte(pTemp, off, 8);

                // copy UEYE_ETH_DEVICE_INFO_CONTROL data
                off = (int)k_iDllSize_DeviceInfo_Hrtbt;
                pDeviceInfo.infoDevControl.dwDeviceID = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDevControl.dwControlStatus = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));

                // copy UEYE_ETH_ADAPTER_INFO data
                off = k_iDllSize_DeviceInfo_Hrtbt + k_iDllSize_DeviceInfo_Ctrl;
                pDeviceInfo.infoAdapter.dwAdapterID = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                off += 4;
                pDeviceInfo.infoAdapter.ethcfg.ipcfg.ipAddress.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoAdapter.ethcfg.ipcfg.ipSubnetmask.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                off += 4;
                pDeviceInfo.infoAdapter.ethcfg.mac.abyOctet = GetStringFromByte(pTemp, off, 6);
                off += 6;
                off += 2;
                pDeviceInfo.infoAdapter.bIsEnabledDHCP = Convert.ToBoolean(pTemp[off]);
                off += 4;
                pDeviceInfo.infoAdapter.autoCfgIp.ipAutoCfgIpRangeBegin.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoAdapter.autoCfgIp.ipAutoCfgIpRangeEnd.dwAddr = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                off += 4;
                pDeviceInfo.infoAdapter.bIsValidAutoCfgIpRange = Convert.ToBoolean(pTemp[off]);
                off += 4;
                pDeviceInfo.infoAdapter.dwCntDevicesKnown = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoAdapter.dwCntDevicesPaired = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoAdapter.wPacketFilter = (ushort)((pTemp[off + 1] << 8) + (pTemp[off]));

                // copy UEYE_ETH_DRIVER_INFO data
                off = k_iDllSize_DeviceInfo_Hrtbt + k_iDllSize_DeviceInfo_Ctrl + k_iDllSize_AdapterInfo;
                pDeviceInfo.infoDriver.dwMinVerStarterFirmware = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
                off += 4;
                pDeviceInfo.infoDriver.dwMaxVerStarterFirmware = (uint)((pTemp[off + 3] << 24) + (pTemp[off + 2] << 16) + (pTemp[off + 1] << 8) + (pTemp[off]));
            }

            return ret;
        }

        // ---------------------  SetPersistentIpCfg  -----------------------
        //
        public int SetPersistentIpCfg(int iDeviceID, ref UEYE_ETH_IP_CONFIGURATION pIpCfg)
        {
            const uint k_uDllSize_IpCfg = 12;

            uint off = 0;

            byte[] pTemp = new byte[k_uDllSize_IpCfg];

            // copy the UEYE_ETH_IP_CONFIGURATION data
            off = 0;
            pTemp[off] = pIpCfg.ipAddress.by1;
            off += 1;
            pTemp[off] = pIpCfg.ipAddress.by2;
            off += 1;
            pTemp[off] = pIpCfg.ipAddress.by3;
            off += 1;
            pTemp[off] = pIpCfg.ipAddress.by4;
            off += 1;
            pTemp[off] = pIpCfg.ipSubnetmask.by1;
            off += 1;
            pTemp[off] = pIpCfg.ipSubnetmask.by2;
            off += 1;
            pTemp[off] = pIpCfg.ipSubnetmask.by3;
            off += 1;
            pTemp[off] = pIpCfg.ipSubnetmask.by4;
            off += 1;
            pTemp[off] = 0xff;
            off += 1;
            pTemp[off] = 0xff;
            off += 1;
            pTemp[off] = 0xff;
            off += 1;
            pTemp[off] = 0xff;

            return is_SetPersistentIpCfg(iDeviceID, pTemp, k_uDllSize_IpCfg);
        }

        // ---------------------  SetStarterFirmware  -----------------------
        //
        public int SetStarterFirmware(int iDeviceID, byte[] pcFilepath, uint uFilepathLen)
        {
            return is_SetStarterFirmware(iDeviceID, pcFilepath, uFilepathLen);
        }

        // ---------------------  SetAutoCfgIpSetup  -----------------------
        //
        public int SetAutoCfgIpSetup(int iAdapterID, ref UEYE_ETH_AUTOCFG_IP_SETUP pSetup)
        {
            const uint k_uDllSize_AutocfgIp_Stp = 12;

            uint off = 0;

            byte[] pTemp = new byte[k_uDllSize_AutocfgIp_Stp];

            // copy the UEYE_ETH_AUTOCFG_IP_SETUP data
            off = 0;
            pTemp[off] = pSetup.ipAutoCfgIpRangeBegin.by1;
            off += 1;
            pTemp[off] = pSetup.ipAutoCfgIpRangeBegin.by2;
            off += 1;
            pTemp[off] = pSetup.ipAutoCfgIpRangeBegin.by3;
            off += 1;
            pTemp[off] = pSetup.ipAutoCfgIpRangeBegin.by4;
            off += 1;
            pTemp[off] = pSetup.ipAutoCfgIpRangeEnd.by1;
            off += 1;
            pTemp[off] = pSetup.ipAutoCfgIpRangeEnd.by2;
            off += 1;
            pTemp[off] = pSetup.ipAutoCfgIpRangeEnd.by3;
            off += 1;
            pTemp[off] = pSetup.ipAutoCfgIpRangeEnd.by4;
            off += 1;
            pTemp[off] = 0;
            off += 1;
            pTemp[off] = 0;
            off += 1;
            pTemp[off] = 0;
            off += 1;
            pTemp[off] = 0;

            return is_SetAutoCfgIpSetup(iAdapterID, pTemp, k_uDllSize_AutocfgIp_Stp);
        }

        // ---------------------  SetAutoCfgIpSetup  -----------------------
        //
        public int SetPacketFilter(int iAdapterID, uint uFilterSetting)
        {
            return is_SetPacketFilter(iAdapterID, uFilterSetting);
        }

        // ---------------------  GetComportNumber  -----------------------
        //
        public int GetComportNumber(ref uint pComportNumber)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetComportNumber(m_hCam, ref pComportNumber);
        }

        // -------------------  GetSupportedTestImages  -----------------------
        //
        public int GetSupportedTestImages(ref int SupportedTestImages)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetSupportedTestImages(m_hCam, ref SupportedTestImages);
        }

        // -------------------  GetTestImageValueRange  -----------------------
        //
        public int GetTestImageValueRange(int TestImage, ref int TestImageValueMin, ref int TestImageValueMax)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetTestImageValueRange(m_hCam, TestImage, ref TestImageValueMin, ref TestImageValueMax);
        }

        // -------------------  SetSensorTestImage  -----------------------
        //
        public int SetSensorTestImage(int Param1, int Param2)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetSensorTestImage(m_hCam, Param1, Param2);
        }



        // -------------------  SetCameraLUT  -----------------------
        //
        public int SetCameraLUT(uint Mode, uint NumberOfEntries, double[] Red_Grey, double[] Green, double[] Blue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetCameraLUT(m_hCam, Mode, NumberOfEntries, Red_Grey, Green, Blue);
        }

        // SetCameraID
        public int SetCameraID(int cameraID)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetCameraID(m_hCam, cameraID);
        }


        // -------------------  GetCameraLUT  -----------------------
        //
        public int GetCameraLUT(uint Mode, uint NumberOfEntries, double[] Red_Grey, double[] Green, double[] Blue)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetCameraLUT(m_hCam, Mode, NumberOfEntries, Red_Grey, Green, Blue);
        }


        // -------------------  SetColorConverter  -----------------------
        //
        public int SetColorConverter(int ColorMode, int ConvertMode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetColorConverter(m_hCam, ColorMode, ConvertMode);
        }


        // -------------------  GetColorConverter  -----------------------
        //
        public int GetColorConverter(int ColorMode, ref int pCurrentConvertMode, ref int pDefaultConvertMode, ref int pSupportedConvertModes)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetColorConverter(m_hCam, ColorMode, ref pCurrentConvertMode, ref pDefaultConvertMode, ref pSupportedConvertModes);
        }


        // -------------------  GetCaptureErrorInfo  -----------------------
        //
        public int GetCaptureErrorInfo(ref UEYE_CAPTURE_ERROR_INFO pCaptureErrorInfo, int SizeCaptureErrorInfo)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            // Temporary byte array
            byte[] pTemp = new byte[SizeCaptureErrorInfo];

            int ret = is_GetCaptureErrorInfo(m_hCam, pTemp, SizeCaptureErrorInfo);
            if (ret == IS_SUCCESS)
            {
                pCaptureErrorInfo.dwCapErrCnt_Total = pTemp[0];
                int i;
                for (i = 0; i < 256; i++)
                {
                    pCaptureErrorInfo.adwCapErrCnt_Detail[i] = pTemp[64 + i];
                }
            }

            return ret;
        }


        // -------------------  ResetCaptureErrorInfo  -----------------------
        //
        public int ResetCaptureErrorInfo()
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_ResetCaptureErrorInfo(m_hCam);
        }


        // -------------------  GetDuration -----------------------
        //
        public int GetDuration(uint nMode, ref uint pnTime)
        {
            return is_GetDuration(m_hCam, nMode, ref pnTime);
        }


        // -------------------  GetSensorScalerInfo -----------------------
        //
        public int GetSensorScalerInfo(ref SENSORSCALERINFO pSensorScalerInfo, int nSensorScalerInfoSize)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetSensorScalerInfo(m_hCam, ref pSensorScalerInfo, nSensorScalerInfoSize);
        }


        // -------------------  SetSensorScaler -----------------------
        //
        public int SetSensorScaler(uint nMode, double dblFactor)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetSensorScaler(m_hCam, nMode, dblFactor);
        }


        // -------------------  GetImageInfo -----------------------
        //
        public int GetImageInfo(int nImageBufferID, ref UEYEIMAGEINFO pImageInfo, int nImageInfoSize)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_GetImageInfo(m_hCam, nImageBufferID, ref pImageInfo, nImageInfoSize);
        }


        // -------------------  DirectRenderer -----------------------
        //
        public int DirectRenderer(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_DirectRenderer(m_hCam, nMode, pParam, SizeOfParam);
        }

        // ----------------- LoadD3DOverlayFromFile ------------------
        //
        public int LoadD3DOverlayFromFile(string strFilename)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            byte[] byTemp = new byte[255];
            for (int i = 0; i < strFilename.Length; i++)
            {
                byTemp[i] = (byte)strFilename[i];
            }

            unsafe
            {
                fixed (byte* pstrFilename = &byTemp[0])
                {
                    IntPtr IntPtrFilename = (IntPtr)pstrFilename;
                    int nRet = is_DirectRenderer(m_hCam, uEyeDriverWrapper.DR_LOAD_OVERLAY_FROM_FILE, IntPtrFilename, 0);
                    if (nRet != IS_SUCCESS)
                    {
                        if (nRet == IS_NOT_SUPPORTED)
                        {
                            MessageBox.Show("ERROR: File cannot be loaded. It is probably no bitmap");
                        }
                        else
                        {
                            MessageBox.Show("ERROR: File cannot be loaded");
                        }
                    }
                    return nRet;
                }
            }
        }


        // -------------------  HotPixel -----------------------
        //
        public int HotPixel(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_HotPixel(m_hCam, nMode, pParam, SizeOfParam);
        }

        // -------------------  AOI -----------------------
        //
        public int AOI(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_AOI(m_hCam, nMode, pParam, SizeOfParam);
        }

        // -------------------  Transfer -----------------------
        //
        public int Transfer(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_Transfer(m_hCam, nMode, pParam, SizeOfParam);
        }

        // -------------------  IpConfig -----------------------
        //
        public int IpConfig(int iID, UEYE_ETH_ADDR_MAC mac, uint nCommand, IntPtr pParam, uint cbSizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_IpConfig(iID, mac, nCommand, pParam, cbSizeOfParam);
        }

        // -------------------  DeviceFeature -----------------------
        //
        public int DeviceFeature(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_DeviceFeature(m_hCam, nMode, pParam, SizeOfParam);
        }

        // -------------------  Configuration -----------------------
        //
        public int Configuration(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            return is_Configuration(nMode, pParam, SizeOfParam);
        }

        // -------------------  Exposure -----------------------
        //
        public int Exposure(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_Exposure(m_hCam, nMode, pParam, SizeOfParam);
        }

        // -------------------  Trigger -----------------------
        //
        public int Trigger(uint nMode, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_Trigger(m_hCam, nMode, pParam, SizeOfParam);
        }

        // -------------------  Hardware Gamma -----------------------
        //
        public int SetHardwareGamma(int nMode)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_SetHardwareGamma(m_hCam, nMode);
        }

        // -------------------  CaptureStatus -----------------------
        //
        public int CaptureStatus(uint nCommand, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_CaptureStatus(m_hCam, nCommand, pParam, SizeOfParam);
        }

        // -------------------  BootBoost -----------------------
        //
        public int BootBoost(uint nCommand, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_BootBoost(m_hCam, nCommand, pParam, SizeOfParam);
        }

        // -------------------  IO -----------------------
        //
        public int IO(uint nCommand, IntPtr pParam, uint SizeOfParam)
        {
            if (m_hCam == 0)
                return IS_INVALID_CAMERA_HANDLE;

            return is_IO(m_hCam, nCommand, pParam, SizeOfParam);
        }
    }
} // namespace

#pragma warning restore 0169
#pragma warning restore 0649
