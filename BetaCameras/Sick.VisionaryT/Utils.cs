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
        /// Converts an big endian uint32_t and converts it to little if the system is little endian.
        /// </summary>
        /// <param name="value">big endian uint32_t</param>
        /// <returns>corresponding little endian uint32_t</returns>
        internal static UInt32 ConvertEndiannessUInt32(UInt32 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Converts an big endian uint16_t and converts it to little if the system is little endian.
        /// </summary>
        /// <param name="value">big endian uint16_t</param>
        /// <returns>corresponding little endian uint16_t</returns>
        internal static UInt16 ConvertEndiannessUInt16(UInt16 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        /// Converts an big endian uint64_t and converts it to little if the system is little endian.
        /// </summary>
        /// <param name="value">big endian uint64_t</param>
        /// <returns>corresponding little endian uint64_t</returns>
        internal static UInt64 ConvertEndiannessUInt64(UInt64 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            return BitConverter.ToUInt64(bytes, 0);
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
    }
}
