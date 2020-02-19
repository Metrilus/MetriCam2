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

            Hikvision camera1 = new Hikvision();
            camera1.IPAddress = "192.168.1.190";
            camera1.Port = 554;
            camera1.Username = "admin";
            camera1.Password = "MetriX123";
            camera1.Connect();

            Hikvision camera2 = new Hikvision();
            camera2.IPAddress = "192.168.1.159";
            camera2.Port = 554;
            camera2.Username = "admin";
            camera2.Password = "MetriX123";
            camera2.Connect();

            // Hikvision camera3 = new Hikvision();
            // camera3.IPAddress = "192.168.1.121";
            // camera3.Port = 554;
            // camera3.Username = "admin";
            // camera3.Password = "MetriX123";
            // camera3.Connect();

            int i = 0;

            while (true)
            {

                camera1.Update();
                ColorImage img1 = (ColorImage)camera1.CalcChannel(ChannelNames.Color);
                camera2.Update();
                ColorImage img2 = (ColorImage)camera2.CalcChannel(ChannelNames.Color);
                //camera3.Update();
                //ColorImage img3 = (ColorImage)camera3.CalcChannel(ChannelNames.Color);
                //
                img1.ToBitmap().Save($@"G:\hikvision\hik1_{i}.jpg");
                img2.ToBitmap().Save($@"G:\hikvision\hik2_{i}.jpg");
                //img3.ToBitmap().Save(@"d:\data\hik3.jpg");

                i++;
            }

            //Console.WriteLine("Finished. Press any key to exit.");
            //Console.ReadKey();
        }
    }
}
