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

            AstraOpenNI camera;

            #region Connect and Disconnect
            using (camera = CreateCamera())
            {
                camera.Connect();
                Console.WriteLine($"Vendor = {camera.Vendor}");
                Console.WriteLine($"Model  = {camera.Model}");
                Console.WriteLine($"DeviceType = {camera.DeviceType}");
                Console.WriteLine($"SerialNumber = {camera.SerialNumber}");
            }
            #endregion

            #region Get intrinsics and extrinsics
            using (camera = CreateCamera())
            {
                camera.Connect();
                camera.Update();
                // ZImage == Intensity
                ProjectiveTransformationZhang intrinsics_Intensity = (ProjectiveTransformationZhang)camera.GetIntrinsics(ChannelNames.Intensity);
                ProjectiveTransformationZhang intrinsics_Color = (ProjectiveTransformationZhang)camera.GetIntrinsics(ChannelNames.Color);
                RigidBodyTransformation depthToColor = camera.GetExtrinsics(ChannelNames.ZImage, ChannelNames.Color);
            }
            #endregion

            #region Activate and deactivate channels
            using (camera = CreateCamera())
            {
                camera.Connect();
                // This should work
                camera.ActivateChannel(ChannelNames.Color);
                // This should throw
                try
                {
                    camera.ActivateChannel(ChannelNames.Intensity);
                }
                catch { }
                // This should work
                camera.DeactivateChannel(ChannelNames.Color);
                camera.DeactivateChannel(ChannelNames.ZImage);
                camera.DeactivateChannel(ChannelNames.Point3DImage);
                camera.ActivateChannel(ChannelNames.Intensity);
            }
            #endregion

            #region Fetching frames
            Console.WriteLine("Fetching frames");
            using (camera = CreateCamera())
            {
                camera.Connect();

                string channelName = ChannelNames.ZImage;
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        Console.Write($"Accessing {channelName} data");
                        FloatCameraImage ampData = (FloatCameraImage)camera.CalcChannel(channelName);
                        FloatImage fImg = new FloatImage(ref ampData);
                        string tmp = fImg.ShowInDebug;
                        Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }
            }
            #endregion

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }

        private static AstraOpenNI CreateCamera()
        {
            AstraOpenNI camera;
            try
            {
                camera = new AstraOpenNI();
            }
            catch (Exception e)
            {
                Console.WriteLine(Environment.NewLine + "Error: Could not create a PrimeSense camera object.");
                Console.WriteLine((e.InnerException == null) ? e.Message : e.InnerException.Message);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return null;
            }

            camera.SerialNumber = "18042730138"; // 18042730138 18042730319
            return camera;
        }
    }
}
