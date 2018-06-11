using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public class Spatial : FilterBase
    {
        private SpatialFilter _filter = new SpatialFilter();

        public enum HolesFillingMode
        {
            Disabled = 0,
            TwoPixelRadius = 1,
            FourPixelRadius = 2,
            EightPixelRadius = 3,
            SixteenPixelRadius = 4,
            Unlimited = 5,
        }

        public int Magnitude
        {
            get
            {
                return (int)_filter.Options[Option.FilterMagnitude].Value;
            }
            set
            {
                if (value != Magnitude)
                {
                    int max = (int)_filter.Options[Option.FilterMagnitude].Max;
                    int min = (int)_filter.Options[Option.FilterMagnitude].Min;

                    if (value > max || value < min)
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Realsense2 SpatialFilter Magnitude: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterMagnitude].Value = value;
                }
            }
        }

        public float SmoothAlpha
        {
            get
            {
                return _filter.Options[Option.FilterSmoothAlpha].Value;
            }
            set
            {
                if (value != SmoothAlpha)
                {
                    int max = (int)_filter.Options[Option.FilterSmoothAlpha].Max;
                    int min = (int)_filter.Options[Option.FilterSmoothAlpha].Min;

                    if (value > max || value < min)
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Realsense2 SpatialFilter SmoothAlpha: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterSmoothAlpha].Value = value;
                }
            }
        }

        public float SmoothDelta
        {
            get
            {
                return _filter.Options[Option.FilterSmoothDelta].Value;
            }
            set
            {
                if (value != SmoothAlpha)
                {
                    int max = (int)_filter.Options[Option.FilterSmoothDelta].Max;
                    int min = (int)_filter.Options[Option.FilterSmoothDelta].Min;

                    if (value > max || value < min)
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Realsense2 SpatialFilter SmoothDelta: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterSmoothDelta].Value = value;
                }
            }
        }

        public HolesFillingMode HolesFilling
        {
            get
            {
                return (HolesFillingMode)((int)_filter.Options[Option.HolesFill].Value);
            }
            set
            {
                _filter.Options[Option.HolesFill].Value = (float)value;
            }
        }

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
