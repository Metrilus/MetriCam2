﻿using System;
using System.Text;

namespace SICK_TIM561_Client
{
    internal sealed class CoLaBTelegram
    {
        public string CommandType { get; }
        public string CommandName { get; }

        public byte[] Data { get; }
        public int Offset { get; }
        public int Length { get; }

        internal CoLaBTelegram(byte[] data)
        {
            // Validate the Check-Sum and detect SOPAS command separators (two spaces)
            int space0 = -1;
            int space1 = -1;
            byte checksum = 0x00;
            for (int i = 0; i < (data.Length - 1); ++i)
            {
                checksum ^= data[i];

                if (0x20 == data[i])
                {
                    if (space0 < 0) space0 = i;
                    else if (space1 < 0) space1 = i;
                }
            }

            if (data[data.Length - 1] != checksum)
            {
                throw new CoLaBException($"Corrupt CoLa (binary) Telegram: downstream check-sum mismatch");
            }
            else if ((space0 < 0) || (space1 < 0))
            {
                throw new CoLaBException($"Corrupt CoLa (binary) Telegram: missing or invalid SOPAS command");
            }

            // Parse SOPAS Command
            CommandType = Encoding.ASCII.GetString(data, 0, space0);
            CommandName = Encoding.ASCII.GetString(data, space0 + 1, space1 - (space0 + 1));

            // Initialize Structure
            Data = data;
            Offset = space1 + 1;
            Length = data.Length - 1 - Offset;
        }

        public override string ToString()
        {
            return $"{CommandType} {CommandName}";
        }
    }
}
