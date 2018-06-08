using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Intel.RealSense;

namespace MetriCam2.Cameras.RealSense2Filter
{
    #region FilterBase
    public abstract class FilterBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool _enabled = false;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        internal VideoFrame Apply(VideoFrame frame)
        {
            if (!this.Enabled)
                return frame;

            return ApplyImpl(frame);
        }

        internal abstract VideoFrame ApplyImpl(VideoFrame frame);
    }
    #endregion

    #region DecimationFilter
    public class Decimation : FilterBase
    {
        public delegate void ResolutionChangeEvent();
        public event ResolutionChangeEvent ResolutionChange;
        private DecimationFilter _filter = new DecimationFilter();

        public Decimation()
        {
            base.PropertyChanged += Decimation_PropertyChanged;
        }

        ~Decimation()
        {
            base.PropertyChanged -= Decimation_PropertyChanged;
        }

        private void Decimation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(base.Enabled))
            {
                ResolutionChange();
            }
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
                        throw new InvalidOperationException(
                            $"Realsense2 Decimationfilter Magnitude: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterMagnitude].Value = value;
                    ResolutionChange();
                }
            }
        }

        internal override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
    #endregion

    #region TemporalFilter
    public class Temporal : FilterBase
    {
        private TemporalFilter _filter = new TemporalFilter();

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
            get
            {
                return (PersistencyMode)((int)_filter.Options[Option.HolesFill].Value);
            }
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
                        throw new InvalidOperationException(
                            $"Realsense2 TemporalFilter SmoothAlpha: value ouf of bounds - max: {max}, min: {min}");
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
                        throw new InvalidOperationException(
                            $"Realsense2 TemporalFilter SmoothDelta: value ouf of bounds - max: {max}, min: {min}");
                    }

                    _filter.Options[Option.FilterSmoothDelta].Value = value;
                }
            }
        }

        internal override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
    #endregion

    #region SpatialFilter
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
                        throw new InvalidOperationException(
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
                        throw new InvalidOperationException(
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
                        throw new InvalidOperationException(
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

        internal override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
    #endregion

    #region Depth2Disparity
    public class Depth2Disparity : FilterBase
    {
        private DisparityTransform _filter = new DisparityTransform(true);

        internal override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
    #endregion

    #region Disparity2Depth
    public class Disparity2Depth : FilterBase
    {
        private DisparityTransform _filter = new DisparityTransform(false);

        internal override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
    #endregion

    #region HolesFill
    public class HolesFill : FilterBase
    {
        private HoleFillingFilter _filter = new HoleFillingFilter();

        public enum FillingMode
        {
            FillFromLeft = 0,
            FarestFromAround = 1,
            NearestFromAround = 2,
        }

        public FillingMode Mode
        {
            get
            {
                return (FillingMode)((int)_filter.Options[Option.HolesFill].Value);
            }
            set
            {
                _filter.Options[Option.HolesFill].Value = (float)value;
            }
        }

        internal override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
    #endregion
}
