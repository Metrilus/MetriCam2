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
using System.Runtime.CompilerServices;
using System.Diagnostics;

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

            //#region Connect and Disconnect
            //using (camera = CreateCamera())
            //{
            //    camera.Connect();
            //    Console.WriteLine($"Vendor = {camera.Vendor}");
            //    Console.WriteLine($"Model  = {camera.Model}");
            //    Console.WriteLine($"DeviceType = {camera.DeviceType}");
            //    Console.WriteLine($"SerialNumber = {camera.SerialNumber}");
            //}
            //#endregion

            //#region Test Emitter on/off
            //using (camera = CreateCamera())
            //{
            //    camera.Connect();
            //    camera.IRGain = 8;
            //    string channelName = ChannelNames.ZImage;
            //    // warm-up
            //    for (int i = 0; i < 10; i++)
            //    {
            //        camera.Update();
            //        try
            //        {
            //            Console.Write($"Accessing {channelName} data");
            //            FloatCameraImage ampData = (FloatCameraImage)camera.CalcChannel(channelName);
            //            FloatImage fImg = new FloatImage(ref ampData);
            //            Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
            //        }
            //        catch (ArgumentException ex)
            //        {
            //            Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
            //        }
            //    }

            //    camera.SetEmitterStatusAndWait(false);

            //    for (int i = 0; i < 3; i++)
            //    {
            //        camera.Update();
            //        try
            //        {
            //            Console.Write($"Accessing {channelName} data");
            //            FloatCameraImage ampData = (FloatCameraImage)camera.CalcChannel(channelName);
            //            FloatImage fImg = new FloatImage(ref ampData);
            //            Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
            //        }
            //        catch (ArgumentException ex)
            //        {
            //            Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
            //        }
            //    }

            //    Console.WriteLine($"Switching emitter on.");
            //    Console.WriteLine($"Exposure is at {camera.IRExposure}.");
            //    Console.WriteLine($"Gain is at {camera.IRGain}.");
            //    camera.SetEmitterStatusAndWait(true);

            //    for (int i = 0; i < 3; i++)
            //    {
            //        camera.Update();
            //        try
            //        {
            //            Console.Write($"Accessing {channelName} data");
            //            FloatCameraImage ampData = (FloatCameraImage)camera.CalcChannel(channelName);
            //            FloatImage fImg = new FloatImage(ref ampData);
            //            string tmp = fImg.ShowInDebug;
            //            Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
            //        }
            //        catch (ArgumentException ex)
            //        {
            //            Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
            //        }
            //    }
            //}
            //#endregion

            //#region Test Fast Emitter Switching
            //using (camera = CreateCamera())
            //{
            //    Stopwatch sw = new Stopwatch();
            //    camera.Connect();
            //    SetAcquisitionMode(camera, AcquisitionModes.Idle);
            //    sw.Start();
            //    for (int i = 0; i < 10; i++)
            //    {
            //        SetAcquisitionMode(camera, AcquisitionModes.ColorAndZImage);
            //        camera.Update();

            //        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(ChannelNames.ZImage);
            //        FloatImage fImg = new FloatImage(ref rawData);
            //        Console.WriteLine($"\tMean = {fImg.ComputeMean()}");

            //        FloatCameraImage rawColorData = camera.CalcChannel(ChannelNames.Color).ToFloatCameraImage();
            //        FloatImage fColorImg = new FloatImage(ref rawColorData);
            //        Console.WriteLine($"\tMean (color) = {fColorImg.ComputeMean()}");

            //        SetAcquisitionMode(camera, AcquisitionModes.Idle);
            //        Console.WriteLine($"\tElappsed: {sw.ElapsedMilliseconds}");
            //    }
            //}
            //#endregion

            #region Test Fast Emitter Switching
            using (camera = CreateCamera())
            {
                using (AstraOpenNI otherCamera = CreateCamera("18042730319"))
                {
                    AstraOpenNI[] cameras = { camera, otherCamera };

                    Stopwatch sw = new Stopwatch();
                    camera.Connect();
                    otherCamera.Connect();

                    // Set camera to ColorAndZImage mode
                    if (camera.IsChannelActive(ChannelNames.Intensity))
                    {
                        camera.DeactivateChannel(ChannelNames.Intensity);
                    }
                    camera.ActivateChannel(ChannelNames.Color);
                    camera.ActivateChannel(ChannelNames.Point3DImage);
                    camera.ActivateChannel(ChannelNames.ZImage);
                    //camera.IRFlooderEnabled = false;
                    //camera.SetEmitterStatusAndWait(true);

                    // Set otherCamera to ColorAndZImage mode
                    if (otherCamera.IsChannelActive(ChannelNames.Intensity))
                    {
                        otherCamera.DeactivateChannel(ChannelNames.Intensity);
                    }
                    otherCamera.ActivateChannel(ChannelNames.Color);
                    otherCamera.ActivateChannel(ChannelNames.Point3DImage);
                    otherCamera.ActivateChannel(ChannelNames.ZImage);
                    //otherCamera.IRFlooderEnabled = false;
                    //otherCamera.SetEmitterStatusAndWait(true);

                    // Set camera to Idle mode
                    //camera.IRFlooderEnabled = false;
                    //camera.SetEmitterStatusAndWait(false);
                    // Set otherCamera to Idle mode
                    //otherCamera.IRFlooderEnabled = false;
                    //otherCamera.SetEmitterStatusAndWait(false);

                    // Extra
                    //camera.SetEmitterStatusAndWait(false);
                    //otherCamera.SetEmitterStatusAndWait(false);

                    sw.Start();
                    for (int i = 0; i < 10; i++)
                    {
                        for (int c = 0; c < cameras.Length; c++)
                        {
                            AstraOpenNI curr = cameras[c];

                            SetAcquisitionMode(curr, AcquisitionModes.ColorAndZImage);
                            System.Threading.Thread.Sleep(100);
                            curr.Update();

                            FloatCameraImage rawData = (FloatCameraImage)curr.CalcChannel(ChannelNames.ZImage);
                            FloatImage fImg = new FloatImage(ref rawData);
                            Console.WriteLine($"\tMean = {fImg.ComputeMean()} (#{curr.SerialNumber})");

                            FloatCameraImage rawColorData = curr.CalcChannel(ChannelNames.Color).ToFloatCameraImage();
                            FloatImage fColorImg = new FloatImage(ref rawColorData);
                            Console.WriteLine($"\tMean (color) = {fColorImg.ComputeMean()} (#{curr.SerialNumber})");

                            SetAcquisitionMode(curr, AcquisitionModes.Idle);
                        }
                        Console.WriteLine($"\tElappsed: {sw.ElapsedMilliseconds}");
                    }
                }
            }
            #endregion

            #region Test IR Exposure
            using (camera = CreateCamera())
            {
                camera.Connect();
                camera.DeactivateChannel(ChannelNames.Color);
                camera.DeactivateChannel(ChannelNames.ZImage);
                camera.DeactivateChannel(ChannelNames.Point3DImage);
                camera.ActivateChannel(ChannelNames.Intensity);
                string channelName = ChannelNames.Intensity;

                // warm-up
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // 10 frames with current exposure
                Console.WriteLine($"Exposure is at {camera.IRExposure}.");
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                        FloatImage fImg = new FloatImage(ref rawData);
                        Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // Double exposure
                camera.IRExposure *= 2;

                // warm-up
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // 10 frames with new exposure
                Console.WriteLine($"Exposure is at {camera.IRExposure}.");
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                        FloatImage fImg = new FloatImage(ref rawData);
                        Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // Half exposure
                camera.IRExposure /= 2;
            }
            #endregion

            #region Test IR Gain
            using (camera = CreateCamera())
            {
                camera.Connect();
                camera.DeactivateChannel(ChannelNames.Color);
                camera.DeactivateChannel(ChannelNames.ZImage);
                camera.DeactivateChannel(ChannelNames.Point3DImage);
                camera.ActivateChannel(ChannelNames.Intensity);
                string channelName = ChannelNames.Intensity;

                // warm-up
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // 10 frames with current gain
                Console.WriteLine($"Gain is at {camera.IRGain}.");
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                        FloatImage fImg = new FloatImage(ref rawData);
                        Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // Double gain
                camera.IRGain *= 2;

                // warm-up
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // 10 frames with new gain
                Console.WriteLine($"Gain is at {camera.IRGain}.");
                for (int i = 0; i < 10; i++)
                {
                    camera.Update();
                    try
                    {
                        FloatCameraImage rawData = (FloatCameraImage)camera.CalcChannel(channelName);
                        FloatImage fImg = new FloatImage(ref rawData);
                        Console.WriteLine($"\tMean = {fImg.ComputeMean()}");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(string.Format("Error getting channel {0}: {1}.", channelName, ex.Message));
                    }
                }

                // Half gain
                camera.IRGain /= 2;
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

        private static AstraOpenNI CreateCamera(string serial = "18042730138")
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

            camera.SerialNumber = serial; // 18042730138 18042730319
            return camera;
        }

        private enum AcquisitionModes
        {
            Unknown,
            Idle,
            IntensityImage,
            ColorAndZImage,
        }

        private const uint _defaultEmitterDelay = 300;
        public static uint EmitterDelay { get => _defaultEmitterDelay; }
        private static AcquisitionModes _currentAcquisitionMode = AcquisitionModes.Unknown;
        private static void SetAcquisitionMode(AstraOpenNI camera, AcquisitionModes mode)
        {
            if (mode == _currentAcquisitionMode)
            {
                return;
            }

            switch (mode)
            {
                case AcquisitionModes.ColorAndZImage:
                    // Color+Depth is ok, just make sure that Intensity is off
                    if (camera.IsChannelActive(ChannelNames.Intensity))
                    {
                        camera.DeactivateChannel(ChannelNames.Intensity);
                    }

                    camera.ActivateChannel(ChannelNames.Color);
                    camera.ActivateChannel(ChannelNames.Point3DImage);
                    camera.ActivateChannel(ChannelNames.ZImage);

                    //camera.IRFlooderEnabled = false;
                    //camera.SetEmitterStatusAndWait(true);
                    break;
                case AcquisitionModes.Idle:
                    // don't care about channels, just switch off Emitter and Flooder
                    //camera.IRFlooderEnabled = false;
                    //camera.SetEmitterStatusAndWait(false);
                    break;
                case AcquisitionModes.IntensityImage:
                    // Intensity does not support any other active channels
                    ActivateChannels(camera, new HashSet<string>() { ChannelNames.Intensity });
                    camera.IRFlooderEnabled = true;
                    camera.EmitterEnabled = false;
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(EmitterDelay));
                    break;
            }
            _currentAcquisitionMode = mode;
        }

        /// <summary>
        /// Activates only the given <paramref name="channels"/> and deactivates all other.
        /// </summary>
        /// <param name="channels"></param>
        private static void ActivateChannels(AstraOpenNI camera, HashSet<string> channels)
        {
            HashSet<string> copy = new HashSet<string>(channels);
            for (int j = camera.ActiveChannels.Count - 1; j >= 0; --j)
            {
                string channelName = camera.ActiveChannels[j].Name;
                if (!copy.Remove(channelName))
                {
                    camera.DeactivateChannel(channelName);
                }
            }

            foreach (string channelName in copy)
            {
                camera.ActivateChannel(channelName);
            }
        }
    }
}
