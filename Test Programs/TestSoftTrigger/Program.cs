using MetriCam2.Cameras;
using Metrilus.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestSoftTrigger
{
    class Program
    {
        private static MetriLog log = new MetriLog("TestSoftTrigger");

        static void Main(string[] args)
        {
            log.SetLogLevel(log4net.Core.Level.Debug);

            SVS cam = new SVS();
            try
            {
                cam.Connect();
            }
            catch (MetriCam2.Exceptions.ConnectionFailedException e)
            {
                log.FatalFormat("Could not connect to camera: {0}", e.Message);
                return;
            }
            log.InfoFormat("Camera connected: {0} (S/N {1})", cam.Name, cam.SerialNumber);

            //log.DebugFormat("activating software trigger (current setting: {0})", cam.AcquisitionMode);
            cam.SetParameter("AutoGain", false);
            //log.DebugFormat("activated software trigger (current setting: {0})", cam.AcquisitionMode);

            cam.SetParameter("Exposure", 5.0f * 1000f);
            log.DebugFormat("exposure: {0}", cam.Exposure);

            log.DebugFormat("activating software trigger (current setting: {0})", cam.AcquisitionMode);
            cam.AcquisitionMode = MetriCam2.Cameras.Internal.SVS.GigeApi.ACQUISITION_MODE.ACQUISITION_MODE_SOFTWARE_TRIGGER;
            log.DebugFormat("activated software trigger (current setting: {0})", cam.AcquisitionMode);

            Console.WriteLine("Press Esc to quit. Press any other key to capture a frame.");
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                cam.Update();
                log.InfoFormat("Updated camera. Frame number is {0}", cam.FrameNumber);
                //cam.CalcChannel()
            }

            cam.Disconnect();
            log.Info("Camera disconnected");
        }
    }
}
