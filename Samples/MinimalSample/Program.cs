// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Cameras;
using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MetriPrimitives.Data;

namespace MetriCam2.Samples.MinimalSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("MetriCam 2 Minimal Sample.");
            Console.WriteLine("Get MetriCam 2 at http://www.metricam.net/");
            Console.WriteLine("------------------------------------------");

            // Create camera object
            O3D3xx camera;
            try
            {
                camera = new O3D3xx();
            }
            catch (Exception e)
            {
                Console.WriteLine(Environment.NewLine + "Error: Could not create a PrimeSense camera object.");
                Console.WriteLine((e.InnerException == null) ? e.Message : e.InnerException.Message);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Connect, get one frame, disconnect
            Console.WriteLine("Connecting camera");
            camera.CameraIP = "192.168.1.232";
            camera.Connect();
            //camera.Resolution100k = true;

            bool breakLoop = false;

            while(true)
            {
                Console.WriteLine("Fetching one frame");
                camera.Update();
                FloatCameraImage fcImg = (FloatCameraImage)camera.CalcChannel(ChannelNames.Distance);
                FloatImage fImg = new FloatImage(ref fcImg);
                string res = fImg.ShowInDebug;

                if (breakLoop)
                    break;
            }
            


            Console.WriteLine("Disconnecting camera");
             camera.Disconnect();

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
