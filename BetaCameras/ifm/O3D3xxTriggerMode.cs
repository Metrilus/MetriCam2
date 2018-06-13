using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// ifm O3D3xx Trigger Modes
    /// </summary>
    public enum O3D3xxTriggerMode
    {
        FreeRun = 1,
        ProcessInterface = 2,
        PositiveEdge = 3,
        NegatigeEdge = 4,
        PositiveAndNegativeEdge = 5,
    }
}
