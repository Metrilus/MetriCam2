// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Exceptions;
using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading;
#if NETSTANDARD2_0
#else
using System.Drawing.Imaging;
#endif


namespace MetriCam2.Cameras
{
    public class Hikvision : Camera, IDisposable
    {
        private const double _timeoutSeconds = 30.0f;

        private bool _disposed = false;
        private RTSPClient _client = null;
        private Bitmap _currentBitmap = null;

        ParamDesc<string> IPAddressDesc
        {
            get
            {
                ParamDesc<string> res = new ParamDesc<string>
                {
                    Description = "IP address of the camera",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
                return res;
            }
        }
        public string IPAddress { get; set; } = "";

        ParamDesc<uint> PortDesc
        {
            get
            {
                ParamDesc<uint> res = new ParamDesc<uint>
                {
                    Description = "Port of RTSP",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
                return res;
            }
        }
        public uint Port { get; set; } = 554;

        ParamDesc<string> UsernameDesc
        {
            get
            {
                ParamDesc<string> res = new ParamDesc<string>
                {
                    Description = "Username to access the camera",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
                return res;
            }
        }
        public string Username { get; set; } = "";

        ParamDesc<string> PasswordDesc
        {
            get
            {
                ParamDesc<string> res = new ParamDesc<string>
                {
                    Description = "Password to access the camera",
                    ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected,
                    WritableWhen = ParamDesc.ConnectionStates.Disconnected
                };
                return res;
            }
        }
        public string Password { get; set; } = "";


        public Hikvision()
        {
            
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

        protected unsafe override void ConnectImpl()
        {
            if (ActiveChannels.Count == 0)
            {
                AddToActiveChannels(ChannelNames.Color);
            }

            _client = new RTSPClient(IPAddress, Port, Username, Password);
            _client.Connect();
        }

        private void OnErrorCallback(Exception error)
        {

        }
        protected override void DisconnectImpl()
        {
            _client.Disconnect();
            _client = null;
        }

        protected override void UpdateImpl()
        {
            if(!_client.NewBitmapAvailable.WaitOne(TimeSpan.FromSeconds(_timeoutSeconds)))
            {
                throw new ImageAcquisitionFailedException($"Timeout: no image sent within the last {_timeoutSeconds} seconds.");
            }
            _currentBitmap = _client.GetCurrentBitmap();
        }

        protected override ImageBase CalcChannelImpl(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Color:
                    return new ColorImage(_currentBitmap);
            }

            throw new ImageAcquisitionFailedException($"{Name}: No valid channel name");
        }

        /// <summary>
        /// Overrides the standard GetIntrinsic method.
        /// </summary>
        /// <param name="channelName">The channel name.</param>
        /// <returns>The ProjectiveTransformationRational</returns>
        /// <remarks>The method first searches for a pt file on disk. If this fails it provides default intrinsics for the model WL-IC8BE.</remarks>
        public override ProjectiveTransformation GetIntrinsics(string channelName)
        {
            ProjectiveTransformation result = null;

            log.Info("Trying to load projective transformation from file.");
            try
            {
                result = base.GetIntrinsics(channelName);
            }
            catch { /* empty */ }

            if (result == null)
            {
                log.Info("Projective transformation file not found.");
                log.Info("Using default intrinsics for type Hikvision WL-IC8BE as projective transformation.");
                switch (channelName)
                {
                    case ChannelNames.Color:
                        result = new ProjectiveTransformationRational(3840, 2160, 3068.753f, 3073.629f, 1892.428f, 1245.486f,
                            -0.5675009f, 0.4332804f, -0.183668f, 0, 0, 0, -0.001460855f, -0.0001179822f);
                        break;
                    default:
                        log.Error("Unsupported channel in GetIntrinsics().");
                        return null;
                }
            }
            return result;
        }
    }
}
