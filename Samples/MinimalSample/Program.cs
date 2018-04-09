// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Cameras;
using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

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
            BaslerACE camera;
            try
            {
                camera = new BaslerACE();
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
            camera.Connect();

            Console.WriteLine("Fetching one frame");
            camera.Update();

            try
            {
                Console.WriteLine("Accessing color data");
                ColorCameraImage img = (ColorCameraImage)camera.CalcChannel(ChannelNames.Color);
                Bitmap rgbBitmapData = img.ToBitmap();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Error getting channel {0}: {1}.", ChannelNames.Color, ex.Message));
            }

            try
            {
                Console.WriteLine("Accessing distance data");
                FloatCameraImage distancesData = (FloatCameraImage)camera.CalcChannel(ChannelNames.Distance);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Error getting channel {0}: {1}.", ChannelNames.Distance, ex.Message));
            }

            Console.WriteLine("Disconnecting camera");
            camera.Disconnect();

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
