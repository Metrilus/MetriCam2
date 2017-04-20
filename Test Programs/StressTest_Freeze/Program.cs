using MetriCam2;
using MetriCam2.Cameras;
using Metrilus.Logging;
using Metrilus.Util;
using MetriPrimitives.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace StressTests.Freeze
{
    class Program
    {
        static void Main(string[] args)
        {
            MetriLog log = new MetriLog();
            Random rand = new Random();
            const int MAX_DELAY_IN_MIN = 20;

            Camera leftCam = new TheImagingSource();
            Camera rightCam = new TheImagingSource();
            SerialPort triggerPort = new SerialPort("COM7", 9600);
            TriggeredStereoCamera cam = new TriggeredStereoCamera(leftCam, rightCam, triggerPort);
            FloatImage left, leftOld = null, right, rightOld = null;
            float thres;
            // thres = 1000.0f; // uEye
            thres = 100000.0f; // TIS
            int cnt = 0;
            const float MAX_EXPOSURE = 10f;
            const float DARK_THRES = 50f; // uEye: 10f, TIS: 50f
            const int NUM_WARMUP_IMAGES = 50;

            ConfigureLogging(StressTests.Freeze.Resources.LoggingConfigInfo);
            log.SetLogFile(@"D:\temp\stress_test_TIS.log");

            cam.Connect();
            cam.Exposure = 4;
            log.Info("Warming up.");
            for (int i = 0; i < NUM_WARMUP_IMAGES; i++)
            {
                Capture(cam, out left, out right, ref cnt);
            }

            log.Info("Starting test.");
            bool running = true;
            while (running)
            {
                log.Debug("Another round starts.");
                for (int i = 0; i < 10; i++)
                {
                    if (cam.Exposure > MAX_EXPOSURE)
                    {
                        cam.Exposure = MAX_EXPOSURE;
                        leftOld = null;
                        rightOld = null;
                        continue;
                    }

                    Capture(cam, out left, out right, ref cnt);

                    float minL, maxL, minR, maxR;
                    left.GetMinMax(out minL, out maxL);
                    right.GetMinMax(out minR, out maxR);

                    log.Debug("MAX = " + maxL + "   " + maxR);
                    if (maxL == 255f || maxR == 255f)
                    {
                        log.Info("Overexposed, reducing exposure time.");
                        cam.Exposure = cam.Exposure * (3f / 4f);
                        leftOld = null;
                        rightOld = null;
                        continue;
                    }
                    if (maxL < DARK_THRES && maxR < DARK_THRES)
                    {
                        if (cam.Exposure < MAX_EXPOSURE)
                        {
                            log.Info("Underexposed, increasing exposure time.");
                            cam.Exposure = cam.Exposure * (4f / 3f);
                            leftOld = null;
                            rightOld = null;
                            continue;
                        }

                        log.Info("seems to be dark, let's sleep an hour.");
                        Thread.Sleep(1000 * 60 * 60);
                        leftOld = null;
                        rightOld = null;
                        continue;
                    }

                    rightOld = Compare(right, rightOld, thres, cnt, "R", log);
                    leftOld = Compare(left, leftOld, thres, cnt, "L", log);

                    if (null == leftOld || null == rightOld)
                        break;
                }
                int random = rand.Next(100);
                float delayInMinutes = (float)random / 100f * (float)MAX_DELAY_IN_MIN;
                log.Debug("Sleeping for " + delayInMinutes + " minutes");
                Thread.Sleep((int)(1000 * 60 * delayInMinutes));
                //Thread.Sleep(500);
            }

            cam.Disconnect();
        }

        private static void ConfigureLogging(string loggingConfig)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(loggingConfig);
                    writer.Flush();
                    stream.Position = 0;
                    log4net.Config.XmlConfigurator.Configure(stream);
                }
            }
        }

        private static FloatImage Compare(FloatImage currentImg, FloatImage oldImg, float thres, int cnt, string channel, MetriLog log)
        {
            if (null != oldImg)
            {
                FloatImage diff = oldImg - currentImg;
                float sad = diff.Abs().Sum();
                log.Debug("SAD = " + sad);
                if (sad < thres)
                {
                    log.ErrorFormat("Image {0}{1}: SAD ({2}) was below the threshold of {3}.", cnt, channel, sad, thres);
                    return null;
                }
            }
            oldImg = currentImg;
            return oldImg;
        }

        private static void Capture(TriggeredStereoCamera cam, out FloatImage left, out FloatImage right, ref int cnt)
        {
            cam.Update();
            cnt++;
            FloatCameraImage leftCamImg = cam.CalcChannel(ChannelNames.Left).ToFloatCameraImage();
            FloatCameraImage rightCamImg = cam.CalcChannel(ChannelNames.Right).ToFloatCameraImage();
            long ts_l = leftCamImg.TimeStamp;
            long ts_r = rightCamImg.TimeStamp;
            int fn_l = leftCamImg.FrameNumber;
            int fn_r = rightCamImg.FrameNumber;
            unsafe
            {
                left = new FloatImage(leftCamImg.Width, leftCamImg.Height, leftCamImg.AbandonDataBuffer());
                right = new FloatImage(rightCamImg.Width, rightCamImg.Height, rightCamImg.AbandonDataBuffer());
            }
            left.TimeStamp = ts_l;
            right.TimeStamp = ts_r;
            left.FrameNumber = fn_l;
            right.FrameNumber = fn_r;
        }
    }
}
