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

            Kinect4Azure camera = new Kinect4Azure();
            camera.Connect();
            camera.Update();

            FloatCameraImage fCImg = (FloatCameraImage)camera.CalcChannel(ChannelNames.ZImage);
            ProjectiveTransformationRational pTrans = (ProjectiveTransformationRational)camera.GetIntrinsics(ChannelNames.ZImage);
            Point3fCameraImage p3fImg = pTrans.ZImageToWorld(fCImg);
            PointCloud3D pc3d = new PointCloud3D(p3fImg);
            pc3d.Save("G:\\k4a.pointcloud3d");






            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
