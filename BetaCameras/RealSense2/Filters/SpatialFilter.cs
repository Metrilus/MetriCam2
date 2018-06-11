using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public class SpatialFilter : FilterBase
    {
        private readonly Intel.RealSense.SpatialFilter _filter = new Intel.RealSense.SpatialFilter();

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
            get => (int)_filter.Options[Option.FilterMagnitude].Value;
            set
            {
                if (value != Magnitude)
                {
                    if (!CheckMinMax(_filter, Option.FilterMagnitude, value, out int min, out int max))
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
            get => _filter.Options[Option.FilterSmoothAlpha].Value;
            set
            {
                if (value != SmoothAlpha)
                {
                    if (!CheckMinMax(_filter, Option.FilterSmoothAlpha, value, out int min, out int max))
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
            get => _filter.Options[Option.FilterSmoothDelta].Value;
            set
            {
                if (value != SmoothAlpha)
                {
                    if (!CheckMinMax(_filter, Option.FilterSmoothDelta, value, out int min, out int max))
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
            get => (HolesFillingMode)((int)_filter.Options[Option.HolesFill].Value);
            set =>  _filter.Options[Option.HolesFill].Value = (float)value;
        }

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
