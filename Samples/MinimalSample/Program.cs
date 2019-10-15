// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Cameras;
using Metrilus.Util;
using System;
using System.Threading;
using Metrilus.Logging;

namespace MetriCam2.Samples.MinimalSample
{
    class Program
    {
        static MetriLog _log = new MetriLog();

        static void Main(string[] args)
        {
            _log.LogLevel = MetriLog.Levels.All;

            Console.WriteLine("------------------------------------------");
            Console.WriteLine("MetriCam 2 Minimal Sample.");
            Console.WriteLine("Get MetriCam 2 at http://www.metricam.net/");
            Console.WriteLine("------------------------------------------");

            AzureKinect camera1 = new AzureKinect();
            //camera1.SerialNumber = "000067192412";
            camera1.Connect();

            AzureKinect camera2 = new AzureKinect();
            //camera2.SerialNumber = "000049192312";
            camera2.Connect();

            camera1.Update();
            camera2.Update();
            FloatImage fCImg1 = (FloatImage)camera1.CalcChannel(ChannelNames.ZImage);
            FloatImage fCImg2 = (FloatImage)camera2.CalcChannel(ChannelNames.ZImage);

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
