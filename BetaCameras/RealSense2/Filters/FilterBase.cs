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
    public abstract class FilterBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            if (!Enabled)
            {
                return frame;
            }

            return ApplyImpl(frame);
        }

        protected abstract VideoFrame ApplyImpl(VideoFrame frame);
    }
}
