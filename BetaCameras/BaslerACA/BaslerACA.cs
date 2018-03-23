// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using PylonC.NETSupportLibrary;
using PylonC.NET;

namespace MetriCam2.Cameras
{
    public class BaslerACA : Camera
    {
        private const string DeviceClass = "acA1300";
        private ImageProvider _imageProvider;
        private ImageProvider.ImageReadyEventHandler _imageReady;
        private Bitmap _bitmap;
        private TaskCompletionSource<bool> _tsc = null;

#if !NETSTANDARD2_0
        public override Icon CameraIcon { get => Properties.Resources.BaslerIcon; }
#endif

        private static bool _IsInitialized = false;
        private void Init()
        {
            if(!_IsInitialized)
            {
                Pylon.Initialize();
                _IsInitialized = true;
            }
        }

        private void Terminate()
        {
            if(_IsInitialized)
            {
                Pylon.Terminate();
            }
        }

        public BaslerACA()
        {
            Init();
            _imageReady = new ImageProvider.ImageReadyEventHandler(OnImageReadyEventCallback);
            _imageProvider = new ImageProvider();
            _imageProvider.ImageReadyEvent += _imageReady;
        }

        ~BaslerACA()
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
                    string msg = string.Format("BaslerACA: no device with s/n {0} found.", SerialNumber);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }
            }
            else
            {
                dev = list.Where(d => d.Model.StartsWith(DeviceClass)).First();
                if (null == dev)
                {
                    string msg = string.Format("BaslerACA: no device of type {0} found.", DeviceClass);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }
            }

            _imageProvider.Open(dev.Index);

            if (ActiveChannels.Count == 0)
            {
                ActivateChannel(ChannelNames.Color);
            }
        }

        protected override void DisconnectImpl()
        {
            _imageProvider.Close();
        }

        protected override void UpdateImpl()
        {
            _tsc = new TaskCompletionSource<bool>();
            _imageProvider.OneShot();
            _tsc.Task.Wait();
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

            if (BitmapFactory.IsCompatible(_bitmap, image.Width, image.Height, image.Color))
            {
                BitmapFactory.UpdateBitmap(_bitmap, image.Buffer, image.Width, image.Height, image.Color);
            }
            else
            {
                if (_bitmap != null)
                {
                    _bitmap.Dispose();
                }

                BitmapFactory.CreateBitmap(out _bitmap, image.Width, image.Height, image.Color);
                BitmapFactory.UpdateBitmap(_bitmap, image.Buffer, image.Width, image.Height, image.Color);
            }

            _imageProvider.ReleaseImage();
            _tsc?.TrySetResult(true);
        }
    }
}
