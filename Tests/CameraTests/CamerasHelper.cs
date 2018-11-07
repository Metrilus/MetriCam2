using Metrilus.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetriCam2.CameraTests
{
    public static class CamerasHelper
    {
        private static MetriLog _log = new MetriLog();
        private static List<Camera> _connectableCameras = null;

        /// <summary>
        /// Returns an instance of each camera type.
        /// </summary>
        /// <remarks>
        /// TODO:
        /// extend return type:
        ///    * include list of tests which should be skipped for that camera
        /// </remarks>
        public static IEnumerable<Camera> AllCameras
        {
            get
            {
                if (null == _connectableCameras)
                {
                    _connectableCameras = new List<Camera>();
                    CameraManagement.ScanForCameraDLLs = true;
                    _connectableCameras = CameraManagement.GetConnectableCameras().Where(c => "CameraTemplate" != c.Name).ToList();
                    if (0 == _connectableCameras.Count)
                    {
                        _log.Info("No connectable cameras found");
                    }
                }

                foreach (var item in _connectableCameras)
                {
                    yield return item;
                }
            }
        }

        public static Camera GetNewInstanceOf(Camera cam)
        {
            var t = cam.GetType();
            var ctor = t.GetConstructor(new Type[] { });
            return (Camera)ctor.Invoke(new object[] { });
        }
    }
}
