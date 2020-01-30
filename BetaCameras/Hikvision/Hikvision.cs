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
        private bool _disposed = false;
        private RTSPClient _client = null;
        private long _currentBitmapTimestamp = 0;
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
            long timestamp = 0;
            Bitmap bitmap = null;
            long startTime = DateTime.Now.Ticks;
            long maxFrameTime = TimeSpan.FromSeconds(3).Ticks;

            do
            {
                (bitmap, timestamp) = _client.GetCurrentBitmap();
                if (DateTime.Now.Ticks > startTime + maxFrameTime)
                {
                    throw new ImageAcquisitionFailedException("error?!");
                }
            }
            while (timestamp <= _currentBitmapTimestamp);

            _currentBitmapTimestamp = timestamp;
            _currentBitmap = bitmap;
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
    }
}
