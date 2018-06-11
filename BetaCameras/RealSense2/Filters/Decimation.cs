using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public class Decimation : FilterBase
    {
        public delegate void ResolutionChangeEvent();
        public event ResolutionChangeEvent ResolutionChange;

        private readonly DecimationFilter _filter = new DecimationFilter();

        public Decimation()
        {
            PropertyChanged += Decimation_PropertyChanged;
        }

        ~Decimation()
        {
            PropertyChanged -= Decimation_PropertyChanged;
        }

        private void Decimation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // NOTE: why not overwrite property?
            if (e.PropertyName == nameof(Enabled))
            {
                ResolutionChange();
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
                    ResolutionChange();
                }
            }
        }

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
