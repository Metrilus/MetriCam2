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
            RealSense2 camera;
            try
            {
                camera = new RealSense2();
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

            camera.LoadConfigPreset(AdvancedMode.Preset.SHORT_RANGE);
            camera.DeactivateChannel(ChannelNames.ZImage);
            camera.ActivateChannel(ChannelNames.Left);
            camera.ActivateChannel(ChannelNames.Right);

            Console.WriteLine("Fetching one frame");

            for(int i = 0; i < 15; i++)
            {
                camera.Update();
            }


            ProjectiveTransformationZhang proj = (ProjectiveTransformationZhang)camera.GetIntrinsics(ChannelNames.Color);
            RigidBodyTransformation rbt = camera.GetExtrinsics(ChannelNames.Color, ChannelNames.ZImage);

            try
            {
                Console.WriteLine("Accessing color data");
                ColorCameraImage img = (ColorCameraImage)camera.CalcChannel(ChannelNames.Color);
                Bitmap rgbBitmapData = img.ToBitmap();
                rgbBitmapData.Save("0Color.bmp");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Error getting channel {0}: {1}.", ChannelNames.Color, ex.Message));
            }

            try
            {
                Console.WriteLine("Accessing distance data");
                FloatCameraImage distancesData = (FloatCameraImage)camera.CalcChannel(ChannelNames.ZImage);
                FloatImage fimg = new FloatImage(ref distancesData);
                fimg.Save("depth.flt");
                //Bitmap depthBitmapData = distancesData.ToBitmap();
                //depthBitmapData.Save("0Depth.bmp");
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
