using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    /// <summary>
    /// Decimation filter reduces the resolution of the depth image.
    /// For this reason en/disabling the filter or changing its magnitude
    /// will trigger an update of the camera.
    /// </summary>
    public class DecimationFilter : FilterBase
    {
        public delegate void ResolutionChangeEvent();
        public event ResolutionChangeEvent ResolutionChanged;

        private readonly Intel.RealSense.DecimationFilter _filter = new Intel.RealSense.DecimationFilter();

        public override bool Enabled
        {
            get => base.Enabled;
            set
            {
                if (value != base.Enabled)
                {
                    base.Enabled = value;
                    if (ResolutionChanged == null)
                    {
                        throw new InvalidOperationException(
                            "RealSense2 Decimationfilter: only activate the filter after the camera is connected.");
                    }

                    ResolutionChanged();
                }
            }
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
                            $"Realsense2 Decimationfilter Magnitude: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterMagnitude].Value = value;
                    if (Enabled)
                    {
                        ResolutionChanged();
                    }
                }
            }
        }

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
