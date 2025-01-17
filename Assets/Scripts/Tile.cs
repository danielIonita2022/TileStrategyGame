using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts
{
    public class Tile : NetworkBehaviour
    {
        [Header("Tile Data")]
        public TileData tileData;

        [Header("Tile Position")]
        public Vector2Int GridPosition;

        private int rotationState = 0;
        private readonly float[] rotationAngles = { 0f, 90f, 180f, 270f };

        public FeatureType CurrentNorthEdge { get; set; }
        public FeatureType CurrentEastEdge { get; set; }
        public FeatureType CurrentSouthEdge { get; set; }
        public FeatureType CurrentWestEdge { get; set; }
        public FeatureType CurrentCenterFeature { get; set; }

        public void Awake()
        {
            if (tileData != null)
            {
                CurrentNorthEdge = tileData.northEdge;
                CurrentEastEdge = tileData.eastEdge;
                CurrentSouthEdge = tileData.southEdge;
                CurrentWestEdge = tileData.westEdge;
                CurrentCenterFeature = tileData.centerFeature;

                Debug.Log($"Tile at {GridPosition} original edges stored.");
            }
            else
            {
                Debug.LogWarning($"Tile at {GridPosition} has no TileData assigned.");
            }
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"Tile: Entered OnNetworkSpawn for tile {this.name}");
            
        }

        private void Start()
        {
            if (tileData != null)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = tileData.tileSprite;
                }
            }
        }

        public Vector3 GridToWorldPosition2D()
        {
            return new Vector3(GridPosition[0], GridPosition[1], 0);
        }

        public void AssignFeatures()
        {
            if (tileData != null)
            {
                CurrentNorthEdge = tileData.northEdge;
                CurrentEastEdge = tileData.eastEdge;
                CurrentSouthEdge = tileData.southEdge;
                CurrentWestEdge = tileData.westEdge;
                CurrentCenterFeature = tileData.centerFeature;
            }
        }

        /// <summary>
        /// Retrieves the feature type for a specific edge index of a tile.
        /// </summary>
        public FeatureType GetFeature(int edgeIndex)
        {
            switch (edgeIndex)
            {
                case 0:
                    return CurrentCenterFeature;
                case 1:
                    return CurrentNorthEdge;
                case 2:
                    return CurrentEastEdge;
                case 3:
                    return CurrentSouthEdge;
                case 4:
                    return CurrentWestEdge;
                
                default:
                    return FeatureType.NONE;
            }
        }

        public List<(FeatureType, int)> GetAllFeatures()
        {
            List<(FeatureType, int)> featuresAndEdgeIndexes = new List<(FeatureType, int)>();

            for (int i = 0; i < 5; i++)
            {
                FeatureType feature = GetFeature(i);
                featuresAndEdgeIndexes.Add((feature, i));
            }
            return featuresAndEdgeIndexes;
        }

        public FeatureType GetSpecialFeatures()
        {
            if (CurrentCenterFeature.HasFlag(FeatureType.SHIELD))
            {
                return FeatureType.SHIELD;
            }

            else if (CurrentNorthEdge.HasFlag(FeatureType.SHIELD))
            {
                return FeatureType.SHIELD;
            }

            else if (CurrentEastEdge.HasFlag(FeatureType.SHIELD))
            {
                return FeatureType.SHIELD;
            }

            else if (CurrentSouthEdge.HasFlag(FeatureType.SHIELD))
            {
                return FeatureType.SHIELD;
            }

            else if (CurrentWestEdge.HasFlag(FeatureType.SHIELD))
            {
                return FeatureType.SHIELD;
            }

            return FeatureType.NONE;
        }

        /// <summary>
        /// Rotates the tile to the specified rotation state.
        /// </summary>
        /// <param name="rotationState">0-3 representing 0°, 90°, 180°, 270°</param>
        public void RotateTile(int rotationState)
        {
            this.rotationState = rotationState % 4; // Ensure it's within 0-3
            ApplyRotation();
            RotateFeatures();

            Debug.Log($"Rotated tile at {GridPosition} to {rotationState * 90} degrees.");
        }

        /// <summary>
        /// Applies the current rotation state to the tile's transform.
        /// </summary>
        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(0, 0, -rotationAngles[rotationState]);
        }

        /// <summary>
        /// Rotates the edge features based on the current rotation state.
        /// </summary>
        private void RotateFeatures()
        {

            // Apply rotation steps based on rotationState
            for (int i = 0; i < rotationState; i++)
            {
                FeatureType temp = CurrentNorthEdge;
                CurrentNorthEdge = CurrentWestEdge;
                CurrentWestEdge = CurrentSouthEdge;
                CurrentSouthEdge = CurrentEastEdge;
                CurrentEastEdge = temp;
            }

            Debug.Log($"Tile at {GridPosition} edges after rotation: North={CurrentNorthEdge}, East={CurrentEastEdge}, South={CurrentSouthEdge}, West={CurrentWestEdge}");
        }

        /// <summary>
        /// Retrieves the current rotation state.
        /// </summary>
        public int GetRotationState()
        {
            return rotationState;
        }
    }
}
