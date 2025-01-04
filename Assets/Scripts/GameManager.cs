using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;

        [SerializeField] private BoardManager boardManager;
        [SerializeField] private GameUIController gameUIController;
        public LobbyManager lobbyManager;
        private MeepleManager meepleManager;

        private List<NetworkedPlayer> players = new List<NetworkedPlayer>();
        private int currentPlayerIndex = 0;
        private NetworkedPlayer currentPlayer;

        private Tile currentTile;

        private GameState gameState = GameState.Idle;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                boardManager = BoardManager.Instance;
                boardManager.OnNoMoreTilePlacements += EndGame;
                boardManager.OnHighlightTileCreated += HandleHighlightTileCreated;
                boardManager.OnPreviewImageUpdate += HandlePreviewImageUpdate;
                gameUIController.OnMeepleSkipped += SkipTurn;
                meepleManager = MeepleManager.Instance;
                DontDestroyOnLoad(gameObject);
                Instance.NetworkObject.Spawn();
                Debug.Log("GameManager: Exiting Awake");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log("GameManager: Entered OnNetworkSpawn");
            Debug.Log($"IsHost: {IsHost} \n IsServer: {IsServer} \n IsClient: {IsClient}");
            if (IsHost)
            {
                // Subscribe to player connection events
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var player = client.PlayerObject.GetComponent<NetworkedPlayer>();
                    if (player != null && !players.Contains(player))
                    {
                        players.Add(player);
                        Debug.Log($"GameManager: Player {player.PlayerName.Value} connected with color {player.PlayerColorEnum.Value}");
                    }
                }
                Debug.Log("GameManager: Inside OnNetworkSpawn, before checking player count.");
                if (players.Count >= 2) 
                {
                    StartCoroutine(StartTurn());
                }
                else
                {
                    Debug.Log($"GameManager: Currently there are {players.Count} players. Waiting for players to connect...");
                }
            }
        }

        public override void OnDestroy()
        {
            if (IsHost)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            // Retrieve the player's NetworkedPlayer component
            var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkedPlayer>();
            if (player != null && !players.Contains(player))
            {
                players.Add(player);
                Debug.Log($"GameManager: Player {player.PlayerName.Value} connected with color {player.PlayerColorEnum.Value}");
            }

        }

        private void HandleClientDisconnected(ulong clientId)
        {
            var player = players.FirstOrDefault(p => p.OwnerClientId == clientId);
            if (player != null)
            {
                players.Remove(player);
                Debug.Log($"GameManager: Player {player.PlayerName.Value} disconnected.");
            }
        }

        private IEnumerator StartTurn()
        {
            // Wait until all players are ready
            while (players.Count < 2) // Adjust based on your game's player requirements
            {
                yield return null;
            }

            boardManager.LoadTileDeck();
            boardManager.ShuffleTileDeck();
            boardManager.DrawNextTile(true);
            Vector2Int centerPos = Vector2Int.zero;
            boardManager.PlaceTile(centerPos, 0, true);

            while (true)
            {
                DisablePlayerControls();
                gameState = GameState.Idle;
                currentPlayer = players[currentPlayerIndex];
                Debug.Log($"GameManager: It's now {currentPlayer.PlayerName.Value}'s turn.");
                TurnPlayerClientRpc(currentPlayer.OwnerClientId);

                DrawNextTileServerRpc();
                boardManager.HighlightAvailablePositions();

                // Wait for the player to complete their turn
                yield return new WaitUntil(() => gameState == GameState.TurnCompleted);

                // Move to the next player
                CalculateCompletedFeatures();

                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            }
        }

        [ClientRpc]
        private void TurnPlayerClientRpc(ulong playerId)
        {
            if (playerId == NetworkManager.Singleton.LocalClientId)
            {
                // Enable player controls
                EnablePlayerControls();
            }
            else
            {
                // Disable player controls
                DisablePlayerControls();
            }
        }

        private void EnablePlayerControls()
        {
            gameUIController.HideEndTurnButton();
            gameUIController.ShowPreview();
            gameUIController.EnableRotation();
        }

        private void DisablePlayerControls()
        {
            gameUIController.HidePreview();
            gameUIController.HideEndTurnButton();
            // Optionally, display a message like "Waiting for other players..."
        }

        [ServerRpc(RequireOwnership = false)]
        private void DrawNextTileServerRpc(ServerRpcParams rpcParams = default)
        {
            boardManager.DrawNextTile();
        }

        private void SkipTurn()
        {
            Debug.Log($"GameManager: Player {currentPlayer.PlayerColorEnum.Value} skipped their turn.");
            foreach (var meeple in gameUIController.InstantiatedGrayMeeples)
            {
                meeple.OnGrayMeepleClicked -= OnMeepleSelected;
                meepleManager.RemoveMeeple(meeple.MeepleData);
            }
            gameUIController.ClearMeepleOptions();
            CompleteTurn();
        }

        /// <summary>
        /// Ends the game.
        /// </summary>
        private void EndGame()
        {
            gameState = GameState.Finished;
            DisableFurtherPlacement();
            Debug.Log("GameManager: Ending the game.");
            foreach (NetworkedPlayer player in players)
            {
                Debug.Log($"GameManager: Player {player.PlayerColorEnum.Value} scored {player.Score.Value} points.");
            }
            // Implement your end-of-game logic here
            // Example: Display end-game UI, calculate scores, determine winner, etc.
            // uiManager.DisplayEndGameScreen(players);
        }

        /// <summary>
        /// Called when a highlight tile is selected (clicked).
        /// </summary>
        public void OnTileSelected(Vector2Int position)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
            {
                Debug.LogError("NetworkManager is not initialized or not connected.");
                return;
            }

            if (gameState != GameState.Idle)
            {
                return;
            }

            if (currentPlayer.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("GameManager: It's not your turn.");
                return;
            }


            Debug.Log($"GameManager: Tile selected at position: {position}");
            int rotationState = gameUIController.GetPreviewRotationState();
            PlaceTileServerRpc(position, rotationState);
            
        }

        /// <summary>
        /// ServerRpc to handle tile placement requested by a client.
        /// Validates and places the tile server-side.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void PlaceTileServerRpc(Vector2Int position, int rotationState, ServerRpcParams rpcParams = default)
        {
            // Validate that it's the requesting player's turn
            if (currentPlayer.OwnerClientId != rpcParams.Receive.SenderClientId)
            {
                Debug.LogWarning("GameManager: Unauthorized tile placement attempt.");
                return;
            }

            Tile placedTile = boardManager.PlaceTile(position, rotationState);
            if (placedTile != null)
            {
                gameState = GameState.PlacingTile;
                gameUIController.ResetPreviewRotationState();
                gameUIController.HidePreview();
                gameUIController.DisableRotation();
                gameUIController.ShowEndTurnButton();
                InitiateMeeplePlacement(currentPlayer, placedTile);
            }
            else
            {
                Debug.LogWarning($"GameManager: Failed to place tile at {position}");
                // Optionally, notify the client about the failure
            }
        }

        private void InitiateMeeplePlacement(NetworkedPlayer player, Tile placedTile)
        {
            currentPlayer = player;
            currentTile = placedTile;

            if (currentPlayer.MeepleCount.Value == 0)
            {
                Debug.Log($"GameManager: Player {currentPlayer.PlayerColorEnum.Value} has no meeples left to place.");
                CompleteTurn();
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
                gameUIController.DisplayMeepleOptions(vector3, availableFeaturesForMeeples);

                List<Meeple> instantiatedGrayMeeples = gameUIController.InstantiatedGrayMeeples;

                gameState = GameState.PlacingMeeple;

                foreach (var grayMeeple in instantiatedGrayMeeples)
                {
                    grayMeeple.OnGrayMeepleClicked += OnMeepleSelected;
                }
            }
            else
            {
                Debug.Log("GameManager: No meeple placement options available.");
                CompleteTurn();
            }
        }

        private void OnMeepleSelected(Meeple selectedMeeple)
        {
            if (currentPlayer.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("GameManager: It's not your turn.");
                return;
            }

            PlayerColor playerColor = currentPlayer.PlayerColorEnum.Value;
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
            gameUIController.RemoveUIMeeple(selectedMeeple);
            foreach (var meeple in gameUIController.InstantiatedGrayMeeples)
            {
                meeple.OnGrayMeepleClicked -= OnMeepleSelected;
                meepleManager.RemoveMeeple(meeple.MeepleData);
            }
            gameUIController.ClearMeepleOptions();
            gameUIController.AddInstantiatedPlayerMeeple(selectedMeeple);
            currentPlayer.MeepleCount.Value--;

            CompleteTurn();
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
                bool isMeepleFlaggedForRemoval = meeplesFlaggedForRemoval.Any(m => m.Equals(meepleData));
                if (!isMeepleFlaggedForRemoval)
                {
                    if (boardManager.IsFeatureComplete(tile, featureType, featureIndex))
                    {
                        List<PlayerColor> scoringPlayerColors = meepleManager.GetScoringPlayerColorMeeples(tile, featureType, featureIndex);
                        List<NetworkedPlayer> scoringPlayers = players.FindAll(p => scoringPlayerColors.Contains(p.PlayerColorEnum.Value));
                        foreach (NetworkedPlayer scoringPlayer in scoringPlayers)
                        {
                            int score = CalculateScore(tile, featureType, featureIndex);
                            scoringPlayer.AddScore(score);
                            Debug.Log($"GameManager: Player {scoringPlayer.PlayerColorEnum.Value} scored {score} points. TOTAL: {scoringPlayer.Score.Value}");
                        }
                        Dictionary<TileFeatureKey, MeepleData> connectedMeeples = meepleManager.GetConnectedMeeples(tile, featureType, featureIndex);
                        foreach (var connectedMeeple in connectedMeeples)
                        {
                            meeplesFlaggedForRemoval.Add(connectedMeeple.Value);
                        }
                    }
                }
            }
            RemovePlayerMeeples(meeplesFlaggedForRemoval);
        }
        private void RemovePlayerMeeples(List<MeepleData> meeplesFlaggedForRemoval)
        {
            foreach (MeepleData meepleData in meeplesFlaggedForRemoval)
            {
                PlayerColor playerColor = meepleData.GetPlayerColor();
                NetworkedPlayer player = players.Find(p => p.PlayerColorEnum.Value == playerColor);
                player.MeepleCount.Value++;
                meepleManager.RemoveMeeple(meepleData);
                gameUIController.RemoveUIMeeple(meepleData.MeepleID);
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
                HashSet<Tile> connected_cities = boardManager.GetConnectedTiles(tile, featureType, featureIndex);
                int bonusPoints = 0;
                foreach (Tile connected_city in connected_cities)
                {
                    if (connected_city.GetSpecialFeatures() == FeatureType.SHIELD)
                    {
                        bonusPoints += 2;
                    }
                }
                if (endGame)
                {
                    return connected_cities.Count + bonusPoints / 2;
                }
                return connected_cities.Count * 2 + bonusPoints;
            }
            else throw new InvalidOperationException("Invalid feature type.");
        }

        private void DisableFurtherPlacement()
        {
            Debug.Log("GameManager: Disabling further tile placements.");
            // Hide or disable the preview image
            if (gameUIController != null)
            {
                gameUIController.HidePreview();
                gameUIController.DisableRotation();
            }
        }

        /// <summary>
        /// Handler method called when the preview image needs to be updated.
        /// </summary>
        /// <param name="sprite">The sprite to display in the preview UI.</param>
        private void HandlePreviewImageUpdate(Sprite sprite)
        {
            if (gameUIController != null)
            {
                gameUIController.UpdatePreview(sprite);
                Debug.Log("GameManager: Updated the preview UI.");
            }
            else
            {
                Debug.LogError("GameManager: GameUIController reference is not assigned.");
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

        /// <summary>
        /// Call this method to mark the current turn as completed.
        /// </summary>
        private void CompleteTurn()
        {
            gameState = GameState.TurnCompleted;
        }

        /// <summary>
        /// Assigns the next available color to a new player.
        /// </summary>
        /// <returns>The next available PlayerColor.</returns>
        public PlayerColor GetNextAvailableColor()
        {
            foreach (PlayerColor color in Enum.GetValues(typeof(PlayerColor)))
            {
                if (color == PlayerColor.GRAY) continue;

                if (!players.Any(p => p.PlayerColorEnum.Value == color))
                {
                    return color;
                }
            }
            return PlayerColor.GRAY; // Default or handle as needed
        }
    }
}
