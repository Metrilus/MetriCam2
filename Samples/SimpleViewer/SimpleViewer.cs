// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Logging;
using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing.Imaging;

namespace MetriCam2.Samples.SimpleViewer
{
    public partial class SimpleViewer : Form
    {
        private const string txtConnect = "Connect Camera";
        private const string txtDisconnect = "Disconnect Camera";

        private static MetriLog log = new MetriLog("SimpleViewer");

        private Camera cam = null;
        private AutoResetEvent isBgwFinished = new AutoResetEvent(false);
        private bool saveSnapshot = false;

        /// <summary>
        /// Initializes camera and parses configuration to set camera parameters.
        /// </summary>
        /// <remarks>Different parameters are separated by semicolon (;), entries for list-parameters are separated by vertical hyphen (|).</remarks>
        public SimpleViewer()
        {
            InitializeComponent();
            // load camera DLL
            try
            {
                CameraManagement.ScanForCameraDLLs = false;
                CameraManagement.ScanAssembly(Properties.Settings.Default.CameraDLLPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load camera library " + Properties.Settings.Default.CameraDLLPath + Environment.NewLine + ex.Message, "Error");
                buttonConfigure.Enabled = false;
                buttonConnect.Enabled = false;
                return;
            }
            // construct camera object
            string dummy;
            try
            {
                cam = CameraManagement.GetCameraInstanceByName(Properties.Settings.Default.CameraName, out dummy);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not initialize camera driver." + Environment.NewLine + ex.Message, "Error");
                buttonConfigure.Enabled = false;
                buttonConnect.Enabled = false;
                return;
            }
            buttonConnect.Text = txtConnect;
            try
            {
                // set pre-connect parameters
                string[] preConnectSettings = Properties.Settings.Default.PreConnectParameters.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string preConnectSetting in preConnectSettings)
                {
                    string[] settingNameValuePair = preConnectSetting.Split('=');
                    MetriCam2.Camera.ParamDesc param = cam.GetParameter(settingNameValuePair[0]);
                    if (param.Type == typeof(string))
                    {
                        cam.SetParameter(settingNameValuePair[0], settingNameValuePair[1]);
                    }
                    else if (param.Type == typeof(List<string>))
                    {
                        List<string> paramValue = new List<string>(settingNameValuePair[1].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
                        cam.SetParameter(settingNameValuePair[0], paramValue);
                    }
                    else
                    {
                        // if this doesn't work, additional conversion should be tried.
                        cam.SetParameter(settingNameValuePair[0], settingNameValuePair[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Setting pre-connect parameters failed." + Environment.NewLine + ex.Message, "Error");
                buttonConfigure.Enabled = false;
                buttonConnect.Enabled = false;
                return;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            buttonConnect.Enabled = false;
            try
            {
                if (!cam.IsConnected)
                {
                    ConnectCamera();

                    if (!cam.IsConnected)
                    {
                        return;
                    }

                    buttonSnapshot.Enabled = true;
                    backgroundWorker.RunWorkerAsync();
                }
                else
                {
                    buttonSnapshot.Enabled = false;
                    StopBackgroundWorker();
                }
            }
            catch (Exception ex)
            {
                // Failure -> Reset BackgroundWorker and GUI
                if (backgroundWorker.IsBusy)
                {
                    StopBackgroundWorker();
                }
                else
                {
                    DisconnectCamera();
                }
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void buttonConfigure_Click(object sender, EventArgs e)
        {
            MetriCam2.Controls.CameraConfigurationDialog diag = new MetriCam2.Controls.CameraConfigurationDialog(cam);
            diag.ShowDialog();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime lastSecond = DateTime.Now;
            int fps = 0;
            while (!backgroundWorker.CancellationPending)
            {
                cam.Update();

                CameraImage camImg = cam.CalcSelectedChannel();
                if (null == camImg)
                {
                    // ignore errors. However, this might be a hint that something is wrong in your application.
                    continue;
                }

                if (camImg is FloatCameraImage && (cam.SelectedChannel == ChannelNames.Distance || cam.SelectedChannel == ChannelNames.ZImage))
                {
                    TrimDepthImage((FloatCameraImage)camImg);
                }

                Bitmap bmp= camImg.ToBitmap();

                if (saveSnapshot)
                {
                    string snapName = "MetriCam 2 Snapshot.png";
                    string snapFilename = Path.GetTempPath() + snapName;
                    bmp.Save(snapFilename);
                    MessageBox.Show(string.Format("Snapshot saved as '{0}'.", snapFilename), "Snapshot saved", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    saveSnapshot = false;
                }
                this.BeginInvokeEx(f => pictureBox.Image = bmp);

                fps++;
                DateTime now = DateTime.Now;
                if(now - lastSecond > new TimeSpan(0, 0, 1))
                {
                    int fpsCopy = fps;
                    this.BeginInvokeEx(f => labelFps.Text = $"{fpsCopy} fps");
                    lastSecond = now;
                    fps = 0;
                }

            }
            DisconnectCamera();
            isBgwFinished.Set();
        }

        private static void TrimDepthImage(FloatCameraImage img)
        {
            float minDepth = Properties.Settings.Default.MinDepthToDisplay;
            float maxDepth = Properties.Settings.Default.MaxDepthToDisplay;
            for (int i = 0; i < img.Length; i++)
            {
                if (img[i] < minDepth)
                {
                    img[i] = minDepth;
                }

                if (img[i] > maxDepth)
                {
                    img[i] = maxDepth;
                }
            }
        }

        private unsafe static Bitmap ToBitmap(FloatCameraImage img)
        {
            if (img.Data == null)
            {
                return null;
            }
            float maxVal = float.MinValue;
            float minVal = float.MaxValue;

            fixed (float* imgData = img.Data)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    float* dataPtr = imgData + y * img.Stride;
                    for (int x = 0; x < img.Width; x++)
                    {
                        float val = *dataPtr++;
                        if (val > maxVal)
                        {
                            maxVal = val;
                        }
                        if (val < minVal)
                        {
                            minVal = val;
                        }
                    }
                }

                maxVal = 0.9f * maxVal;
                for (int y = 0; y < img.Height; y++)
                {
                    float* dataPtr = imgData + y * img.Stride;
                    for (int x = 0; x < img.Width; x++)
                    {
                        if (*dataPtr > maxVal)
                        {
                            *dataPtr = maxVal;
                        }
                        dataPtr++;
                    }
                }
                Bitmap bitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
                if (maxVal == minVal)
                {
                    // avoid division by zero.
                    return bitmap;
                }
                Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
                BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                byte* bmpPtr = (byte*)bitmapData.Scan0;
                for (int y = 0; y < img.Height; y++)
                {
                    byte* linePtr = bmpPtr + bitmapData.Stride * y;
                    float* dataPtr = imgData + y * img.Stride;
                    for (int x = 0; x < img.Width; x++)
                    {
                        byte value = (byte)(byte.MaxValue * (*dataPtr++ - minVal) / (maxVal - minVal));
                        *linePtr++ = value;
                        *linePtr++ = value;
                        *linePtr++ = value;
                    }
                }
                bitmap.UnlockBits(bitmapData);
                return bitmap;
            }           
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
        }

        private void StopBackgroundWorker()
        {
            backgroundWorker.CancelAsync();
            isBgwFinished.WaitOne();
        }

        private void ConnectCamera()
        {
            try
            {
                cam.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed." + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message, "Error");
                DisconnectCamera();
                return;
            }

            if (string.IsNullOrWhiteSpace(cam.SelectedChannel))
            {
                // To be flexible, test several channels and select the first existing one.
                string[] preferedChannels = new string[]
                {
                    ChannelNames.Color,
                    ChannelNames.Intensity,
                    ChannelNames.Amplitude,
                };
                foreach (var channelName in preferedChannels)
                {
                    try
                    {
                        if (cam.IsChannelActive(channelName))
                        {
                            cam.SelectChannel(channelName);
                            break;
                        }
                    }
                    catch { /* empty */ }
                }
            }

            this.BeginInvokeEx((f) =>
            {
                buttonConnect.Text = txtDisconnect;
                buttonConnect.Enabled = true;
            });
        }

        private void DisconnectCamera()
        {
            if (null != cam && cam.IsConnected)
            {
                try
                {
                    cam.Disconnect();
                }
                catch { /* empty */ }
            }

            if (!IsDisposed)
            {
                this.BeginInvokeEx((f) =>
                {
                    buttonConnect.Text = txtConnect;
                    buttonConnect.Enabled = true;
                });
            }
        }

        private void SimpleViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                StopBackgroundWorker();
            }
            else
            {
                DisconnectCamera();
            }
        }

        private void buttonSnapshot_Click(object sender, EventArgs e)
        {
            saveSnapshot = true;
        }
    }
}
