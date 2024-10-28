using UnityEngine;

public class HighlightTile : MonoBehaviour
{
	[HideInInspector]
	public BoardManager boardManager; // Reference to the BoardManager

	void OnMouseDown()
	{
		if (boardManager != null)
		{
			Vector2Int position = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
			boardManager.OnTileSelected(position, this.gameObject);
			Debug.Log("Highlight tile at position " + position + " clicked.");
		}
		else
		{
			Debug.LogError("BoardManager reference not set on HighlightTile.");
		}
	}
}
