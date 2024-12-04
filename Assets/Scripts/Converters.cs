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
            if (type == FeatureType.ROAD)
                return MeepleType.Road;
            else if (type == FeatureType.CITY)
                return MeepleType.Knight;
            else if ((type & FeatureType.MONASTERY) == FeatureType.MONASTERY)
                return MeepleType.Bishop;
            else
                throw new ArgumentException($"Invalid FeatureType {type}", nameof(type));
        }

        public static FeatureType ConvertMeepleTypeToFeatureType(MeepleType type)
        {
            if (type == MeepleType.Road)
                return FeatureType.ROAD;
            else if (type == MeepleType.Knight)
                return FeatureType.CITY;
            else if (type == MeepleType.Bishop)
                return FeatureType.MONASTERY;
            else
                throw new ArgumentException($"Invalid MeepleType {type}", nameof(type));
        }

        public static int ConvertDirectionToEdgeIndex(Vector2Int direction)
        {
            if (direction == Vector2Int.zero)
                return 0;
            if (direction == Vector2Int.up)
                return 3;
            if (direction == Vector2Int.right)
                return 4;
            if (direction == Vector2Int.down)
                return 1;
            if (direction == Vector2Int.left)
                return 2;

            throw new ArgumentException("Invalid direction", nameof(direction));
        }

        public static Vector2Int ConvertEdgeIndexToDirection(int edgeIndex)
        {
            switch (edgeIndex)
            {
                case 0:
                    return Vector2Int.zero;
                case 1:
                    return Vector2Int.up;
                case 2:
                    return Vector2Int.right;
                case 3:
                    return Vector2Int.down;
                case 4:
                    return Vector2Int.left;
                default:
                    throw new ArgumentException("Invalid edge index", nameof(edgeIndex));
            }
        }

        public static string ConvertPlayerColorToString(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.GRAY:
                    return "Gray";
                case PlayerColor.RED:
                    return "Red";
                case PlayerColor.BLUE:
                    return "Blue";
                case PlayerColor.GREEN:
                    return "Green";
                case PlayerColor.YELLOW:
                    return "Yellow";
                default:
                    throw new ArgumentException("Invalid PlayerColor", nameof(color));
            }
        }
    }
}
