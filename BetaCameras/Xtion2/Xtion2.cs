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
        OpenNICAPI.Device _device = new OpenNICAPI.Device();
        OpenNICAPI.Stream _colorStream = new OpenNICAPI.Stream();
        OpenNICAPI.Stream _depthStream = new OpenNICAPI.Stream();
        OpenNICAPI.Stream _irStream = new OpenNICAPI.Stream();
        OpenNICAPI.Frame _colorFrame = new OpenNICAPI.Frame();
        OpenNICAPI.Frame _depthFrame = new OpenNICAPI.Frame();
        OpenNICAPI.Frame _irFrame = new OpenNICAPI.Frame();


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            OpenNICAPI.Shutdown();
            _disposed = true;
        }

        #region Constructor
        public Xtion2()
        {
            OpenNICAPI.Init();
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
            _device = OpenNICAPI.OpenDevice(this.SerialNumber);
            _colorStream = OpenNICAPI.CreateStream(_device, OpenNICAPI.OniSensorType.COLOR);
            _depthStream = OpenNICAPI.CreateStream(_device, OpenNICAPI.OniSensorType.DEPTH);
            _irStream = OpenNICAPI.CreateStream(_device, OpenNICAPI.OniSensorType.IR);

            ActivateChannel(ChannelNames.Color);
        }

        protected override void DisconnectImpl()
        {
            OpenNICAPI.DestroyStream(_colorStream);
            OpenNICAPI.DestroyStream(_depthStream);
            OpenNICAPI.DestroyStream(_irStream);

            OpenNICAPI.CloseDevice(_device);
        }

        protected override void UpdateImpl()
        {
            if(IsChannelActive(ChannelNames.Color))
            {
                OpenNICAPI.ReleaseFrame(_colorFrame);
                _colorFrame = OpenNICAPI.ReadFrame(_colorStream);
            }

            if (IsChannelActive(ChannelNames.ZImage))
            {
                OpenNICAPI.ReleaseFrame(_depthFrame);
                _depthFrame = OpenNICAPI.ReadFrame(_depthStream);
            }

            if (IsChannelActive(ChannelNames.Intensity))
            {
                OpenNICAPI.ReleaseFrame(_irFrame);
                _irFrame = OpenNICAPI.ReadFrame(_irStream);
            }
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            if (channelName == ChannelNames.Color)
            {
                return OpenNICAPI.FrameToColorImage(_colorFrame);
            }
            if (channelName == ChannelNames.ZImage)
            {
                return OpenNICAPI.FrameToFloatImage(_depthFrame);
            }
            if (channelName == ChannelNames.Intensity)
            {
                return OpenNICAPI.FrameToFloatImage(_irFrame);
            }

            log.Error("Unexpected ChannelName in CalcChannel().");
            return null;
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            if (channelName == ChannelNames.Color)
            {
                if (IsChannelActive(ChannelNames.Intensity))
                {
                    string msg = string.Format("Xtion2: Can't have {0} stream active while {1} is still active.", channelName, ChannelNames.Intensity);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }  
                
                OpenNICAPI.StartStream(_colorStream);
            }
            else if (channelName == ChannelNames.ZImage)
            {
                OpenNICAPI.StartStream(_depthStream);
            }
            else if(channelName == ChannelNames.Intensity)
            {
                if (IsChannelActive(ChannelNames.Color))
                {
                    string msg = string.Format("Xtion2: Can't have {0} stream active while {1} is still active.", channelName, ChannelNames.Color);
                    log.Error(msg);
                    throw new InvalidOperationException(msg);
                }

                OpenNICAPI.StartStream(_irStream);
            }
            else
            {
                string msg = string.Format("Xtion2: doesn't implement channel.", channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            if (channelName == ChannelNames.Color)
            {
                OpenNICAPI.StopStream(_colorStream);
            }
            else if (channelName == ChannelNames.ZImage)
            {
                OpenNICAPI.StopStream(_depthStream);
            }
            else if (channelName == ChannelNames.Intensity)
            {
                OpenNICAPI.StopStream(_irStream);
            }
            else
            {
                string msg = string.Format("Xtion2: doesn't implement channel.", channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }
        }
        #endregion
    }
}
