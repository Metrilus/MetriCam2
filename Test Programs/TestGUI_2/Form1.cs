using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MetriCam2;
using MetriCam2.Cameras;
using Metrilus.Logging;
using Metrilus.Util;

namespace TestGUI_2
{
    public partial class Form1 : Form
    {
        #region Types
        private delegate void SetBmpDelegate(Bitmap b, uint position);
        private enum ImagePosition
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
        }
        #endregion

        #region Private Fields
        private IAsyncResult setBMPResult;
        private static List<Camera> cameras = new List<Camera>();
        private MetriLog log;
        #endregion

        #region Public Methods
        public Form1()
        {
            InitializeComponent();

            log = new MetriLog();

            this.FormClosing += Form1_FormClosing;
            int margin = 20;
            int halfWidth = (panelContainerLeft.Parent.Width - margin) / 2;
            panelContainerLeft.Width = halfWidth;
            panelContainerRight.Width = halfWidth;
            panelTL.Height = (panelTL.Parent.Height - margin) / 2;
            panelTR.Height = (panelTR.Parent.Height - margin) / 2;
            panelBL.Height = (panelBL.Parent.Height - margin) / 2;
            panelBR.Height = (panelBR.Parent.Height - margin) / 2;
        }
        #endregion

        #region Testing Methods
#if WIN32
// Odos has no 64-bit version, yet
        private void TestOdos()
        {
            // connect by serial number
            {
                Odos cam = new Odos();
                cam.SerialNumber = "192.168.1.129";
                cam.Connect();
                cam.Update();
                cam.Disconnect();
            }

            // test scan for cameras
            {
                List<string> list = Odos.ScanForCameras();
                foreach (string ip in list)
                {
                    Console.WriteLine("Found camera: " + ip);
                }
            }

            // grab images
            {
                Odos cam = new Odos();
                cam.Connect();
                cameras.Add(cam);

                bgWorkerGetFrames.RunWorkerAsync();
            }
        }
#endif

#if !WIN64
        private void TestVoxelA()
        {
            TIVoxelA cam = new TIVoxelA();
            cam.Connect();
            float temp = cam.SensorTemperature;
            Console.WriteLine("Current Sensor Temperature of Camera is: {0}.", temp);
            cameras.Add(cam);
            // show some frames
            bgWorkerGetFrames.RunWorkerAsync();
            //cam.Disconnect();
        }
#endif
        //private void TestBltCLI()
        //{
        //    // ip: 192.168.1.146
        //    BluetechnixCLI cam = new BluetechnixCLI();
        //    cam.IPAddress = "192.168.1.146";
        //    cam.Connect();
        //    Console.WriteLine("Got camera: {0}", cam.SerialNumber);
        //    cam.MedianFilter = true;
        //    cam.FrameAverageFilter = true;
        //    cam.BilateralFilter = true;
        //    cameras.Add(cam);
        //    cam.ActivateChannel(ChannelNames.Color);
        //    cam.SelectChannel(ChannelNames.Color);
        //    bgWorkerGetFrames.RunWorkerAsync();
        //}

        private void TestV3S()
        {
            VisionaryT cam = new VisionaryT();
            cam.IPAddress = "192.168.1.137"; // DHCP
            cam.Connect();


            // get distance frame
            {
                cam.ActivateChannel(ChannelNames.Distance);
                cam.SelectChannel(ChannelNames.Distance);
                cam.Update();
                FloatCameraImage distances = (FloatCameraImage)cam.CalcSelectedChannel();
                float min = distances[0];
                float max = distances[0];
                float mean = 0F;
                float mid = distances[(int)(cam.Width * (cam.Height / 2) + cam.Width / 2)];
                for (int i = 0; i < cam.Height; ++i)
                {
                    for (int j = 0; j < cam.Width; ++j)
                    {
                        float val = distances[i, j];
                        if (val < min)
                            min = val;
                        if (val > max)
                            max = val;
                        mean += val;
                    }
                }
                mean /= cam.Height * cam.Width;
                Console.WriteLine("Distances: Min: {0}m  Max: {1}m Mean: {2}m Mid: {3}m", min, max, mean, mid);
            }
            cam.ActivateChannel(ChannelNames.Intensity);
            cam.SelectChannel(ChannelNames.Intensity);
            cameras.Add(cam);
            bgWorkerGetFrames.RunWorkerAsync();
            //cam.Disconnect();
        }

        private void RunTest(String title, Action testMethod)
        {
            cameras.Clear();
            log.DebugFormat("\n"
                + "=========================\n"
                + "Test: {0}\n"
                + "=========================\n",
                title
                );
            testMethod.Method.Invoke(this, new object[] { });
        }

        private void TestUEye()
        {
            // test simple connect and disconnect
            {
                UEyeCamera cam = new UEyeCamera();
                cam.Connect();
                Console.WriteLine("Got device! SerialNumber: {0}", cam.SerialNumber);

                cam.Disconnect();
            }

            // test simple connect by serial number
            {
                UEyeCamera cam = new UEyeCamera();
                cam.SerialNumber = "4102702579";
                cam.Connect();
                Console.WriteLine("Got device! SerialNumber: {0}", cam.SerialNumber);
                if (!cam.SerialNumber.Equals("4102702579"))
                {
                    Console.Error.WriteLine("Got wrong device!");
                    Environment.Exit(-1);
                }

                cam.Disconnect();
            }

            // connect two cameras at the same time and show the results
            //{
            //    UEyeCamera cam0 = new UEyeCamera();
            //    UEyeCamera cam1 = new UEyeCamera();
            //    cam0.Connect();
            //    cam1.Connect();
            //    Console.WriteLine("Got first device! SerialNumber: {0}", cam0.SerialNumber);
            //    Console.WriteLine("Got second device! SerialNumber: {0}", cam1.SerialNumber);

            //    cameras.Add(cam0);
            //    cameras.Add(cam1);

            //    // show some frames
            //    bgWorkerGetFrames.RunWorkerAsync();
            //}

            // try connect by incorrect serialnumber
            {
                UEyeCamera cam = new UEyeCamera();
                cam.SerialNumber = "120025";
                try
                {
                    cam.Connect();
                    Console.Error.WriteLine("Huh, connect was successful?");
                    Environment.Exit(-1);
                }
                catch (Exception)
                { ; }
            }

            // connect while connected
            {
                UEyeCamera cam = new UEyeCamera();
                cam.Connect();
                try
                {
                    cam.Connect();
                    Console.Error.WriteLine("Huh, double connect was sucessful?");
                    Environment.Exit(-1);
                }
                catch (Exception)
                { ; }
                cam.Disconnect();
            }

            // test reconnect
            {
                UEyeCamera cam = new UEyeCamera();

                cam.Disconnect();
                cam.Connect();
                cam.Disconnect();
                cam.Connect();
                cam.Disconnect();
            }

            // connect one camera and show results
            {
                UEyeCamera cam = new UEyeCamera();
                cam.Connect();
                Console.WriteLine("Got device! SerialNumber: {0}", cam.SerialNumber);

                cameras.Add(cam);

                // show some frames
                bgWorkerGetFrames.RunWorkerAsync();
            }
        }
        #endregion

        #region GUI Methods
        private void InvokeSetBmp(Bitmap bmp, uint position = (uint)ImagePosition.TopLeft)
        {
            if (setBMPResult == null || setBMPResult.IsCompleted)
            {
                try
                {
                    setBMPResult = this.BeginInvoke(new SetBmpDelegate(this.SetImage), bmp, position);
                }
                catch
                {
                    // due to raceconditions...
                    return;
                }
            }
        }

        private void SetImage(Bitmap bitmap, uint position)
        {
            PictureBox picBoxDest = null;
            switch (position)
            {
                case 0:
                    picBoxDest = pictureBoxTL;
                    break;
                case 1:
                    picBoxDest = pictureBoxTR;
                    break;
                case 2:
                    picBoxDest = pictureBoxBL;
                    break;
                case 3:
                    picBoxDest = pictureBoxBR;
                    break;
            }
            Bitmap oldBitmap = (Bitmap)picBoxDest.Image;
            picBoxDest.Image = bitmap;
            if (oldBitmap != null && oldBitmap != bitmap)
                oldBitmap.Dispose();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bgWorkerGetFrames.IsBusy)
                bgWorkerGetFrames.CancelAsync();
        }

        private void bgWorkerGetFrames_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!bgWorkerGetFrames.CancellationPending)
            {
                // capture a new frame in each camera
                foreach (var cam in cameras)
                {
                    if (null == cam)
                    {
                        continue;
                    }

                    cam.Update();
                }

                uint posIdx = 0;
                foreach (var cam in cameras)
                {
                    // get the current frame
                    Bitmap bitmap = cam.CalcSelectedChannel().ToBitmap();
                    // set the picturebox-bitmap in the main thread to avoid concurrency issues (a few helper methods required, easier/nicer solutions welcome).
                    this.InvokeSetBmp(bitmap, posIdx++);
                }
            }
        }

        private void bgWorkerGetFrames_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (cameras.Count < 1 || null == cameras[0])
                return;

            Camera cam = cameras[0];
            // disconnect camera and re-enable button.
            cam.Disconnect();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //TestKinect2();
        }
        #endregion
    }
}
