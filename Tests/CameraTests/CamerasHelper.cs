using MetriCam2;
using MetriCam2.Cameras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.CameraTests
{
    public static class CamerasHelper
    {
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
                CameraManagement.ScanForCameraDLLs = true;
                var cameras = CameraManagement.GetConnectableCameras();
                foreach (var item in cameras)
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
