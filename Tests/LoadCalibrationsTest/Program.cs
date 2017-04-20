using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MetriCam2.Cameras;
using System.Threading;
using Metrilus.Logging;
using Metrilus.Util;

namespace MetriCam2.Tests.LoadCalibrationsTest
{
    /// <summary>
    /// This is a simple program to test the logic of loading intrinsic and extrinsic calibrations.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            MetriLog log = new MetriLog();

            Kinect2 cam = new Kinect2();
            try
            {
                cam.Connect();
            }
            catch (MetriCam2.Exceptions.ConnectionFailedException)
            {
                log.Error("Connection failed. Closing window in 5 sec.");
                Thread.Sleep(5 * 1000);
                return;
            }

            cam.ActivateChannel(ChannelNames.Color);

            bool running = false;
            while (running)
            {
                cam.Update();
            }

            ProjectiveTransformationZhang pt;
            try
            {
                pt = (ProjectiveTransformationZhang)cam.GetIntrinsics(ChannelNames.Color);
            }
            catch (FileNotFoundException)
            {
                log.Warn("No PT found.");
            }
            try
            {
                pt = (ProjectiveTransformationZhang)cam.GetIntrinsics(ChannelNames.Color);
            }
            catch (FileNotFoundException)
            {
                log.Warn("No PT found.");
            }
            try
            {
                pt = (ProjectiveTransformationZhang)cam.GetIntrinsics(ChannelNames.Color);
            }
            catch (FileNotFoundException)
            {
                log.Warn("No PT found.");
            }

            try
            {
                RigidBodyTransformation rbt = cam.GetExtrinsics(ChannelNames.Color, ChannelNames.ZImage);
            }
            catch (FileNotFoundException)
            {
                log.Warn("No fwd RBT found.");
            }

            try
            {
                RigidBodyTransformation rbtInverse = cam.GetExtrinsics(ChannelNames.ZImage, ChannelNames.Color);
            }
            catch (FileNotFoundException)
            {
                log.Warn("No inverse RBT found.");
            }

            cam.Disconnect();

            log.Info("Program ended. Closing window in 5 sec.");
            Thread.Sleep(5 * 1000);
        }
    }
}
