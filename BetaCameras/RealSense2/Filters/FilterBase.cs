using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public abstract class FilterBase
    {
        public virtual bool Enabled { get; set; }

        internal VideoFrame Apply(VideoFrame frame)
        {
            if (!Enabled)
            {
                return frame;
            }

            return ApplyImpl(frame);
        }

        protected abstract VideoFrame ApplyImpl(VideoFrame frame);

        protected static bool CheckMinMax(ProcessingBlock filter, Option option, float value, out int min, out int max)
        {
            max = (int)filter.Options[option].Max;
            min = (int)filter.Options[option].Min;

            return (value <= max && value >= min);
        }

        protected static bool CheckMinMax(ProcessingBlock filter, Option option, int value, out int min, out int max)
        {
            max = (int)filter.Options[option].Max;
            min = (int)filter.Options[option].Min;

            return (value <= max && value >= min);
        }
    }
}
