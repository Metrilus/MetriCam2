using System;
using System.Text;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// Utility class for reading binary data that is in Network Order (big-endian)
    /// </summary>
    internal sealed class NetworkBinaryReader
    {
        private byte[] _data;
        private int _position;

        private static readonly bool _reverse = BitConverter.IsLittleEndian;

        public NetworkBinaryReader(CoLaBTelegram telegram)
        {
            _data = new byte[telegram.Length];
            Array.Copy(telegram.Data, telegram.Offset, _data, 0, telegram.Length);
            _position = 0;
        }

        public void Skip(int delta)
        {
            _position += delta;
        }

        public byte ReadByte() => _data[_position++];

        public UInt16 ReadUInt16()
        {
            if (_reverse) Array.Reverse(_data, _position, sizeof(UInt16));

            UInt16 value = BitConverter.ToUInt16(_data, _position);
            _position += sizeof(UInt16);
            return value;
        }

        public UInt32 ReadUInt32()
        {
            if (_reverse) Array.Reverse(_data, _position, sizeof(UInt32));

            UInt32 value = BitConverter.ToUInt32(_data, _position);
            _position += sizeof(UInt32);
            return value;
        }

        public Int32 ReadInt32()
        {
            if (_reverse) Array.Reverse(_data, _position, sizeof(Int32));

            Int32 value = BitConverter.ToInt32(_data, _position);
            _position += sizeof(Int32);
            return value;
        }

        public Single ReadSingle()
        {
            if (_reverse) Array.Reverse(_data, _position, sizeof(Single));

            float value = BitConverter.ToSingle(_data, _position);
            _position += sizeof(Single);
            return value;
        }

        public string ReadString(int length)
        {
            string s = Encoding.ASCII.GetString(_data, _position, length);
            _position += length;
            return s;
        }
    }
}
