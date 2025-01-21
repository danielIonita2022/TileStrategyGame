using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

namespace Assets.Scripts
{
    /// <summary>
    /// Manages the associations between Tiles and Meeples.
    /// </summary>
    public class MeepleManager : NetworkBehaviour
    {
        // Singleton Instance
        public static MeepleManager Instance { get; private set; }

        [SerializeField] private BoardManager boardManager;

        // Dictionary to map Tile and Feature to Meeple
        private Dictionary<TileFeatureKey, MeepleData> tileFeatureMeepleMap = new Dictionary<TileFeatureKey, MeepleData>();

        public Dictionary<TileFeatureKey, MeepleData> TileFeatureMeepleMap => tileFeatureMeepleMap;

        private static HashSet<int> _usedMeepleIDs = new HashSet<int>();
        private static Random _random = new Random();

        public Action<MeepleData> GetMeepleData;

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

        private int GenerateUniqueID()
        {
            int newID;
            do
            {
                newID = _random.Next(1, 1000000);
            } while (_usedMeepleIDs.Contains(newID));
            _usedMeepleIDs.Add(newID);
            return newID;
        }

        /// <summary>
        /// Attempts to place a meeple on a specific feature of a tile.
        /// </summary>
        /// <param name="tile">The tile where the meeple is to be placed.</param>
        /// <param name="featureType">The feature type to place the meeple on.</param>
        /// <param name="featureIndex">The unique index of the feature on the tile.</param>
        /// <param name="meepleType">The type of meeple to place.</param>
        public MeepleData PlaceMeeple(Tile tile, FeatureType featureType, int featureIndex)
        {
            TileFeatureKey key = new TileFeatureKey(tile, featureType, featureIndex);

            if (tileFeatureMeepleMap.ContainsKey(key))
            {
                Debug.Log($"Feature {featureType}#{featureIndex} on Tile at {tile.GridPosition} is already occupied.");
                return null;
            }

            MeepleType convertedMeepleType = Converters.ConvertFeatureTypeToMeepleType(featureType);
            int meepleID = GenerateUniqueID();
            MeepleData meepleData = new MeepleData(PlayerColor.GRAY, convertedMeepleType, meepleID);
            if (meepleData == null)
            {
                Debug.LogError($"No available meeples of type {convertedMeepleType}.");
                return null;
            }

            tileFeatureMeepleMap.Add(key, meepleData);
            Debug.Log($"MeepleManager: Added to tileFeatureMeepleMap meeple with ID {meepleData.MeepleID}");
            Debug.Log($"MeepleManager: tileFeatureMeepleMap size upon placing a meeple: {tileFeatureMeepleMap.Count}");
            return meepleData;
        }

        [ServerRpc (RequireOwnership = false)]
        public void RemoveMeepleServerRpc(int meepleDataId)
        {
            MeepleData meepleData = tileFeatureMeepleMap.FirstOrDefault(x => x.Value.MeepleID == meepleDataId).Value;
            TileFeatureKey key = tileFeatureMeepleMap.FirstOrDefault(x => x.Value == meepleData).Key;
            tileFeatureMeepleMap.Remove(key);
            Debug.Log($"MeepleManager: Removed from tilefeatureMeepleMap meeple with ID {meepleDataId}");
        }

        public void UpdateMeepleData(int meepleID, PlayerColor playerColor)
        {
            MeepleData meepleData = tileFeatureMeepleMap.FirstOrDefault(x => x.Value.MeepleID == meepleID).Value;
            meepleData.SetPlayerColor(playerColor);
        }

        /// <summary>
        /// Determines if a meeple can be placed on the specified feature according to game rules.
        /// </summary>
        public bool CanPlaceMeeple(Tile tile, FeatureType featureType, int featureIndex)
        {
            // Implement your game-specific rules here
            // Example Rules:
            // 1. Feature is eligible for meeple placement.
            // 2. Feature is not already occupied.
            // 3. Connected features do not have meeples.

            // Rule 1: Eligibility
            if (((featureType & FeatureType.CITY) != FeatureType.CITY) &&
                ((featureType & FeatureType.ROAD) != FeatureType.ROAD) &&
                ((featureType & FeatureType.MONASTERY) != FeatureType.MONASTERY))
            {
                return false;
            }

            if ((featureType & FeatureType.MONASTERY) != FeatureType.MONASTERY && featureIndex == 0)
            {
                return false;
            }

            // Rule 3: Connected Features Check
            Dictionary<TileFeatureKey, MeepleData> connectedMeeples = GetConnectedMeeples(tile, featureType, featureIndex);

            if (connectedMeeples.Count() > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves all meeples connected to the specified feature.
        /// </summary>
        public Dictionary<TileFeatureKey, MeepleData> GetConnectedMeeples(Tile tile, FeatureType featureType, int featureIndex)
        {
            Dictionary<TileFeatureKey, MeepleData> connectedMeeples = new Dictionary<TileFeatureKey, MeepleData>();
            HashSet<TileFeatureKey> connectedKeys = boardManager.GetConnectedFeatureKeys(tile, featureType, featureIndex);

            foreach (var key in connectedKeys)
            {
                if (tileFeatureMeepleMap.TryGetValue(key, out MeepleData meepleData))
                {
                    connectedMeeples.Add(key, meepleData);
                }
                else
                {
                    Debug.Log($"No meeple found on connected feature {key.featureType}#{key.featureIndex} of Tile at {key.tile.GridPosition}.");
                }
            }
            return connectedMeeples;
        }

        public List<PlayerColor> GetScoringPlayerColorMeeples(Tile tile, FeatureType featureType, int featureIndex)
        {
            Dictionary<TileFeatureKey, MeepleData> connectedMeeples = GetConnectedMeeples(tile, featureType, featureIndex);
            Dictionary<PlayerColor, int> playersWithMeepleAmount = new Dictionary<PlayerColor, int>();
            foreach (PlayerColor playerColor in Enum.GetValues(typeof(PlayerColor)))
            {
                playersWithMeepleAmount[playerColor] = 0;
            }
            foreach (var meepleData in connectedMeeples.Values)
            {
                PlayerColor playerColor = meepleData.GetPlayerColor();
                playersWithMeepleAmount[playerColor] += 1;
            }
            int maxMeepleAmount = playersWithMeepleAmount.Values.Max();
            List<PlayerColor> scoringPlayers = playersWithMeepleAmount.Where(x => x.Value == maxMeepleAmount).Select(x => x.Key).ToList();
            return scoringPlayers;
        }

    }
}
