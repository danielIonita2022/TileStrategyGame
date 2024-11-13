using System;
using UnityEngine;

namespace Assets.Scripts
{
	public class HighlightTile : MonoBehaviour
	{
        public event Action<Vector2Int, GameObject> OnTileClicked;

        [HideInInspector]
		public Vector2Int TilePosition;

		private void OnMouseDown()
		{
            if (OnTileClicked != null)
            {
                OnTileClicked?.Invoke(TilePosition, this.gameObject);
                Debug.Log("Highlight tile at position " + TilePosition + " clicked.");
            }
            else
            {
                Debug.LogError("No subscribers for OnTileClicked event.");
            }
        }
	}
}
