using System;
using System.ComponentModel;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// SICK Visionary-T Integration Time options (in microseconds)
    /// </summary>
    public enum VisionaryTIntegrationTime : byte
    {
        [Description("500 μs")]
        Microseconds500 = 0,
        [Description("1000 μs")]
        Microseconds1000 = 1,
        [Description("1500 μs")]
        Microseconds1500 = 2,
        [Description("2000 μs")]
        Microseconds2000 = 3,
        [Description("2500 μs")]
        Microseconds2500 = 4,
    }
}
