// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2;
using MetriCam2.Cameras;
using MetriCam2.Exceptions;
using Metrilus.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MetriCam2.Tests.TestCameraSettings
{
    class Program
    {
        static Camera cam;
        static MetriLog log = new MetriLog();

        static void Main(string[] args)
        {
            log.LogLevel = MetriLog.Levels.Info;

            cam = new UEyeCamera(); // Change this type if you want to test your camera implementation.
            List<Camera.ParamDesc> allParameters;

            log.Info("Testing " + cam.Name);

            allParameters = cam.GetParameters();
            log.InfoFormat("Camera {0} has {1} parameter(s):\n{2}", cam.Name, allParameters.Count, Camera.ParamDesc.ToString(allParameters));


            // TEST: setting a writable parameter while disconnected
            if (cam is UEyeCamera)
            {
                TestSetParameterSuccess("TriggerMode", "SOFTWARE");
            }

            // TEST: setting a non-writable parameter while disconnected
            TestSetParameterDisconnected("Gain", 40);

            cam.Connect();

            log.InfoFormat("Connected {0} camera with S/N \"{1}\".", cam.Name, cam.SerialNumber);

            allParameters = cam.GetParameters();
            log.InfoFormat("Camera {0} has {1} parameter(s):\n{2}", cam.Name, allParameters.Count, Camera.ParamDesc.ToString(allParameters));

            if (cam is UEyeCamera)
            {
                // TEST: setting a list parameter to a valid value.
                TestSetParameterSuccess("TriggerMode", "HARDWARE");

                // TEST: setting a list parameter to an invalid value.
                TestSetParameterToInvalid("TriggerMode", "---");
            }

            // TEST: setting a range parameter to a valid value.
            TestSetParameterSuccess("Gain", 40);

            // TEST: setting a range parameter to an invalid value.
            TestSetParameterToInvalid("Gain", 305);

            // TEST: setting an Auto* parameter to a valid value
            TestSetParameterSuccess("AutoGain", true);

            // TEST: setting an Auto* parameter to an invalid value
            TestSetParameterToInvalid("AutoGain", 13);

            Dictionary<string, object> params1 = new Dictionary<string, object>();
            params1["Gain"] = 40;
            params1["AutoGain"] = true;
            if (cam is UEyeCamera)
            {
                params1["TriggerMode"] = "FREERUN";
            }
            cam.SetParameters(params1);
            List<Camera.ParamDesc> allParameters1 = cam.GetParameters();
            log.InfoFormat("Camera {0} has {1} parameter(s):\n{2}", cam.Name, allParameters1.Count, Camera.ParamDesc.ToString(allParameters1));

            Dictionary<string, object> params2 = new Dictionary<string, object>();
            params2["Gain"] = 40;
            params2["AutoGain"] = false;
            if (cam is UEyeCamera)
            {
                params2["TriggerMode"] = "FREERUN";
            }
            cam.SetParameters(params2);
            List<Camera.ParamDesc> allParameters2 = cam.GetParameters();
            log.InfoFormat("Camera {0} has {1} parameter(s):\n{2}", cam.Name, allParameters2.Count, Camera.ParamDesc.ToString(allParameters2));

            cam.Disconnect();

            log.Info("Press any key to close.");
            Console.ReadKey();
        }

        private static void TestSetParameterToInvalid(string name, object value)
        {
            try
            {
                cam.SetParameter(name, value);
                log.ErrorFormat("Setting {0} to \"{1}\" should have thrown an Exception!", name, value);
            }
            catch (ArgumentException)
            {
                log.InfoFormat("Setting {0} to \"{1}\" expectedly caused an error.", name, value);
            }
            catch (ParameterNotSupportedException)
            {
                log.InfoFormat("Setting {0} to \"{1}\" caused an unexpected error." + Environment.NewLine
                    + "Probably the camera does not support this parameter.", name, value);
                return;
            }
            LogParameterValue(name);
        }
        private static void TestSetParameterSuccess(string name, object value)
        {
            try
            {
                cam.SetParameter(name, value);
            }
            catch (ParameterNotSupportedException)
            {
                log.InfoFormat("Setting {0} to \"{1}\" while being connected caused an unexpected error." + Environment.NewLine
                    + "Probably the camera does not support this parameter.", name, value);
                return;
            }
            LogParameterValue(name);
        }
        private static void TestSetParameterDisconnected(string name, object value)
        {
            try
            {
                cam.SetParameter(name, value);
                log.ErrorFormat("Setting \"{0}\" while being disconnected should have thrown an Exception!", name);
            }
            catch (ParameterNotSupportedException)
            {
                log.InfoFormat("Setting {0} to \"{1}\" while being disconnected caused an unexpected error." + Environment.NewLine
                    + "Probably the camera does not support this parameter.", name, value);
                return;
            }
            catch (InvalidOperationException)
            {
                log.InfoFormat("Setting \"{0}\" while being disconnected expectedly caused an error.", name);
            }
            LogParameterValue(name);
        }

        private static void LogParameterValue(string name)
        {
            log.Info(cam.GetParameter(name));
            if (Camera.ParamDesc.IsAutoParameterName(name))
            {
                log.Info(cam.GetParameter(Camera.ParamDesc.GetBaseParameterName(name)));
            }
        }
    }
}
