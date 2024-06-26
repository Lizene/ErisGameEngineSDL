﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ErisMath
{
    internal static class Constants
    {
        // Class for storing precalculated constants, for optimization
        public const float deg2rad = (float)(Math.Tau / 360d);
        public const float rad2deg = (float)(360d / Math.Tau);
        public const float rad120 = (float)(Math.Tau/3d);
    }
}
