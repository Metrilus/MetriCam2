// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.Generic;
using Basler.Pylon;
using MetriCam2.Exceptions;

namespace MetriCam2.Cameras
{
    public class BaslerACE : Camera, IDisposable
    {
        public List<string> SupportedDevices { get; set; } = new List<string>()
        {
            "acA1300",
        };
        private Basler.Pylon.Camera _camera;
        private Bitmap _bitmap;
        private PixelDataConverter _converter;
        private bool _disposed = false;

#if !NETSTANDARD2_0
        public override Icon CameraIcon { get => Properties.Resources.BaslerIcon; }
#endif

        public override string Vendor { get => "Basler"; }

        public BaslerACE() : base(modelName: "Ace")
        {
            _converter = new PixelDataConverter();
        }

        ~BaslerACE()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (IsConnected)
                DisconnectImpl();

            if (disposing)
            {
                // dispose managed resources
            }

            _disposed = true;
        }

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
        }

        protected override void ConnectImpl()
        {
            if(!string.IsNullOrEmpty(SerialNumber))
            {
                try
                {
                    _camera = new Basler.Pylon.Camera(SerialNumber);
                }
                catch(Exception e)
                {
                    string msg = $"No {Name} with S/N {SerialNumber} found: {e.Message}";
                    log.Error(msg);
                    throw new ConnectionFailedException(msg);
                }
            }
            else
            {
                List<ICameraInfo> devices = CameraFinder.Enumerate();
                ICameraInfo device = devices.Where(i => ContainsAny(i[CameraInfoKey.FullName], SupportedDevices)).First();
                if(null == device)
                {
                    string msg = $"{Name}: No device of supported types {SupportedDevices} found";
                    log.Error(msg);
                    throw new ConnectionFailedException(msg);
                }

                try
                {
                    _camera = new Basler.Pylon.Camera(device);
                    this.SerialNumber = _camera.CameraInfo.ContainsKey("SerialNumber") ? _camera.CameraInfo["SerialNumber"] : null;
                    this.Model = _camera.CameraInfo.ContainsKey("ModelName") ? _camera.CameraInfo["ModelName"] : "Basler ace";
                }
                catch(Exception e)
                {
                    string msg = $"{Name} failed to connect: {e.Message}";
                    log.Error(msg);
                    throw new ConnectionFailedException(msg);
                }
            }

            _camera.CameraOpened += Configuration.AcquireSingleFrame;
            _camera.Open();
            _camera.StreamGrabber.Start();

            AddToActiveChannels(ChannelNames.Color);
        }

        protected override void DisconnectImpl()
        {
            _camera.CameraOpened -= Configuration.AcquireSingleFrame;
            _camera.StreamGrabber.Stop();
            _camera.Close();
        }

        protected override void UpdateImpl()
        {
            using (IGrabResult grabResult = _camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException))
            {
                if (grabResult.GrabSucceeded)
                {
                    _bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
                    byte[] buffer = grabResult.PixelData as byte[];
                    PixelType type = grabResult.PixelTypeValue;
                    
                    BitmapData bmpData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadWrite, _bitmap.PixelFormat);

                    _converter.OutputPixelFormat = PixelType.BGRA8packed;
                    IntPtr ptrBmp = bmpData.Scan0;
                    _converter.Convert(ptrBmp, bmpData.Stride * _bitmap.Height, grabResult);
                    _bitmap.UnlockBits(bmpData);
                }
                else
                {
                    string msg = string.Format("{0}: {1} {2}", Name, grabResult.ErrorCode, grabResult.ErrorDescription);
                    log.Error(msg);
                    throw new ImageAcquisitionFailedException(msg);
                }
            }
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Color:
                    return new ColorCameraImage(_bitmap);
            }

            log.Error($"{Name}: Unexpected ChannelName in CalcChannel().");
            return null;
        }

        public bool ContainsAny(string haystack, List<string> needles)
        {
            foreach (string needle in needles)
            {
                if (haystack.Contains(needle))
                    return true;
            }

            return false;
        }
    }
}
