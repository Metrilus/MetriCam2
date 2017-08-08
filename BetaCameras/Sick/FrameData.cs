// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Xml;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Metrilus.Logging;
using System.Globalization;

namespace MetriCam2.Cameras.Internal.Sick
{
    /// <summary>
    /// This class represents exactly one frame received from camera.
    /// </summary>
    internal class FrameData
    {
        #region Private Variables
        // camera instance and log
        private Camera cam;
        private MetriLog log;
        // image data
        private byte[] imageData;
        // internal definitions
        private uint magicWord;
        private uint pkgLength;
        private ushort protocolVersion;
        private byte packetType;
        // image context
        private ushort numSegments;
        private uint[] offsets;
        private uint[] changedCounters;
        // camera parameters
        private int width;
        private int height;
        private float[] cam2WorldMatrix;
        private float fx;
        private float fy;
        private float cx;
        private float cy;
        private float k1;
        private float k2;
        private float f2rc;
        // image properties
        private int numBytesIntensity;
        private int numBytesDistance;
        private int numBytesConfidence;
        private int numBytesPerIntensityValue;
        private int numBytesPerDistanceValue;
        private int numBytesPerConfidenceValue;
        private int intensityStartOffset;
        private int distanceStartOffset;
        private int confidenceStartOffset;
        private ulong timeStamp;
        #endregion

        #region Properties
        /// <summary>
        /// Width of image.
        /// </summary>
        public int Width
        {
            get { return width; }
        }

        /// <summary>
        /// Height of image.
        /// </summary>
        public int Height
        {
            get { return height; }
        }

        /// <summary>
        /// FX.
        /// </summary>
        public float FX
        {
            get { return fx; }
        }

        /// <summary>
        /// FY.
        /// </summary>
        public float FY
        {
            get { return fy; }
        }

        /// <summary>
        /// CX.
        /// </summary>
        public float CX
        {
            get { return cx; }
        }

        /// <summary>
        /// CY.
        /// </summary>
        public float CY
        {
            get { return cy; }
        }

        /// <summary>
        /// K1.
        /// </summary>
        public float K1
        {
            get { return k1; }
        }

        /// <summary>
        /// K2.
        /// </summary>
        public float K2
        {
            get { return k2; }
        }

        /// <summary>
        /// Focal to ray cross.
        /// </summary>
        public float F2RC
        {
            get { return f2rc; }
        }

        /// <summary>
        /// Total number of bytes for intensity image.
        /// </summary>
        public int NumBytesIntensity
        {
            get { return numBytesIntensity; }
        }

        /// <summary>
        /// How many bytes are used to store one intensity value.
        /// </summary>
        public int NumBytesPerIntensityValue
        {
            get { return numBytesPerIntensityValue; }
        }

        /// <summary>
        /// Where does the intensity data start (relative to start of imageData).
        /// </summary>
        public int IntensityStartOffset
        {
            get { return intensityStartOffset; }
        }

        /// <summary>
        /// Total number of bytes for distance image.
        /// </summary>
        public int NumBytesDistance
        {
            get { return numBytesDistance; }
        }

        /// <summary>
        /// How many bytes are used to store one distance value.
        /// </summary>
        public int NumBytesPerDistanceValue
        {
            get { return numBytesPerIntensityValue; }
        }

        /// <summary>
        /// Where does the intensity data start (relative to start of imageData).
        /// </summary>
        public int DistanceStartOffset
        {
            get { return distanceStartOffset; }
        }

        /// <summary>
        /// Total number of bytes for confidence image.
        /// </summary>
        public int NumBytesConfidence
        {
            get { return numBytesConfidence; }
        }

        /// <summary>
        /// How many bytes are used to store one confidence value.
        /// </summary>
        public int NumBytesPerConfidenceValue
        {
            get { return numBytesPerConfidenceValue; }
        }

        /// <summary>
        /// Where does the confidence data start (relative to start of imageData).
        /// </summary>
        public int ConfidenceStartOffset
        {
            get { return confidenceStartOffset; }
        }

        /// <summary>
        /// Time stamp of image.
        /// </summary>
        public ulong TimeStamp
        {
            get { return timeStamp; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize this frame with the received data by Device.Stream_GetFrame().
        /// </summary>
        /// <param name="data">image data</param>
        /// <param name="cam">camera instance</param>
        /// <param name="log">metri log from camera instance</param>
        public FrameData(byte[] data, Camera cam, MetriLog log)
        {
            imageData = data;
            this.cam  = cam;
            this.log  = log;
            DefaultValues();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// This methods parses the data provided by constructor and sets the properties accordingly.
        /// </summary>
        public void Read()
        {
            // first 11 bytes: internal definitions consisting of:
            // 4 bytes STx
            magicWord = BitConverter.ToUInt32(imageData, 0);
            if (0x02020202 != magicWord)
            {
                log.Error("The framing header is not 0x02020202 as expected.");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_unknown", "The framing header does not have the expected value.");
            }
            magicWord = Utils.ConvertEndiannessUInt32(magicWord);
            
            // 4 bytes packet length
            pkgLength = BitConverter.ToUInt32(imageData, 4);
            pkgLength = Utils.ConvertEndiannessUInt32(pkgLength);
            
            // 2 bytes protocol version
            protocolVersion = BitConverter.ToUInt16(imageData, 8);
            protocolVersion = Utils.ConvertEndiannessUInt16(protocolVersion);
            if (0x0001 != protocolVersion)
            {
                log.Error("The protocol version is not 0x0001 as expected.");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_unknown", "Unexpected protocol version");
            }

            // 1 byte packet type
            packetType = imageData[10];
            if (0x62 != packetType)
            {
                log.Error("The packet type is not 0x62 as expected.");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_unknown", "Unexpected packet type");
            }

            UInt16 blobId = BitConverter.ToUInt16(imageData, 11);
            if (0x0001 == blobId)
            {
                log.Error("The blob id is not 0x0001 as expected.");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_unknown", "Unexpected blob id");
            }
            blobId = Utils.ConvertEndiannessUInt16(blobId);

            // Next 4 bytes: blob id and number of segments
            numSegments = BitConverter.ToUInt16(imageData, 13);
            numSegments = Utils.ConvertEndiannessUInt16(numSegments);

            if (numSegments != 3)
            {
                log.Error("Number of segments is not three.");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_unknown", "No segments found.");
            }

            // Next 8 * numSegments bytes: Offset and change counter for each segment
            offsets = new uint[numSegments];
            changedCounters = new uint[numSegments];
            for (int i = 0; i < numSegments; ++i)
            {
                int index = i * 8 + 15; // 8 per item + 15 is offset
                offsets[i]         = BitConverter.ToUInt32(imageData, index);
                changedCounters[i] = BitConverter.ToUInt32(imageData, index + 4);

                offsets[i]         = Utils.ConvertEndiannessUInt32(offsets[i]);
                changedCounters[i] = Utils.ConvertEndiannessUInt32(changedCounters[i]);

                // First internal defintions took up 11 bytes
                offsets[i] += 11;
            }

            // now: XML segment
            string xml = Encoding.ASCII.GetString(imageData, (int)offsets[0], (int)offsets[1] - (int)offsets[0]);
            ReadXML(xml);

            // calc sizes
            numBytesIntensity  = width * height * numBytesPerIntensityValue;
            numBytesDistance   = width * height * numBytesPerDistanceValue;
            numBytesConfidence = width * height * numBytesPerConfidenceValue;

            // now: save image data offsets
            ReadBinary();
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Reads the XML part and saves the image properties (e.g. width/height).
        /// </summary>
        /// <param name="xml">XML string</param>
        private void ReadXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(new StringReader(xml));

            XmlNode dataSets = doc["SickRecord"]["DataSets"];
            XmlNode dataSetDepthMap = dataSets["DataSetDepthMap"];
            XmlNode formatDescriptionDepthMap = dataSetDepthMap["FormatDescriptionDepthMap"];
            XmlNode dataStream = formatDescriptionDepthMap["DataStream"];

            // get camera/image parameters
            width  = Convert.ToInt32(dataStream["Width"].InnerText);
            height = Convert.ToInt32(dataStream["Height"].InnerText);

            XmlNode cameraToWorldTransform = dataStream["CameraToWorldTransform"];
            List<float> cam2WorldList = new List<float>();
            foreach (XmlNode child in cameraToWorldTransform)
            {
                cam2WorldList.Add(float.Parse(child.InnerText, CultureInfo.InvariantCulture.NumberFormat));
            }
            cam2WorldMatrix = cam2WorldList.ToArray();
            cam2WorldList = null;

            XmlNode cameraMatrix = dataStream["CameraMatrix"];
            fx = float.Parse(cameraMatrix.ChildNodes[0].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            fy = float.Parse(cameraMatrix.ChildNodes[1].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            cx = float.Parse(cameraMatrix.ChildNodes[2].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            cy = float.Parse(cameraMatrix.ChildNodes[3].InnerText, CultureInfo.InvariantCulture.NumberFormat);

            XmlNode distortionParams = dataStream["CameraDistortionParams"];
            k1 = float.Parse(distortionParams["K1"].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            k2 = float.Parse(distortionParams["K2"].InnerText, CultureInfo.InvariantCulture.NumberFormat);

            f2rc = Convert.ToSingle(dataStream["FocalToRayCross"].InnerText, CultureInfo.InvariantCulture.NumberFormat);

            // data types, should always be uint16_t
            if (dataStream["Distance"].InnerText.ToLower() == "uint16")
            {
                numBytesPerDistanceValue = 2;
            }
            else
            {
                log.ErrorFormat("Bytes per distance value has unexpected value \"{0}\"", dataStream.ChildNodes[8].InnerText.ToLower());
                ExceptionBuilder.Throw(typeof(NotImplementedException), cam, "error_unknown", "Bytes per distance value has unexpected value.");
            }
            if (dataStream["Intensity"].InnerText.ToLower() == "uint16")
            {
                numBytesPerIntensityValue = 2;
            }
            else
            {
                log.ErrorFormat("Bytes per intensity value has unexpected value \"{0}\"", dataStream.ChildNodes[8].InnerText.ToLower());
                ExceptionBuilder.Throw(typeof(NotImplementedException), cam, "error_unknown", "Bytes per intensity value has unexpected value.");
            }
            if (dataStream["Confidence"].InnerText.ToLower() == "uint16")
            {
                numBytesPerConfidenceValue = 2;
            }
            else
            {
                log.ErrorFormat("Bytes per confidence value has unexpected value \"{0}\"", dataStream.ChildNodes[8].InnerText.ToLower());
                ExceptionBuilder.Throw(typeof(NotImplementedException), cam, "error_unknown", "Bytes per confidence value has unexpected value.");
            }
        }

        /// <summary>
        /// Calculates the offsets where to find the image data for channels.
        /// </summary>
        private void ReadBinary()
        {
            int offset = (int)offsets[1];
            
            // 4 bytes length per dataset
            uint datasetLength = BitConverter.ToUInt32(imageData, offset);
            datasetLength = Utils.ConvertEndiannessUInt32(datasetLength);
            offset += 4;

            // 8 bytes timestamp
            timeStamp = BitConverter.ToUInt64(imageData, offset);
            timeStamp = Utils.ConvertEndiannessUInt64(timeStamp);
            offset += 8;

            // 2 bytes version
            UInt16 version = BitConverter.ToUInt16(imageData, offset);
            version = Utils.ConvertEndiannessUInt16(version);
            offset += 2;

            // 4 bytes frame number
            uint frameNumber = BitConverter.ToUInt32(imageData, offset);
            frameNumber = Utils.ConvertEndiannessUInt32(frameNumber);
            offset += 4;

            // 1 byte data quality
            byte dataQuality = imageData[offset];
            offset += 1;

            // 1 byte device status
            byte deviceStatus = imageData[offset];
            offset += 1;

            // 176 * 144 * 2 bytes distance data
            distanceStartOffset = offset;
            offset += numBytesDistance;
            // 176 * 144 * 2 bytes intensity data
            intensityStartOffset = offset;
            offset += numBytesIntensity;
            // 176 * 144 * 2 bytes confidence data
            confidenceStartOffset = offset;
            offset += numBytesConfidence;

            // 4 bytes CRC of data
            uint crc = BitConverter.ToUInt32(imageData, offset);
            crc = Utils.ConvertEndiannessUInt32(crc);
            offset += 4;

            // 4 bytes same length as first value
            uint datasetLengthAgain = BitConverter.ToUInt32(imageData, offset);
            datasetLengthAgain = Utils.ConvertEndiannessUInt32(datasetLengthAgain);
            offset += 4;

            if (datasetLength != datasetLengthAgain)
            {
                log.Error("First and last 4 bytes, which encode the length of the dataset, did not match!");
                ExceptionBuilder.Throw(typeof(InvalidOperationException), cam, "error_unknown", "Unexpected value in dataset buffer");
            }
        }

        /// <summary>
        /// Setup default values for camera parameters.
        /// </summary>
        private void DefaultValues()
        {
            width = 176;
            height = 144;
            fx = 146.5f;
            fy = 146.5f;
            cx = 84.4f;
            cy = 71.2f;
            k1 = 0.326442f;
            k2 = 0.219623f;
            f2rc = 0.0f;
            cam2WorldMatrix = new float[16];
            cam2WorldMatrix[0 ] = 1.0f;
            cam2WorldMatrix[1 ] = 0.0f;
            cam2WorldMatrix[2 ] = 0.0f;
            cam2WorldMatrix[3 ] = 0.0f;
            cam2WorldMatrix[4 ] = 0.0f;
            cam2WorldMatrix[5 ] = 1.0f;
            cam2WorldMatrix[6 ] = 0.0f;
            cam2WorldMatrix[7 ] = 0.0f;
            cam2WorldMatrix[8 ] = 0.0f;
            cam2WorldMatrix[9 ] = 0.0f;
            cam2WorldMatrix[10] = 1.0f;
            cam2WorldMatrix[11] = 0.0f;
            cam2WorldMatrix[12] = 0.0f;
            cam2WorldMatrix[13] = 0.0f;
            cam2WorldMatrix[14] = 0.0f;
            cam2WorldMatrix[15] = 1.0f;
            numBytesPerDistanceValue   = 2;
            numBytesPerIntensityValue  = 2;
            numBytesPerConfidenceValue = 2;
        }
        #endregion
    }
}
