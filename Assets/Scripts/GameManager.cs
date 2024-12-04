using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private PreviewUIController previewUIController;
        private MeepleManager meepleManager;
        private List<Player> players = new List<Player>();
        private int currentPlayerIndex = 0;

        private Player currentPlayer;
        private Tile currentTile;

        void Awake()
        {
            InitializePlayers();
            boardManager = BoardManager.Instance;
            boardManager.OnNoMoreTilePlacements += DisableFurtherPlacement;
            boardManager.OnHighlightTileCreated += HandleHighlightTileCreated;
            boardManager.OnPreviewImageUpdate += HandlePreviewImageUpdate;
            meepleManager = MeepleManager.Instance;
        }

        void Start()
        {
            
        }

        private void InitializePlayers()
        {
            // Example initialization. Replace with dynamic player setup if needed.
            players.Add(new Player("Alice", PlayerColor.RED, 1));
            players.Add(new Player("Bob", PlayerColor.YELLOW, 2));
        }

        /// <summary>
        /// Switches the turn to the next player.
        /// </summary>
        private void SwitchTurn()
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            Debug.Log($"GameManager: It's now {players[currentPlayerIndex].PlayerName}'s turn.");

            boardManager.DrawNextTile();
            boardManager.HighlightAvailablePositions();
            previewUIController.EnableRotation();
            previewUIController.ShowPreview();

            // Update UI to reflect the current player's turn
            //uiManager.UpdateCurrentPlayer(players[currentPlayerIndex]);
        }

        /// <summary>
        /// Ends the game.
        /// </summary>
        private void EndGame()
        {
            Debug.Log("GameManager: Ending the game.");
            foreach(Player player in players)
            {
                Debug.Log($"GameManager: Player {player.PlayerName} scored {player.Score} points.");
            }
            // Implement your end-of-game logic here
            // Example: Display end-game UI, calculate scores, determine winner, etc.
            //uiManager.DisplayEndGameScreen(players);
        }

        /// <summary>
        /// Called when a highlight tile is selected (clicked).
        /// </summary>
        public void OnTileSelected(Vector2Int position, GameObject highlightTile)
        {
            Debug.Log($"GameManager: Tile selected at position: {position}");
            int rotationState = previewUIController.GetPreviewRotationState();
            Tile placedTile = boardManager.PlaceTile(position, rotationState, highlightTile);
            if (placedTile != null)
            {
                previewUIController.ResetPreviewRotationState();
                previewUIController.HidePreview();
                previewUIController.DisableRotation();
                InitiateMeeplePlacement(players[currentPlayerIndex], placedTile);
            }
        }

        private void InitiateMeeplePlacement(Player player, Tile placedTile)
        {
            currentPlayer = player;
            currentTile = placedTile;

            if (currentPlayer.MeepleCount == 0)
            {
                Debug.Log($"GameManager: Player {currentPlayer.PlayerName} has no meeples left to place.");
                CalculateCompletedFeatures();
                SwitchTurn();
                return;
            }

            List<(FeatureType, int)> featuresAndEdgeIndexes = placedTile.GetAllFeatures();
            List<(FeatureType, int, MeepleData)> availableFeaturesForMeeples = new List<(FeatureType, int, MeepleData)>();

            foreach (var featureAndEdgeIndex in featuresAndEdgeIndexes)
            {
                FeatureType featureType = featureAndEdgeIndex.Item1;
                int edgeIndex = featureAndEdgeIndex.Item2;
                if (meepleManager == null)
                {
                    meepleManager = MeepleManager.Instance;
                }
                bool canPlaceMeeple = meepleManager.CanPlaceMeeple(placedTile, featureType, edgeIndex);
                if (canPlaceMeeple)
                {
                    MeepleData candidateMeepleData = meepleManager.PlaceMeeple(placedTile, featureType, edgeIndex);
                    availableFeaturesForMeeples.Add((featureType, edgeIndex, candidateMeepleData));
                }
            }
            if (availableFeaturesForMeeples.Count != 0)
            {
                Vector3 vector3 = new Vector3(placedTile.GridPosition[0], placedTile.GridPosition[1], 0);
                previewUIController.DisplayMeepleOptions(vector3, availableFeaturesForMeeples);

                List<Meeple> instantiatedGrayMeeples = previewUIController.InstantiatedGrayMeeples;

                foreach (var grayMeeple in instantiatedGrayMeeples)
                {
                    grayMeeple.OnGrayMeepleClicked += OnMeepleSelected;
                }
            }
            else
            {
                Debug.Log("GameManager: No meeple placement options available.");
                CalculateCompletedFeatures();
                SwitchTurn();
            }
        }

        private void OnMeepleSelected(Meeple selectedMeeple)
        {
            PlayerColor playerColor = currentPlayer.PlayerColor;
            MeepleData selectedMeepleData = selectedMeeple.MeepleData;
            if (selectedMeepleData == null)
            {
                Debug.LogError("GameManager: MeepleData is null.");
                return;
            }
            MeepleType meepleType = selectedMeepleData.GetMeepleType();
            selectedMeepleData.SetPlayerColor(playerColor);
            selectedMeepleData.SetMeepleType(meepleType);
            selectedMeeple.UpdateMeepleVisual(meepleType, playerColor);
            selectedMeeple.OnGrayMeepleClicked -= OnMeepleSelected;
            previewUIController.RemoveUIMeeple(selectedMeeple);
            foreach (var meeple in previewUIController.InstantiatedGrayMeeples)
            {
                meeple.OnGrayMeepleClicked -= OnMeepleSelected;
                meepleManager.RemoveMeeple(meeple.MeepleData);
            }
            previewUIController.ClearMeepleOptions();
            previewUIController.AddInstantiatedPlayerMeeple(selectedMeeple);
            currentPlayer.MeepleCount--;

            CalculateCompletedFeatures();
            SwitchTurn();
        }


        private void CalculateCompletedFeatures()
        {
            Dictionary<TileFeatureKey, MeepleData> tileFeatureMeepleMap = meepleManager.TileFeatureMeepleMap;
            List<MeepleData> meeplesFlaggedForRemoval = new List<MeepleData>();
            foreach (TileFeatureKey key in tileFeatureMeepleMap.Keys)
            {
                Tile tile = key.tile;
                FeatureType featureType = key.featureType;
                int featureIndex = key.featureIndex;
                MeepleData meepleData = tileFeatureMeepleMap[key];
                if (boardManager.IsFeatureComplete(tile, featureType, featureIndex))
                {
                    PlayerColor playerColor = meepleData.GetPlayerColor();
                    int score = CalculateScore(tile, featureType, featureIndex);
                    Player scoringPlayer = players.Find(p => p.PlayerColor == playerColor);
                    scoringPlayer.Score += score;
                    scoringPlayer.MeepleCount++;
                    Debug.Log($"GameManager: Player {scoringPlayer.PlayerName} scored {score} points. TOTAL: {scoringPlayer.Score}");
                    meeplesFlaggedForRemoval.Add(meepleData);
                }
            }
            RemovePlayerMeeples(meeplesFlaggedForRemoval);
        }
        private void RemovePlayerMeeples(List<MeepleData> meeplesFlaggedForRemoval)
        {
            foreach (MeepleData meepleData in meeplesFlaggedForRemoval)
            {
                meepleManager.RemoveMeeple(meepleData);
                previewUIController.RemoveUIMeeple(meepleData.MeepleID);
            }
        }

        private int CalculateScore(Tile tile, FeatureType featureType, int featureIndex, bool endGame = false)
        {
            if ((featureType & FeatureType.MONASTERY) == FeatureType.MONASTERY)
            {
                return 9;
            }
            else if ((featureType & FeatureType.ROAD) == FeatureType.ROAD)
            {
                return boardManager.GetConnectedTiles(tile, featureType, featureIndex).Count;
            }
            else if ((featureType & FeatureType.CITY) == FeatureType.CITY)
            {
                if (endGame)
                {
                    return boardManager.GetConnectedTiles(tile, featureType, featureIndex).Count;
                }
                return boardManager.GetConnectedTiles(tile, featureType, featureIndex).Count * 2;
            }
            else throw new InvalidOperationException("Invalid feature type.");
        }

        private void DisableFurtherPlacement()
        {
            Debug.Log("GameManager: Disabling further tile placements.");
            // Hide or disable the preview image
            if (previewUIController != null)
            {
                previewUIController.HidePreview();
                previewUIController.DisableRotation();
            }
            EndGame();
        }

        /// <summary>
        /// Handler method called when the preview image needs to be updated.
        /// </summary>
        /// <param name="sprite">The sprite to display in the preview UI.</param>
        private void HandlePreviewImageUpdate(Sprite sprite)
        {
            if (previewUIController != null)
            {
                previewUIController.UpdatePreview(sprite);
                Debug.Log("GameManager: Updated the preview UI.");
            }
            else
            {
                Debug.LogError("GameManager: PreviewUIController reference is not assigned.");
            }
        }

        /// <summary>
        /// Handles the creation of a new HighlightTile by subscribing to its click event.
        /// </summary>
        /// <param name="ht">The newly created HighlightTile.</param>
        private void HandleHighlightTileCreated(HighlightTile ht)
        {
            if (ht != null)
            {
                ht.OnTileClicked += OnTileSelected;
            }
        }
    }
}
