using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using MetriCam2;
using MetriCam2.Cameras;
using Metrilus.Presentation;
using Metrilus.Util;
using MetriPrimitives.Data;
using MetriX.Algorithms;
using MetriX.Debug;
using MetriX.Models;
using MetriX.Utils.Logging;
using ProjectiveTransformationZhang = MetriPrimitives.Transformations.ProjectiveTransformationZhang;

namespace MetriX.Cameras.Debug
{
    [Description("Orbbec Astra")]
    public class OrbbecAstra : CameraBase, IDisposable
    {
        private enum AcquisitionModes
        {
            Unknown,
            Idle,
            IntensityImage,
            ColorAndZImage,
        }

        public string Serial { get; internal set; }
        public byte IRGain { get; internal set; }
        public uint IRExposure { get; internal set; }
        public uint EmitterDelay { get; internal set; }

        // An old, obsolete XML ElementName, used for backward-compatibility
        internal const string OldElementName = "LocalOrbbec";
        internal const string ElementName = "OrbbecAstra";

        /// <summary>
        /// Number of intensity images to average in calibration mode.
        /// </summary>
        private const int NumberOfIntensityImages = 10;

        private object _lock = new object();
        private AstraOpenNI _camera;
        private bool _multiOrbbecMode;
        private AcquisitionModes _currentAcquisitionMode = AcquisitionModes.Unknown;
        private bool _useIntensity;

        private const byte _minimumIRGain = 8;
        private const byte _maximumIRGain = 96;
        private const byte _defaultIRGain = 90;
        private const uint _minimumIRExposure = 0;
        private const uint _maximumIRExposure = 4096;
        private const uint _defaultIRExposure = 2048;
        private const uint _minimumEmitterDelay = 0;
        private const uint _defaultEmitterDelay = 300;

        internal OrbbecAstra() : base(DefaultCalibrationParameters, DefaultPreFilteringParameters)
        {
            IRGain = _defaultIRGain;
            IRExposure = _defaultIRExposure;
            EmitterDelay = _defaultEmitterDelay;

            _camera = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            lock (_lock)
            {
                if ((null != _camera) && (_camera.IsConnected))
                {
                    _camera.Disconnect();
                }

                _camera = null;
            }
        }

        public override string ToString()
        {
            if (String.IsNullOrEmpty(Serial))
                return base.ToString();
            else
                return base.ToString() + " S/N: " + Serial;
        }

        public override Camera GetCamera() => _camera;

        #region Frame Acquisition

        private void SetAcquisitionMode(AcquisitionModes mode)
        {
            if (mode == _currentAcquisitionMode)
            {
                return;
            }

            switch (mode)
            {
                case AcquisitionModes.ColorAndZImage:
                    // Color+Depth is ok, just make sure that Intensity is off
                    if (_camera.IsChannelActive(ChannelNames.Intensity))
                    {
                        _camera.DeactivateChannel(ChannelNames.Intensity);
                    }

                    _camera.ActivateChannel(ChannelNames.Color);
                    _camera.ActivateChannel(ChannelNames.Point3DImage);
                    _camera.ActivateChannel(ChannelNames.ZImage);

                    _camera.IRFlooderEnabled = false;
                    _camera.EmitterEnabled = true;
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(EmitterDelay));
                    _camera.Update();
                    break;
                case AcquisitionModes.Idle:
                    // don't care about channels, just switch off Emitter and Flooder
                    _camera.IRFlooderEnabled = false;
                    _camera.EmitterEnabled = false;
                    break;
                case AcquisitionModes.IntensityImage:
                    // Intensity does not support any other active channels
                    ActivateChannels(new HashSet<string>() { ChannelNames.Intensity });
                    _camera.IRFlooderEnabled = true;
                    _camera.EmitterEnabled = false;
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(EmitterDelay));
                    break;
            }
            _currentAcquisitionMode = mode;
        }

        private void CalcDepthChannel(out DataFrame dataFrame)
        {
            Point3fCameraImage points = (Point3fCameraImage)_camera.CalcChannel(ChannelNames.Point3DImage);
            FloatImage distanceImage = ToDistance(points);

            dataFrame = new DataFrame(distanceImage, distanceImage, RigidBodyTransformation, ProjectiveTransformation, PreFilteringParameters);
        }

        /// <summary>
        /// Acquires a single frame of 3D data from the camera.
        /// </summary>
        /// <remarks>
        /// Use the dual-output variant of this method to acquire both 3D and 2D data from the same frame.
        /// Calling the single-output variants in sequence will result in 3D or 2D data from different frames.
        /// </remarks>
        public override void AcquireFrame(FramePurposes purposes, out DataFrame dataFrame)
        {
            if (_multiOrbbecMode)
            {
                SetAcquisitionMode(AcquisitionModes.ColorAndZImage);
            }

            _camera.Update();
            CalcDepthChannel(out dataFrame);
            if (_multiOrbbecMode)
            {
                SetAcquisitionMode(AcquisitionModes.Idle);
            }
        }

        private void CalcColorChannel(out DisplayFrame displayFrame)
        {
            BitmapSource bitmapSource;
            ColorCameraImage cameraImage = (ColorCameraImage)_camera.CalcChannel(ChannelNames.Color);
            bitmapSource = BitmapUtils.CopySource(cameraImage.Data);
            bitmapSource.Freeze();

            displayFrame = new DisplayFrame(bitmapSource, RigidBodyTransformationColour, ProjectiveTransformationColour);
        }

        private void CalcIrChannel(FloatImage intensity, out DisplayFrame displayFrame)
        {
            BitmapSource bitmapSource;
            bitmapSource = BitmapUtils.CopySource(intensity.ToBitmap());
            bitmapSource.Freeze();

            displayFrame = new DisplayFrame(bitmapSource, RigidBodyTransformationColour, ProjectiveTransformationColour);
        }

        /// <summary>
        /// Acquires a single frame of 2D data from the camera.
        /// </summary>
        /// <remarks>
        /// Use the dual-output variant of this method to acquire both 3D and 2D data from the same frame.
        /// Calling the single-output variants in sequence will result in 3D or 2D data from different frames.
        /// </remarks>
        public override void AcquireFrame(FramePurposes purposes, out DisplayFrame displayFrame)
        {
            if (!_useIntensity)
            {
                if (_multiOrbbecMode)
                {
                    SetAcquisitionMode(AcquisitionModes.ColorAndZImage);
                }
                _camera.Update();
                CalcColorChannel(out displayFrame);
            }
            else
            {
                var intensity = AcquireIntensityImage(1);
                CalcIrChannel(intensity, out displayFrame);
            }

            if (_multiOrbbecMode)
            {
                SetAcquisitionMode(AcquisitionModes.Idle);
            }
        }

        /// <summary>
        /// Acquires both the 3D and 2D data from a single frame from the camera.
        /// </summary>
        public override void AcquireFrame(FramePurposes purposes, out DataFrame dataFrame, out DisplayFrame displayFrame)
        {
            if (_multiOrbbecMode)
            {
                SetAcquisitionMode(AcquisitionModes.ColorAndZImage);
            }
            _camera.Update();
            CalcDepthChannel(out dataFrame);
            if (!_useIntensity)
            {
                CalcColorChannel(out displayFrame);
            }
            else
            {
                var avgIntensity = AcquireIntensityImage();
                dataFrame = new DataFrame(dataFrame.DistanceImage, avgIntensity, dataFrame.RigidBodyTransformation, dataFrame.ProjectiveTransformation, dataFrame.PreFilteringParameters);
                CalcIrChannel(avgIntensity, out displayFrame);
            }

            if (_multiOrbbecMode)
            {
                SetAcquisitionMode(AcquisitionModes.Idle);
            }
        }

        private FloatImage AcquireIntensityImage(int numImages = NumberOfIntensityImages)
        {
            var oldMode = _currentAcquisitionMode;
            SetAcquisitionMode(AcquisitionModes.IntensityImage);

            FloatImage avgIntensity = null;
            for (int i = 0; i < numImages; i++)
            {
                _camera.Update();
                var intensity = (FloatCameraImage)_camera.CalcChannel(ChannelNames.Intensity);
                if (null == avgIntensity)
                {
                    avgIntensity = new FloatImage(intensity.Size);
                }
                avgIntensity.ChannelName = intensity.ChannelName;
                avgIntensity.FrameNumber = intensity.FrameNumber;
                avgIntensity.TimeStamp = intensity.TimeStamp;
                avgIntensity += new FloatImage(ref intensity);
            }
            avgIntensity /= NumberOfIntensityImages;

            // In _multiOrbbecMode the camera will be set to idle mode outside this method.
            // Otherwise, the previous mode must be restored.
            if (!_multiOrbbecMode)
            {
                SetAcquisitionMode(oldMode);
            }
            return avgIntensity;
        }

        #endregion Frame Acquisition

        #region Engine Initialisation

        /// <summary>
        /// Called by the MetriX Engine after all Cameras have been added to the Engine but before the Engine is used for the first time.
        /// </summary>
        /// <remarks>
        /// Camera Implementations may override this method in order to modify the Engine according to their requirements.
        /// </remarks>
        /// <param name="engine">
        /// The MetriX Engine instance that is being initialised.
        /// </param>
        public override void Initialize(MockEngine engine)
        {
            base.Initialize(engine);

            // Use Intensity or Colour channels for display / calibration
            _useIntensity = !engine.CalibrationParameters.Checkerboard.UseColourFrame;

            // Constrain Parameters
            IRGain = (IRGain < _minimumIRGain) ? _minimumIRGain
                   : (IRGain > _maximumIRGain) ? _maximumIRGain
                   : IRGain;
            IRExposure = (IRExposure < _minimumIRExposure) ? _minimumIRExposure
                       : (IRExposure > _maximumIRExposure) ? _maximumIRExposure
                       : IRExposure;
            EmitterDelay = (EmitterDelay < _minimumEmitterDelay) ? _minimumEmitterDelay
                         : EmitterDelay;

            // Connect to Camera
            lock (_lock)
            {
                if (null == _camera)
                {
                    Dictionary<string, string> serialNumberMap = AstraOpenNI.GetSerialToUriMappingOfAttachedCameras();
                    if (0 == serialNumberMap.Count)
                    {
                        throw new MetriCam2.Exceptions.ConnectionFailedException("No connected Orbbec cameras found. This may be a hardware or driver failure.");
                    }
                    else if (("" == Serial || null == Serial) && 1 == serialNumberMap.Count)
                    {
                        Serial = serialNumberMap.Keys.First();
                    }
                    else if (!serialNumberMap.ContainsKey(Serial))
                    {
                        Logs.Write(Severity.Error, "Failed to find Orbbec Astra camera with serial number: " + Serial + Environment.NewLine
                                                + "(available cameras: " + String.Join(", ", serialNumberMap.Keys) + ")");

                        throw new MetriCam2.Exceptions.ConnectionFailedException("No connected Orbbec camera with serial number: " + Serial + ".");
                    }
                    _camera = new AstraOpenNI()
                    {
                        SerialNumber = Serial
                    };
                }

                if (!_camera.IsConnected)
                {
                    _camera.Connect();
                    _camera.IRGain = IRGain;
                    _camera.IRExposure = (int)IRExposure;
                }
            }

            SetAcquisitionMode(AcquisitionModes.ColorAndZImage);

            // Fetch Camera Intrinsics and Extrinsics
            IProjectiveTransformation ipt = _camera.GetIntrinsics(ChannelNames.Intensity);
            Metrilus.Util.ProjectiveTransformationZhang pt = (Metrilus.Util.ProjectiveTransformationZhang)ipt;
            Calibration.ProjectiveTransformation = new ProjectiveTransformationZhang(pt);
            Calibration.ProjectiveTransformationColour = new ProjectiveTransformationZhang((Metrilus.Util.ProjectiveTransformationZhang)_camera.GetIntrinsics(ChannelNames.Color));
            Calibration.DepthToColourTransformation = _camera.GetExtrinsics(ChannelNames.Intensity, ChannelNames.Color);

            // Count the Orbbec cameras in the Camera collection
            int orbbecCount = engine.Cameras.Count((CameraBase camera) => (camera is OrbbecAstra));
            if (orbbecCount > 1)
            {
                engine.FrameAcquisitionOptions.MaxDegreeOfParallelism = 1;
                _multiOrbbecMode = true;
                SetAcquisitionMode(AcquisitionModes.Idle);
            }
        }

        #endregion Engine Initialisation

        #region Camera-Specific Parameters

        public static CameraCalibrationParameters DefaultCalibrationParameters
        {
            get => new CameraCalibrationParameters(
                new CameraCalibrationParameters.SegmentationParameters()
                {
                    MinimumSegmentSize = 2000,
                    OpeningHalfSize = 1,
                }
            );
        }

        public static FramePreFilteringParameters DefaultPreFilteringParameters
        {
            get => new FramePreFilteringParameters(
                new PixelThresholdFilter.PixelThresholdFilterParameters()
                {
                    MinimumAmplitude = 0.01f,
                    MinimumDistance = 0.1f,
                },
                new GuidedImageFilter.GuidedImageFilterParameters()
                {
                    Enabled = false,
                },
                new BrinkPixelEliminator.BrinkPixelEliminatorParameters()
                {
                    Enabled = true,
                },
                new MaskBorderPixelsFilter.MaskBorderPixelsFilterParameters()
                {
                    Enabled = true,
                    CropLeftBorder = 10,
                }
            );
        }

        #endregion Camera-Specific Parameters

        #region XML Serialization

        private const string XmlSerialAttributeName = "serial";
        private const string XmlIRGainAttributeName = "irGain";
        private const string XmlIRExposureAttributeName = "irExposure";
        private const string XmlEmitterDelayAttributeName = "emitterDelay";

        protected override void WriteXmlStartElement(XmlWriter xmlWriter)
        {
            string prefix = xmlWriter.LookupPrefix(Constants.XmlNamespaces.Configuration);
            xmlWriter.WriteStartElement(prefix, OrbbecAstra.ElementName, Constants.XmlNamespaces.Configuration);
            xmlWriter.WriteAttributeString(prefix, XmlSerialAttributeName, Constants.XmlNamespaces.Configuration, Serial);
            xmlWriter.WriteAttributeString(prefix, XmlIRGainAttributeName, Constants.XmlNamespaces.Configuration, IRGain.ToString());
            xmlWriter.WriteAttributeString(prefix, XmlIRExposureAttributeName, Constants.XmlNamespaces.Configuration, IRExposure.ToString());
            xmlWriter.WriteAttributeString(prefix, XmlEmitterDelayAttributeName, Constants.XmlNamespaces.Configuration, EmitterDelay.ToString());
        }

        public static OrbbecAstra ReadXml(XmlReader xmlReader, Func<string, Stream> streamProvider)
        {
            return CameraBase.ReadXml<OrbbecAstra>(xmlReader, streamProvider, () => new OrbbecAstra(), (camera, attribute, value) =>
            {
                switch (attribute)
                {
                    case XmlSerialAttributeName:
                        camera.Serial = value;
                        break;

                    case XmlIRGainAttributeName:
                        if ((!String.IsNullOrWhiteSpace(value)) && (Utils.TypeUtils.TryParse<Byte>(value, out Byte irGain)) && (0 != irGain))
                        {
                            camera.IRGain = irGain;
                        }
                        break;

                    case XmlIRExposureAttributeName:
                        if ((!String.IsNullOrWhiteSpace(value)) && (Utils.TypeUtils.TryParse<UInt32>(value, out UInt32 irExposure)) && (0 != irExposure))
                        {
                            camera.IRExposure = irExposure;
                        }
                        break;

                    case XmlEmitterDelayAttributeName:
                        if ((!String.IsNullOrWhiteSpace(value)) && (Utils.TypeUtils.TryParse<UInt32>(value, out UInt32 emitterDelay)) && (0 != emitterDelay))
                        {
                            camera.EmitterDelay = emitterDelay;
                        }
                        break;
                }
            });
        }

        #endregion XML Serialization

        private FloatImage ToDistance(Point3fCameraImage p3f)
        {
            FloatImage result = new FloatImage(p3f.Size)
            {
                TimeStamp = p3f.TimeStamp,
                FrameNumber = p3f.FrameNumber,
                ChannelName = ChannelNames.Distance,
                Intrinsics = p3f.Intrinsics
            };
            for (int y = 0; y < p3f.Height; ++y)
            {
                Parallel.For(0, p3f.Width, x =>
                {
                    result[y, x] = p3f[y, x].GetLength();
                });
            }
            return result;
        }

        /// <summary>
        /// Activates only the given <paramref name="channels"/> and deactivates all other.
        /// </summary>
        /// <param name="channels"></param>
        private void ActivateChannels(HashSet<string> channels)
        {
            HashSet<string> copy = new HashSet<string>(channels);
            for (int j = _camera.ActiveChannels.Count - 1; j >= 0; --j)
            {
                string channelName = _camera.ActiveChannels[j].Name;
                if (!copy.Remove(channelName))
                {
                    _camera.DeactivateChannel(channelName);
                }
            }

            foreach (string channelName in copy)
            {
                _camera.ActivateChannel(channelName);
            }
        }
    }
}
