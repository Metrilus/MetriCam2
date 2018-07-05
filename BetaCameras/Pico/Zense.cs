using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Metrilus.Util;
using MetriCam2.Cameras.Pico.ZenseAPI;

namespace MetriCam2.Cameras
{
    public class Zense : Camera, IDisposable
    {
        private bool _disposed = false;

        public int DeviceCount
        {
            get
            {
                CheckReturnStatus(Methods.GetDeviceCount(out int count));
                return count;
            }
        }

        private int DeviceIndex { set; get; }


        public Zense() : base()
        {
            CheckReturnStatus(Methods.Initialize());
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

            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
            Channels.Add(cr.RegisterChannel(ChannelNames.Intensity));
        }

        protected unsafe override void ConnectImpl()
        {
            if(String.IsNullOrEmpty(SerialNumber))
            {
                DeviceIndex = 0;
                CheckReturnStatus(Methods.OpenDevice(DeviceIndex));
                SerialNumber = GetStringProperty(DeviceIndex, PropertyType.SN_Str);
            }
            else
            {
                GetDeviceIndexFromSerial(SerialNumber);
            }
        }

        protected override void DisconnectImpl()
        {
            CheckReturnStatus(Methods.CloseDevice(DeviceIndex));
            CheckReturnStatus(Methods.Shutdown());
        }

        protected override void UpdateImpl()
        {
            CheckReturnStatus(Methods.ReadNextFrame(DeviceIndex));
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            throw new NotImplementedException();
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            FrameType type = GetFrameTypeFromChannelName(channelName);
            CheckReturnStatus(Methods.StartFrame(DeviceIndex, type));
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            FrameType type = GetFrameTypeFromChannelName(channelName);
            CheckReturnStatus(Methods.StopFrame(DeviceIndex, type));
        }

        private FrameType GetFrameTypeFromChannelName(string channelName)
        {
            switch (channelName)
            {
                case ChannelNames.Color:
                    return FrameType.RGBFrame;
                case ChannelNames.ZImage:
                    return FrameType.DepthFrame;
                case ChannelNames.Intensity:
                    return FrameType.IRFrame;
                default:
                    throw new Exception("asdfsaf");
            }
        }

        private void CheckReturnStatus(ReturnStatus status)
        {
            switch (status)
            {
                case ReturnStatus.NoDeviceConnected:
                    throw new Exception("No Device Connected");

                case ReturnStatus.InvalidDeviceIndex:
                    throw new Exception("Invalid Device Index");

                case ReturnStatus.DevicePointerIsNull:
                    throw new Exception("Device Pointer is null");

                case ReturnStatus.InvalidFrameType:
                    throw new Exception("Invalid Frame Type");

                case ReturnStatus.FramePointerIsNull:
                    throw new Exception("Frame pointer is null");

                case ReturnStatus.NoPropertyValueGet:
                case ReturnStatus.NoPropertyValueSet:
                    throw new Exception("No property value");

                case ReturnStatus.PropertyPointerIsNull:
                    throw new Exception("Invalid property");

                case ReturnStatus.PropertySizeNotEnough:
                    throw new Exception("Property buffer insufficient");

                case ReturnStatus.InvalidDepthRange:
                    throw new Exception("Invalid depth range");

                case ReturnStatus.ReadNextFrameError:
                    throw new Exception("Error reading next frame");

                case ReturnStatus.InputPointerIsNull:
                    throw new Exception("Input pointer is null");

                case ReturnStatus.CameraNotOpened:
                    throw new Exception("Camera not opened");

                case ReturnStatus.InvalidCameraType:
                    throw new Exception("Invalid camera type");

                case ReturnStatus.InvalidParams:
                    throw new Exception("Invalid Parameter");
            }
        }

        private int GetDeviceIndexFromSerial(string serial)
        {
            for(int i = 0; i < DeviceCount; i++)
            {
                CheckReturnStatus(Methods.OpenDevice(i));
                if(serial == GetStringProperty(i, PropertyType.SN_Str))
                {
                    return i;
                }

                CheckReturnStatus(Methods.CloseDevice(i));
            }

            throw new Exception(String.Format("Camera with S/N '{0}' not found.", serial));
        }

        private unsafe string GetStringProperty(int deviceIndex, PropertyType type)
        {
            int size = 64;
            char[] serial = new char[size];
            fixed (char* s = serial)
            {
                CheckReturnStatus(Methods.GetProperty(deviceIndex, PropertyType.SN_Str, (IntPtr)s, &size));
                return Marshal.PtrToStringAnsi((IntPtr)s);
            }

            throw new Exception("Failed to receive property: " + type.ToString());
        }
    }
}
