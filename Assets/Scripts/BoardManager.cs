using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject highlightTilePrefab;
    [SerializeField] private PreviewUIController previewUIController;
    [SerializeField] private TileData starterTileData;

    private List<TileData> tileDeck = new List<TileData>();
    private TileData currentPreviewTileData;

    private Dictionary<Vector2Int, GameObject> placedTiles = new Dictionary<Vector2Int, GameObject>();
    private HashSet<Vector2Int> currentHighlightPositions;
    private int maxTiles = 50;
    private int currentTileCount = 0;

    void Start()
    {
        LoadTileDeck();
        ShuffleTileDeck();
        currentHighlightPositions = new HashSet<Vector2Int>();
        DrawNextTile();
        Vector2Int centerPos = Vector2Int.zero;
        PlaceTile(centerPos, null, true);
    }

    void LoadTileDeck()
    {
        TileData[] loadedTiles = Resources.LoadAll<TileData>("Sprites/Tiles");
        tileDeck = new List<TileData>(loadedTiles);
        Debug.Log($"Loaded {tileDeck.Count} TileData assets into the deck.");

        foreach (var tile in tileDeck)
        {
            Debug.Log($"Loaded TileData: {tile.name}");
        }

        if (tileDeck.Count == 0)
        {
            Debug.LogError("No TileData assets found in Resources/Tiles");
        }
    }

    void ShuffleTileDeck()
    {
        for (int i = tileDeck.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            TileData temp = tileDeck[i];
            tileDeck[i] = tileDeck[j];
            tileDeck[j] = temp;
        }
        Debug.Log("Shuffled the tile deck.");
    }

    void DrawNextTile()
    {
        if (tileDeck.Count > 0)
        {
            currentPreviewTileData = tileDeck[0];
            tileDeck.RemoveAt(0);
            UpdatePreviewImage();
        }
        else
        {
            currentPreviewTileData = null;
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
        if (currentPreviewTileData != null && previewUIController != null)
        {
            previewUIController.UpdatePreview(currentPreviewTileData.tileSprite);
            Debug.Log($"Previewed TileData: {currentPreviewTileData.name}");
        }
        else
        {
            Debug.Log("No TileData to preview or previewUIController is not assigned.");
        }
    }

    void PlaceTile(Vector2Int position, GameObject highlightTile = null, bool isStarter = false)
    {
        if (currentTileCount >= maxTiles)
        {
            Debug.Log("Maximum tile count reached. No more tiles can be placed.");
            DisableFurtherPlacement();
            return;
        }

        // Ensure the position is within bounds and not already occupied
        if (placedTiles.ContainsKey(position))
        {
            Debug.LogWarning($"Cannot place tile at {position}: Position already occupied.");
            return;
        }

        // Instantiate the tile prefab
        GameObject newTileObj = Instantiate(tilePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
        Tile newTile = newTileObj.GetComponent<Tile>();
        if (newTile == null)
        {
            Debug.LogError("Tile component not found on instantiated tile.");
            Destroy(newTileObj);
            return;
        }

        // Assign the grid position
        newTile.gridPosition = position;

        if (!isStarter)
        {
            // Assign the current preview tile data
            newTile.tileData = currentPreviewTileData;
            if (newTile.tileData == null)
            {
                Debug.LogError($"No TileData assigned for tile at position: {position}");
                Destroy(newTileObj);
                return;
            }

            // Check compatibility with adjacent tiles
            Vector2Int[] directions = {
                Vector2Int.up * 8,
                Vector2Int.right * 8,
                Vector2Int.down * 8,
                Vector2Int.left * 8
            };

            FeatureType[] newTileEdges = {
                newTile.tileData.northEdge,
                newTile.tileData.eastEdge,
                newTile.tileData.southEdge,
                newTile.tileData.westEdge
            };

            FeatureType newTileCenter = newTile.tileData.centerFeature;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int adjacentPos = position + directions[i];
                if (placedTiles.ContainsKey(adjacentPos))
                {
                    GameObject adjacentTileObj = placedTiles[adjacentPos];
                    Tile adjacentTile = adjacentTileObj.GetComponent<Tile>();
                    if (adjacentTile == null)
                    {
                        Debug.LogError($"Tile component not found on adjacent tile at {adjacentPos}.");
                        continue;
                    }

                    // Determine corresponding edge index
                    int oppositeEdgeIndex = (i + 2) % 4; // Opposite direction
                    FeatureType existingEdge = GetFeature(adjacentTile.tileData, oppositeEdgeIndex);
                    FeatureType newEdge = newTileEdges[i];
                    FeatureType existingCenter = adjacentTile.tileData.centerFeature;

                    if (!AreFeaturesCompatible(existingEdge, newEdge, newTileCenter, existingCenter))
                    {
                        Debug.LogWarning($"Cannot place tile at {position}: Feature mismatch on {directions[i]} side.");
                        Destroy(newTileObj);
                        return;
                    }
                }
            }

            // All edges are compatible; proceed

            if (highlightTile != null)
            {
                Debug.Log($"Destroying highlight tile: {highlightTile.name}");
                Destroy(highlightTile);
            }
            else
            {
                Debug.LogWarning("Attempted to destroy a null highlight tile.");
            }
        }
        else
        {
            // Assign a specific TileData for the starter tile, if different
            // For simplicity, assuming the first tile is the starter
            // Alternatively, have a separate starter tile or assign as needed
            Debug.Log("Placing starter tile.");
        }

        placedTiles.Add(position, newTileObj);
        currentTileCount++;
        Debug.Log($"Tile placed at {position}. Current tile count: {currentTileCount}");


        // After placing a tile, draw the next one and update highlights
        DrawNextTile();
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

    FeatureType GetFeature(TileData tileData, int edgeIndex)
    {
        // edgeIndex: 0 = North, 1 = East, 2 = South, 3 = West, 4 = Center
        switch (edgeIndex)
        {
            case 0:
                return tileData.northEdge;
            case 1:
                return tileData.eastEdge;
            case 2:
                return tileData.southEdge;
            case 3:
                return tileData.westEdge;
            case 4:
                return tileData.centerFeature;
            default:
                return FeatureType.NONE;
        }
    }

    bool AreFeaturesCompatible(FeatureType existingEdge, FeatureType newEdge, FeatureType newCenter, FeatureType existingCenter)
    {
        if (existingEdge == FeatureType.NONE || newEdge == FeatureType.NONE)
            return false;

        // Iterate through each feature in existingEdge
        foreach (FeatureType feature in Enum.GetValues(typeof(FeatureType)))
        {
            if (existingEdge.HasFlag(feature))
            {
                switch (feature)
                {
                    case FeatureType.CITY:
                        if (!newEdge.HasFlag(FeatureType.CITY))
                            return false;
                        break;
                    case FeatureType.ROAD:
                        if (!(newEdge.HasFlag(FeatureType.ROAD) ||
                              newEdge.HasFlag(FeatureType.ROAD_INTERSECTION) ||
                              newEdge.HasFlag(FeatureType.ROAD_END)))
                            return false;
                        break;
                    case FeatureType.ROAD_INTERSECTION:
                        if (!(newEdge.HasFlag(FeatureType.ROAD) ||
                              newEdge.HasFlag(FeatureType.ROAD_INTERSECTION) ||
                              newEdge.HasFlag(FeatureType.ROAD_END)))
                            return false;
                        break;
                    case FeatureType.ROAD_END:
                        if (!(newEdge.HasFlag(FeatureType.ROAD) ||
                              newEdge.HasFlag(FeatureType.CITY) ||
                              newEdge.HasFlag(FeatureType.ROAD_INTERSECTION)))
                            return false;
                        break;
                    case FeatureType.MONASTERY:
                        // Define MONASTERY compatibility
                        if (!newEdge.HasFlag(FeatureType.MONASTERY))
                            return false;
                        break;
                    case FeatureType.FIELD:
                        // Define FIELD compatibility
                        if (!newEdge.HasFlag(FeatureType.FIELD))
                            return false;
                        break;
                    case FeatureType.SHIELD:
                        // Define SHIELD compatibility
                        // For example, SHIELD might not interfere with other features
                        // Or it might have specific rules
                        // Here, assuming SHIELD is a special marker and doesn't affect compatibility
                        // Therefore, no action needed
                        break;
                    default:
                        break;
                }
            }
        }

        // Additional checks based on center features
        // For example, ensure that if roads pass through the center, they continue seamlessly

        // Example: If new tile has ROAD_INTERSECTION at center, ensure road continuity
        if (newCenter.HasFlag(FeatureType.ROAD_INTERSECTION))
        {
            // Implement specific logic if roads must pass through intersections
            // This could involve ensuring that roads are connected to at least two edges
        }

        if (existingCenter.HasFlag(FeatureType.ROAD_INTERSECTION))
        {
            // Similar logic if the existing tile has a ROAD_INTERSECTION at center
        }

        // SHIELD handling can be implemented here if it affects compatibility

        return true;
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

    bool CanPlaceTileAtPosition(Vector2Int position)
    {
        // Retrieve adjacent positions and ensure compatibility
        Vector2Int[] directions = {
            Vector2Int.up * 8,
            Vector2Int.right * 8,
            Vector2Int.down * 8,
            Vector2Int.left * 8
        };

        // Iterate through all directions to check compatibility
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int adjacentPos = position + directions[i];
            if (placedTiles.ContainsKey(adjacentPos))
            {
                GameObject adjacentTileObj = placedTiles[adjacentPos];
                Tile adjacentTile = adjacentTileObj.GetComponent<Tile>();
                if (adjacentTile == null)
                {
                    Debug.LogError($"Tile component not found on adjacent tile at {adjacentPos}.");
                    continue;
                }

                // Determine corresponding edge feature
                FeatureType existingEdge = GetFeature(adjacentTile.tileData, (i + 2) % 4);
                // Simulate compatibility with all possible TileData
                bool compatible = false;
                foreach (TileData potentialTile in tileDeck)
                {
                    FeatureType newEdge = GetFeature(potentialTile, i);
                    FeatureType newTileCenter = potentialTile.centerFeature;
                    FeatureType existingCenter = adjacentTile.tileData.centerFeature;

                    if (AreFeaturesCompatible(existingEdge, newEdge, newTileCenter, existingCenter))
                    {
                        compatible = true;
                        break;
                    }
                }

                if (!compatible)
                {
                    // No compatible tile can be placed here
                    return false;
                }
            }
        }

        return true; // All adjacent edges are compatible
    }

    public void OnTileSelected(Vector2Int position, GameObject highlightTile)
    {
        Debug.Log($"Tile selected at position: {position}");
        // Destroy the highlight tile
        // Place a regular tile at the selected position
        PlaceTile(position, highlightTile);
        // No need to call HighlightAvailablePositions here as PlaceTile already does it
    }
}
