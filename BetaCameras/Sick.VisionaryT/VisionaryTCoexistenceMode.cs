using System;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// SICK Visionary-T Coexistence Modes or Modulation Frequencies
    /// </summary>
    public enum VisionaryTCoexistenceMode : byte
    {
        Code1 = 0,
        Code2 = 1,
        Code3 = 2,
        Code4 = 3,
        Code5 = 4,
        Code6 = 5,
        Code7 = 6,
        Code8 = 7,

        Automatic = 8
    }
}
