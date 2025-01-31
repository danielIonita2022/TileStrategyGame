using System;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts
{
    public class HighlightTile : NetworkBehaviour
    {
        public event Action<Vector2Int> OnTileClicked;

        [HideInInspector]
        public Vector2Int TilePosition;

        private void OnMouseDown()
        {
            if (OnTileClicked != null)
            {
                OnTileClicked?.Invoke(TilePosition);
                Debug.Log("Highlight tile at position " + TilePosition + " clicked.");
            }
            else
            {
                Debug.LogError("No subscribers for OnTileClicked event.");
            }
        }
    }
}
