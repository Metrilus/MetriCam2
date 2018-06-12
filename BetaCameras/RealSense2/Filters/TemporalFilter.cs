using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public class TemporalFilter : FilterBase
    {
        private readonly Intel.RealSense.TemporalFilter _filter = new Intel.RealSense.TemporalFilter();

        public enum PersistencyMode
        {
            Disabled = 0,
            Valid8in8 = 1,
            Valid2in3 = 2,
            Valid2in4 = 3,
            Valid2in8 = 4,
            Valid1in2 = 5,
            Valid1in5 = 6,
            Valid1in8 = 7,
            AlwaysOn = 8,
        }

        public PersistencyMode Persistency
        {
            get => (PersistencyMode)((int)_filter.Options[Option.HolesFill].Value);
            set
            {
                if (value != Persistency)
                {
                    _filter.Options[Option.HolesFill].Value = (float)value;
                }
            }
        }

        public float SmoothAlpha
        {
            get => _filter.Options[Option.FilterSmoothAlpha].Value;
            set
            {
                if (value != SmoothAlpha)
                {
                    if (!CheckMinMax(_filter, Option.FilterSmoothAlpha, value, out int min, out int max))
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Realsense2 TemporalFilter SmoothAlpha: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterSmoothAlpha].Value = value;
                }
            }
        }

        public float SmoothDelta
        {
            get => _filter.Options[Option.FilterSmoothDelta].Value;
            set
            {
                if (value != SmoothDelta)
                {
                    if (!CheckMinMax(_filter, Option.FilterSmoothDelta, value, out int min, out int max))
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Realsense2 TemporalFilter SmoothDelta: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterSmoothDelta].Value = value;
                }
            }
        }

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
