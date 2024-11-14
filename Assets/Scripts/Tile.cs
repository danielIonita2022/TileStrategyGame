using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class Tile : MonoBehaviour
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

        private void Awake()
        {
            if (tileData != null)
            {
                // Store original edges
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

        private void Start()
        {
            if (tileData != null)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = tileData.tileSprite;
                }
                HandleSpecialFeatures();
            }
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

        private void HandleSpecialFeatures()
        {
            if (CurrentCenterFeature.HasFlag(FeatureType.SHIELD))
            {
                // Implement logic for SHIELD, e.g., doubling points
                Debug.Log($"Tile at {GridPosition} has a SHIELD at the center. Points doubled.");
                // Example: Add a shield sprite overlay or modify game state
            }

            // Check edges for SHIELD if applicable
            else if (CurrentNorthEdge.HasFlag(FeatureType.SHIELD))
            {
                Debug.Log($"Tile at {GridPosition} has a SHIELD on the North edge.");
                // Implement specific logic for SHIELD on the North edge
            }

            else if (CurrentEastEdge.HasFlag(FeatureType.SHIELD))
            {
                Debug.Log($"Tile at {GridPosition} has a SHIELD on the East edge.");
                // Implement specific logic for SHIELD on the East edge
            }

            else if (CurrentSouthEdge.HasFlag(FeatureType.SHIELD))
            {
                Debug.Log($"Tile at {GridPosition} has a SHIELD on the South edge.");
                // Implement specific logic for SHIELD on the South edge
            }

            else if (CurrentWestEdge.HasFlag(FeatureType.SHIELD))
            {
                Debug.Log($"Tile at {GridPosition} has a SHIELD on the West edge.");
                // Implement specific logic for SHIELD on the West edge
            }
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
