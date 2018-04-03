// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using PylonC.NETSupportLibrary;
using PylonC.NET;

namespace MetriCam2.Cameras
{
    public class BaslerACE : Camera
    {
        private const string DeviceClass = "acA1300";
        private ImageProvider _imageProvider;
        private Bitmap _bitmap;
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);

#if !NETSTANDARD2_0
        public override Icon CameraIcon { get => Properties.Resources.BaslerIcon; }
#endif

        public override string Vendor { get => "Basler"; }

        private static bool _isInitialized = false;
        private void Init()
        {
            if(!_isInitialized)
            {
                Pylon.Initialize();
                _isInitialized = true;
            }
        }

        private void Terminate()
        {
            if(_isInitialized)
            {
                Pylon.Terminate();
            }
        }

        public BaslerACE() : base(modelName: "Ace")
        {
            Init();
            _imageProvider = new ImageProvider();
            _imageProvider.ImageReadyEvent += new ImageProvider.ImageReadyEventHandler(OnImageReadyEventCallback);
        }

        ~BaslerACE()
        {
            Terminate();
        }

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
        }

        protected override void ConnectImpl()
        {
            List<DeviceEnumerator.Device> list = DeviceEnumerator.EnumerateDevices();
            DeviceEnumerator.Device dev = null;

            if (!String.IsNullOrEmpty(SerialNumber))
            {
                dev = list.Where(d => d.SerialNumber == SerialNumber).First();
                if(null == dev)
                {
                    string msg = string.Format("{0}: no {1} device with s/n {2} found.", Name, DeviceClass, SerialNumber);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }
            }
            else
            {
                dev = list.Where(d => d.Model.StartsWith(DeviceClass)).First();
                if (null == dev)
                {
                    string msg = string.Format("{0}: no device of type {1} found.", Name, DeviceClass);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }
            }

            _imageProvider.Open(dev.Index);
            ActivateChannel(ChannelNames.Color);
        }

        protected override void DisconnectImpl()
        {
            _imageProvider.Close();
        }

        protected override void UpdateImpl()
        {
            _resetEvent.Reset();
            _imageProvider.OneShot();
            _resetEvent.WaitOne();
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Color:
                    return new ColorCameraImage(_bitmap);
            }

            log.Error("Unexpected ChannelName in CalcChannel().");
            return null;
        }

        private void OnImageReadyEventCallback()
        {
            ImageProvider.Image image = _imageProvider.GetLatestImage();

            
            if (!BitmapFactory.IsCompatible(_bitmap, image.Width, image.Height, image.Color))
            {
                if (_bitmap != null)
                {
                    _bitmap.Dispose();
                }

                BitmapFactory.CreateBitmap(out _bitmap, image.Width, image.Height, image.Color);
            }

            BitmapFactory.UpdateBitmap(_bitmap, image.Buffer, image.Width, image.Height, image.Color);
            _imageProvider.ReleaseImage();
            _resetEvent.Set();
        }
    }
}
