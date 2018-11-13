// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Cameras.Internal.SVS;
using Metrilus.Util;
using Metrilus.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// MetriCam 2 Wrapper for SVS VISTEK cameras.
    /// </summary>
    public class SVS : Camera
    {
        #region Types
        /// <summary>
        /// Defines the custom channel names for easier handling.
        /// </summary>
        /// <remarks>Similar to MetriCam2.ChannelNames for standard channel names.</remarks>
        public class CustomChannelNames
        {
            /// <summary>
            /// When acquiring 12 or 16 bit data, this channel provides the raw image data.
            /// </summary>
            /// <remarks>If you acquire 12-bit data it will be converted and scaled to 16 bit.</remarks>
            public const string Raw16 = "Raw16Bit";
            /// <summary>
            /// When acquiring 12 or 16 bit data, this channel provides the de-bayered red color plane.
            /// </summary>
            /// <remarks>
            /// While the type of this channel is <see cref="FloatCameraImage"/> the range of the image data will be ushort.
            /// If you acquire 12-bit data it will be converted and scaled to 16 bit.
            /// </remarks>
            public const string Red16 = "Red16Bit";
            /// <summary>
            /// When acquiring 12 or 16 bit data, this channel provides the de-bayered green color plane.
            /// </summary>
            /// <remarks>
            /// While the type of this channel is <see cref="FloatCameraImage"/> the range of the image data will be ushort.
            /// If you acquire 12-bit data it will be converted and scaled to 16 bit.
            /// </remarks>
            public const string Green16 = "Green16Bit";
            /// <summary>
            /// When acquiring 12 or 16 bit data, this channel provides the de-bayered blue color plane.
            /// </summary>
            /// <remarks>
            /// While the type of this channel is <see cref="FloatCameraImage"/> the range of the image data will be ushort.
            /// If you acquire 12-bit data it will be converted and scaled to 16 bit.
            /// </remarks>
            public const string Blue16 = "Blue16Bit";
        }
        #endregion

        #region Public Constants
        public const int NumStreamingChannelBuffers = 2;
        #endregion

        #region Private Fields
        /// <summary>
        /// If the camera does not receive any traffic within that many seconds it will drop the connection.
        /// </summary>
        private const float HeartbeatTimeout = 15.0f;
        /// <summary>
        /// Timeout after which Update is considered to be failed (because it has not gotten any image data).
        /// </summary>
        private const int UpdateTimeout_MS = 5 * 1000;

        private static GigeApi gigeApi = null;
        private static GigeApi.LogMessageCallback logMessageCallbackDelegate;// Save a reference to the callback. Otherwise it will be disposed.

        private int container;
        private IntPtr hCamera;
        private IntPtr streamingChannel;
        private GigeApi.SVGIGE_PIXEL_DEPTH pixelDepth;
        private GigeApi.GVSP_PIXEL_TYPE pixelType;
        private GigeApi.StreamCallback streamCallBack;
        private GigeApi.EventCallback eventCallback;
        private GigeApi.BAYER_METHOD bayerMethod;
        private int packetSize;
        private int width;
        private int height;
        private int bitCount;
        private bool isMonochrome;
        private bool useGrayScale;
        private byte[] imageBuffer;
        private object dataLock;
        private AutoResetEvent dataAvailable;
        private Bitmap currentBmp;
        private int latestCameraFrameNumber;

        //event IDs
        private IntPtr eventIdFrameCompleted = new IntPtr(GigeApi.SVGigE_NO_EVENT);
        private IntPtr eventIdStartTransfer = new IntPtr(GigeApi.SVGigE_NO_EVENT);
        #endregion

        #region Public Properties

#if !NETSTANDARD2_0
        public override Icon CameraIcon => Properties.Resources.SVSIcon;
#endif

        private ListParamDesc<string> AcquisitionModeDesc
        {
            get
            {
                log.Debug("Get AcquisitionModeDesc");

                return new ListParamDesc<string>(typeof(GigeApi.ACQUISITION_MODE))
                {
                    Description = "Acquisition mode",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }
        /// <summary>
        /// Set the AcquisitionMode
        /// </summary>
        public GigeApi.ACQUISITION_MODE AcquisitionMode
        {
            get
            {
                GigeApi.ACQUISITION_MODE value = GigeApi.ACQUISITION_MODE.ACQUISITION_MODE_FIXED_FREQUENCY;
                GigeApi.SVSGigeApiReturn error;
                error = gigeApi.Gige_Camera_getAcquisitionMode(hCamera, ref value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameter", "AcquisitionMode: " + error.ToString());
                }
                return value;
            }
            set
            {
                log.DebugFormat("{0}: Setting AcquisitionMode={1}", Name, value.ToString());
                GigeApi.SVSGigeApiReturn error;
                error = gigeApi.Gige_Camera_setAcquisitionMode(hCamera, value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    log.ErrorFormat("{0}: Could not set AcquisitionMode to {1}. {2}", Name, value.ToString(), error.ToString());
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "AcquisitionMode: " + error.ToString());
                }

                if (GigeApi.ACQUISITION_MODE.ACQUISITION_MODE_EXT_TRIGGER_EXT_EXPOSURE == value
                    || GigeApi.ACQUISITION_MODE.ACQUISITION_MODE_EXT_TRIGGER_INT_EXPOSURE == value)
                {
                    // read and output TriggerPolarity
                    GigeApi.TRIGGER_POLARITY tp = GigeApi.TRIGGER_POLARITY.TRIGGER_POLARITY_NEGATIVE;
                    gigeApi.Gige_Camera_getTriggerPolarity(hCamera, ref tp);
                    log.InfoFormat("{0}: TriggerPolarity = {1}", Name, tp.ToString());

                    // Enable LED override for easier debugging
                    // - blue:    waiting for trigger
                    // - cyan:    exposure
                    // - magenta: read-out
                    gigeApi.Gige_Camera_setAcqLEDOverride(hCamera, true);
                }
                else
                {
                    // Disable LED override
                    gigeApi.Gige_Camera_setAcqLEDOverride(hCamera, false);
                }
            }
        }

        /// <summary>
        /// Enable/disable auto gain.
        /// </summary>
        public bool AutoGain
        {
            get
            {
                bool value = false;
                GigeApi.SVSGigeApiReturn error;
                error = gigeApi.Gige_Camera_getAutoGainEnabled(hCamera, ref value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameter", "AutoGainEnabled: " + error.ToString());
                }
                return value;
            }
            set
            {
                GigeApi.SVSGigeApiReturn error;
                error = gigeApi.Gige_Camera_setAutoGainEnabled(hCamera, value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "AutoGainEnabled: " + error.ToString());
                }
            }
        }

        private RangeParamDesc<int> LogLevelDesc
        {
            get
            {
                return new RangeParamDesc<int>(0, 7)
                {
                    Description = "Log level",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        /// <summary>
        /// Set the log level of the camera SDK.
        /// </summary>
        /// <remarks>
        /// This property is only considered during <see cref="Connect"/>.
        /// 
        /// Log messages can be requested for various log levels:
        /// 0 - logging off
        /// 1 - CRITICAL errors that prevent from further operation
        /// 2 - ERRORs that prevent from proper functioning
        /// 3 - WARNINGs which usually do not affect proper work
        /// 4 - INFO for listing camera communication (default)
        /// 5 - DIAGNOSTICS for investigating image callbacks
        /// 6 - DEBUG for receiving multiple parameters for image callbacks
        /// 7 - DETAIL for receiving multiple signals for each image callback
        /// </remarks>
        public int LogLevel { get; set; }

        private ParamDesc<bool> LogToFileDesc
        {
            get
            {
                return new ParamDesc<bool>()
                {
                    Description = "Log to file",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected,
                };
            }
        }
        /// <summary>
        /// If enabled before calling <see cref="Connect"/> then a SVS-specific log file is created.
        /// </summary>
        /// <seealso cref="LogFilename"/>
        /// <seealso cref="LogLevel"/>
        public bool LogToFile { get; set; }

        /// <summary>
        /// Filename (read-only) of the detailed log file.
        /// </summary>
        public const string LogFilename = "MetriCam2.SVS.LogDetail.txt";

        private RangeParamDesc<float> ExposureDesc
        {
            get
            {
                log.Debug("Get ExposureDesc");

                float min = 0, max = 0, increment = 0;
                if (IsConnected)
                {
                    log.Debug("    IsConnected = true");
                    GigeApi.SVSGigeApiReturn error;
                    error = gigeApi.Gige_Camera_getExposureTimeMin(hCamera, ref min);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameterValues", "ExposureTime.Min: " + error.ToString());
                    }
                    error = gigeApi.Gige_Camera_getExposureTimeMax(hCamera, ref max);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameterValues", "ExposureTime.Max: " + error.ToString());
                    }
                    error = gigeApi.Gige_Camera_getExposureTimeIncrement(hCamera, ref increment);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameterValues", "ExposureTime.Increment: " + error.ToString());
                    }
                }

                return new RangeParamDesc<float>(min, max)
                {
                    Description = "Exposure time",
                    Unit = "ms",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }
        /// <summary>
        /// Exposure time in [ms].
        /// </summary>
        /// <remarks>Has no effect if set while not connected.</remarks>
        public float Exposure
        {
            get
            {
                GigeApi.SVSGigeApiReturn error;
                float value = 0f;
                error = gigeApi.Gige_Camera_getExposureTime(hCamera, ref value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameter", "ExposureTime: " + error.ToString());
                }
                return value * 0.001f;
            }
            set
            {
                GigeApi.SVSGigeApiReturn error;
                float value_ms = value * 1000f;
                error = gigeApi.Gige_Camera_setAutoExposureLimits(hCamera, value_ms, value_ms);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "AutoExposureLimits: " + error.ToString());
                }
                error = gigeApi.Gige_Camera_setExposureTime(hCamera, value_ms);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "ExposureTime: " + error.ToString());
                }
            }
        }

        private RangeParamDesc<float> FrameRateDesc
        {
            get
            {
                log.Debug("Get FrameRateDesc");

                float min = 0, max = 0, increment = 0;
                if (IsConnected)
                {
                    log.Debug("    IsConnected = true");
                    GigeApi.SVSGigeApiReturn error;
                    error = gigeApi.Gige_Camera_getFrameRateRange(hCamera, ref min, ref max);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameterValues", "FrameRate.Range: " + error.ToString());
                    }
                    error = gigeApi.Gige_Camera_getFrameRateIncrement(hCamera, ref increment);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameterValues", "FrameRate.Increment: " + error.ToString());
                    }
                }

                return new RangeParamDesc<float>(min, max)
                {
                    Description = "Frame rate",
                    Unit = "Hz",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }
        /// <summary>
        /// FrameRate in Hz.
        /// </summary>
        /// <remarks>Has no effect if set while not connected.</remarks>
        public float FrameRate
        {
            get
            {
                GigeApi.SVSGigeApiReturn error;
                float value = 0f;
                error = gigeApi.Gige_Camera_getFrameRate(hCamera, ref value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameter", "FrameRate: " + error.ToString());
                }
                return value;
            }
            set
            {
                GigeApi.SVSGigeApiReturn error;
                error = gigeApi.Gige_Camera_setFrameRate(hCamera, value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "FrameRate: " + error.ToString());
                }
            }
        }

        private RangeParamDesc<float> GainDesc
        {
            get
            {
                log.Debug("Get GainDesc");

                float min = 0, max = 0, increment = 0;
                if (IsConnected)
                {
                    log.Debug("    IsConnected = true");
                    GigeApi.SVSGigeApiReturn error;
                    error = gigeApi.Gige_Camera_getGainMax(hCamera, ref max);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameterValues", "Gain.Max: " + error.ToString());
                    }
                    error = gigeApi.Gige_Camera_getGainIncrement(hCamera, ref increment);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameterValues", "Gain.Increment: " + error.ToString());
                    }
                }

                return new RangeParamDesc<float>(min, max)
                {
                    Description = "Gain factor",
                    Unit = "%",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }
        /// <summary>
        /// Gain in [%].
        /// </summary>
        /// <remarks>Only master gain can be changed, not the individual color channels.</remarks>
        /// <remarks>Has no effect if set while not connected.</remarks>
        public float Gain
        {
            get
            {
                GigeApi.SVSGigeApiReturn error;
                float value = 0f;
                error = gigeApi.Gige_Camera_getGain(hCamera, ref value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameter", "Gain: " + error.ToString());
                }
                return value;
            }
            set
            {
                GigeApi.SVSGigeApiReturn error;
                error = gigeApi.Gige_Camera_setGain(hCamera, value);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "Gain: " + error.ToString());
                }
            }
        }

        private ParamDesc<Point3f> WhiteBalanceDesc
        {
            get
            {
                log.Debug("Get WhiteBalanceDesc");

                return new ParamDesc<Point3f>()
                {
                    Description = "White balance",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }
        /// <summary>
        /// White balance for the three color channels.
        /// </summary>
        /// <remarks>The X, Y, Z components of this property correspond to the Red, Green, Blue channel, respectively.</remarks>
        /// <remarks>Has no effect if set while not connected.</remarks>
        public Point3f WhiteBalance
        {
            get
            {
                GigeApi.SVSGigeApiReturn error;
                float r = 0f, g = 0f, b = 0f;
                error = gigeApi.Gige_Camera_getWhiteBalance(hCamera, ref r, ref g, ref b);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_getParameter", "WhiteBalance: " + error.ToString());
                }
                return new Point3f(r, g, b);
            }
            set
            {
                GigeApi.SVSGigeApiReturn error;
                error = gigeApi.Gige_Camera_setWhiteBalance(hCamera, value.X, value.Y, value.Z);
                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "WhiteBalance: " + error.ToString());
                }
            }
        }

        private ParamDesc<bool> EnableGrayScaleDesc
        {
            get
            {
                log.Debug("Get EnableGrayScaleDesc");

                return new ParamDesc<bool>()
                {
                    Description = "Enable raw gray scale mode with 16 bpp",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected,
                    WritableWhen = ParamDesc.ConnectionStates.Connected,
                };
            }
        }
        /// <summary>
        /// Decides whether to provide gray scale bitmaps with 16 bits per pixel.
        /// </summary>
        /// <remarks>The current implementation requires that you disconnect and connect the camera after changing this parameter.</remarks>
        public bool EnableGrayScale
        {
            get
            {
                return useGrayScale;
            }
            set
            {
                if (value == useGrayScale)
                {
                    return;
                }

                log.DebugFormat("Changing EnableGrayScale to {0}", value);

                GigeApi.SVSGigeApiReturn error;
                GigeApi.ACQUISITION_MODE acqMode;

                lock (cameraLock)
                {
                    lock (dataLock)
                    {
                        // Stop camera and remember acquisition mode
                        acqMode = AcquisitionMode;
                        StopGrabbing();
                    } // release data lock for a moment, because the driver might wait for it in the stream callback

                    CloseStreamingChannel();

                    log.Debug("setting currentBmp := null [EnableGrayScale/1]");
                    currentBmp = null;

                    lock (dataLock)
                    {
                        log.DebugFormat("bitCount = {0}", bitCount);
                        // gray scale
                        if (value)
                        {
                            // check if 12 bit is supported. if so, set it
                            // otherwise, 8 bit will stay selected
                            if (bitCount != 12)
                            {
                                log.DebugFormat("Gray: Trying to set pixel depth to 12bpp");
                                if (gigeApi.Gige_Camera_isCameraFeature(hCamera, GigeApi.CAMERA_FEATURE.CAMERA_FEATURE_COLORDEPTH_12BPP))
                                {
                                    log.DebugFormat("Camera supports 12bpp");
                                    log.DebugFormat("Gray: setting pixel depth to 12bpp");
                                    error = gigeApi.Gige_Camera_setPixelDepth(hCamera, GigeApi.SVGIGE_PIXEL_DEPTH.SVGIGE_PIXEL_DEPTH_12);
                                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                                    {
                                        log.ErrorFormat("    Could not set PixelDepth to 12bpp: {0}", error.ToString());
                                        throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "PixelDepth: " + error.ToString());
                                    }
                                }
                                bitCount = 12;
                            }
                        }
                        else
                        {
                            // color
                            if (bitCount != 8)
                            {
                                log.DebugFormat("Color: setting pixel depth to 8bpp");
                                error = gigeApi.Gige_Camera_setPixelDepth(hCamera, GigeApi.SVGIGE_PIXEL_DEPTH.SVGIGE_PIXEL_DEPTH_8);
                                if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                                {
                                    log.ErrorFormat("    Could not set PixelDepth to 8bpp: {0}", error.ToString());
                                    throw ExceptionBuilder.Build(typeof(Exception), Name, "error_setParameter", "PixelDepth: " + error.ToString());
                                }
                                bitCount = 8;
                            }
                        }
                        log.DebugFormat("    bitCount={0}", bitCount);

                        useGrayScale = value;
                        isMonochrome = useGrayScale;

                        SetupBuffer();

                        CreateStreamingChannel();

                        log.Debug("setting currentBmp := null [EnableGrayScale/2]");
                        currentBmp = null;

                        // Restore acquisition mode - restarts camera
                        StartGrabbing(acqMode);
                    }
                }
            }
        }

        /// <summary>
        /// Flag indicating whether the camera is monochrome, or in a monochrome mode.
        /// </summary>
        public bool IsMonochrome { get { return isMonochrome; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new SVS camera.
        /// </summary>
        public SVS()
            : base()
        {
            enableImplicitThreadSafety = true;

            container = GigeApi.SVGigE_NO_CLIENT;
            hCamera = IntPtr.Zero;
            streamingChannel = IntPtr.Zero;
            lock (cameraLock)
            {
                if (null == gigeApi)
                {
                    gigeApi = new GigeApi();
                }
            }
            packetSize = 0;
            isMonochrome = false;
            bitCount = 0;
            bayerMethod = GigeApi.BAYER_METHOD.BAYER_METHOD_HQLINEAR;
            dataAvailable = new AutoResetEvent(false);
            dataLock = new object();
            useGrayScale = false;
            LogLevel = 4;// default log level of the SDK
        }
        #endregion

        #region MetriCam2 Camera Interface Methods
        /// <summary>
        /// Reset list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
        /// </summary>
        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.Raw16, typeof(UShortCameraImage)));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.Red16, typeof(FloatCameraImage)));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.Green16, typeof(FloatCameraImage)));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.Blue16, typeof(FloatCameraImage)));
        }

        /// <summary>
        /// Activates a channel if is not yet active.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        protected override void ActivateChannelImpl(string channelName)
        {
            log.DebugFormat("Activating channel {0}", channelName);
            switch (channelName)
            {
                case CustomChannelNames.Raw16:
                case CustomChannelNames.Red16:
                case CustomChannelNames.Green16:
                case CustomChannelNames.Blue16:
                    DeactivateChannel(ChannelNames.Color);
                    EnableGrayScale = true;
                    break;
                case ChannelNames.Color:
                    DeactivateChannel(CustomChannelNames.Raw16);
                    DeactivateChannel(CustomChannelNames.Red16);
                    DeactivateChannel(CustomChannelNames.Green16);
                    DeactivateChannel(CustomChannelNames.Blue16);
                    EnableGrayScale = false;
                    break;
            }
        }

        /// <summary>
        /// Connects the camera.
        /// </summary>
        protected override void ConnectImpl()
        {
            log.DebugFormat("{0}: connecting", Name);
            GigeApi.SVSGigeApiReturn error = GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS;
            container = GigeApi.SVGigE_NO_CLIENT;
            hCamera = IntPtr.Zero;
            int numCameras;

            // 1. check for available cameras
            container = gigeApi.Gige_CameraContainer_create(GigeApi.SVGigETL_Type.SVGigETL_TypeFilter);
            if (container < 0)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "ContainerCreate failed");
            }
            error = gigeApi.Gige_CameraContainer_discovery(container);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Camera discovery failed: " + error.ToString());
            }
            numCameras = gigeApi.Gige_CameraContainer_getNumberOfCameras(container);
            if (numCameras <= 0)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "No cameras found.");
            }
            log.DebugFormat("    found {0} cameras", numCameras);

            if ("" == SerialNumber)
            {
                // 2a. use first free camera
                log.Debug("    no serial number requested, connecting to first free camera");
                for (int i = 0; i < numCameras; i++)
                {
                    IntPtr tmpHandle = gigeApi.Gige_CameraContainer_getCamera(container, i);
                    // 3a. try to open connection
                    error = gigeApi.Gige_Camera_openConnection(tmpHandle, HeartbeatTimeout);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        // try next camera
                        continue;
                    }

                    hCamera = tmpHandle;
                    Model = gigeApi.Gige_Camera_getModelName(hCamera);
                    SerialNumber = gigeApi.Gige_Camera_getSerialNumber(hCamera);
                    break;
                }
            }
            else
            {
                // TODO: Use SDK functions CameraContainer_findCamera(container, CameraItem) instead of manually testing all camera objects
                // 2b. find camera matching the SerialNumber
                log.DebugFormat("    serial number {0} requested", SerialNumber);
                for (int i = 0; i < numCameras; i++)
                {
                    IntPtr tmpHandle = gigeApi.Gige_CameraContainer_getCamera(container, i);
                    string tmpSerialNumber = gigeApi.Gige_Camera_getSerialNumber(tmpHandle);
                    log.DebugFormat("    camera {0} has serial number {1}", i, tmpSerialNumber);
                    if (tmpSerialNumber == SerialNumber)
                    {
                        hCamera = tmpHandle;
                        Model = gigeApi.Gige_Camera_getModelName(hCamera);
                        break;
                    }
                }

                // 3b. try to open connection
                error = gigeApi.Gige_Camera_openConnection(hCamera, HeartbeatTimeout);
            }

            // check if connection was successful
            if (IntPtr.Zero == hCamera)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Failed to open connection to camera");
            }
            log.InfoFormat("    connected to camera with serial number {0}", SerialNumber);

            // 4. create stream
            error = CreateStreamingChannel();
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Failed to create stream: " + error.ToString());
            }

            // Register log message callback
            if (LogLevel >= LogLevelDesc.Min && LogLevel <= LogLevelDesc.Max)
            {
                // SVS can log either to callback, or to file.
                // Since the log can be quite polluted, logging to file is preferred.
                if (LogToFile)
                {
                    gigeApi.Gige_Camera_registerForLogMessages(hCamera, LogLevel, LogFilename, LogCallback: null, MessageContext: IntPtr.Zero);
                }
                else
                {
                    logMessageCallbackDelegate = new GigeApi.LogMessageCallback(CameraLogMessageCallback);
                    error = gigeApi.Gige_Camera_registerForLogMessages(hCamera, LogLevel, LogFilename: "", LogCallback: logMessageCallbackDelegate, MessageContext: IntPtr.Zero);
                    if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                    {
                        throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Failed to register for log messages from camera: " + error.ToString());
                    }
                }
            }

            // 5. get width and height
            if (gigeApi.Gige_Camera_getSizeX(hCamera, ref width) != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS ||
                gigeApi.Gige_Camera_getSizeY(hCamera, ref height) != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Failed to get width and/or height of current stream");
            }
            log.DebugFormat("    resolution is {0}x{1}", width, height);

            // 6. get pixel depth
            pixelDepth = GigeApi.SVGIGE_PIXEL_DEPTH.SVGIGE_PIXEL_DEPTH_16;
            error = gigeApi.Gige_Camera_getPixelDepth(hCamera, ref pixelDepth);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Failed to get pixel depth: " + error.ToString());
            }
            switch (pixelDepth)
            {
                case GigeApi.SVGIGE_PIXEL_DEPTH.SVGIGE_PIXEL_DEPTH_8:
                    bitCount = 8;
                    break;
                case GigeApi.SVGIGE_PIXEL_DEPTH.SVGIGE_PIXEL_DEPTH_12:
                    bitCount = 12;
                    break;
                case GigeApi.SVGIGE_PIXEL_DEPTH.SVGIGE_PIXEL_DEPTH_16:
                    bitCount = 16;
                    break;
                default:
                    throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Unsupported pixel depth");
            }
            log.DebugFormat("    pixel depth is {0}", pixelDepth.ToString());

            // 7. get pixel type
            error = GetPixelType();
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                throw ExceptionBuilder.Build(typeof(Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "Failed to get pixel type: " + error.ToString());
            }
            log.DebugFormat("    pixel type is {0}", pixelType.ToString());

            // 8. select default channel
            ActivateChannel(ChannelNames.Color);
            SelectChannel(ChannelNames.Color);
            latestCameraFrameNumber = 0;

            // 9. start grabbing
            log.Debug("    start grabbing");
            SetupBuffer();
            StartGrabbing();
            log.Debug("    connect complete");
        }

        /// <summary>
        /// Disconnects the camera.
        /// </summary>
        protected override void DisconnectImpl()
        {
            // make sure Update can leave its waiting state
            dataAvailable.Set();

            GigeApi.SVSGigeApiReturn error;
            // stop grabbing
            error = StopGrabbing();
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Debug(Name + ": StopGrabbing() failed: " + error.ToString());
            }
            // close stream
            error = CloseStreamingChannel();

            // @TESTING
            // this logging code is just for a testing release.
            // Unregister logging
            int myLogLevel_off = 0;
            string myLogFilename = "logDetail.txt";
            gigeApi.Gige_Camera_registerForLogMessages(hCamera, myLogLevel_off, myLogFilename, LogCallback: null, MessageContext: IntPtr.Zero);

            // close connection
            error = gigeApi.Gige_Camera_closeConnection(hCamera);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Debug(Name + ": Camera_closeConnection() failed: " + error.ToString());
            }
            hCamera = IntPtr.Zero;
            // destroy container
            gigeApi.Gige_CameraContainer_delete(container);
        }

        /// <summary>
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        protected override void UpdateImpl()
        {
            //log.EnterMethod();
            log.Debug("setting currentBmp := null [UpdateImpl]");
            currentBmp = null;

            // wait for image
            //if (!dataAvailable.WaitOne(UpdateTimeout))
            //{
            //    log.Error(Name + ": Update Timeout");
            //    return;
            //}
            //GigeApi.SVSGigeApiReturn apiReturn = GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS;

            //log.DebugFormat("{0}: UpdateImpl", Name);

            // If in Software Trigger mode: trigger frame acquisition and wait for the frame.
            if (AcquisitionMode == GigeApi.ACQUISITION_MODE.ACQUISITION_MODE_SOFTWARE_TRIGGER)
            {
                log.DebugFormat("{0}: Calling soft-trigger", Name);
                GigeApi.SVSGigeApiReturn apiReturn = gigeApi.Gige_Camera_softwareTrigger(hCamera);
                if (apiReturn != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                {
                    log.ErrorFormat("{0}: Call to soft-trigger failed: {1}.", Name, apiReturn.ToString());
                    return;
                }
            }

            Rectangle BoundsRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData;
            IntPtr ptr;

            log.DebugFormat("{0}: Waiting for data", Name);
            bool signalReceived = dataAvailable.WaitOne(UpdateTimeout_MS);
            if (!signalReceived)
            {
                log.ErrorFormat("{0}: Did not receive a frame within {1} ms", Name, UpdateTimeout_MS);
                // TODO: if continuous logging fails [to be tested], output camera debug info from file.
                return;
            }
            log.DebugFormat("{0}: UpdateImpl: Received a frame (dataAvailable event has been set)", Name);
            // The dataAvailable event may have been raised by DisconnectImpl.
            // In that case, silently go away.
            if (!IsConnected)
            {
                return;
            }

            log.Debug("setting currentBmp := data [UpdateImpl]");
            // gray scale
            if (useGrayScale || isMonochrome)
            {
                switch (bitCount)
                {
                    case 8:
                        currentBmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                        // fix palette
                        ColorPalette p = currentBmp.Palette;
                        for (uint i = 0; i < 256; ++i)
                        {
                            p.Entries[i] = Color.FromArgb(
                            (byte)0xFF,
                            (byte)i,
                            (byte)i,
                            (byte)i);
                        }
                        currentBmp.Palette = p;
                        break;
                    case 12:
                    case 16:
                        currentBmp = new Bitmap(width, height, PixelFormat.Format16bppGrayScale);
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unknown bit count ({0})", bitCount));
                }
            }
            else
            {
                // color
                currentBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            }

            lock (dataLock)
            {
                // copy byte data into bitmap
                int numBytes = imageBuffer.Length;
                bmpData = currentBmp.LockBits(BoundsRect,
                                                ImageLockMode.WriteOnly,
                                                currentBmp.PixelFormat);
                ptr = bmpData.Scan0;
                Marshal.Copy(imageBuffer, 0, ptr, numBytes);
                currentBmp.UnlockBits(bmpData);

                // copy camera frame number
                FrameNumber = latestCameraFrameNumber;
            }
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>Copy of driver's image buffer.</returns>
        protected unsafe override CameraImage CalcChannelImpl(string channelName)
        {
            lock (dataLock)
            {
                if (currentBmp == null)
                {
                    log.Error(Name + " CalcChannelImpl: currentBmp is NULL.");
                    return null;
                }

                switch (channelName)
                {
                    case ChannelNames.Color:
                        return new ColorCameraImage((Bitmap)currentBmp.Clone());

                    case CustomChannelNames.Red16:
                        return DeBayerBilinear(true, false, false)["red"];

                    case CustomChannelNames.Green16:
                        return DeBayerBilinear(false, true, false)["green"];

                    case CustomChannelNames.Blue16:
                        return DeBayerBilinear(false, false, true)["blue"];

                    case CustomChannelNames.Raw16:
                        int width = currentBmp.Width;
                        int height = currentBmp.Height;

                        UShortCameraImage resRaw16 = new UShortCameraImage(width, height);

                        Rectangle rect = new Rectangle(0, 0, width, height);
                        BitmapData bmpData = currentBmp.LockBits(rect, ImageLockMode.ReadOnly, currentBmp.PixelFormat);
                        for (int y = 0; y < height; y++)
                        {
                            ushort* linePtr = (ushort*)(bmpData.Scan0 + y * bmpData.Stride);
                            for (int x = 0; x < width; x++)
                            {
                                resRaw16[y, x] = *linePtr++;
                            }
                        }
                        currentBmp.UnlockBits(bmpData);

                        return resRaw16;

                    default:
                        throw new NotImplementedException(string.Format("{0}: Currently only the channels {1}, {2}, {3}, {4}, and {5} are supported.", Name, ChannelNames.Color, CustomChannelNames.Raw16, CustomChannelNames.Red16, CustomChannelNames.Green16, CustomChannelNames.Blue16));
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the camera's packet resend limit.
        /// </summary>
        /// <returns></returns>
        public unsafe uint GetPacketResendLimit()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Camera must be connected.");
            }

            const uint registerAddress = 0xB304; // adresse für packet resend limit
            uint registerValue;
            const uint GigECameraAccessKey = 42;

            //aktuel wert lesen:
            GigeApi.SVSGigeApiReturn error = GigeApi.Camera_getGigECameraRegister(hCamera.ToPointer(),
                               registerAddress,
                               &registerValue,
                               GigECameraAccessKey);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Warn("Failed to get current packet resend limit.");
                return 0;
            }

            log.DebugFormat("Packet resend limit = {0}", registerValue);
            return registerValue;
        }

        /// <summary>
        /// Sets the camera's packet resend limit.
        /// </summary>
        /// <param name="limit">New packet resend limit.</param>
        /// <remarks>The default value is 32.</remarks>
        public unsafe void SetPacketResendLimit(uint limit)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Camera must be connected.");
            }

            const uint registerAddress = 0xB304; // adresse für packet resend limit
            const uint GigECameraAccessKey = 42;

            // packet resend limit erhöhen
            //In Register Schreiben
            log.DebugFormat("Setting packet resend limit to {0}", limit);
            GigeApi.SVSGigeApiReturn error = GigeApi.Camera_setGigECameraRegister(hCamera.ToPointer(),
                               registerAddress,
                               limit,
                               GigECameraAccessKey);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Warn("Failed to set new packet resend limit.");
            }

            // konfiguration speichern.
            //Save to EEPROM 
            //gigeApi.Camera_writeEEPROM(Camera_handle hCamera);
        }

        /// <summary>
        /// Get the maximal usable packet size based on given network hardware.
        /// </summary>
        /// <returns></returns>
        public int GetMaximalPacketSize()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Camera must be connected.");
            }

            GigeApi.SVSGigeApiReturn error = gigeApi.Gige_Camera_evaluateMaximalPacketSize(hCamera, ref packetSize);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.ErrorFormat("{0}: Failed to evaluate maximal packet size: {1}", Name, error.ToString());
                return -1;
            }

            return packetSize;
        }

        /// <summary>
        /// Flushes the stream channel's image buffers.
        /// Afterwards, the camera is back in its original <see cref="AcquisitionMode"/>.
        /// </summary>
        /// <see cref="FlushImageBuffers(GigeApi.ACQUISITION_MODE)"/>
        public void FlushImageBuffers()
        {
            FlushImageBuffers(AcquisitionMode);
        }

        /// <summary>
        /// Flushes the stream channel's image buffers.
        /// Afterwards, the camera is set to the given <see cref="GigeApi.ACQUISITION_MODE"/>.
        /// </summary>
        /// <param name="acquisitionMode">The <see cref="GigeApi.ACQUISITION_MODE"/> in which the camera will be after the operation.</param>
        /// <remarks>
        /// The camera is temporarily set to fixed frequency mode during this method and at least <see cref="NumStreamingChannelBuffers"/> images are fetched.
        /// </remarks>
        /// <seealso cref="FlushImageBuffers()"/>
        public void FlushImageBuffers(GigeApi.ACQUISITION_MODE acquisitionMode)
        {
            float oldFramerate = FrameRate;

            // Switch to fixed frequency mode
            AcquisitionMode = GigeApi.ACQUISITION_MODE.ACQUISITION_MODE_FIXED_FREQUENCY;
            FrameRate = 5;

            log.DebugFormat("Acquiring {0} images to flush image buffers...", NumStreamingChannelBuffers + 1);
            for (int i = 0; i < NumStreamingChannelBuffers + 1; i++)
            {
                log.DebugFormat("Acquiring image {0}", i);
                Update();
            }

            // Switch to the desired acquisition mode
            FrameRate = oldFramerate;
            AcquisitionMode = acquisitionMode;
        }

        /// <summary>
        /// Estimate white balance on a captured image and apply values.
        /// </summary>
        /// <seealso cref="EstimateWhiteBalance"/>
        public void EstimateAndSetWhiteBalance()
        {
            Point3f wb;
            if (!EstimateWhiteBalance(out wb))
            {
                return;
            }
            WhiteBalance = wb;
        }

        /// <summary>
        /// Call API function to estimate white balance on a captured image.
        /// </summary>
        /// <param name="whiteBalance">White balance values for the three color channels.</param>
        /// <returns>True on success, false otherwise.</returns>
        /// <seealso cref="EstimateAndSetWhiteBalance"/>
        public bool EstimateWhiteBalance(out Point3f whiteBalance)
        {
            GigeApi.SVSGigeApiReturn apiReturn = GigeApi.SVSGigeApiReturn.SVGigE_ERROR;
            float r = 0f, g = 0f, b = 0f;
            int bufferLength = 3 * width * height;

            lock (cameraLock)
                lock (dataLock)
                {
                    unsafe
                    {
                        fixed (byte* imgPtr = imageBuffer)
                        {
                            apiReturn = GigeApi.SVGigE_estimateWhiteBalance(imgPtr, bufferLength, &r, &g, &b);
                        }
                    }
                }

            if (apiReturn != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.ErrorFormat("{0}: Failed to set white balance: {1}", Name, apiReturn.ToString());
                whiteBalance = Point3f.NaP;
                return false;
            }

            whiteBalance = new Point3f(r, g, b);
            return true;
        }

        /// <summary>
        /// Saves the current white balance.
        /// </summary>
        /// <param name="filename">Filename for white balance values.</param>
        public void SaveWhiteBalance(string filename)
        {
            Point3f wb = WhiteBalance;

            using (BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                bw.Write(wb);
            }
        }

        /// <summary>
        /// Reads the white balance from a file.
        /// </summary>
        /// <remarks>White balance property will be updated after loading.</remarks>
        /// <param name="filename">File from which to read the white balance values</param>
        public void LoadWhiteBalance(string filename)
        {
            Point3f wb;

            using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                wb = Point3f.ReadFromMetriStream(br);
            }

            WhiteBalance = wb;
        }
        #endregion

        #region Private Methods
        private Dictionary<string, FloatCameraImage> DeBayerBilinear(bool computeRedPlane = true, bool computeGreenPlane = true, bool computeBluePlane = true)
        {
#if DEBUG
            // Check pixel format of currentBmp
            if (currentBmp.PixelFormat != PixelFormat.Format16bppGrayScale)
            {
                string errMsg = string.Format("Debayering does not support the pixel format of the acquired image. Is: {0}, must be: {1}", currentBmp.PixelFormat, PixelFormat.Format16bppGrayScale);
                log.Error(errMsg);
                throw new ArgumentException(errMsg);
            }
#endif

            width = currentBmp.Width;
            height = currentBmp.Height;
            int one = 0;
            int two = 0;
            int three = 0;
            int four = 0;
            float div2 = 0.5f;
            float div4 = 0.25f;
            FloatCameraImage red16, green16, blue16;

            Dictionary<string, FloatCameraImage> rgb = new Dictionary<string, FloatCameraImage>();

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = currentBmp.LockBits(rect, ImageLockMode.ReadOnly, currentBmp.PixelFormat);
            unsafe
            {
                byte* scan0 = (byte*)bmpData.Scan0;

                if (computeBluePlane)
                {
                    blue16 = new FloatCameraImage(width, height);

                    // TODO:
                    // process output row y = 0
                    // (same as regular rows, but don't write to y + 1)

                    for (int y = 1; y < height - 2; y += 2)
                    {
                        ushort* curRowPtr = (ushort*)(scan0 + y * bmpData.Stride);
                        ushort* otherRowPtr = (ushort*)(scan0 + (y + 2) * bmpData.Stride);
                        for (int x = 0; x < width; x += 2)
                        {
                            int x2 = x + 2;
                            one = curRowPtr[x];//x, y
                            two = curRowPtr[x2];//x + 2, y
                            three = otherRowPtr[x];//x, y + 2
                            four = otherRowPtr[x2];//x + 2, y + 2

                            blue16[y, x] = one;
                            blue16[y, x + 1] = (one + two) * div2;
                            blue16[y + 1, x] = (one + three) * div2;
                            blue16[y + 1, x + 1] = (one + two + three + four) * div4;
                        }
                    }

                    // TODO:
                    // process output rows y = height - 2 and y = height - 1
                    // 

                    rgb["blue"] = blue16;
                }

                if (computeRedPlane)
                {
                    red16 = new FloatCameraImage(width, height);

                    for (int y = 0; y < height - 2; y += 2)
                    {
                        ushort* curRowPtr = (ushort*)(scan0 + y * bmpData.Stride);
                        ushort* otherRowPtr = (ushort*)(scan0 + (y + 2) * bmpData.Stride);
                        for (int x = 1; x < width; x += 2)
                        {
                            int x2 = x + 2;
                            one = curRowPtr[x];//x, y
                            two = curRowPtr[x2];//x + 2, y
                            three = otherRowPtr[x];//x, y + 2
                            four = otherRowPtr[x2];//x + 2, y + 2

                            red16[y, x] = one;
                            red16[y, x + 1] = (one + two) * div2;
                            red16[y + 1, x] = (one + three) * div2;
                            red16[y + 1, x + 1] = (one + two + three + four) * div4;
                        }
                    }

                    // FIXME: Randbehandlung fuer y=height-2

                    rgb["red"] = red16;
                }

                if (computeGreenPlane)
                {
                    green16 = new FloatCameraImage(width, height);

                    // TODO: Randbehandlung fuer y=0

                    for (int y = 2; y < height - 1; y += 2)
                    {
                        ushort* curRowPtr = (ushort*)(scan0 + y * bmpData.Stride);
                        ushort* prevRowPtr = (ushort*)(scan0 + (y - 1) * bmpData.Stride);
                        ushort* nextRowPtr = (ushort*)(scan0 + (y + 1) * bmpData.Stride);
                        for (int x = 0; x < width; x += 2)
                        {
                            int x1 = x + 1;
                            one = curRowPtr[x];//x, y
                            two = curRowPtr[x + 2];//x + 2, y
                            three = nextRowPtr[x1];//x + 1, y + 1
                            four = prevRowPtr[x1];//x + 1, y - 1

                            green16[y, x] = one;
                            green16[y, x + 1] = (one + two + three + four) * div4;
                        }
                    }

                    for (int y = 1; y < height - 1; y += 2)
                    {
                        ushort* curRowPtr = (ushort*)(scan0 + y * bmpData.Stride);
                        ushort* prevRowPtr = (ushort*)(scan0 + (y - 1) * bmpData.Stride);
                        ushort* nextRowPtr = (ushort*)(scan0 + (y + 1) * bmpData.Stride);
                        for (int x = 1; x < width; x += 2)
                        {
                            int x1 = x + 1;
                            one = curRowPtr[x];//x, y
                            two = curRowPtr[x + 2];//x + 2, y
                            three = nextRowPtr[x1];//x + 1, y + 1
                            four = prevRowPtr[x1];//x + 1, y - 1

                            green16[y, x] = one;
                            green16[y, x + 1] = (one + two + three + four) * div4;
                        }
                    }

                    // TODO: Randbehandlung fuer y=height-1 (even/odd unterscheidung)

                    rgb["green"] = green16;
                }
            }

            currentBmp.UnlockBits(bmpData);

            return rgb;
        }

        private GigeApi.SVSGigeApiReturn CameraLogMessageCallback(string LogMessage, IntPtr MessageContext)
        {
            GigeApi.SVSGigeApiReturn apiReturn = GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS;
            log.InfoFormat("{0} [SVS SDK]: {1}", Name, LogMessage);
            return apiReturn;
        }

        
        /// <summary>
        /// Event callback function to be registered for demonstrating a messaging channel's capabilities
        /// </summary>
        /// <remarks>Copied from SVS Minimal Sample.</remarks>
        /// <param name="EventID"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Error)]
        public GigeApi.SVSGigeApiReturn EventCallback(IntPtr EventID, IntPtr Context)
        {

            int MessageID = 0;
            GigeApi.SVGigE_SIGNAL_TYPE MessageType = GigeApi.SVGigE_SIGNAL_TYPE.SVGigE_SIGNAL_NONE;
            gigeApi.Gige_Stream_getMessage(streamingChannel, EventID, ref MessageID, ref MessageType);
            log.Debug("Event:   " + MessageType.ToString());
            return GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS;
        }

        /// <summary>
        /// Streaming channel callback function.
        /// </summary>
        /// <param name="image">Image handle</param>
        /// <param name="context"></param>
        /// <returns>Gige error code (may be success)</returns>
        /// <remarks>
        /// Fetches the new data from the camera into an internal buffer.
        /// If that would be done in <see cref="UpdateImpl"/> the image data may already be unavailable.
        /// </remarks>
        [return: MarshalAs(UnmanagedType.Error)]
        private GigeApi.SVSGigeApiReturn StreamCallback(int image, IntPtr context)
        {
            log.DebugFormat("{0}: StreamCallback({1}, ...)", Name, image);
            GigeApi.SVSGigeApiReturn apiReturn = GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS;

            lock (dataLock)
            {
                IntPtr imgPtr = gigeApi.Gige_Image_getDataPointer(image);
                if (imgPtr == IntPtr.Zero)
                {
                    // lost image
                    GigeApi.SVGigE_SIGNAL_TYPE sig = gigeApi.Gige_Image_getSignalType(image);
                    log.ErrorFormat("{0}: StreamCallback: imgPtr is NULL. SignalType = {1}.", Name, sig.ToString());

                    // Check if connection has been lost
                    if (sig == GigeApi.SVGigE_SIGNAL_TYPE.SVGigE_SIGNAL_CAMERA_CONNECTION_LOST)
                    {
                        log.Debug(Name + ": Disconnecting camera.");
                        Disconnect(false);
                        return GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS;
                    }

                    return GigeApi.SVSGigeApiReturn.SVGigE_ERROR;
                }

                GigeApi.GVSP_PIXEL_TYPE myPixelType = gigeApi.Gige_Image_getPixelType(image);
                latestCameraFrameNumber = gigeApi.Gige_Image_getImageID(image);

                int size = width * height;

                // gray scale
                if (useGrayScale || isMonochrome)
                {
                    switch (bitCount)
                    {
                        case 8:
                            Marshal.Copy(imgPtr, imageBuffer, 0, size);
                            break;
                        case 12:
                            unsafe
                            {
                                // convert 12 bits per pixel to 16
                                fixed (byte* dest = imageBuffer)
                                {
                                    apiReturn = gigeApi.Gige_Image_getImage12bitAs16bit(image, dest, size * 2);
                                    if (apiReturn != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                                    {
                                        log.Error(Name + ": Gige_Image_getImage12bitAs16bit() failed: " + apiReturn.ToString());
                                    }
                                }
                            }
                            break;
                        case 16:
                            Marshal.Copy(imgPtr, imageBuffer, 0, size * 2);
                            break;
                        default:
                            log.ErrorFormat(Name + ": BitCount (={0}) seems to be wrong", bitCount);
                            return GigeApi.SVSGigeApiReturn.SVGigE_ERROR;
                    }
                }
                else
                {
                    // color
                    unsafe
                    {
                        // convert to rgb image
                        fixed (byte* dest = imageBuffer)
                        {
                            apiReturn = gigeApi.Gige_Image_getImageRGB(image, dest, size * 3, bayerMethod);
                            if (apiReturn != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
                            {
                                log.Error(Name + ": Gige_Image_getImageRGB() failed: " + apiReturn.ToString());
                                return apiReturn;
                            }
                        }
                    }
                }
            }

            //log.DebugFormat("{0}: Copied index of new frame. Signaling dataAvailable event.", Name);
            dataAvailable.Set();

            return apiReturn;
        }

        /// <summary>
        /// Allocates memory for internal image buffer which is used by <see cref="StreamCallback"/> and <see cref="UpdateImpl"/>.
        /// </summary>
        private void SetupBuffer()
        {
            log.DebugFormat("{0}: Setting up image buffers", Name);
            lock (dataLock)
            {
                // size in bytes
                long size = width * height;

                // gray scale
                if (useGrayScale || isMonochrome)
                {
                    // mono: buffer size is size, or 2 * size, depending whether we have 8 or 12/16 bits per pixel
                    switch (bitCount)
                    {
                        case 8:
                            break;
                        case 12:
                        case 16:
                            size *= 2;
                            break;
                    }
                }
                else
                {
                    // color: rgb -> 3 * 8 bits
                    size *= 3;
                }

                imageBuffer = new byte[size];
            }
        }

        private GigeApi.SVSGigeApiReturn CreateStreamingChannel()
        {
            GigeApi.SVSGigeApiReturn error;
            log.DebugFormat("{0}: Creating streaming channel", Name);

            streamCallBack = new GigeApi.StreamCallback(this.StreamCallback);
            error = gigeApi.Gige_Camera_evaluateMaximalPacketSize(hCamera, ref packetSize);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.ErrorFormat("{0}: Failed to evaluate maximal packet size: {1}", Name, error.ToString());
            }
            else
            {
                log.DebugFormat("{0}: PacketSize: {1}B", Name, packetSize);
            }
            error = gigeApi.Gige_Camera_setBinningMode(hCamera, GigeApi.BINNING_MODE.BINNING_MODE_OFF);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.ErrorFormat("{0}: Failed to set the binning mode: {1}", Name, error.ToString());
            }
            error = gigeApi.Gige_StreamingChannel_create(ref streamingChannel, container, hCamera, NumStreamingChannelBuffers, streamCallBack, IntPtr.Zero);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.ErrorFormat("{0}: Failed to create stream: {1}", Name, error.ToString());
            }

            RegisterEventCallback();

            return error;
        }

        private GigeApi.SVSGigeApiReturn CloseStreamingChannel()
        {
            log.DebugFormat("{0}: Closing streaming channel", Name);

            UnregisterEventCallback();

            GigeApi.SVSGigeApiReturn error = gigeApi.Gige_StreamingChannel_delete(streamingChannel);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.ErrorFormat("{0}: StreamingChannel_delete failed: {1}", Name, error.ToString());
            }
            streamingChannel = IntPtr.Zero;
            return error;
        }

        //---------------------------------------------------------------------
        // Create an event for demonstrating a messaging channel's capabilities
        //---------------------------------------------------------------------
        private void RegisterEventCallback()
        {
            log.EnterMethod();
            GigeApi.SVSGigeApiReturn error;
            const int SIZE_FIFO = 100;

            error = gigeApi.Gige_Stream_createEvent(streamingChannel, ref eventIdFrameCompleted, SIZE_FIFO);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Error("Error creating Event: " + error.ToString());
            }

            error = gigeApi.Gige_Stream_createEvent(streamingChannel, ref eventIdStartTransfer, SIZE_FIFO);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Error("Error creating Event: " + error.ToString());
            }

            IntPtr stream = streamingChannel;

            error = gigeApi.Gige_Stream_addMessageType(stream, eventIdFrameCompleted, GigeApi.SVGigE_SIGNAL_TYPE.SVGigE_SIGNAL_FRAME_COMPLETED);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Error("Error adding event message: " + error.ToString());
            }

            error = gigeApi.Gige_Stream_addMessageType(stream, eventIdStartTransfer, GigeApi.SVGigE_SIGNAL_TYPE.SVGigE_SIGNAL_START_OF_TRANSFER);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.Error("Error adding event message: " + error.ToString());
            }

            eventCallback = new GigeApi.EventCallback(EventCallback);

            error = gigeApi.Gige_Stream_registerEventCallback(stream, eventIdFrameCompleted, eventCallback, new IntPtr(0));
            error = gigeApi.Gige_Stream_registerEventCallback(stream, eventIdStartTransfer, eventCallback, new IntPtr(0));
        }

        private void UnregisterEventCallback()
        {
            log.EnterMethod();
            gigeApi.Gige_Stream_unregisterEventCallback(streamingChannel, eventIdFrameCompleted, eventCallback);
            gigeApi.Gige_Stream_unregisterEventCallback(streamingChannel, eventIdStartTransfer, eventCallback);
        }

        /// <summary>
        /// Starts grabbing. Make sure that TCP/IP connection has been established.
        /// </summary>
        private void StartGrabbing(GigeApi.ACQUISITION_MODE mode = GigeApi.ACQUISITION_MODE.ACQUISITION_MODE_FIXED_FREQUENCY)
        {
            log.DebugFormat("StartGrabbing({0})", mode.ToString());
            // set acquisition mode
            AcquisitionMode = mode;
            gigeApi.Gige_Camera_setAcquisitionControl(hCamera, GigeApi.ACQUISITION_CONTROL.ACQUISITION_CONTROL_START);
        }

        /// <summary>
        /// Stops grabbing on current camera handle.
        /// </summary>
        /// <returns>Gige API return value</returns>
        private GigeApi.SVSGigeApiReturn StopGrabbing()
        {
            log.Debug("StopGrabbing");
            GigeApi.SVSGigeApiReturn error = gigeApi.Gige_Camera_setAcquisitionControl(hCamera, GigeApi.ACQUISITION_CONTROL.ACQUISITION_CONTROL_STOP);
            if (error != GigeApi.SVSGigeApiReturn.SVGigE_SUCCESS)
            {
                log.ErrorFormat("{0}: StopGrabbing failed: {1}", Name, error.ToString());
            }
            return error;
        }

        private GigeApi.SVSGigeApiReturn GetPixelType()
        {
            GigeApi.SVSGigeApiReturn error = GigeApi.SVSGigeApiReturn.SVGigE_ERROR;
            pixelType = GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_UNKNOWN;
            error = gigeApi.Gige_Camera_getPixelType(hCamera, ref pixelType);
            switch (pixelType)
            {
                // Color buffer formats
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYGR8:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYGB8:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYBG8:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYRG8:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYGB10:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYBG10:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYRG10:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYGR10:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYBG12:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYGB12:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYGR12:
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_BAYRG12:
                    isMonochrome = false;
                    break;
                // Mono buffer formats
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_MONO8:    // 8 bit
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_MONO12:     // 16 bit
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_MONO12_PACKED:   // 12 bit
                case GigeApi.GVSP_PIXEL_TYPE.GVSP_PIX_MONO16:        // 16 bit
                    isMonochrome = true;
                    break;
                default:
                    error = GigeApi.SVSGigeApiReturn.SVGigE_PIXEL_TYPE_NOT_SUPPORTED;
                    break;
            }
            return error;
        }
        #endregion
    }
}
