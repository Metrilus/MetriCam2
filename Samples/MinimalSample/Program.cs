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
            camera1.IPAddress = "192.168.1.114";
            camera1.Port = 554;
            camera1.Username = "admin";
            camera1.Password = "MetriX123";
            camera1.Connect();

            camera1.Update();
             ColorImage fCImg1 = (ColorImage)camera1.CalcChannel(ChannelNames.Color);

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
