using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Enums
{
    /// <summary>
    /// Possible connection states for a camera.
    /// </summary>
    [Flags]
    public enum ConnectionStates
    {
        /// <summary>Camera is connected.</summary>
        Connected = 0x01,
        /// <summary>Camera is disconnected.</summary>
        Disconnected = 0x02,
    };

    public enum Unit
    {
        Millimeter,
        Centimeter,
        Meter,
        Kilometer,
        Pixel,
        FPS,
        DegreeCelsius
    }
}
