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
        private readonly Camera cam;
        private readonly MetriLog log;
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
        private float[] cam2WorldMatrix;
        #endregion

        #region Properties
        /// <summary>
        /// Width of image.
        /// </summary>
        internal int Width { get; private set; }

        /// <summary>
        /// Height of image.
        /// </summary>
        internal int Height { get; private set; }

        /// <summary>
        /// FX.
        /// </summary>
        internal float FX { get; private set; }

        /// <summary>
        /// FY.
        /// </summary>
        internal float FY { get; private set; }

        /// <summary>
        /// CX.
        /// </summary>
        internal float CX { get; private set; }

        /// <summary>
        /// CY.
        /// </summary>
        internal float CY { get; private set; }

        /// <summary>
        /// K1.
        /// </summary>
        internal float K1 { get; private set; }

        /// <summary>
        /// K2.
        /// </summary>
        internal float K2 { get; private set; }

        /// <summary>
        /// Focal to ray cross.
        /// </summary>
        internal float F2RC { get; private set; }

        /// <summary>
        /// Where does the intensity data start (relative to start of imageData).
        /// </summary>
        internal int IntensityStartOffset { get; private set; }

        /// <summary>
        /// Where does the intensity data start (relative to start of imageData).
        /// </summary>
        internal int DistanceStartOffset { get; private set; }

        /// <summary>
        /// Where does the confidence data start (relative to start of imageData).
        /// </summary>
        internal int ConfidenceStartOffset { get; private set; }

        /// <summary>
        /// Time stamp of image.
        /// </summary>
        internal ulong TimeStamp { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize this frame with the received data by Device.Stream_GetFrame().
        /// </summary>
        /// <param name="data">image data</param>
        /// <param name="cam">camera instance</param>
        /// <param name="log">metri log from camera instance</param>
        internal FrameData(byte[] data, Camera cam, MetriLog log)
        {
            imageData = data;
            this.cam  = cam;
            this.log  = log;
            SetDefaultValues();
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// This methods parses the data provided by constructor and sets the properties accordingly.
        /// </summary>
        internal void Read()
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
            ReadXML(xml, out int numBytesPerIntensityValue, out int numBytesPerDistanceValue, out int numBytesPerConfidenceValue);

            // calc sizes
            int numBytesIntensity  = Width * Height * numBytesPerIntensityValue;
            int numBytesDistance   = Width * Height * numBytesPerDistanceValue;
            int numBytesConfidence = Width * Height * numBytesPerConfidenceValue;

            // now: save image data offsets
            ReadBinary(numBytesIntensity, numBytesDistance, numBytesConfidence);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Reads the XML part and saves the image properties (e.g. width/height).
        /// </summary>
        /// <param name="xml">XML string</param>
        private void ReadXML(string xml, out int numBytesPerIntensityValue, out int numBytesPerDistanceValue, out int numBytesPerConfidenceValue)
        {
            // set default values to make compiler happy
            numBytesPerIntensityValue = 2;
            numBytesPerDistanceValue = 2;
            numBytesPerConfidenceValue = 2;

            XmlDocument doc = new XmlDocument();
            doc.Load(new StringReader(xml));

            XmlNode dataSets = doc["SickRecord"]["DataSets"];
            XmlNode dataSetDepthMap = dataSets["DataSetDepthMap"];
            XmlNode formatDescriptionDepthMap = dataSetDepthMap["FormatDescriptionDepthMap"];
            XmlNode dataStream = formatDescriptionDepthMap["DataStream"];

            // get camera/image parameters
            Width  = Convert.ToInt32(dataStream["Width"].InnerText);
            Height = Convert.ToInt32(dataStream["Height"].InnerText);

            XmlNode cameraToWorldTransform = dataStream["CameraToWorldTransform"];
            List<float> cam2WorldList = new List<float>();
            foreach (XmlNode child in cameraToWorldTransform)
            {
                cam2WorldList.Add(float.Parse(child.InnerText, CultureInfo.InvariantCulture.NumberFormat));
            }
            cam2WorldMatrix = cam2WorldList.ToArray();
            cam2WorldList = null;

            XmlNode cameraMatrix = dataStream["CameraMatrix"];
            FX = float.Parse(cameraMatrix.ChildNodes[0].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            FY = float.Parse(cameraMatrix.ChildNodes[1].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            CX = float.Parse(cameraMatrix.ChildNodes[2].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            CY = float.Parse(cameraMatrix.ChildNodes[3].InnerText, CultureInfo.InvariantCulture.NumberFormat);

            XmlNode distortionParams = dataStream["CameraDistortionParams"];
            K1 = float.Parse(distortionParams["K1"].InnerText, CultureInfo.InvariantCulture.NumberFormat);
            K2 = float.Parse(distortionParams["K2"].InnerText, CultureInfo.InvariantCulture.NumberFormat);

            F2RC = Convert.ToSingle(dataStream["FocalToRayCross"].InnerText, CultureInfo.InvariantCulture.NumberFormat);

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
        private void ReadBinary(int numBytesIntensity, int numBytesDistance, int numBytesConfidence)
        {
            int offset = (int)offsets[1];
            
            // 4 bytes length per dataset
            uint datasetLength = BitConverter.ToUInt32(imageData, offset);
            datasetLength = Utils.ConvertEndiannessUInt32(datasetLength);
            offset += 4;

            // 8 bytes timestamp
            TimeStamp = BitConverter.ToUInt64(imageData, offset);
            TimeStamp = Utils.ConvertEndiannessUInt64(TimeStamp);
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
            DistanceStartOffset = offset;
            offset += numBytesDistance;
            // 176 * 144 * 2 bytes intensity data
            IntensityStartOffset = offset;
            offset += numBytesIntensity;
            // 176 * 144 * 2 bytes confidence data
            ConfidenceStartOffset = offset;
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
        private void SetDefaultValues()
        {
            Width = 176;
            Height = 144;
            FX = 146.5f;
            FY = 146.5f;
            CX = 84.4f;
            CY = 71.2f;
            K1 = 0.326442f;
            K2 = 0.219623f;
            F2RC = 0.0f;
            cam2WorldMatrix = new float[16];
            cam2WorldMatrix[0]  = 1.0f;
            cam2WorldMatrix[1]  = 0.0f;
            cam2WorldMatrix[2]  = 0.0f;
            cam2WorldMatrix[3]  = 0.0f;
            cam2WorldMatrix[4]  = 0.0f;
            cam2WorldMatrix[5]  = 1.0f;
            cam2WorldMatrix[6]  = 0.0f;
            cam2WorldMatrix[7]  = 0.0f;
            cam2WorldMatrix[8]  = 0.0f;
            cam2WorldMatrix[9]  = 0.0f;
            cam2WorldMatrix[10] = 1.0f;
            cam2WorldMatrix[11] = 0.0f;
            cam2WorldMatrix[12] = 0.0f;
            cam2WorldMatrix[13] = 0.0f;
            cam2WorldMatrix[14] = 0.0f;
            cam2WorldMatrix[15] = 1.0f;
        }
        #endregion
    }
}
