using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Data")]
    public TileData tileData;

    [Header("Tile Position")]
    public Vector2Int gridPosition;

    // Rotation state (e.g., 0, 90, 180, 270 degrees)
    private int rotationState = 0;

    void Start()
    {
        if (tileData != null && tileData.tileSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = tileData.tileSprite;
            }

            // Handle special features
            HandleSpecialFeatures();
        }
        else
        {
            Debug.LogWarning($"Tile at {gridPosition} has no TileData or sprite assigned.");
        }
    }

    void HandleSpecialFeatures()
    {
        if (tileData.centerFeature.HasFlag(FeatureType.SHIELD))
        {
            // Implement logic for SHIELD, e.g., doubling points
            Debug.Log($"Tile at {gridPosition} has a SHIELD at the center. Points doubled.");
            // Example: Add a shield sprite overlay or modify game state
        }

        // Check edges for SHIELD if applicable
        else if (tileData.northEdge.HasFlag(FeatureType.SHIELD))
        {
            Debug.Log($"Tile at {gridPosition} has a SHIELD on the North edge.");
            // Implement specific logic for SHIELD on the North edge
        }

        else if (tileData.eastEdge.HasFlag(FeatureType.SHIELD))
        {
            Debug.Log($"Tile at {gridPosition} has a SHIELD on the East edge.");
            // Implement specific logic for SHIELD on the East edge
        }

        else if (tileData.southEdge.HasFlag(FeatureType.SHIELD))
        {
            Debug.Log($"Tile at {gridPosition} has a SHIELD on the South edge.");
            // Implement specific logic for SHIELD on the South edge
        }

        else if (tileData.westEdge.HasFlag(FeatureType.SHIELD))
        {
            Debug.Log($"Tile at {gridPosition} has a SHIELD on the West edge.");
            // Implement specific logic for SHIELD on the West edge
        }
    }

    // Method to rotate the tile
    public void RotateTile(float angle)
    {
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Debug.Log($"Rotated tile at {gridPosition} by {angle} degrees.");

        // Update edge features based on rotation
        RotateFeatures(angle);
    }

    void RotateFeatures(float angle)
    {
        // Update edge features based on rotation
        // Assuming rotation is in multiples of 90 degrees
        int rotationSteps = Mathf.RoundToInt(angle / 90f) % 4;

        for (int i = 0; i < rotationSteps; i++)
        {
            FeatureType temp = tileData.northEdge;
            tileData.northEdge = tileData.westEdge;
            tileData.westEdge = tileData.southEdge;
            tileData.southEdge = tileData.eastEdge;
            tileData.eastEdge = temp;
        }

        // If center feature rotation affects any logic, handle here
        // For SHIELD, if it's a central feature, it may not require rotation
    }
}
