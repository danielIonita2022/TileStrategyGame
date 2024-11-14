using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Manages the associations between Tiles and Meeples.
    /// </summary>
    public class MeepleManager : MonoBehaviour
    {
        // Singleton Instance
        public static MeepleManager Instance { get; private set; }

        [SerializeField] private BoardManager boardManager;

        // Dictionary to map Tile and Feature to Meeple
        private Dictionary<TileFeatureKey, Meeple> tileFeatureMeepleMap = new Dictionary<TileFeatureKey, Meeple>();

        void Awake()
        {
            // Implement Singleton Pattern
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            boardManager = BoardManager.Instance;
        }

        /// <summary>
        /// Attempts to place a meeple on a specific feature of a tile.
        /// </summary>
        /// <param name="tile">The tile where the meeple is to be placed.</param>
        /// <param name="featureType">The feature type to place the meeple on.</param>
        /// <param name="featureIndex">The unique index of the feature on the tile.</param>
        /// <param name="meepleType">The type of meeple to place.</param>
        /// <returns>True if placement is successful; otherwise, false.</returns>
        public bool PlaceMeeple(Tile tile, FeatureType featureType, int featureIndex, MeepleType meepleType)
        {
            TileFeatureKey key = new TileFeatureKey(tile, featureType, featureIndex);

            // Check if the feature is already occupied
            if (tileFeatureMeepleMap.ContainsKey(key))
            {
                Debug.LogWarning($"Feature {featureType}#{featureIndex} on Tile at {tile.GridPosition} is already occupied.");
                return false;
            }

            // Check if placement is valid according to game rules
            if (!CanPlaceMeeple(tile, featureType, featureIndex))
            {
                Debug.LogWarning($"Cannot place meeple on Feature {featureType}#{featureIndex} of Tile at {tile.GridPosition}.");
                return false;
            }

            MeepleType convertedMeepleType = Converters.ConvertFeatureTypeToMeepleType(featureType);
            Meeple meeple = new Meeple(PlayerColor.RED, convertedMeepleType);
            if (meeple == null)
            {
                Debug.LogError($"No available meeples of type {convertedMeepleType}.");
                return false;
            }

            // Register the association
            tileFeatureMeepleMap[key] = meeple;

            Debug.Log($"Placed {convertedMeepleType} meeple on {featureType}#{featureIndex} of Tile at {tile.GridPosition}.");

            // Check for feature completion
            if (IsFeatureComplete(tile, featureType, featureIndex))
            {
                //HandleFeatureCompletion(tile, featureType, featureIndex);
            }

            return true;
        }

        /// <summary>
        /// Determines if a meeple can be placed on the specified feature according to game rules.
        /// </summary>
        private bool CanPlaceMeeple(Tile tile, FeatureType featureType, int featureIndex)
        {
            // Implement your game-specific rules here
            // Example Rules:
            // 1. Feature is eligible for meeple placement.
            // 2. Feature is not already occupied.
            // 3. Connected features do not have meeples.

            // Rule 1: Eligibility
            if (featureType != FeatureType.CITY &&
                featureType != FeatureType.ROAD &&
                featureType != FeatureType.MONASTERY)
            {
                return false;
            }

            // Rule 2: Occupied Check is already done in PlaceMeeple

            // Rule 3: Connected Features Check
            List<Meeple> connectedMeeples = GetConnectedMeeples(tile, featureType, featureIndex);
            foreach (var connectedMeeple in connectedMeeples)
            {
                if (connectedMeeple != null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves all meeples connected to the specified feature.
        /// </summary>
        private List<Meeple> GetConnectedMeeples(Tile tile, FeatureType featureType, int featureIndex)
        {
            List<Meeple> connectedMeeples = new List<Meeple>();

            // Implement logic to traverse connected features and collect meeples
            // This may involve querying adjacent tiles and their features

            // Placeholder Implementation:
            // Assuming you have a method to get connected tiles and features
            List<TileFeatureKey> connectedKeys = boardManager.GetConnectedFeatureKeys(tile, featureType, featureIndex);

            foreach (var key in connectedKeys)
            {
                if (tileFeatureMeepleMap.TryGetValue(key, out Meeple meeple))
                {
                    connectedMeeples.Add(meeple);
                }
            }

            return connectedMeeples;
        }

        /// <summary>
        /// Determines if a feature is complete based on game rules.
        /// </summary>
        private bool IsFeatureComplete(Tile tile, FeatureType featureType, int featureIndex)
        {
            return boardManager.IsFeatureComplete(tile, featureType, featureIndex);
        }

    }
}
