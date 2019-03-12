// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.
using Newtonsoft.Json;

namespace MetriCam2.Cameras.Internal.Sick
{
    class CameraObject
    {
        [JsonProperty(PropertyName = "class")]
        public string CameraClass { get; set; }

        public CameraData Data { get; set; }
    }

    class CameraData
    {
        public string CameraID { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public float FocalDistance { get; set; }
        public string FocalDistanceUnit { get; set; }
        public float[][] IntrinsicK { get; set; }
        public float[][] WorldToSensorDistortion { get; set; }
        public float[][] SensorToWorldDistortion { get; set; }
        public float[][] WorldToView { get; set; }
        public Point PixelSize { get; set; }
        public Point Origin { get; set; }
        public string HandleZeroPixels { get; set; }
        public ImageData Data { get; set; }
    }

    class ImageData
    {
        public string ImageType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Pixels Pixels { get; set; }
    }

    class Pixels
    {
        public int numOfElems { get; set; }
        public int elemSz { get; set; }
        public string endian { get; set; }
        public string[] elemTypes { get; set; }
        public string data { get; set; }
    }

    class Point
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
    }
}
