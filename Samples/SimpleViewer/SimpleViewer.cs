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
using MetriX.Cameras.Debug;
using MetriX.Models;
using System.Windows.Media.Imaging;
using System.Windows;
using MetriX.Debug;
using MetriX.Freight.Views.Debug;
using Metrilus.Presentation;
using MetriX.Utils.Logging;

namespace MetriCam2.Samples.SimpleViewer
{
    public partial class SimpleViewer : Form
    {
        private const string TxtConnect = "Connect Camera";
        private const string TxtDisconnect = "Disconnect Camera";

        private static MetriLog _log = new MetriLog("SimpleViewer");

        private bool _closing = false;
        private Thread _worker;
        private readonly object _workerLock = new Object();
        private CancellationTokenSource _workerCancelled = new CancellationTokenSource();
        private CancellationTokenSource _drawCancelled = new CancellationTokenSource();

        DateTime lastSecond = DateTime.Now;
        int fps = 0;

        //private bool _saveSnapshot = false;

        /// <summary>
        /// Initializes camera and parses configuration to set camera parameters.
        /// </summary>
        /// <remarks>Different parameters are separated by semicolon (;), entries for list-parameters are separated by vertical hyphen (|).</remarks>
        public SimpleViewer()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;

            Logs.Add(new ConsoleLog(Severity.Debug));

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
                // Load Orbbec DLL
                var cam = CameraManagement.GetCameraInstanceByName(Properties.Settings.Default.CameraName, out dummy);
                cam = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not initialize camera driver." + Environment.NewLine + ex.Message, "Error");
                buttonConfigure.Enabled = false;
                buttonConnect.Enabled = false;
                return;
            }
            buttonConnect.Text = TxtConnect;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            buttonConnect.Enabled = false;
            try
            {
                Window_StartWorker(ProcessDisplayFrames, FramePurposes.Display);
            }
            catch (Exception ex)
            {
                // Failure -> Reset BackgroundWorker and GUI
                MessageBox.Show(this, ex.Message, "Error");
                buttonConnect.Enabled = true;
            }
        }

        private void Window_StartWorker(Action<DisplayFrame[]> callback, FramePurposes purposes = FramePurposes.Display, Func<Exception, bool> exceptionHandler = null)
        {
            MockEngine engine = new MockEngine();
            foreach (var cam in engine.Cameras)
            {
                cam.Initialize(engine);
            }

            StartWorker(new WorkerArgs()
            {
                Engine = engine,
                Callback = (CancellationToken cancellationToken) =>
                {
                    var result = engine.AcquireDisplayFrames(purposes);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        callback(result);
                        return true;
                    }
                    else return false;
                },
                ExceptionHandler = exceptionHandler
            });
        }

        private void StartWorker(WorkerArgs workerArgs)
        {
            if (_closing) throw new InvalidOperationException("MetriX Window is closing");

            lock (_workerLock)
            {
                if ((null != _worker) && (_worker.IsAlive)) throw new InvalidOperationException($"{_worker.Name} is running");

                if (_drawCancelled.IsCancellationRequested)
                {
                    _drawCancelled = new CancellationTokenSource();
                }

                if (_workerCancelled.IsCancellationRequested)
                {
                    _workerCancelled = new CancellationTokenSource();
                }

                // Initialize the Worker
                _worker = new Thread((object args) => WorkerProc((WorkerArgs)args))
                {
                    IsBackground = true,
                    Name = "MetriX Freight Worker"
                };

                // Start the Worker
                _worker.Start(workerArgs);
            }
        }

        private void WorkerProc(WorkerArgs workerArgs)
        {
            try
            {
                while (!_workerCancelled.IsCancellationRequested)
                {
                    if (!workerArgs.Callback(_workerCancelled.Token))
                    {
                        _workerCancelled.Cancel();
                        break;
                    }
                }
            }
            catch (DeviceException /*deviceException*/)
            {
                _workerCancelled.Cancel();
                //Dispatcher.BeginInvoke((Action<DeviceException>)ShowExceptionView, deviceException);
            }
            catch (AggregateException aggregateException) when ((aggregateException.InnerExceptions.Count == 1) && (aggregateException.InnerException is DeviceException deviceException))
            {
                _workerCancelled.Cancel();
                //Dispatcher.BeginInvoke((Action<DeviceException>)ShowExceptionView, deviceException);
            }
            catch (Exception primaryException)
            {
                bool handled = false;
                try
                {
                    // Write Failure Snapshots if data is available
                    if ((primaryException is AlgorithmException algorithmException) && (null != algorithmException.Context))
                    {
                        //MetriXApplication.CurrentContext.SnapshotManager.WriteSnapshotAsync(workerArgs.FailureSnapshotType, workerArgs.Engine, algorithmException.Context, exception: algorithmException, namePrefix: workerArgs.FailureSnapshotPrefix);
                    }

                    // If an exception handler is registered for background-thread exceptions, call it
                    if ((null != workerArgs.ExceptionHandler) && (!_closing) && (!_workerCancelled.IsCancellationRequested))
                    {
                        handled = workerArgs.ExceptionHandler(primaryException);
                    }
                }
                catch (Exception secondaryException)
                {
                    _log.Error(secondaryException.Message);
                    throw;
                }

                // If the exception was handled, log it as a warning and suppress it. Throw it otherwise.
                if (handled)
                {
                    _log.Warn(primaryException.Message);
                }
                else throw;
            }
        }

        private void ProcessDisplayFrames(DisplayFrame[] displayFrames)
        {
            Bitmap[] bmps = new Bitmap[displayFrames.Length];
            for (int i = 0; i < displayFrames.Length; ++i)
            {
                bmps[i] = GetBitmap(displayFrames[i].BitmapSource);
            }
            this.BeginInvokeEx(t =>
            {
                pictureBox.Image = bmps[0];
            });

            fps++;
            DateTime now = DateTime.Now;
            if (now - lastSecond > new TimeSpan(0, 0, 1))
            {
                int fpsCopy = fps;
                this.BeginInvokeEx(f => labelFps.Text = $"{fpsCopy} fps");
                lastSecond = now;
                fps = 0;
            }

            //Window.InvokeUpdate((cancellationToken) =>
            //{
            //    Model.InitialiseCalibrationProgress(MetriXApplication.CurrentContext.Engine.CalibrationParameters, MetriXApplication.CurrentContext.Engine.Cameras.Count);

            //    for (int i = 0; i < displayFrames.Length; ++i)
            //    {
            //        if (i >= Model.CardsViewModel.Count)
            //        {
            //            Model.CardsViewModel.Add(new CardViewItem(displayFrames[i], $"Camera {i + 1}", "Camera View"));
            //        }
            //        else
            //        {
            //            bool maximized = Object.ReferenceEquals(Model.CardsViewModel[i].Model, Model.ActiveViewModel);
            //            Model.CardsViewModel[i].Model = displayFrames[i];
            //            if (maximized)
            //            {
            //                Model.ActiveViewModel = displayFrames[i];
            //            }

            //            Model.CardsViewModel[i].Description = "Camera View";
            //        }
            //    }
            //});

            //Model.EngineIdle = true;
        }

        private static void TrimImage(FloatCameraImage img, float minVal, float maxVal)
        {           
            for (int i = 0; i < img.Length; i++)
            {
                if (img[i] < minVal)
                {
                    img[i] = minVal;
                }

                if (img[i] > maxVal)
                {
                    img[i] = maxVal;
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

        private void SimpleViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(source.PixelWidth, source.PixelHeight, PixelFormat.Format24bppRgb);
            BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }
    }
}
