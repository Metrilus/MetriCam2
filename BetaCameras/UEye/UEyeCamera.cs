// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Cameras.Internal.uEye;
using Metrilus.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MetriCam2.Enums;
using MetriCam2.Attributes;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// Wrapper for the IDS uEye camera series.
    /// </summary>
    public unsafe class UEyeCamera : Camera
    {
        #region Types
        private struct UEYEIMAGE
        {
            public IntPtr pMemory;
            public int MemID;
            public int SeqNum;
        }
        /// <summary>Trigger mode for image aquisition.</summary>
        public enum TriggerModeInternal
        {
            /// <summary>The camera continously acquires frames. Non-processed frames are dropped.</summary>
            FREERUN,
            /// <summary>The frame acquisition is triggered by trying to fetch a frame.</summary>
            SOFT,
            /// <summary>The frame acquisition is triggerd by a signal of a camera-type specific hardware pin of the camera.</summary>
            HARD
        };
        #endregion

        #region Private Fields
        private uEyeDriverWrapper m_uEye;
        private const int IMAGE_COUNT = 1; // size of images buffer [in nr of images]
        private UEYEIMAGE[] uEyeImages;
        private byte* imagePtr;
        private int imageWidth;
        private int imageHeight;
        private TriggerModeInternal triggerMode;
        private int stride;
        private int bitsPerPixel;
        private bool autoExposure;
        private bool autoGain;
        private bool autoWhiteBalance;
        #endregion

        #region Public Properties

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.IDSIcon; }
#endif

        /// <summary>
        /// The currently selected trigger mode.
        /// </summary>
        [Description("Trigger Mode")]
        [AllowedValueList(typeof(TriggerModeInternal))]
        [AccessState(
            readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected,
            writeableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public TriggerModeInternal TriggerMode
        {
            get { return triggerMode; }
            set { triggerMode = value; }
        }

        /// <summary>
        /// Number of columns in the output image.
        /// </summary>
        [Description("Width", "Image width")]
        [Unit(Unit.Pixel)]
        [AccessState(readableWhen: ConnectionStates.Connected)]
        public uint Width { get { return (uint)imageWidth; } }

        /// <summary>
        /// Number of lines in the output image.
        /// </summary>
        [Description("Height", "Image Height")]
        [Unit(Unit.Pixel)]
        [AccessState(readableWhen: ConnectionStates.Connected)]
        public uint Height { get { return (uint)imageHeight; } }

        /// <summary>
        /// Flag indicating monochrome cameras.
        /// </summary>
        [Description("Monochrome", "Flag indicating monochrome cameras")]
        [AccessState(readableWhen: ConnectionStates.Connected | ConnectionStates.Disconnected)]
        public bool IsMonochrome { get; private set; }

        /// <summary>
        /// Enable/disable auto exposure.
        /// </summary>
        [Description("Auto Exposure", "Automatic exposure time")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool AutoExposure
        {
            get { return autoExposure; }
            set
            {
                if (value == autoExposure)
                    return;

                // ignore invalid camera handle (i.e. not connected)
                double dValue = value ? 1 : 0, dummy = 0;
                m_uEye.SetAutoParameter(uEyeDriverWrapper.IS_SET_ENABLE_AUTO_SHUTTER, ref dValue, ref dummy);
                // Alternativ: IS_SET_ENABLE_AUTO_SENSOR_SHUTTER

                autoExposure = value;
            }
        }

        /// <summary>
        /// Enable/disable auto gain.
        /// </summary>
        [Description("Auto Gain", "Automatic gain control")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool AutoGain
        {
            get { return autoGain; }
            set
            {
                if (value == autoGain)
                    return;

                // ignore invalid camera handle (i.e. not connected)
                double dValue = value ? 1 : 0, dummy = 0;
                m_uEye.SetAutoParameter(uEyeDriverWrapper.IS_SET_ENABLE_AUTO_GAIN, ref dValue, ref dummy);
                // Alternativ: IS_SET_ENABLE_AUTO_SENSOR_GAIN

                autoGain = value;
            }
        }

        /// <summary>
        /// Enable/disable auto white balance.
        /// </summary>
        [Description("Auto White Balance")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        public bool AutoWhiteBalance
        {
            get { return autoWhiteBalance; }
            set
            {
                if (value == autoWhiteBalance)
                    return;

                // ignore invalid camera handle (i.e. not connected)
                double dValue = value ? 1 : 0, dummy = 0;
                m_uEye.SetAutoParameter(uEyeDriverWrapper.IS_SET_ENABLE_AUTO_WHITEBALANCE, ref dValue, ref dummy);
                // Alternativ: IS_SET_ENABLE_AUTO_SENSOR_WHITEBALANCE

                autoWhiteBalance = value;
            }
        }

        /// <summary>
        /// Exposure time in [ms].
        /// </summary>
        /// <remarks>Has no effect if set while not connected.</remarks>
        [Description("Exposure", "Exposure time")]
        [Unit(Unit.Milliseconds)]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(nameof(ExposureRange))]
        public float Exposure
        {
            get
            {
                double ex;
                m_uEye.Exposure(uEyeDriverWrapper.IS_EXPOSURE_CMD_GET_EXPOSURE, new IntPtr(&ex), sizeof(double));
                return (float)ex;
            }
            set
            {
                if (!m_uEye.IsOpen())
                    return;

                double val_d = value;
                AutoExposure = false;
                double min = 0, max = 0, intervall = 0;
                m_uEye.GetExposureRange(ref min, ref max, ref intervall);
                if ((value < min || value > max) && value != 0)
                {
                    throw new ArgumentOutOfRangeException(String.Format("Exposure time must be between {0} and {1} ms.", min, max));
                }
                m_uEye.Exposure(uEyeDriverWrapper.IS_EXPOSURE_CMD_SET_EXPOSURE, new IntPtr(&val_d), sizeof(double));
            }
        }

        public Range<float> ExposureRange
        {
            get
            {
                double min = 0, max = 0, intervall = 0;
                m_uEye.GetExposureRange(ref min, ref max, ref intervall);
                return new Range<float>((float)min, (float)max);
            }
        }

        /// <summary>
        /// Gain in [%].
        /// </summary>
        /// <remarks>Only master gain can be changed, not the individual color channels.</remarks>
        /// <remarks>Has no effect if set while not connected.</remarks>
        [Description("Gain", "Gain factor")]
        [Unit("%")]
        [AccessState(readableWhen: ConnectionStates.Connected, writeableWhen: ConnectionStates.Connected)]
        [Range(0, 100)]
        public int Gain
        {
            get
            {
                return m_uEye.SetHardwareGain(uEyeDriverWrapper.IS_GET_MASTER_GAIN, uEyeDriverWrapper.IS_IGNORE_PARAMETER, uEyeDriverWrapper.IS_IGNORE_PARAMETER, uEyeDriverWrapper.IS_IGNORE_PARAMETER);
            }
            set
            {
                if (!m_uEye.IsOpen())
                    return;

                AutoGain = false;
                m_uEye.SetHardwareGain(value, uEyeDriverWrapper.IS_IGNORE_PARAMETER, uEyeDriverWrapper.IS_IGNORE_PARAMETER, uEyeDriverWrapper.IS_IGNORE_PARAMETER);
            }
        }

        /// <summary>
        /// Gamma.
        /// </summary>
        /// <remarks>Has no effect if set while not connected.</remarks>
        [Description("Gamma", "Enable gamma correction")]
        [AccessState(ConnectionStates.Connected, ConnectionStates.Connected)]
        public bool Gamma
        {
            get
            {
                return uEyeDriverWrapper.IS_SET_GAMMA_ON == m_uEye.SetGamma(uEyeDriverWrapper.IS_GET_GAMMA_MODE);
            }
            set
            {
                if (!m_uEye.IsOpen())
                    return;

                m_uEye.SetGamma(value ? uEyeDriverWrapper.IS_SET_GAMMA_ON : uEyeDriverWrapper.IS_SET_GAMMA_OFF);
            }
        }

        /// <summary>
        /// Frame rate in [fps].
        /// </summary>
        /// <remarks>After changing the frame rate you have to set the exposure time again.</remarks>
        /// <remarks>Has no effect if set while not connected.</remarks>
        [Description("Framerate")]
        [Unit(Unit.FPS)]
        [AccessState(ConnectionStates.Connected, ConnectionStates.Connected)]
        public float Framerate
        {
            get
            {
                double fps = 0;
                m_uEye.GetFramesPerSecond(ref fps);
                return (float)fps;
            }
            set
            {
                if (!m_uEye.IsOpen())
                    return;
                double newFPS = 0;
                m_uEye.SetFrameRate(value, ref newFPS);
            }
        }

        /// <summary>
        /// Pixel clock in [MHz].
        /// </summary>
        /// <remarks>
        /// Setting the pixel clock may alter the frame rate and exposure time. Make sure to set those parameters after setting the pixel clock.
        /// Has no effect if set while not connected.
        /// </remarks>
        [Description("Pixel Clock")]
        [Unit("MHz")]
        [AccessState(ConnectionStates.Connected, ConnectionStates.Connected)]
        public int PixelClock
        {
            set
            {
                if (!m_uEye.IsOpen())
                    return;

                int min = 0, max = 0;
                m_uEye.GetPixelClockRange(ref min, ref max);
                if ((value < min || value > max) && value != 0)
                {
                    throw new ArgumentOutOfRangeException(String.Format("Pixel clock must be between {0} and {1} MHz.", min, max));
                }
                m_uEye.SetPixelClock(value);
            }
        }
        #endregion

        #region Constructor
        public UEyeCamera() :
            base()
        {
            m_uEye = new uEyeDriverWrapper();

            IsMonochrome = false;
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

            Channels.Add(cr.RegisterChannel(ChannelNames.Red));
            Channels.Add(cr.RegisterChannel(ChannelNames.Green));
            Channels.Add(cr.RegisterChannel(ChannelNames.Blue));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
        }

        /// <summary>
        /// Device-specific implementation of Connect.
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
        protected override void ConnectImpl()
        {
            // 0 == choose any camera
            int deviceID = 0;

            // if opened before, close now
            if (m_uEye.IsOpen())
            {
                m_uEye.ExitCamera();
            }

            // serial number given to connect to?
            if (this.SerialNumber != null && !this.SerialNumber.Equals(""))
            {
                deviceID = GetDeviceIDbySerial();
                if (deviceID <= 0)
                {
                    ExceptionBuilder.Throw(typeof(MetriCam2.Exceptions.ConnectionFailedException), Name, "error_connectionFailed", String.Format("Could not connect to camera with the serial number {0}.", SerialNumber));
                }
                deviceID |= uEyeDriverWrapper.IS_USE_DEVICE_ID;
            }

            // Open the Camera
            int nRet = m_uEye.InitCamera(deviceID, 0);
            if (nRet == uEyeDriverWrapper.IS_STARTER_FW_UPLOAD_NEEDED)
            {
                // TODO: Driver allows upload of new firmware. Check IDS docs for sample code.
                ExceptionBuilder.Throw(typeof(MetriCam2.Exceptions.ConnectionFailedException), Name, "error_connectionFailed", "This camera requires a new firmware. Please install with the appropriate tool from the camera vendor.");

            }
                
            if (nRet != uEyeDriverWrapper.IS_SUCCESS)
            {
                ExceptionBuilder.Throw(typeof(MetriCam2.Exceptions.ConnectionFailedException), Name, "error_connectionFailed");
            }
            

            // Get Sensor Info from Camera
            uEyeDriverWrapper.SENSORINFO sensorInfo = new uEyeDriverWrapper.SENSORINFO();
            m_uEye.GetSensorInfo(ref sensorInfo);

            // Set the image size
            // @TODO: The SetImageSize of the uEye driver is deprecated: http://lagis-vi.univ-lille1.fr/~lo/ens/ivi/uEye_Programming_Manual/index.html?is_setimagesize.htm
            int x = 0;
            int y = 0;
            bool isAOISupported = true;
            unsafe
            {
                int nAOISupported = -1;
                IntPtr pnAOISupported = (IntPtr)((uint*)&nAOISupported);

                // check if an arbitrary AOI is supported
                if (m_uEye.ImageFormat(uEyeDriverWrapper.IMGFRMT_CMD_GET_ARBITRARY_AOI_SUPPORTED, pnAOISupported, 4) == uEyeDriverWrapper.IS_SUCCESS)
                {
                    isAOISupported = (nAOISupported != 0);
                }
            }
            // If an arbitrary AOI is supported -> take maximum sensor size
            if (isAOISupported)
            {
                x = sensorInfo.nMaxWidth;
                y = sensorInfo.nMaxHeight;
            }
            // Take the image size of the current image format
            else
            {
                x = m_uEye.SetImageSize(uEyeDriverWrapper.IS_GET_IMAGE_SIZE_X, 0);
                y = m_uEye.SetImageSize(uEyeDriverWrapper.IS_GET_IMAGE_SIZE_Y, 0);
            }
            m_uEye.SetImageSize(x, y);

            // Set the Color Mode
            IsMonochrome = (uEyeDriverWrapper.IS_COLORMODE_MONOCHROME == sensorInfo.nColorMode);
            // Monochrome camera
            if (IsMonochrome)
            {
                // monochrome allows for different bit depths.
                // 32-bit uses RGB32 color mode and returns red channel
                bitsPerPixel = 8;
                switch (bitsPerPixel)
                {
                    case 8:
                        m_uEye.SetColorMode(uEyeDriverWrapper.IS_CM_MONO8);
                        break;
                    case 16:
                        m_uEye.SetColorMode(uEyeDriverWrapper.IS_CM_MONO16);
                        break;
                    case 32:
                        m_uEye.SetColorMode(uEyeDriverWrapper.IS_SET_CM_RGB32);
                        break;
                }
            }
            else
            {
                m_uEye.SetColorMode(uEyeDriverWrapper.IS_SET_CM_RGB32);
                bitsPerPixel = 32;
            }
            // Automatic Corrections
            double dEnable = 1, dDisable = 0, dummy = 0, dValue = 0;
            // Set auto exposure:
            dValue = autoExposure ? dEnable : dDisable;
            m_uEye.SetAutoParameter(uEyeDriverWrapper.IS_SET_ENABLE_AUTO_SHUTTER, ref dValue, ref dummy);
            // Set auto gain:
            dValue = autoGain ? dEnable : dDisable;
            m_uEye.SetAutoParameter(uEyeDriverWrapper.IS_SET_ENABLE_AUTO_GAIN, ref dValue, ref dummy);
            // Set auto white balance:
            dValue = autoWhiteBalance ? dEnable : dDisable;
            m_uEye.SetAutoParameter(uEyeDriverWrapper.IS_SET_ENABLE_AUTO_WHITEBALANCE, ref dValue, ref dummy);

            this.imageWidth = x;
            this.imageHeight = y;
            //this.stride = m_uEye.GetImageMemPitch(ref stride);
            this.stride = imageWidth * bitsPerPixel / 8;
            if (stride % 4 != 0)
                stride += 4 - stride % 4;
            this.uEyeImages = new UEYEIMAGE[IMAGE_COUNT];
            // alloc images
            m_uEye.ClearSequence();
            for (int i = 0; i < IMAGE_COUNT; i++)
            {
                m_uEye.AllocImageMem(x, y, bitsPerPixel, ref uEyeImages[i].pMemory, ref uEyeImages[i].MemID);
                m_uEye.AddToSequence(uEyeImages[i].pMemory, uEyeImages[i].MemID);
                uEyeImages[i].SeqNum = i + 1;
            }

            m_uEye.EnableMessage(uEyeDriverWrapper.IS_FRAME, 0);

            switch (TriggerMode)
            {
                case TriggerModeInternal.FREERUN:
                    m_uEye.FreezeVideo(uEyeDriverWrapper.IS_WAIT);
                    break;
                case TriggerModeInternal.HARD:
                    m_uEye.CaptureVideo(uEyeDriverWrapper.IS_DONT_WAIT); // the appropriate treatment of hard triggers is unclear.
                    break;
                case TriggerModeInternal.SOFT:
                    m_uEye.CaptureVideo(uEyeDriverWrapper.IS_DONT_WAIT);
                    break;
                default:
                    break;
            }

            InitChannels();
            SerialNumber = GetSerialNumber();
        }

        /// <summary>
        /// Device-specific implementation of Disconnect.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
        protected override void DisconnectImpl()
        {
            unsafe
            {
                if (imagePtr != null)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)imagePtr);
                    imagePtr = null;
                }
            }
            // release marshal object pointers
            if (null != uEyeImages)
            {
                for (int i = 0; i < IMAGE_COUNT; i++)
                {
                    if (IntPtr.Zero != uEyeImages[i].pMemory)
                    {
                        Marshal.FreeCoTaskMem(uEyeImages[i].pMemory);
                        uEyeImages[i].pMemory = IntPtr.Zero;
                    }
                }
            }

            if (null != m_uEye)
            {
                m_uEye.ExitCamera();
            }
        }

        /// <summary>
        /// Device-specific implementation of Update.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
        protected override void UpdateImpl()
        {
            if (triggerMode == TriggerModeInternal.SOFT)
                m_uEye.FreezeVideo(uEyeDriverWrapper.IS_WAIT);
            else
                m_uEye.CaptureVideo(uEyeDriverWrapper.IS_DONT_WAIT);
            IntPtr ptr = new IntPtr();
            int pnId = 0;
            m_uEye.GetActiveImageMem(ref ptr, ref pnId);
            m_uEye.LockSeqBuf(pnId, ptr);
            unsafe
            {
                if (imagePtr == null)
                    imagePtr = (byte*)System.Runtime.InteropServices.Marshal.AllocHGlobal((int)(stride * Height));
                CopyMemory(imagePtr, (byte*)ptr, (uint)stride * Height);
            }
            m_uEye.UnlockSeqBuf(pnId, ptr);
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        protected override CameraImage CalcChannelImpl(string channelName)
        {   
            switch (channelName)
            {
                case ChannelNames.Blue:
                    return CalcBlue();
                case ChannelNames.Green:
                    return CalcGreen();
                case ChannelNames.Red:
                    return CalcRed();
                case ChannelNames.Color:
                    return CalcColor();
                case ChannelNames.Intensity:
                    return CalcIntensity();
                default:
                    ExceptionBuilder.Throw(typeof(ArgumentException), this, "error_invalidChannelName", channelName);
                    return null;
            }
        }
        #endregion

        #region Private Methods
        private void SetTriggerModeOnCamera()
        {
            switch (triggerMode)
            {
            case TriggerModeInternal.SOFT:
                m_uEye.SetExternalTrigger(uEyeDriverWrapper.IS_SET_TRIGGER_OFF);
                m_uEye.FreezeVideo(uEyeDriverWrapper.IS_WAIT);
                break;
            case TriggerModeInternal.FREERUN:
                m_uEye.SetExternalTrigger(uEyeDriverWrapper.IS_SET_TRIGGER_OFF);
                m_uEye.CaptureVideo(uEyeDriverWrapper.IS_DONT_WAIT);
                break;
            case TriggerModeInternal.HARD:
                // the appropriate treatment of hard triggers is unclear.
                int ret = m_uEye.SetExternalTrigger(uEyeDriverWrapper.IS_SET_TRIGGER_HI_LO);
                if (uEyeDriverWrapper.IS_SUCCESS != ret)
                {
                    Disconnect();
                    //throw new MetriCamConnectException(String.Format("Could not set hardware triggering. Error code {0}.", ret));
                }
                m_uEye.SetTriggerDelay(0);
                ret = m_uEye.CaptureVideo(uEyeDriverWrapper.IS_DONT_WAIT);
                if (uEyeDriverWrapper.IS_SUCCESS != ret && uEyeDriverWrapper.IS_CAPTURE_RUNNING != ret)
                {
                    // try to reconnect
                    Disconnect();
                    Connect();
                }
                break;
            default:
                break;
            }
        }

        /// <summary>
        /// Searches through all availables cameras and returns the cameraID of the camera specified by SerialNumber.
        /// Make sure SerialNumber is set.
        /// </summary>
        /// <returns></returns>
        private int GetDeviceIDbySerial()
        {
            int id = -1;
            if (this.SerialNumber == null || this.SerialNumber.Equals(""))
                return id;
            uEyeDriverWrapper.UEYE_CAMERA_LIST pucl = new uEyeDriverWrapper.UEYE_CAMERA_LIST();
            int num = m_uEye.GetNumberOfDevices();
            if (num <= 0)
                return id;

            pucl.dwCount = num;
            pucl.uci = new uEyeDriverWrapper.UEYE_CAMERA_INFO[num];
            m_uEye.GetCameraList(ref pucl);
            for (int i = 0; i < pucl.dwCount; ++i)
            {
                string serial = pucl.uci[i].SerNo;
                // correct serial number
                int idx = serial.IndexOf('\0');
                if (idx > 0)
                    serial = serial.Remove(idx);
                if (serial.Equals(this.SerialNumber))
                {
                    // found
                    id = (int)pucl.uci[i].dwDeviceID;
                    break;
                }
            }

            return id;
        }

        /// <summary>
        /// Updates channels. Depends on IsMonochrome.
        /// </summary>
        private void InitChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            if (IsMonochrome)
            {
                Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
                ActivateChannel(ChannelNames.Intensity);
                SelectChannel(ChannelNames.Intensity);
            }
            else
            {
                Channels.Add(cr.RegisterChannel(ChannelNames.Red));
                Channels.Add(cr.RegisterChannel(ChannelNames.Blue));
                Channels.Add(cr.RegisterChannel(ChannelNames.Green));
                Channels.Add(cr.RegisterChannel(ChannelNames.Color));
                ActivateChannel(ChannelNames.Red);
                SelectChannel(ChannelNames.Red);
            }
        }

        private FloatCameraImage CalcBlue()
        {
            return CalcChannelFloat(2);
        }

        private FloatCameraImage CalcGreen()
        {
            return CalcChannelFloat(1);
        }

        private FloatCameraImage CalcRed()
        {
            return CalcChannelFloat(0);
        }

        private FloatCameraImage CalcIntensity()
        {
            // get data
            switch (bitsPerPixel)
            {
                case 16:
                    return CalcChannelShort();
                case 32:
                    return CalcChannelFloat(0); // red channel
                case 8:
                default:
                    return CalcChannelByte();
            }
        }

        private FloatCameraImage CalcChannelByte()
        {
            FloatCameraImage result = new FloatCameraImage((int)Width, (int)Height);
            result.FrameNumber = FrameNumber;

            unsafe
            {
                byte* dataPtr = (byte*)imagePtr;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        result[y, x] = (float)(*dataPtr);
                        dataPtr++;
                    }
                }
            }

            return result;
        }

        private FloatCameraImage CalcChannelShort()
        {
            FloatCameraImage result = new FloatCameraImage((int)Width, (int)Height);
            result.FrameNumber = FrameNumber;

            unsafe
            {
                short* dataPtr = (short*)imagePtr;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        result[y, x] = (float)(*dataPtr);
                        dataPtr++;
                    }
                }
            }

            return result;
        }

        private FloatCameraImage CalcChannelFloat(int channelNr)
        {
            FloatCameraImage result = new FloatCameraImage((int)Width, (int)Height);
            result.FrameNumber = FrameNumber;

            float scale = 1.0f / 255.0f;

            unsafe
            {
                byte* dataPtr = (byte*)imagePtr;
                dataPtr += channelNr;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        result[y, x] = (float)(*dataPtr) * scale;
                        dataPtr += 4;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts a bitmap representation from the current raw-data.
        /// </summary>
        /// <returns>Bitmap representation of the current frame.</returns>
        private ColorCameraImage CalcColor()
        {
            ColorCameraImage result = new ColorCameraImage((int)Width, (int)Height);
            result.FrameNumber = FrameNumber;

            if (IsMonochrome)
            {
                FloatCameraImage img = CalcChannelFloat(0);
                float factor = (32 == bitsPerPixel) ? 255.0f : 1.0f;

                BitmapData bData = result.Data.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                for (int y = 0; y < result.Data.Height; y++)
                {
                    byte* linePtr = (byte*)bData.Scan0 + y * bData.Stride;
                    for (int x = 0; x < result.Data.Width; x++)
                    {
                        byte d = (byte)(img[y, x] * factor);
                        *linePtr++ = d;
                        *linePtr++ = d;
                        *linePtr++ = d;
                    }
                }
                result.Data.UnlockBits(bData);
            }
            else
            {
                BitmapData bData = result.Data.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                if (bData.Stride != stride)
                {
                    result.Data.UnlockBits(bData);
                    throw new InvalidOperationException("Stride is incompatible.");
                }
                CopyMemory((byte*)bData.Scan0, imagePtr, (uint)(stride * imageHeight));
                result.Data.UnlockBits(bData);
            }

            return result;
        }

        /// <summary>
        /// Gets the serial number of the connected device.
        /// </summary>
        /// <returns>serial number of connected device</returns>
        private string GetSerialNumber()
        {
            uEyeDriverWrapper.CAMINFO cameraInfo = new uEyeDriverWrapper.CAMINFO();
            m_uEye.GetCameraInfo(ref cameraInfo);
            string serial = cameraInfo.SerNo;
            int idx = serial.IndexOf('\0');
            if (idx > 0)
                serial = serial.Remove(idx);
            return serial;
        }

        /// <summary>
        /// Copies unmanaged memory.
        /// </summary>
        /// <param name="destination">Destination pointer.</param>
        /// <param name="source">Source pointer.</param>
        /// <param name="lengthInBytes">Size of data.</param>
        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private unsafe static extern void CopyMemory(byte* destination, byte* source, [MarshalAs(UnmanagedType.U4)] uint lengthInBytes);
        #endregion
    }
}
