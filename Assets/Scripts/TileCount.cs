using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [System.Serializable]
    public class TileCount
    {
        [Tooltip("Reference to the TileData asset.")]
        public TileData tileData;

        [Tooltip("Number of copies for this tile.")]
        public int count = 1;
    }
}