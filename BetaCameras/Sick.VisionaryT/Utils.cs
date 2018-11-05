// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace MetriCam2.Cameras.Internal.Sick
{
    /// <summary>
    /// This class contains utilities routines e.g. for endianness conversion.
    /// </summary>
    internal class Utils
    {
        /// <summary>
        /// Converts an big endian uint16_t and converts it to little if the system is little endian.
        /// </summary>
        /// <param name="value">big endian uint16_t</param>
        /// <returns>system-uint16_t</returns>
        private static UInt16 FromBigEndianUInt16(UInt16 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            return BitConverter.ToUInt16(bytes, 0);
        }

        internal static UInt16 FromBigEndianUInt16(byte[] buffer, int offset)
        {
            UInt16 val = BitConverter.ToUInt16(buffer, offset);
            return FromBigEndianUInt16(val);
        }

        /// <summary>
        /// Converts an big endian uint32_t and converts it to little if the system is little endian.
        /// </summary>
        /// <param name="value">big endian uint32_t</param>
        /// <returns>system-endian uint32_t</returns>
        private static UInt32 FromBigEndianUInt32(UInt32 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        internal static UInt32 FromBigEndianUInt32(byte[] buffer, int offset)
        {
            UInt32 val = BitConverter.ToUInt32(buffer, offset);
            return FromBigEndianUInt32(val);
        }

        /// <summary>
        /// Converts an big endian int32_t and converts it to little if the system is little endian.
        /// </summary>
        /// <param name="value">big endian int32_t</param>
        /// <returns>system-endian int32_t</returns>
        private static Int32 FromBigEndianInt32(Int32 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        internal static Int32 FromBigEndianInt32(byte[] buffer, int offset)
        {
            Int32 val = BitConverter.ToInt32(buffer, offset);
            return FromBigEndianInt32(val);
        }

        /// <summary>
        /// Converts an big endian uint64_t and converts it to little if the system is little endian.
        /// </summary>
        /// <param name="value">big endian uint64_t</param>
        /// <returns>system-endian uint64_t</returns>
        private static UInt64 FromBigEndianUInt64(UInt64 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }


        internal static UInt64 FromBigEndianUInt64(byte[] buffer, int offset)
        {
            UInt64 val = BitConverter.ToUInt64(buffer, offset);
            return FromBigEndianUInt64(val);
        }

        /// <summary>
        /// Converts a uint32_t to big endian.
        /// </summary>
        /// <param name="value">system-endian uint32_t</param>
        /// <returns>big endian uint32_t</returns>
        internal static byte[] ToBigEndian(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// Converts a int32_t to big endian.
        /// </summary>
        /// <param name="value">system-endian int32_t</param>
        /// <returns>big endian int32_t</returns>
        internal static byte[] ToBigEndian(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// Gets the byte array as hex string. Used for debugging purpose only.
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>hex string</returns>
        internal static string GetHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes);
        }

        internal static byte[] CalculatePasswordHash(byte[] password)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(password);
            var hash2 = md5.Hash;

            //dig = m.digest()
            for (int i = 0; i < hash.Length; i++)
            {
                hash[i] = (byte)hash[i];
            }
            //var dig = [ord(x) for x in dig]; // convert bytes to int
            var dig = hash;
            // 128 bit to 32 bit by XOR
            var byte0 = (byte)(dig[0] ^ dig[4] ^ dig[8] ^ dig[12]);
            var byte1 = (byte)(dig[1] ^ dig[5] ^ dig[9] ^ dig[13]);
            var byte2 = (byte)(dig[2] ^ dig[6] ^ dig[10] ^ dig[14]);
            var byte3 = (byte)(dig[3] ^ dig[7] ^ dig[11] ^ dig[15]);
            //var retValue = byte0 | (byte1 << 8) | (byte2 << 16) | (byte3 << 24);
            var retValue = new byte[] { byte3, byte2, byte1, byte0 };
            return retValue;
        }

        /// <summary>
        /// Reads from the network until the Sick header 0x02020202 has been found.
        /// </summary>
        internal static bool SyncCoLa(NetworkStream stream)
        {
            // Todo: this can be optimized by reading four bytes at once, counting the trailing non-0x02 bytes, etc.
            uint elements = 0;
            int buffer;

            while (elements < 4)
            {
                buffer = stream.ReadByte();
                if (-1 == buffer)
                {
                    return false;
                }

                if (0x02 == buffer)
                {
                    elements++;
                }
                else
                {
                    elements = 0;
                }
            }

            return true;
        }

        /// <summary>
        /// Receives bytes from a network stream into a buffer.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="bytesToReceive"></param>
        /// <returns></returns>
        internal static bool Receive(NetworkStream stream, ref byte[] buffer, int bytesToReceive)
        {
            Array.Resize(ref buffer, bytesToReceive);
            int offset = 0;

            int bytesReceived = 0;
            while (bytesToReceive > 0)
            {
                bytesReceived = stream.Read(buffer, offset, bytesToReceive);

                if (0 == bytesReceived)
                {
                    return false;
                }
                offset += bytesReceived;
                bytesToReceive -= bytesReceived;
            }
            
            return true;
        }
    }
}
