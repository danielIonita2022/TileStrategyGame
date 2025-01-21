using System;
using System.Diagnostics;


namespace Assets.Scripts
{
    [Flags]
    public enum FeatureType
    {
        NONE = 0,
        FIELD = 1 << 0,
        ROAD = 1 << 1,
        CITY = 1 << 2,
        MONASTERY = 1 << 3,
        ROAD_INTERSECTION = 1 << 4,
        ROAD_END = 1 << 5,
        SHIELD = 1 << 6
    }
}

