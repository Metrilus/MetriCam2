using Metrilus.Logging;
using NUnit.Framework;
using System;
using System.Linq;

namespace MetriCam2.CameraTests
{
    [TestFixture]
    public class LiveCameraTests
    {
        private static MetriLog _log = new MetriLog();

        /// <summary>
        /// Tests Connect and Disconnect.
        /// </summary>
        /// <param name="cam"></param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void ConnectTest(Camera cam)
        {
            _log.Debug(nameof(ConnectTest));
            cam.Connect();
            cam.Disconnect();
        }

        /// <summary>
        /// Tests repeated Connect to same camera object.
        /// </summary>
        /// <param name="cam"></param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void DoubleConnectTest1(Camera cam)
        {
            _log.Debug(nameof(DoubleConnectTest1));
            cam.Connect();
            Assert.Throws<InvalidOperationException>(() => cam.Connect());
            cam.Disconnect();
        }

        /// <summary>
        /// Tests repeated Connect using a second camera object.
        /// </summary>
        /// <param name="cam"></param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void DoubleConnectTest2(Camera cam)
        {
            _log.Debug(nameof(DoubleConnectTest2));
            var secondCam = CamerasHelper.GetNewInstanceOf(cam);

            cam.Connect();
            secondCam.Connect();

            // Test if both instances work
            cam.Update();
            secondCam.Update();

            cam.Disconnect();
            secondCam.Disconnect();
        }

        /// <summary>
        /// Tests repeated Connect using a second camera object after the first one has been GC'd.
        /// </summary>
        /// <param name="cam"></param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void DoubleConnectTest3(Camera cam)
        {
            _log.Debug(nameof(DoubleConnectTest3));
            var secondCam = CamerasHelper.GetNewInstanceOf(cam);

            cam.Connect();
            cam = null;
            GC.Collect();

            secondCam.Connect();
            secondCam.Disconnect();
        }

        /// <summary>
        /// Tests repeated Disconnect to same camera object.
        /// </summary>
        /// <param name="cam"></param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void DoubleDisconnectTest(Camera cam)
        {
            _log.Debug(nameof(DoubleDisconnectTest));
            cam.Connect();
            cam.Disconnect();
            cam.Disconnect();
        }

        /// <summary>
        /// Tests repeated Connect and Disconnect.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="numberOfCycles">Number of connect/disconnect cycles to perform (default: 3).</param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void ReconnectTest(Camera cam)
        {
            _log.Debug(nameof(ReconnectTest));
            const int numberOfCycles = 3;
            for (int i = 0; i < numberOfCycles; i++)
            {
                _log.Debug($"ReconnectTest cycle {i}");
                cam.Connect();
                cam.Disconnect();
            }
        }

        /// <summary>
        /// Tests Update (but does not Calc any channels).
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="numberOfUpdates">Nubmer of calls to Update (default: 10).</param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void UpdateTest(Camera cam)
        {
            _log.Debug(nameof(UpdateTest));
            const int numberOfUpdates = 10;
            cam.Connect();
            for (int i = 0; i < numberOfUpdates; i++)
            {
                _log.Debug($"UpdateTest cycle {i}");
                cam.Update();
            }
            cam.Disconnect();
        }

        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void SettingsTest(Camera cam)
        {
            _log.Debug(nameof(SettingsTest));
            var allParameters = cam.GetParameters();

            // TODO: add tests for parameters which can be read/written while disconnected

            cam.Connect();

            // TODO: add tests for parameters (all)

            cam.Disconnect();
        }

        /// <summary>
        /// Tests to activate and deactivate all channels.
        /// </summary>
        /// <param name="cam"></param>
        [TestCaseSource(typeof(CamerasHelper), nameof(CamerasHelper.AllCameras))]
        public void ChannelActivationTest(Camera cam)
        {
            _log.Debug(nameof(ChannelActivationTest));

            // Set up: connect and deactivate all channels
            cam.Connect();
            var activeChannelNames = cam.ActiveChannels.Select(cd => cd.Name).ToList();
            activeChannelNames.ForEach(name => cam.DeactivateChannel(name));

            foreach (var chanDesc in cam.Channels)
            {
                var name = chanDesc.Name;
                cam.ActivateChannel(name);
                cam.Update();
                cam.CalcChannel(name);
                cam.DeactivateChannel(name);
            }

            cam.Disconnect();
        }
    }
}
