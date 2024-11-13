using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public static class Converters
    {
        public static MeepleType ConvertFeatureTypeToMeepleType(FeatureType type)
        {
            switch (type)
            {
                case FeatureType.ROAD:
                    return MeepleType.Road;
                case FeatureType.CITY:
                    return MeepleType.Knight;
                case FeatureType.MONASTERY:
                    return MeepleType.Bishop;
                default:
                    throw new ArgumentException("Invalid FeatureType", nameof(type));
            }
        }

        public static int ConvertDirectionToEdgeIndex(Vector2Int direction)
        {
            if (direction == Vector2Int.zero)
                return 0;
            if (direction == Vector2Int.up)
                return 1;
            if (direction == Vector2Int.right)
                return 2;
            if (direction == Vector2Int.down)
                return 3;
            if (direction == Vector2Int.left)
                return 4;

            throw new ArgumentException("Invalid direction", nameof(direction));
        }
    }
}
