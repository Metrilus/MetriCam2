using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public class Depth2DisparityTransform : FilterBase
    {
        private readonly DisparityTransform _filter = new DisparityTransform(transform_to_disparity: true);

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
