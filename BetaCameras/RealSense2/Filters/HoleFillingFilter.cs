﻿using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.RealSense2Filters
{
    public class HoleFillingFilter : FilterBase
    {
        private readonly Intel.RealSense.HoleFillingFilter _filter = new Intel.RealSense.HoleFillingFilter();

        public enum FillingMode
        {
            FillFromLeft = 0,
            FarestFromAround = 1,
            NearestFromAround = 2,
        }

        public FillingMode Mode
        {
            get => (FillingMode)((int)_filter.Options[Option.HolesFill].Value);
            set => _filter.Options[Option.HolesFill].Value = (float)value;
        }

        protected override VideoFrame ApplyImpl(VideoFrame frame)
        {
            return _filter.ApplyFilter(frame);
        }
    }
}
