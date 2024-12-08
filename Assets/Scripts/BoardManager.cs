using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private GameObject highlightTilePrefab;
        
        [SerializeField] private TileData starterTileData;
        [SerializeField] private List<TileCount> tileCounts = new List<TileCount>();

        private List<TileData> tileDeck = new List<TileData>();
        private TileData currentPreviewTileData;

        private Dictionary<Vector2Int, GameObject> placedTiles = new Dictionary<Vector2Int, GameObject>();
        private HashSet<Vector2Int> currentHighlightPositions;
        private int maxTiles = 50;
        private int currentTileCount = 0;

        public event Action OnNoMoreTilePlacements;
        public event Action<Sprite> OnPreviewImageUpdate;
        public event Action<HighlightTile> OnHighlightTileCreated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            LoadTileDeck();
            ShuffleTileDeck();
            currentHighlightPositions = new HashSet<Vector2Int>();
            DrawNextTile(true);
            Vector2Int centerPos = Vector2Int.zero;
            PlaceTile(centerPos, 0, null, true);
        }

        private void LoadTileDeck()
        {
            tileDeck.Clear();

            foreach (TileCount tileCount in tileCounts)
            {
                if (tileCount.tileData == null)
                {
                    Debug.LogWarning("A TileCount entry has a null TileData reference and will be skipped.");
                    continue;
                }

                for (int i = 0; i < tileCount.count; i++)
                {
                    TileData tileDataCopy = Instantiate(tileCount.tileData); // Create a unique copy
                    tileDeck.Add(tileDataCopy);
                    Debug.Log($"Added copy {i + 1} of TileData: {tileDataCopy.name}");
                }
            }

            Debug.Log($"Loaded {tileDeck.Count} TileData assets into the deck.");
        }

        private void ShuffleTileDeck()
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

        public void DrawNextTile(bool isStarter = false)
        {
            int attempts = 0;
            int maxAttempts = tileDeck.Count;
            while (tileDeck.Count > 0 && attempts < maxAttempts)
            {
                TileData candidateTile = tileDeck[0];
                if (isStarter || CanTileBePlaced(candidateTile))
                {
                    currentPreviewTileData = candidateTile;
                    tileDeck.RemoveAt(0);
                    if (currentPreviewTileData != null)
                    {
                        UpdatePreviewImage();
                    }
                    return;
                }
                else
                {
                    tileDeck.RemoveAt(0);
                    tileDeck.Add(candidateTile);
                    attempts++;
                }
            }
            if (attempts >= maxAttempts)
            {
                currentPreviewTileData = null;
                Debug.Log("No more tiles to draw.");
                OnNoMoreTilePlacements?.Invoke();
            }
        }

        private bool CanTileBePlaced(TileData candidateTileData)
        {
            if (candidateTileData != null)
            {
                List<Vector2Int> availablePositions = GetAvailablePositions();
                foreach (Vector2Int pos in availablePositions)
                {
                    for (int rotationState = 0; rotationState < 4; rotationState++)
                    {
                        List<FeatureType> rotatedEdges = GetRotatedEdges(candidateTileData, rotationState);
                        FeatureType rotatedNorth = rotatedEdges[0];
                        FeatureType rotatedEast = rotatedEdges[1];
                        FeatureType rotatedSouth = rotatedEdges[2];
                        FeatureType rotatedWest = rotatedEdges[3];

                        bool isCompatible = true;
                        Vector2Int[] directions = {
                        Vector2Int.up * 8,
                        Vector2Int.right * 8,
                        Vector2Int.down * 8,
                        Vector2Int.left * 8
                    };

                        FeatureType[] tileEdges = {
                        rotatedNorth,
                        rotatedEast,
                        rotatedSouth,
                        rotatedWest
                    };
                        for (int i = 0; i < directions.Length; i++)
                        {
                            Vector2Int adjacentPos = pos + directions[i];
                            if (placedTiles.ContainsKey(adjacentPos))
                            {
                                Tile adjacentTile = placedTiles[adjacentPos].GetComponent<Tile>();
                                if (adjacentTile == null || adjacentTile.tileData == null)
                                {
                                    // Invalid adjacent tile, skip
                                    continue;
                                }

                                // Determine corresponding edge index
                                int oppositeEdgeIndex = (i + 2) % 4 + 1; // Opposite direction

                                // Get the adjacent tile's edge
                                FeatureType adjacentEdge = adjacentTile.GetFeature(oppositeEdgeIndex);

                                // Get current tile's edge
                                FeatureType currentEdge = tileEdges[i];

                                // Check compatibility
                                if (!AreFeaturesCompatible(adjacentEdge, currentEdge, candidateTileData.centerFeature, adjacentTile.tileData.centerFeature))
                                {
                                    isCompatible = false;
                                    break;
                                }
                            }
                        }

                        if (isCompatible)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Simulates rotation by returning the edges after rotation.
        /// </summary>
        /// <param name="tileData">Original TileData.</param>
        /// <param name="rotationState">Number of 90° rotations.</param>
        /// <returns>List of FeatureType representing [north, east, south, west] after rotation.</returns>
        private List<FeatureType> GetRotatedEdges(TileData tileData, int rotationState)
        {
            // Clone the edge features
            FeatureType[] edges = new FeatureType[] {
            tileData.northEdge,
            tileData.eastEdge,
            tileData.southEdge,
            tileData.westEdge
        };

            // Apply rotationState times 90 degrees clockwise rotation
            for (int i = 0; i < rotationState; i++)
            {
                FeatureType temp = edges[3]; // westEdge
                edges[3] = edges[2]; // southEdge
                edges[2] = edges[1]; // eastEdge
                edges[1] = edges[0]; // northEdge
                edges[0] = temp; // westEdge
            }

            return new List<FeatureType>(edges);
        }

        /// <summary>
        /// Retrieves all available positions where tiles can be placed.
        /// </summary>
        /// <returns>List of Vector2Int positions.</returns>
        private List<Vector2Int> GetAvailablePositions()
        {
            HashSet<Vector2Int> availablePositions = new HashSet<Vector2Int>();

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
                        availablePositions.Add(neighbor);
                    }
                }
            }

            return new List<Vector2Int>(availablePositions);
        }

        public Tile GetTileAtPosition(Vector2Int position)
        {
            if (placedTiles.ContainsKey(position))
            {
                GameObject tileObj = placedTiles[position];
                Tile tile = tileObj.GetComponent<Tile>();
                if (tile != null)
                {
                    return tile;
                }
                Debug.LogError($"Tile component not found on tile at {position}.");
                return null;
            }
            //Debug.LogWarning($"No tile found at position: {position}");
            return null;
        }

        private void UpdatePreviewImage()
        {
            if (currentPreviewTileData != null)
            {
                OnPreviewImageUpdate?.Invoke(currentPreviewTileData.tileSprite);
                Debug.Log($"BoardManager: Previewed TileData: {currentPreviewTileData.name}");
            }
            else
            {
                Debug.Log("BoardManager: No TileData to preview.");
            }
        }

        /// <summary>
        /// Places a tile at the specified position. If isStarter is true, uses starterTileData.
        /// </summary>
        public Tile PlaceTile(Vector2Int position, int rotationState, GameObject highlightTile = null, bool isStarter = false)
        {
            Tile newTile = null;
            if (currentTileCount >= maxTiles)
            {
                Debug.Log("BoardManager: Maximum tile count reached. No more tiles can be placed.");
                OnNoMoreTilePlacements?.Invoke();
                return null;
            }

            // Ensure the position is within bounds and not already occupied
            if (placedTiles.ContainsKey(position))
            {
                Debug.LogWarning($"BoardManager: Cannot place tile at {position} - Position already occupied.");
                return null;
            }

            // Instantiate the tile prefab
            GameObject newTileObj = Instantiate(tilePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            newTile = newTileObj.GetComponent<Tile>();
            if (newTile == null)
            {
                Debug.LogError("BoardManager: Tile component not found on instantiated tile.");
                Destroy(newTileObj);
                return null;
            }

            // Assign the grid position
            newTile.GridPosition = position;

            if (!isStarter)
            {
                // Assign the current preview tile data
                newTile.tileData = currentPreviewTileData;
                if (newTile.tileData == null)
                {
                    Debug.LogError($"BoardManager: No TileData assigned for tile at position: {position}");
                    Destroy(newTileObj);
                    return null;
                }
                newTile.AssignFeatures();

                newTile.RotateTile(rotationState);

                // Check compatibility with adjacent tiles
                Vector2Int[] directions = {
                Vector2Int.up * 8,
                Vector2Int.right * 8,
                Vector2Int.down * 8,
                Vector2Int.left * 8
            };

                FeatureType[] newTileEdges = {
                newTile.CurrentNorthEdge,
                newTile.CurrentEastEdge,
                newTile.CurrentSouthEdge,
                newTile.CurrentWestEdge
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
                            Debug.LogError($"BoardManager: Tile component not found on adjacent tile at {adjacentPos}.");
                            continue;
                        }

                        // Determine corresponding edge index
                        int oppositeEdgeIndex = (i + 2) % 4 + 1; // Opposite direction
                        FeatureType existingEdge = adjacentTile.GetFeature(oppositeEdgeIndex);
                        FeatureType newEdge = newTileEdges[i];
                        FeatureType existingCenter = adjacentTile.CurrentCenterFeature;

                        if (!AreFeaturesCompatible(existingEdge, newEdge, newTileCenter, existingCenter))
                        {
                            Debug.LogWarning($"BoardManager: Cannot place tile at {position}: Feature mismatch on {directions[i]} side.");
                            Destroy(newTileObj);
                            return null;
                        }
                    }
                }

                // All edges are compatible; proceed

                if (highlightTile != null)
                {
                    Debug.Log($"BoardManager: Destroying highlight tile: {highlightTile.name}");
                    Destroy(highlightTile);
                }
                else
                {
                    Debug.LogWarning("BoardManager: Attempted to destroy a null highlight tile.");
                }
            }
            else
            {
                // Assign a specific TileData for the starter tile, if different
                // For simplicity, assuming the first tile is the starter
                // Alternatively, have a separate starter tile or assign as needed
                Debug.Log("BoardManager: Placing starter tile.");
                placedTiles.Add(position, newTileObj);
                currentTileCount++;
                HighlightAvailablePositions();
                return null;
            }

            placedTiles.Add(position, newTileObj);
            currentTileCount++;
            Debug.Log($"Tile placed at {position}. Current tile count: {currentTileCount}");

            return newTile;
        }

        /// <summary>
        /// Checks if two features are compatible based on game rules.
        /// </summary>
        private bool AreFeaturesCompatible(FeatureType existingEdge, FeatureType newEdge, FeatureType newCenter, FeatureType existingCenter)
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

        /// <summary>
        /// Highlights all valid positions where tiles can be placed.
        /// </summary>
        public void HighlightAvailablePositions()
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
                    Debug.Log("BoardManager: Highlighting position: " + pos);
                    GameObject highlightTile = Instantiate(highlightTilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                    currentHighlightPositions.Add(pos);
                    SpriteRenderer sr = highlightTile.GetComponent<SpriteRenderer>();
                    if (sr.sprite == null)
                    {
                        Debug.LogError("BoardManager: Failed to assign sprite to tile at position: " + pos);
                    }
                    else
                    {
                        Debug.Log("BoardManager: Assigned sprite to tile at position: " + pos);
                    }
                    HighlightTile ht = highlightTile.GetComponent<HighlightTile>();
                    if (ht != null)
                    {
                        ht.TilePosition = pos; // Assign the position
                        OnHighlightTileCreated?.Invoke(ht); // Notify subscribers
                    }
                    else
                    {
                        Debug.LogError("HighlightTile script not found on HighlightTilePrefab.");
                    }
                }
            }
        }

        public bool IsFeatureComplete(Tile tile, FeatureType featureType, int featureIndex)
        {
            if ((featureType & FeatureType.MONASTERY) == FeatureType.MONASTERY)
                return IsMonasteryComplete(tile, featureIndex);
            if ((featureType & FeatureType.CITY) == FeatureType.CITY)
                return IsCityComplete(tile, featureIndex);
            if ((featureType & FeatureType.ROAD) == FeatureType.ROAD)
                return IsRoadComplete(tile, featureIndex);
            throw new Exception("Feature type not recognized.");
        }

        private bool IsCityComplete(Tile tile, int featureIndex)
        {
            TileFeatureKey startingKey = new TileFeatureKey(tile, FeatureType.CITY, featureIndex);
            HashSet<TileFeatureKey> connectedCity = GetConnectedFeatureKeys(tile, FeatureType.CITY, featureIndex);
            foreach (var cityKey in connectedCity)
            {
                int currentFeatureIndex = cityKey.featureIndex;
                Vector2Int currentTilePos = cityKey.tile.GridPosition;
                Vector2Int adjacentPos = currentTilePos + Converters.ConvertEdgeIndexToDirection(currentFeatureIndex) * 8;
                if (placedTiles.ContainsKey(adjacentPos))
                {
                    Tile adjacentTile = placedTiles[adjacentPos].GetComponent<Tile>();
                    if (adjacentTile == null || adjacentTile.tileData == null)
                    {
                        throw new Exception("Position is occupied but the tile itself is not present on the board!");
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsMonasteryComplete(Tile tile, int featureIndex)
        {
            Vector2Int monasteryPosition = tile.GridPosition;
            Vector2Int[] adjacentAndCornerPositions = new Vector2Int[]
            {
                monasteryPosition + Vector2Int.up * 8,
                monasteryPosition + Vector2Int.right * 8,
                monasteryPosition + Vector2Int.down * 8,
                monasteryPosition + Vector2Int.left * 8,
                monasteryPosition + (Vector2Int.up + Vector2Int.left) * 8,
                monasteryPosition + (Vector2Int.up + Vector2Int.right) * 8,
                monasteryPosition + (Vector2Int.down + Vector2Int.left) * 8,
                monasteryPosition + (Vector2Int.down + Vector2Int.right) * 8
            };
            foreach (var position in adjacentAndCornerPositions)
            {
                if (!placedTiles.ContainsKey(position))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if a road feature is complete.
        /// </summary>
        private bool IsRoadComplete(Tile tile, int featureIndex)
        {
            TileFeatureKey startingKey = new TileFeatureKey(tile, FeatureType.ROAD, featureIndex);

            HashSet<TileFeatureKey> connectedRoads = GetConnectedFeatureKeys(tile, FeatureType.ROAD, featureIndex);
            //int countEnds = 0;
            //foreach (var roadKey in connectedRoads)
            //{
            //    FeatureType featureType = roadKey.featureType;
            //    if (((featureType & FeatureType.ROAD_END) == FeatureType.ROAD_END) ||
            //        ((featureType & FeatureType.ROAD_INTERSECTION) == FeatureType.ROAD_INTERSECTION))
            //    {
            //        countEnds++;
            //    }
            //}
            //if (countEnds == 2)
            //{
            //    return true;
            //}
            //else if (countEnds > 2)
            //{
            //    throw new ArgumentException("Road cannot have more than 2 ends.");
            //}

            foreach (var roadKey in connectedRoads)
            {
                int currentFeatureIndex = roadKey.featureIndex;
                Vector2Int currentTilePos = roadKey.tile.GridPosition;
                Vector2Int adjacentPos = currentTilePos + Converters.ConvertEdgeIndexToDirection(currentFeatureIndex) * 8;
                if (placedTiles.ContainsKey(adjacentPos))
                {
                    Tile adjacentTile = placedTiles[adjacentPos].GetComponent<Tile>();
                    if (adjacentTile == null || adjacentTile.tileData == null)
                    {
                        throw new Exception("Position is occupied but the tile itself is not present on the board!");
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;


            //ATENTIE: Nu iei in calcul daca drumul e bucla, de asemenea e problema cu tile-ul biserica cu drum, apare doar preotul si nici nu poti apasa pe el
        }

        /// <summary>
        /// Retrieves the connected tiles for a given tile and feature.
        /// </summary>
        public HashSet<Tile> GetConnectedTiles(Tile tile, FeatureType featureType, int featureIndex)
        {
            HashSet<Tile> connectedTiles = new HashSet<Tile>();
            HashSet<TileFeatureKey> connectedKeys = GetConnectedFeatureKeys(tile, featureType, featureIndex);
            foreach (var key in connectedKeys)
            {
                connectedTiles.Add(key.tile);
            }
            return connectedTiles;
        }

        /// <summary>
        /// Retrieves the connected feature keys for a given feature.
        /// </summary>
        public HashSet<TileFeatureKey> GetConnectedFeatureKeys(Tile tile, FeatureType featureType, int featureIndex)
        {
            HashSet<TileFeatureKey> connectedKeys = new HashSet<TileFeatureKey>();
            HashSet<TileFeatureKey> visitedKeys = new HashSet<TileFeatureKey>();
            TileFeatureKey currentKey = new TileFeatureKey(tile, featureType, featureIndex);

            TraverseConnectedFeatures(currentKey, visitedKeys, connectedKeys, featureType);

            return connectedKeys;
        }

        private void TraverseConnectedFeatures(TileFeatureKey currentKey, HashSet<TileFeatureKey> visitedKeys, HashSet<TileFeatureKey> connectedKeys, FeatureType featureType)
        {
            if (visitedKeys.Contains(currentKey))
                return;

            visitedKeys.Add(currentKey);

            FeatureType currentfeatureType = currentKey.featureType;
            if (((featureType & FeatureType.ROAD_END) == FeatureType.ROAD_END) || ((featureType & FeatureType.ROAD_INTERSECTION) == FeatureType.ROAD_INTERSECTION))
                return;

            connectedKeys.Add(currentKey);
            int currentFeatureIndex = currentKey.featureIndex;
            Tile currentTile = currentKey.tile;

            // Check for multiple adjacent features on the same tile
            if (currentFeatureIndex != 0)
            {
                FeatureType centerFeatureType = currentTile.GetFeature(0);
                if ((centerFeatureType & featureType) != 0) // Check if any of the features match
                {
                    TileFeatureKey newTileFeatureKey = new TileFeatureKey(currentTile, centerFeatureType, 0);
                    TraverseConnectedFeatures(newTileFeatureKey, visitedKeys, connectedKeys, featureType);
                }
            }
            else
            {
                for (int i = 1; i <= 4; i++)
                {
                    if (i == currentFeatureIndex)
                        continue;

                    FeatureType adjacentFeature = currentTile.GetFeature(i);
                    if ((adjacentFeature & featureType) != 0) // Check if any of the features match
                    {
                        TileFeatureKey newTileFeatureKey = new TileFeatureKey(currentTile, adjacentFeature, i);
                        TraverseConnectedFeatures(newTileFeatureKey, visitedKeys, connectedKeys, featureType);
                    }
                }
            }

            // Check adjacent tile that corresponds with the current feature
            if (currentFeatureIndex == 0)
                return;

            Vector2Int direction = Converters.ConvertEdgeIndexToDirection(currentFeatureIndex);

            Vector2Int adjacentPos = currentTile.GridPosition + direction * 8;
            Tile adjacentTile = GetTileAtPosition(adjacentPos);
            if (adjacentTile != null)
            {
                int adjacentFeatureIndex = Converters.ConvertDirectionToEdgeIndex(direction);
                if (adjacentFeatureIndex > 0)
                {
                    FeatureType adjacentFeature = adjacentTile.GetFeature(adjacentFeatureIndex);
                    if ((adjacentFeature & featureType) != 0) // Check if any of the features match
                    {
                        TileFeatureKey newTileFeatureKey = new TileFeatureKey(adjacentTile, adjacentFeature, adjacentFeatureIndex);
                        TraverseConnectedFeatures(newTileFeatureKey, visitedKeys, connectedKeys, featureType);
                    }
                }
            }
        }
    }
}
