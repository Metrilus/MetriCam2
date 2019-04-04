using MetriX.Cameras.Debug;
using MetriX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriX.Debug
{
    public class MockEngine
    {
        private List<CameraBase> _cameras = null;
        public IList<CameraBase> Cameras
        {
            get { return _cameras; }
        }

        ParallelOptions _frameAcquisitionOptions;
        public ParallelOptions FrameAcquisitionOptions
        {
            get => _frameAcquisitionOptions;
        }

        private CalibrationParameters _calibrationParameters;
        public CalibrationParameters CalibrationParameters
        {
            get => _calibrationParameters;
        }

        public MockEngine(int warmupSamples = 6)
        {
            _frameAcquisitionOptions = new ParallelOptions();
            _calibrationParameters = new CalibrationParameters()
            {
                Checkerboard = new CalibrationParameters.CheckerboardParameters()
                {
                    UseColourFrame = true,
                }
            };
            byte irGain = 16;
            uint irExposure = 1500;
            uint emitterDelay = 250;
            OrbbecAstra astra0 = new OrbbecAstra()
            {
                IRGain = irGain,
                IRExposure = irExposure,
                EmitterDelay = emitterDelay,
                Serial = "18042730629",
            };
            OrbbecAstra astra1 = new OrbbecAstra()
            {
                IRGain = irGain,
                IRExposure = irExposure,
                EmitterDelay = emitterDelay,
                Serial = "18042630416",
            };
            _cameras = new List<CameraBase>();
            _cameras.Add(astra0);
            _cameras.Add(astra1);
        }

        public DisplayFrame[] AcquireDisplayFrames(FramePurposes purposes = FramePurposes.Display)
        {
            try
            {
                DisplayFrame[] displayFrames = new DisplayFrame[_cameras.Count];
                //Parallel.For(0, _cameras.Count, _frameAcquisitionOptions, (int cameraIndex) =>
                for(int cameraIndex = 0; cameraIndex < _cameras.Count; cameraIndex++)
                {
                    // Acquire the Frame
                    SafeAcquireFrame(_cameras[cameraIndex], purposes, out displayFrames[cameraIndex]);
                }
                //);

                return displayFrames;
            }
            catch (Exception exception)
            {
                if (FlattenDeviceException(exception, out DeviceException deviceException))
                {
                    throw deviceException;
                }
                else throw;
            }
        }

        private static void SafeAcquireFrame(CameraBase camera, FramePurposes purposes, out DisplayFrame displayFrame)
        {
            try
            {
                camera.AcquireFrame(purposes, out displayFrame);
            }
            catch (CameraException)
            {
                throw;
            }
            catch (Exception unhandledException)
            {
                throw new NotImplementedException(camera.ToString() + $": An unexpected error occurred while attempting to acquire camera data:{Environment.NewLine}{unhandledException.Message}", unhandledException);
            }
        }

        private bool FlattenDeviceException(Exception exception, out DeviceException deviceException)
        {
            // Return false for DeviceException instances so that they can be re-thrown with original stack-trace information
            if (exception is DeviceException de)
            {
                deviceException = de;
                return false;
            }
            else return DeviceException.TryFlattenDeviceException(exception, out deviceException);
        }
    }
}
