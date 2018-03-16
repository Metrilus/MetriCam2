// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;


namespace MetriCam2.Cameras
{
    public class Xtion2 : Camera, IDisposable
    {
        private bool _disposed = false;
        OpenNI2CApi.Device _device = new OpenNI2CApi.Device();
        OpenNI2CApi.Stream _colorStream = new OpenNI2CApi.Stream();
        OpenNI2CApi.Stream _depthStream = new OpenNI2CApi.Stream();
        OpenNI2CApi.Stream _irStream = new OpenNI2CApi.Stream();
        OpenNI2CApi.Frame _colorFrame = new OpenNI2CApi.Frame();
        OpenNI2CApi.Frame _depthFrame = new OpenNI2CApi.Frame();
        OpenNI2CApi.Frame _irFrame = new OpenNI2CApi.Frame();

        public override string Vendor { get => "Asus"; }

#if !NETSTANDARD2_0
        public override System.Drawing.Icon CameraIcon { get => Properties.Resources.XtionIcon; }
#endif


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            OpenNI2CApi.Shutdown();
            _disposed = true;
        }

        #region Constructor
        public Xtion2()
            : base(modelName: "Xtion2")
        {
            OpenNI2CApi.Init();
        }

        ~Xtion2()
        {
            Dispose(false);
        }
        #endregion

        #region MetriCam2 Camera Interface Methods

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
        }


        protected override void ConnectImpl()
        {
            _device = OpenNI2CApi.OpenDevice(this.SerialNumber);
            _colorStream = OpenNI2CApi.CreateStream(_device, OpenNI2CApi.OniSensorType.COLOR);
            _depthStream = OpenNI2CApi.CreateStream(_device, OpenNI2CApi.OniSensorType.DEPTH);
            _irStream = OpenNI2CApi.CreateStream(_device, OpenNI2CApi.OniSensorType.IR);

            if (ActiveChannels.Count == 0)
            {
                ActivateChannel(ChannelNames.Color);
                ActivateChannel(ChannelNames.ZImage);
            }
            else
            {
                InitActiveChannel(ChannelNames.Color);
                InitActiveChannel(ChannelNames.ZImage);
            }
        }

        private void InitActiveChannel(string channelName)
        {
            if (IsChannelActive(channelName))
            {
                ActivateChannelImpl(channelName);
            }
        }

        protected override void DisconnectImpl()
        {
            OpenNI2CApi.DestroyStream(_colorStream);
            OpenNI2CApi.DestroyStream(_depthStream);
            OpenNI2CApi.DestroyStream(_irStream);

            OpenNI2CApi.CloseDevice(_device);
        }

        protected override void UpdateImpl()
        {
            if(IsChannelActive(ChannelNames.Color))
            {
                OpenNI2CApi.ReleaseFrame(_colorFrame);
                _colorFrame = OpenNI2CApi.ReadFrame(_colorStream);
            }

            if (IsChannelActive(ChannelNames.ZImage))
            {
                OpenNI2CApi.ReleaseFrame(_depthFrame);
                _depthFrame = OpenNI2CApi.ReadFrame(_depthStream);
            }

            if (IsChannelActive(ChannelNames.Intensity))
            {
                OpenNI2CApi.ReleaseFrame(_irFrame);
                _irFrame = OpenNI2CApi.ReadFrame(_irStream);
            }
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            if (!IsChannelActive(channelName))
                throw new InvalidOperationException(string.Format("{0}: can't capture image from channel {1}, because it is not active.", this.Name, channelName));

            if (channelName == ChannelNames.Color)
            {
                return OpenNI2CApi.FrameToColorImage(_colorFrame);
            }
            if (channelName == ChannelNames.ZImage)
            {
                return OpenNI2CApi.FrameToFloatImage(_depthFrame);
            }
            if (channelName == ChannelNames.Intensity)
            {
                return OpenNI2CApi.FrameToFloatImage(_irFrame);
            }

            log.Error(string.Format("{0}: Unexpected ChannelName {1} in CalcChannel().", this.Name, channelName));
            return null;
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            if (channelName == ChannelNames.Color)
            {
                if (IsChannelActive(ChannelNames.Intensity))
                {
                    string msg = string.Format("{0}: Can't have {1} stream active while {2} is still active.", this.Name, channelName, ChannelNames.Intensity);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }  
                
                OpenNI2CApi.StartStream(_colorStream);
            }
            else if (channelName == ChannelNames.ZImage)
            {
                OpenNI2CApi.StartStream(_depthStream);
            }
            else if(channelName == ChannelNames.Intensity)
            {
                if (IsChannelActive(ChannelNames.Color))
                {
                    string msg = string.Format("{0}: Can't have {1} stream active while {2} is still active.", this.Name, channelName, ChannelNames.Color);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }

                OpenNI2CApi.StartStream(_irStream);
            }
            else
            {
                string msg = string.Format("{0}: doesn't implement channel {1}.", this.Name, channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            if (channelName == ChannelNames.Color)
            {
                OpenNI2CApi.StopStream(_colorStream);
                _colorFrame = new OpenNI2CApi.Frame(IntPtr.Zero);
            }
            else if (channelName == ChannelNames.ZImage)
            {
                OpenNI2CApi.StopStream(_depthStream);
                _depthFrame = new OpenNI2CApi.Frame(IntPtr.Zero);
            }
            else if (channelName == ChannelNames.Intensity)
            {
                OpenNI2CApi.StopStream(_irStream);
                _irFrame = new OpenNI2CApi.Frame(IntPtr.Zero);
            }
            else
            {
                string msg = string.Format("{0}: doesn't implement channel {1}.", this.Name, channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }
        }
        #endregion
    }
}
