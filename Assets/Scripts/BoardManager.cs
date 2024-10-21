using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{

	public GameObject tilePrefab; //TODO: test making private and add [SerializeField] instead
	private Dictionary<Vector2Int, GameObject> placedTiles = new Dictionary<Vector2Int, GameObject>();
	// Start is called before the first frame update
	void Start()
    {
		Vector2Int centerPos = Vector2Int.zero;
		PlaceTile(centerPos, true);
		//HighlightAvailablePositions();
	}
	void PlaceTile(Vector2Int position, bool isStarter = false)
	{
		Debug.Log("Placing tile at position: " + position + ", isStarter: " + isStarter);
		GameObject newTile = Instantiate(tilePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);

		if (!isStarter)
		{
			SpriteRenderer sr = newTile.GetComponent<SpriteRenderer>();
			sr.sprite = GetRandomTileSprite();
			if (sr.sprite == null)
			{
				Debug.LogError("Failed to assign sprite to tile at position: " + position);
			}
			else
			{
				Debug.Log("Assigned sprite to tile at position: " + position);
			}
		}
		placedTiles.Add(position, newTile);
	}

	Sprite GetRandomTileSprite()
	{
		Sprite[] tileSprites = Resources.LoadAll<Sprite>("Sprites");
		Debug.Log("Number of sprites loaded: " + tileSprites.Length);
		if (tileSprites.Length == 0)
		{
			Debug.LogError("No sprites found in Resources/Sprites");
		}
		return tileSprites[Random.Range(0, tileSprites.Length)];
	}

	void HighlightAvailablePositions()
	{
		Debug.Log("Highlighting available positions");
		HashSet<Vector2Int> availablePositions = new HashSet<Vector2Int>();

		foreach (Vector2Int pos in placedTiles.Keys)
		{
			Vector2Int[] neighbors = {
			pos + Vector2Int.up,
			pos + Vector2Int.down,
			pos + Vector2Int.left,
			pos + Vector2Int.right
		};

			foreach (Vector2Int neighbor in neighbors)
			{
				if (!placedTiles.ContainsKey(neighbor))
				{
					availablePositions.Add(neighbor);
				}
			}
		}

		foreach (Vector2Int pos in availablePositions)
		{
			Debug.Log("Highlighting position: " + pos);
			GameObject highlightTile = Instantiate(tilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
			SpriteRenderer sr = highlightTile.GetComponent<SpriteRenderer>();
			sr.color = new Color(1, 1, 1, 0.5f);

			highlightTile.AddComponent<BoxCollider2D>();
			HighlightTile ht = highlightTile.AddComponent<HighlightTile>();
			ht.boardManager = this;

			// Assign a tag to the highlight tile
			highlightTile.tag = "Highlight";
		}
	}

	public void OnTileSelected(Vector2Int position)
	{
		// Place the tile
		PlaceTile(position);
		// Remove highlights
		foreach (GameObject highlight in GameObject.FindGameObjectsWithTag("Highlight"))
		{
			Destroy(highlight);
		}
		// Update highlights
		HighlightAvailablePositions();
	}
}
