using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{

	[SerializeField] private GameObject tilePrefab;
	[SerializeField] private GameObject highlightTilePrefab;
	[SerializeField] private PreviewUIController previewUIController;

	private List<Sprite> tileDeck = new List<Sprite>();
	private Sprite currentPreviewTile;

	private HashSet<Vector2Int> currentHighlightPositions;
	private Dictionary<Vector2Int, GameObject> placedTiles = new Dictionary<Vector2Int, GameObject>();
	private int maxTiles = 24;
	private int currentTileCount = 0;

	void Start()
	{
		LoadTileDeck();
		ShuffleTileDeck();
		Vector2Int centerPos = Vector2Int.zero;
		currentHighlightPositions = new HashSet<Vector2Int>();
		PlaceTile(centerPos, true);
	}

	void LoadTileDeck()
	{
		tileDeck = new List<Sprite>(Resources.LoadAll<Sprite>("Sprites/Tiles"));
		Debug.Log("Loaded " + tileDeck.Count + " tiles into the deck.");

		foreach (var tile in tileDeck)
		{
			Debug.Log("Loaded tile: " + tile.name);
		}

		if (tileDeck.Count == 0)
		{
			Debug.LogError("No sprites found in Resources/Sprites/Tiles");
		}
	}

	void ShuffleTileDeck()
	{
		for (int i = tileDeck.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			Sprite temp = tileDeck[i];
			tileDeck[i] = tileDeck[j];
			tileDeck[j] = temp;
		}
		Debug.Log("Shuffled the tile deck.");
	}

	void DrawNextTile()
	{
		if (tileDeck.Count > 0)
		{
			currentPreviewTile = tileDeck[0];
			tileDeck.RemoveAt(0);
			UpdatePreviewImage();
		}
		else
		{
			currentPreviewTile = null;
			if (previewUIController != null)
			{
				previewUIController.HidePreview();
			}
			Debug.Log("No more tiles to draw.");
			DisableFurtherPlacement();
		}
	}

	void UpdatePreviewImage()
	{
		if (currentPreviewTile != null)
		{
			if (previewUIController != null)
			{
				previewUIController.UpdatePreview(currentPreviewTile);
			}
			Debug.Log("Previewed tile: " + currentPreviewTile.name);
		}
		else
		{
			Debug.Log("No tile to preview.");
		}
	}

	void PlaceTile(Vector2Int position, bool isStarter = false)
	{
		if (currentTileCount >= maxTiles)
		{
			Debug.Log("Maximum tile count reached. No more tiles can be placed.");
			// Optionally, disable further placements or end the game
			DisableFurtherPlacement();
			return;
		}

		Debug.Log("Placing tile at position: " + position + ", isStarter: " + isStarter);
		GameObject newTile = Instantiate(tilePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);

		if (!isStarter)
		{
			SpriteRenderer sr = newTile.GetComponent<SpriteRenderer>();
			sr.sprite = currentPreviewTile;
			if (sr.sprite == null)
			{
				Debug.LogError("Failed to assign sprite to tile at position: " + position);
			}
			else
			{
				Debug.Log("Assigned sprite to tile at position: " + position);
			}
		}
		else
		{
			// Optionally, assign a specific sprite for the starter tile
			// For example, use a predefined sprite or the first tile from the deck
			if (currentPreviewTile != null)
			{
				SpriteRenderer sr = newTile.GetComponent<SpriteRenderer>();
				sr.sprite = currentPreviewTile;
				Debug.Log("Next tile assigned sprite: " + currentPreviewTile.name);
			}
		}

		placedTiles.Add(position, newTile);
		currentTileCount++;

		// After placing a tile, draw the next one
		DrawNextTile();
		// Update highlights
		HighlightAvailablePositions();
	}

	void DisableFurtherPlacement()
	{
		Debug.Log("Disabling further tile placements.");
		// Hide or disable the preview image
		if (previewUIController != null)
		{
			previewUIController.HidePreview();
		}
		// Optionally, display a "Game Over" message or UI
		// You can also disable input or other relevant components
	}

	Sprite GetRandomTileSprite()
	{
		Sprite[] tileSprites = Resources.LoadAll<Sprite>("Sprites/Tiles");
		Debug.Log("Number of sprites loaded: " + tileSprites.Length);
		if (tileSprites.Length == 0)
		{
			Debug.LogError("No sprites found in Resources/Sprites");
		}
		return tileSprites.Length > 0 ? tileSprites[Random.Range(0, tileSprites.Length)] : null;
	}

	void HighlightAvailablePositions()
	{
		Debug.Log("Highlighting available positions");

		HashSet<Vector2Int> allAvailablePositions = new HashSet<Vector2Int>();


        foreach (Vector2Int pos in placedTiles.Keys)
        {
            Vector2Int[] neighbors = {
                pos + Vector2Int.up * 8,
                pos + Vector2Int.down * 8,
                pos + Vector2Int.left * 8,
                pos + Vector2Int.right * 8
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!placedTiles.ContainsKey(neighbor))
                {
                    allAvailablePositions.Add(neighbor);
                }
            }
        }

        foreach (Vector2Int pos in allAvailablePositions)
		{
			if (allAvailablePositions.Contains(pos) && !currentHighlightPositions.Contains(pos))
			{
				Debug.Log("Highlighting position: " + pos);
				GameObject highlightTile = Instantiate(highlightTilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                currentHighlightPositions.Add(pos);
                SpriteRenderer sr = highlightTile.GetComponent<SpriteRenderer>();
				if (sr.sprite == null)
				{
					Debug.LogError("Failed to assign sprite to tile at position: " + pos);
				}
				else
				{
					Debug.Log("Assigned sprite to tile at position: " + pos);
				}
		
				HighlightTile ht = highlightTile.GetComponent<HighlightTile>();
				if (ht != null)
				{
					ht.boardManager = this;
				}
				else
				{
					Debug.LogError("HighlightTile script not found on HighlightTilePrefab.");
				}
			}
		}
		
	}

	public void OnTileSelected(Vector2Int position, GameObject highlightTile)
	{
	Debug.Log("Tile selected at position: " + position);
	// Destroy the highlight tile
	Destroy(highlightTile);
	// Place a regular tile at the selected position
	PlaceTile(position);
	}
}
