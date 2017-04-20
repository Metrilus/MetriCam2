using MetriCam2;
using MetriCam2.Cameras;
using Metrilus.Util;
using MetriPrimitives.Data;
using MetriPrimitives.ImageProcessing;
using MetriPrimitives.Transformations;
using MetriPrimitives.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestGUI_3D
{
    public partial class Form1 : Form
    {
        private IAsyncResult setBMPResult;
        private IAsyncResult setSecondBMPResult;
        private CameraClient cam;
        private Metri3D.Metri3DPanel panel3D;
        private MetriPrimitives.Transformations.ProjectiveTransformationZhang projectiveTransformation;
        private Metri3D.Objects.RenderTriangleIndexList renderTil;

        private string channel2DName;
        private string channel3DName;

        private delegate void SetBmpDelegate(Bitmap b);

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += TestGUIForm_FormClosing;

            cam = new CameraClient("192.168.1.72", 8081, 8082, "MetriCam2.Cameras.Kinect2", "MetriCam2.Cameras.Kinect2");

            panel3D = new Metri3D.Metri3DPanel();
            panel3D.Dock = DockStyle.Fill;
            panel1.Controls.Add(panel3D);
        }

        private void TestGUIForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundWorkerGetFrames.CancelAsync();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (cam.IsConnected)
            {
                // if we are already connected, just disable the button and cancel the display thread, the actual disconnection takes place in the *_RunWorkerCompleted method.
                buttonConnect.Enabled = false;
                backgroundWorkerGetFrames.CancelAsync();
            }
            else
            {
                // connect the camera and start the display background worker.
                buttonConnect.Enabled = false;
                try
                {
                    if(cam.HasChannel(ChannelNames.Point3DImage))
                    {
                        channel3DName = ChannelNames.Point3DImage;
                    }
                    else if (cam.HasChannel(ChannelNames.Distance))
                    {
                        channel3DName = ChannelNames.Distance;
                        projectiveTransformation = new MetriPrimitives.Transformations.ProjectiveTransformationZhang((Metrilus.Util.ProjectiveTransformationZhang)cam.GetIntrinsics(ChannelNames.Distance));
                    }
                    else if (cam.HasChannel(ChannelNames.ZImage))
                    {
                        channel3DName = ChannelNames.ZImage;
                        
                    }

                    cam.ActivateChannel(channel3DName);

                    if(cam.HasChannel(ChannelNames.Amplitude))
                    {
                        channel2DName = ChannelNames.Amplitude;
                    }
                    else if(cam.HasChannel(ChannelNames.Intensity))
                    {
                        channel2DName = ChannelNames.Intensity;
                    }
                    else if(cam.HasChannel(ChannelNames.Left))
                    {
                        channel2DName = ChannelNames.Left;                        
                    }

                    cam.ActivateChannel(channel2DName);

                    cam.Connect();

                    if (!(channel3DName == ChannelNames.Point3DImage))
                    {
                        try
                        {
                            projectiveTransformation = new MetriPrimitives.Transformations.ProjectiveTransformationZhang((Metrilus.Util.ProjectiveTransformationZhang)cam.GetIntrinsics(channel3DName));
                        }
                        catch
                        {
                        }
                        if(projectiveTransformation == null)
                        {
                            projectiveTransformation = new MetriPrimitives.Transformations.ProjectiveTransformationZhang((Metrilus.Util.ProjectiveTransformationZhang)cam.GetIntrinsics(channel2DName));
                        }
                    }

                    //cam.Invoke("StopStreams", null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Connection error: " + ex.Message);
                    buttonConnect.Enabled = true;
                    return;
                }
                buttonConnect.Text = "Disconnect";
                backgroundWorkerGetFrames.RunWorkerAsync();
                buttonConnect.Enabled = true;
            }
        }

        private void backgroundWorkerGetFrames_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!backgroundWorkerGetFrames.CancellationPending)
            {
                // capture a new frame
                try
                {
                    //cam.Invoke("StartStreams", null);
                    cam.Update();
                    //cam.Invoke("StopStreams", null);
                }
                catch (Exception ex)
                {
                    GC.KeepAlive(ex);
                    cam = new CameraClient("192.168.1.72", 8081, 8082, "MetriCam2.Cameras.Kinect2", "MetriCam2.Cameras.Kinect2");
                    cam.Connect();
                    //cam.Invoke("StopStreams", null);
                }

                Point3fImage p3Image = null;
                if (channel3DName == ChannelNames.Point3DImage)
                {
                    Point3fCameraImage image3D = (Point3fCameraImage)cam.CalcChannel(ChannelNames.Point3DImage);
                    p3Image = new Point3fImage(ref image3D);
                }
                else if (channel3DName == ChannelNames.Distance || channel3DName == ChannelNames.ZImage)
                {
                    FloatCameraImage image3D = (FloatCameraImage)cam.CalcChannel(channel3DName);
                    p3Image = new Point3fImage(new FloatImage(ref image3D), projectiveTransformation, channel3DName == ChannelNames.ZImage);
                }

                FloatImage ir = ConvertToFloatImage(cam.CalcChannel(channel2DName).ToFloatCameraImage());
                Bitmap bitmap = ir.ToBitmap();
                //secondBitmap = zImage.ToBitmap();
                // set the picturebox-bitmap in the main thread to avoid concurrency issues (a few helper methods required, easier/nicer solutions welcome).
                this.InvokeSetBmp(bitmap);

                MaskImage mask = new MaskImage(p3Image.Width, p3Image.Height);
                ir = ir.Normalize();
                for (int y = 0; y < mask.Height; y++)
                {
                    for (int x = 0; x < mask.Width; x++)
                    {
                        Point3f p = p3Image[y, x];
                        if (p.X > -99f && p.X < 99f && p.Y > -99f & p.Y < 99f && p.Z > 0 && p.Z < 99f)
                        {
                            mask[y, x] = 0xff;
                        }
                    }
                }

                p3Image.EliminateFlyingPixels(5, 0.005f, 0.2f);
                p3Image.Mask = mask;

                TriangleIndexList til = new TriangleIndexList(p3Image, false, true);
                if (renderTil == null)
                {
                    renderTil = new Metri3D.Objects.RenderTriangleIndexList(til, Color.White);
                    panel3D.AddRenderObject(renderTil);
                }
                else
                {
                    renderTil.UpdateData(til, Color.White);
                }
                panel3D.Invalidate();
            }
        }

        private static FloatImage ConvertToFloatImage(FloatCameraImage fcImg)
        {
            FloatImage outImg;
            long timestamp = fcImg.TimeStamp;
            int frameNumber = fcImg.FrameNumber;
            unsafe
            {
                outImg = new FloatImage(fcImg.Width, fcImg.Height, fcImg.AbandonDataBuffer());
            }
            outImg.TimeStamp = timestamp;
            outImg.FrameNumber = frameNumber;
            return outImg;
        }


        private void InvokeSetBmp(Bitmap bmp)
        {
            if (setBMPResult == null || setBMPResult.IsCompleted)
            {
                setBMPResult = this.BeginInvoke(new SetBmpDelegate(this.SetImage), bmp);
            }
        }
        private void InvokeSetSecondBmp(Bitmap bmp)
        {
            if (setSecondBMPResult == null || setSecondBMPResult.IsCompleted)
            {
                setSecondBMPResult = this.BeginInvoke(new SetBmpDelegate(this.SetSecondImage), bmp);
            }
        }

        private void SetImage(Bitmap bitmap)
        {
            Bitmap oldBitmap = (Bitmap)pictureBoxImageStream.Image;
            pictureBoxImageStream.Image = bitmap;
            if (oldBitmap != null && oldBitmap != bitmap)
                oldBitmap.Dispose();
        }

        private void SetSecondImage(Bitmap bitmap)
        {
            Bitmap oldBitmap = (Bitmap)pictureBoxSecondImage.Image;
            pictureBoxSecondImage.Image = bitmap;
            if (oldBitmap != null && oldBitmap != bitmap)
                oldBitmap.Dispose();
        }

        private void backgroundWorkerGetFrames_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // disconnect camera and re-enable button.
            cam.Disconnect();
            buttonConnect.Text = "Connect";
            buttonConnect.Enabled = true;
        }
    }
}
