
using System;

namespace Assets.Scripts
{
    public struct TileFeatureKey
    {
        public Tile tile;
        public FeatureType featureType;
        public int featureIndex;

        public TileFeatureKey(Tile tile, FeatureType featureType, int featureIndex)
        {
            this.tile = tile;
            this.featureType = featureType;
            this.featureIndex = featureIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is TileFeatureKey other)
            {
                return tile == other.tile &&
                       featureType == other.featureType &&
                       featureIndex == other.featureIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(tile, featureType, featureIndex);
        }
    }
}
