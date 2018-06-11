using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public class DecimationFilter : FilterBase
    {
        public delegate void ResolutionChangeEvent();
        public event ResolutionChangeEvent ResolutionChanged;

        private readonly Intel.RealSense.DecimationFilter _filter = new Intel.RealSense.DecimationFilter();

        public DecimationFilter()
        {
            PropertyChanged += Decimation_PropertyChanged;
        }

        ~DecimationFilter()
        {
            PropertyChanged -= Decimation_PropertyChanged;
        }

        private void Decimation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // NOTE: why not overwrite property?
            if (e.PropertyName == nameof(Enabled))
            {
                ResolutionChanged();
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
                    ResolutionChanged();
                }
            }
        }

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
