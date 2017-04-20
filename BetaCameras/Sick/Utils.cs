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
        public static UInt32 ConvertEndiannessUInt32(UInt32 value)
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
        public static UInt16 ConvertEndiannessUInt16(UInt16 value)
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
        public static UInt64 ConvertEndiannessUInt64(UInt64 value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
