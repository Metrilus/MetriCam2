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
            VisionaryT camera;
            try
            {
                camera = new VisionaryT();
            }
            catch (Exception e)
            {
                Console.WriteLine(Environment.NewLine + "Error: Could not create a PrimeSense camera object.");
                Console.WriteLine((e.InnerException == null) ? e.Message : e.InnerException.Message);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            camera.IPAddress = "192.168.1.10";

            // Connect, get one frame, disconnect
            Console.WriteLine("Connecting camera");
            camera.Connect();

            //while (true) { }

            Console.WriteLine("Fetching one frame");
            for(int i = 0; i < 100; i++)
            {
                camera.Update();

                try
                {
                    Console.WriteLine("Accessing distance data");
                    FloatCameraImage distancesData = (FloatCameraImage)camera.CalcChannel(ChannelNames.Intensity);
                    FloatImage fImg = new FloatImage(ref distancesData);
                    string tmp = fImg.ShowInDebug;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(String.Format("Error getting channel {0}: {1}.", ChannelNames.ZImage, ex.Message));
                }
            }
            



            

            Console.WriteLine("Disconnecting camera");
            camera.Disconnect();

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
