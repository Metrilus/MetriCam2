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
        private readonly Camera cam;
        private readonly MetriLog log;
        // camera parameters
        private float[] cam2WorldMatrix;
        #endregion

        #region Properties
        /// <summary>
        /// Image data.
        /// </summary>
        internal byte[] ImageBuffer { get; private set; }

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
            ImageBuffer = data;
            this.cam  = cam;
            this.log  = log;
            SetDefaultValues();
            Parse();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Parses the binary data.
        /// </summary>
        private void Parse()
        {
            // first 11 bytes: internal definitions consisting of:
            // 4 bytes STx
            uint magicWord = BitConverter.ToUInt32(ImageBuffer, 0);
            if (0x02020202 != magicWord)
            {
                string msg = string.Format("The framing header is not 0x02020202 as expected: {0:X8}", magicWord);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }
            magicWord = Utils.ConvertEndiannessUInt32(magicWord);
            
            // 4 bytes packet length
            uint pkgLength = BitConverter.ToUInt32(ImageBuffer, 4);
            pkgLength = Utils.ConvertEndiannessUInt32(pkgLength);
            
            // 2 bytes protocol version
            ushort protocolVersion = BitConverter.ToUInt16(ImageBuffer, 8);
            protocolVersion = Utils.ConvertEndiannessUInt16(protocolVersion);
            if (0x0001 != protocolVersion)
            {
                string msg = string.Format("The protocol version is not 0x0001 as expected: {0:X4}", protocolVersion);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }

            // 1 byte packet type
            byte packetType = ImageBuffer[10];
            if (0x62 != packetType)
            {
                string msg = string.Format("The packet type is not 0x62 as expected: {0:X2}", packetType);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }

            // 2 bytes: blob id
            ushort blobId = BitConverter.ToUInt16(ImageBuffer, 11);
            blobId = Utils.ConvertEndiannessUInt16(blobId);
            if (0x0001 == blobId)
            {
                string msg = string.Format("The blob id is not 0x0001 as expected: {0:X4}", blobId);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }

            // 2 bytes: number of segments
            ushort numSegments = BitConverter.ToUInt16(ImageBuffer, 13);
            numSegments = Utils.ConvertEndiannessUInt16(numSegments);
            if (numSegments != 3)
            {
                string msg = string.Format("The number of segments is not 3 as expected: {0}", numSegments);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }

            // Next 8 * numSegments bytes: Offset and change counter for each segment
            uint[] offsets = new uint[numSegments];
            uint[] changedCounters = new uint[numSegments];
            for (int i = 0; i < numSegments; ++i)
            {
                int index = i * 8 + 15; // 8 bytes per item + 15 bytes header so far

                uint offset = BitConverter.ToUInt32(ImageBuffer, index);
                offsets[i] = Utils.ConvertEndiannessUInt32(offset);

                uint changedCounter = BitConverter.ToUInt32(ImageBuffer, index + 4);
                changedCounters[i] = Utils.ConvertEndiannessUInt32(changedCounter);

                // First internal defintions took up 11 bytes
                offsets[i] += 11;
            }

            // now: XML segment
            string xml = Encoding.ASCII.GetString(ImageBuffer, (int)offsets[0], (int)offsets[1] - (int)offsets[0]);
            ParseXML(xml, out int numBytesPerIntensityValue, out int numBytesPerDistanceValue, out int numBytesPerConfidenceValue);

            // calc sizes
            int numBytesIntensity  = Width * Height * numBytesPerIntensityValue;
            int numBytesDistance   = Width * Height * numBytesPerDistanceValue;
            int numBytesConfidence = Width * Height * numBytesPerConfidenceValue;

            // now: save image data offsets
            ParseBinary((int)offsets[1], numBytesIntensity, numBytesDistance, numBytesConfidence);
        }

        /// <summary>
        /// Reads the XML part and saves the image properties (e.g. width/height).
        /// </summary>
        /// <param name="xml">XML string</param>
        private void ParseXML(string xml, out int numBytesPerIntensityValue, out int numBytesPerDistanceValue, out int numBytesPerConfidenceValue)
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
            string dataType = dataStream["Distance"].InnerText.ToLower();
            if (dataType == "uint16")
            {
                numBytesPerDistanceValue = 2;
            }
            else
            {
                string msg = string.Format("Bytes per distance value has unexpected value \"{0}\"", dataType);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }

            dataType = dataStream["Intensity"].InnerText.ToLower();
            if (dataType == "uint16")
            {
                numBytesPerIntensityValue = 2;
            }
            else
            {
                string msg = string.Format("Bytes per intensity value has unexpected value \"{0}\"", dataType);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }

            dataType = dataStream["Confidence"].InnerText.ToLower();
            if (dataType == "uint16")
            {
                numBytesPerConfidenceValue = 2;
            }
            else
            {
                string msg = string.Format("Bytes per confidence value has unexpected value \"{0}\"", dataType);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
            }
        }

        /// <summary>
        /// Calculates the offsets where to find the image data for channels.
        /// </summary>
        private void ParseBinary(int offset, int numBytesIntensity, int numBytesDistance, int numBytesConfidence)
        {
            // 4 bytes length per dataset
            uint datasetLength = BitConverter.ToUInt32(ImageBuffer, offset);
            datasetLength = Utils.ConvertEndiannessUInt32(datasetLength);
            offset += 4;

            // 8 bytes timestamp
            TimeStamp = BitConverter.ToUInt64(ImageBuffer, offset);
            TimeStamp = Utils.ConvertEndiannessUInt64(TimeStamp);
            offset += 8;

            // 2 bytes version
            UInt16 version = BitConverter.ToUInt16(ImageBuffer, offset);
            version = Utils.ConvertEndiannessUInt16(version);
            offset += 2;

            // 4 bytes frame number
            uint frameNumber = BitConverter.ToUInt32(ImageBuffer, offset);
            frameNumber = Utils.ConvertEndiannessUInt32(frameNumber);
            offset += 4;

            // 1 byte data quality
            byte dataQuality = ImageBuffer[offset];
            offset += 1;

            // 1 byte device status
            byte deviceStatus = ImageBuffer[offset];
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
            uint crc = BitConverter.ToUInt32(ImageBuffer, offset);
            crc = Utils.ConvertEndiannessUInt32(crc);
            offset += 4;

            // 4 bytes same length as first value
            uint datasetLengthAgain = BitConverter.ToUInt32(ImageBuffer, offset);
            datasetLengthAgain = Utils.ConvertEndiannessUInt32(datasetLengthAgain);
            offset += 4;

            if (datasetLength != datasetLengthAgain)
            {
                string msg = string.Format("First and last 4 bytes -- which encode the length of the dataset -- did not match: {0} and {1}", datasetLength, datasetLengthAgain);
                log.Error(msg);
                ExceptionBuilder.Throw(typeof(InvalidDataException), cam, "error_unknown", msg);
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
