using UnityEngine;

public class HighlightTile : MonoBehaviour
{
	public BoardManager boardManager; // Reference to the BoardManager

	private Vector2Int position;

	void Start()
	{
		// Calculate the grid position based on the transform's position
		position = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
		gameObject.tag = "Highlight"; // Ensure the highlight tile has the "Highlight" tag
	}

	void OnMouseDown()
	{
		Debug.Log("Highlight tile clicked at position: " + position);
		if (boardManager != null)
		{
			boardManager.OnTileSelected(position, gameObject);
		}
		else
		{
			Debug.LogError("BoardManager reference not set in HighlightTile.");
		}
	}
}
