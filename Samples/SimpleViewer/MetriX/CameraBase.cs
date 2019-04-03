using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using MetriX.Debug;
using MetriX.Models;
using MetriX.Utils.Data;
using MaskImage = MetriPrimitives.Data.MaskImage;
using Point3fCameraImage = Metrilus.Util.Point3fCameraImage;
using Point3fImage = MetriPrimitives.Data.Point3fImage;
using ProjectiveTransformationZhang = MetriPrimitives.Transformations.ProjectiveTransformationZhang;
using RigidBodyTransformation = Metrilus.Util.RigidBodyTransformation;

namespace MetriX.Cameras.Debug
{
    public abstract class CameraBase : IDisposable
    {
        private string _description;

        public string Name { get; set; }

        private CameraCalibration _calibration;
        private CameraCalibrationParameters _calibrationParameters;
        private FramePreFilteringParameters _preFilteringParameters;

        protected CameraBase(CameraCalibrationParameters defaultCalibrationParameters, FramePreFilteringParameters defaultPreFilteringParameters)
        {
            _calibrationParameters = defaultCalibrationParameters;
            _preFilteringParameters = defaultPreFilteringParameters;
        }

        ~CameraBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            if (String.IsNullOrWhiteSpace(Name)) return Description;
            else return $"{Description} '{Name}'";
        }

        public string Description
        {
            get
            {
                if (null == _description)
                {
                    Type cameraType = GetType();
                    object[] attributes = cameraType.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if ((null != attributes) && (attributes.Length > 0) && (attributes[0] is DescriptionAttribute descriptionAttribute))
                        _description = descriptionAttribute.Description;
                    else
                        _description = cameraType.Name;
                }

                return _description;
            }
        }

        public virtual string SerialNumber { get => GetCamera().SerialNumber; }

        /// <summary>
        /// Provides a standardized hardware name for a given camera type.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The standardized hardware name should indicate vendor and model, e.g. "MicrosoftKinect2".
        /// It should not reflect specializations such as <see cref="LocalKinect"/> or <see cref="RemoteKinect"/>.
        /// The return value of this property must be used in the "hardwareName" column of the licenses database.
        /// </remarks>
        public virtual string HardwareName { get => GetType().Name; }

        /// <summary>
        /// Returns a modifiable reference to the camera's Calibration object.
        /// </summary>
        protected CameraCalibration Calibration
        {
            get => _calibration;
            set { _calibration = value; }
        }

        /// <summary>
        /// Returns an independent clone of the camera's Calibration object.
        /// </summary>
        public CameraCalibration GetCalibration()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The Camera's Intrinsic Transformation
        /// </summary>
        public ProjectiveTransformationZhang ProjectiveTransformation { get => _calibration.ProjectiveTransformation; }

        /// <summary>
        /// The Colour Camera's Intrinsic Transformation if a calibrated colour camera is available.
        /// </summary>
        public ProjectiveTransformationZhang ProjectiveTransformationColour { get => _calibration.ProjectiveTransformationColour; }

        /// <summary>
        /// The Transformation from the Infra-Red Camera's vector space to World coordinates.
        /// </summary>
        public RigidBodyTransformation RigidBodyTransformation
        {
            get => _calibration.RigidBodyTransformation;
            internal set { _calibration.RigidBodyTransformation = value; }
        }

        /// <summary>
        /// The Transformation from the Colour Camera's vector space to Pixel coordinates.
        /// </summary>
        public RigidBodyTransformation RigidBodyTransformationColour { get => _calibration.RigidBodyTransformationColour; }

        /// <summary>
        /// Returns parameters for the MetriX calibration algorithms when processing frames from the Camera.
        /// </summary>
        public CameraCalibrationParameters CalibrationParameters
        {
            get => _calibrationParameters;
        }

        /// <summary>
        /// Returns parameters for the MetriX pre-filters applied to frames from the Camera.
        /// </summary>
        public FramePreFilteringParameters PreFilteringParameters
        {
            get => _preFilteringParameters;
        }

        #region Engine Initialisation

        /// <summary>
        /// Called by the MetriX Engine after all Cameras have been added to the Engine but before the Engine is used for the first time.
        /// </summary>
        /// <remarks>
        /// Camera Implementations may override this method in order to modify the Engine according to their requirements.
        /// If a camera implementation does override this method, it should call `base.Initialize(...)` prior to performing
        /// implementation-specific initialization.
        /// </remarks>
        /// <param name="engine">
        /// The MetriX Engine instance that is being initialised.
        /// </param>
        public virtual void Initialize(MockEngine engine)
        {
            // Construct the Calibration object if it is not already available
            if (null == _calibration)
            {
                ConstructorInfo ci = typeof(CameraCalibration).GetTypeInfo().DeclaredConstructors.First();// GetConstructor(BindingFlags.NonPublic, new Type[] { });
                _calibration = (CameraCalibration)ci.Invoke(new object[] { });
            }
        }

        /// <summary>
        /// Applies the parameters and settings from an external camera object to the camera instance.
        /// </summary>
        internal void SetConfiguration(CameraBase configuration)
        {
            Name = configuration.Name;

            if ((null != configuration.Calibration) && (null != configuration.Calibration.RigidBodyTransformation))
            {
                throw new NotImplementedException();
            }

            if (null != configuration.CalibrationParameters)
            {
                _calibrationParameters = new CameraCalibrationParameters(configuration.CalibrationParameters);
            }

            if (null != configuration.PreFilteringParameters)
            {
                _preFilteringParameters = new FramePreFilteringParameters(configuration.PreFilteringParameters);
            }
        }

        #endregion Engine Initialisation

        #region Frame Acquisition

        /// <summary>
        /// Acquires a single frame of 3D data from the camera.
        /// </summary>
        /// <remarks>
        /// Use the dual-output variant of this method to acquire both 3D and 2D data from the same frame.
        /// Calling the single-output variants in sequence will result in 3D or 2D data from different frames.
        /// </remarks>
        public abstract void AcquireFrame(FramePurposes purposes, out DataFrame dataFrame);

        /// <summary>
        /// Acquires a single frame of 2D data from the camera.
        /// </summary>
        /// <remarks>
        /// Use the dual-output variant of this method to acquire both 3D and 2D data from the same frame.
        /// Calling the single-output variants in sequence will result in 3D or 2D data from different frames.
        /// </remarks>
        public virtual void AcquireFrame(FramePurposes purposes, out DisplayFrame displayFrame)
        {
            DataFrame dataFrame;
            AcquireFrame(FramePurposes.Display, out dataFrame);
            displayFrame = DisplayFrame.FromDataFrame(dataFrame);
        }

        /// <summary>
        /// Acquires both the 3D and 2D data from a single frame from the camera.
        /// </summary>
        public virtual void AcquireFrame(FramePurposes purposes, out DataFrame dataFrame, out DisplayFrame displayFrame)
        {
            AcquireFrame(purposes, out dataFrame);
            displayFrame = DisplayFrame.FromDataFrame(dataFrame);
        }

        #endregion Frame Acquisition

        #region XML Serialization

        public static CameraBase ReadCameraElement(XmlReader xmlReader, Func<string, Stream> streamProvider, MetriXConfigurationOptions options)
        {
            if (!xmlReader.IsStartElement())
            {
                throw new CorruptXmlException("Unexpected or unknown XML node encountered in configuration file: " + xmlReader.NodeType.ToString());
            }

            if (0 == (options & MetriXConfigurationOptions.NullDevices))
            {
                string localName = xmlReader.LocalName;
                switch (localName)
                {
                    case OrbbecAstra.ElementName:
                    case OrbbecAstra.OldElementName:
                        return OrbbecAstra.ReadXml(xmlReader, streamProvider);

                    default: throw new CorruptXmlException("Unexpected or unknown XML element encountered in configuration file: " + localName);
                }
            }
            else throw new NotImplementedException();
        }

        /// <summary>
        /// When overridden in a derived class, writes the Camera's XML Element and any necessary attributes thereon.
        /// </summary>
        /// <param name="xmlWriter"></param>
        protected abstract void WriteXmlStartElement(XmlWriter xmlWriter);

        /// <summary>
        /// When overridden in a derived class, writes child XML Elements into the Camera's root element.
        /// </summary>
        /// <param name="xmlWriter"></param>
        protected virtual void WriteXmlChildren(XmlWriter xmlWriter)
        {
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            // Start of Root Element
            WriteXmlStartElement(xmlWriter);

            // `Name' Attribute
            if (!String.IsNullOrWhiteSpace(Name))
            {
                string prefix = xmlWriter.LookupPrefix(Constants.XmlNamespaces.Configuration);
                xmlWriter.WriteAttributeString(prefix, "name", Constants.XmlNamespaces.Configuration, Name);
            }

            // Camera Calibration Element
            if (null != _calibration)
            {
                _calibration.WriteXml(xmlWriter);
            }

            // Camera Calibration Parameters
            if (null != _calibrationParameters)
            {
                _calibrationParameters.WriteXml(xmlWriter);
            }

            // Camera Pre-Filtering Parameters
            if (null != _preFilteringParameters)
            {
                _preFilteringParameters.WriteXml(xmlWriter);
            }

            // Child Elements
            WriteXmlChildren(xmlWriter);

            // End of Root Element
            xmlWriter.WriteEndElement();
        }

        protected static CameraType ReadXml<CameraType>(XmlReader xmlReader, Func<string, Stream> streamProvider, Func<CameraType> cameraFactory, Action<CameraType, string, string> readAttribute = null, Action<CameraType, string, XmlReader, Func<string, Stream>> readChild = null)
            where CameraType : CameraBase
        {
            // Expect Start Element
            if (!xmlReader.IsStartElement())
            {
                throw new CorruptXmlException("Expected XML element not found in configuration file");
            }

            // Construct the Camera
            CameraType camera = cameraFactory();

            // Attributes on Root Element
            if (xmlReader.HasAttributes)
            {
                string namespaceURI = xmlReader.NamespaceURI;
                while (xmlReader.MoveToNextAttribute())
                {
                    if (!namespaceURI.Equals(xmlReader.NamespaceURI, StringComparison.InvariantCultureIgnoreCase)) continue;

                    string attributeName = xmlReader.LocalName;
                    switch (attributeName)
                    {
                        case "name":
                            camera.Name = xmlReader.Value;
                            break;

                        default:
                            if (null != readAttribute) readAttribute(camera, attributeName, xmlReader.Value);
                            break;
                    }
                }
            }

            // Start of Root Element
            xmlReader.ReadStartElement();

            // Child Elements
            while (xmlReader.IsStartElement())
            {
                string elementName = xmlReader.LocalName;
                using (XmlReader subtreeReader = xmlReader.ReadSubtree())
                {
                    switch (elementName)
                    {
                        case CameraCalibration.ElementNames.Calibration:
                            camera._calibration = CameraCalibration.ReadXml(subtreeReader, streamProvider);
                            break;

                        default:
                            if (null != readChild) readChild(camera, elementName, subtreeReader, streamProvider);
                            break;
                    }
                }

                xmlReader.Read();
            }

            // Construct the Calibration object if it was not deserialized
            if (null == camera._calibration)
            {
                throw new NotImplementedException();
            }

            return camera;
        }

        #endregion XML Serialization

        #region Metrilus Framework Integration

        public virtual MetriCam2.Camera GetCamera()
        {
            return null;
        }

        /// <summary>
        /// Calculates Camera Intrinsics from a MetriCam 2 Camera's 'Point3DImage' channel and updates the Camera's Calibration.
        /// </summary>
        /// <remarks>
        /// This method should only be used by camera classes that wrap MetriCam 2 Camera instances
        /// where the MetriCam 2 Camera does not provide usable intrinsics.
        /// </remarks>
        protected void CalculateIntrinsics()
        {
            MetriCam2.Camera camera = GetCamera();
            if (null == camera) throw new NotSupportedException(String.Format("{0} is not a MetriCam 2 camera or does not override the GetCamera method.", GetType().Name));

            // Activate the Point3DImage channel if necessary.
            bool activateChannel = !camera.IsChannelActive(MetriCam2.ChannelNames.Point3DImage);
            if (activateChannel)
            {
                camera.ActivateChannel(MetriCam2.ChannelNames.Point3DImage);
            }

            try
            {
                // Acquire a Point3DImage
                Point3fCameraImage cameraImage;
                do
                {
                    camera.Update();
                    cameraImage = (Point3fCameraImage)camera.CalcChannel(MetriCam2.ChannelNames.Point3DImage);
                }
                while (null == cameraImage);

                // Mask out invalid pixels
                Point3fImage image = new Point3fImage(ref cameraImage);
                image.Mask = new MaskImage(image.Width, image.Height);
                int sz = (image.Width * image.Height);
                for (int i = 0; i < sz; ++i)
                {
                    image.Mask[i] = (image[i].Z < 0.1f) ? (byte)0x00 : (byte)0xff;
                }

                // Reverse-Engineer the Camera Intrinsics
                ProjectiveTransformationZhang projectiveTransformation = ProjectiveTransformationZhang.CreateFromPoint3fImage(image);

                // Set Metadata
                projectiveTransformation.CameraSerial = camera.SerialNumber;

                // Store the new Camera Intrinsics
                Calibration.ProjectiveTransformation = projectiveTransformation;
            }
            finally
            {
                // If the Point3DImage channel was activated by us, deactivate it.
                if (activateChannel)
                {
                    camera.DeactivateChannel(MetriCam2.ChannelNames.Point3DImage);
                }
            }
        }

        #endregion Metrilus Framework Integration
    }
}
