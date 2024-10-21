using UnityEngine;

public class HighlightTile : MonoBehaviour
{
	public BoardManager boardManager;
	private Vector2Int position;

	void Start()
	{
		position = new Vector2Int((int)transform.position.x, (int)transform.position.y);
		gameObject.tag = "Highlight";
	}

	void OnMouseDown()
	{
		boardManager.OnTileSelected(position);
	}
}
