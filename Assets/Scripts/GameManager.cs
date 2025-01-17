using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;

        [SerializeField] private BoardManager boardManager;
        [SerializeField] private UIManager uiManager;
        public LobbyManager lobbyManager;
        private MeepleManager meepleManager;

        private List<NetworkedPlayer> players = new List<NetworkedPlayer>();
        private int currentPlayerIndex = 0;
        private NetworkedPlayer currentPlayer;
        private NetworkVariable<ulong> currentPlayerClientId = new NetworkVariable<ulong>();

        private GameState gameState = GameState.NotStarted;


        public override void OnNetworkSpawn()
        {
            if (Instance == null)
            {
                Instance = this;
                if (BoardManager.Instance == null)
                {
                    Debug.LogError("BoardManager instance is not assigned.");
                    return;
                }
                boardManager = BoardManager.Instance;
                boardManager.OnNoMoreTilePlacements += EndGame;
                boardManager.OnHighlightTileCreated += HandleHighlightTileCreated;
                boardManager.OnPreviewImageUpdate += HandlePreviewImageUpdate;
                boardManager.OnTilePlaced += HandleTilePlaced;
                uiManager.OnMeepleSkipped += SkipTurn;
                uiManager.OnMeeplePlaced += RemoveGrayMeeples;
                meepleManager = MeepleManager.Instance;
                Debug.Log("GameManager: Entered OnNetworkSpawn");
                Debug.Log($"IsHost: {IsHost} \n IsServer: {IsServer} \n IsClient: {IsClient}");
                if (IsHost)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
                    InitializePlayers();
                }
                else
                {
                    Debug.Log("GameManager: Not the host.");
                }

            }
            else
            {
                Debug.LogWarning("GameManager: Instance already exists. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
        }

        private void InitializePlayers()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    Debug.LogWarning($"GameManager: Client {client.ClientId} has no PlayerObject.");
                    continue;
                }

                var player = client.PlayerObject.GetComponent<NetworkedPlayer>();
                if (player != null && !players.Contains(player))
                {
                    player.PlayerColorEnum.Value = GetNextAvailableColor();
                    player.MeepleCount.Value = 6;
                    player.Score.Value = 0;
                    players.Add(player);
                    Debug.Log($"GameManager: Player connected with color {player.PlayerColorEnum.Value}");
                }
                else
                {
                    Debug.LogWarning($"GameManager: NetworkedPlayer component missing on Client {client.ClientId}'s PlayerObject.");
                }
            }

            // Add host player if not already included
            var hostPlayer = NetworkManager.Singleton.LocalClient.PlayerObject?.GetComponent<NetworkedPlayer>();
            if (hostPlayer != null && !players.Contains(hostPlayer))
            {
                hostPlayer.PlayerColorEnum.Value = GetNextAvailableColor();
                hostPlayer.MeepleCount.Value = 6;
                hostPlayer.Score.Value = 0;
                players.Add(hostPlayer);
                Debug.Log($"GameManager: Host Player connected with color {hostPlayer.PlayerColorEnum.Value}");
            }

            Debug.Log($"GameManager: Players connected count after initialization: {players.Count}");
        }

        public override void OnDestroy()
        {
            currentPlayerClientId.OnValueChanged -= OnCurrentPlayerChanged;
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
                if (player != null && !players.Contains(player))
                {
                    player.PlayerColorEnum.Value = GetNextAvailableColor();
                    player.MeepleCount.Value = 6;
                    player.Score.Value = 0;
                    players.Add(player);
                    Debug.Log($"GameManager: Player connected with color {player.PlayerColorEnum.Value}");
                    Debug.Log($"GameManager: Players connected count: {players.Count}");
                }
                else
                {
                    Debug.LogWarning($"GameManager: NetworkedPlayer component missing on Client {clientId}'s PlayerObject.");
                }
            }

        }

        private void HandleClientDisconnected(ulong clientId)
        {
            var player = players.FirstOrDefault(p => p.OwnerClientId == clientId);
            if (player != null)
            {
                players.Remove(player);
                Debug.Log($"GameManager: Player disconnected.");
            }
        }

        private void OnCurrentPlayerChanged(ulong oldClientId, ulong newClientId)
        {
            Debug.Log($"The current player is now {newClientId}.");
            if (newClientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("It's my turn!");
                EnablePlayerControls();
            }
            else
            {
                DisablePlayerControls();
            }
        }

        public void StartGame()
        {
            if (gameState == GameState.NotStarted)
            {
                gameState = GameState.Idle;
                StartGameOnClientRpc();
                SetupGame();
                Debug.Log("GameManager: Current player index: " + currentPlayerIndex);

                StartCoroutine(StartTurn());
            }
        }

        private void SetupGame()
        {
            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            boardManager.ArrangeBoardClientRpc(seed);
            DrawNextTileClientRpc(true);
            PlaceStarterClientRpc();
            currentPlayerIndex = 0;
        }

        [ClientRpc]
        public void StartGameOnClientRpc()
        {
            Debug.Log("GameManager client: Transitioning to Game UI.");
            currentPlayerClientId.OnValueChanged += OnCurrentPlayerChanged;
            uiManager.lobbyUI.SetActive(false);
            uiManager.gameUI.SetActive(true);
        }

        private IEnumerator StartTurn()
        {
            while (true)
            {
                SetCurrentPlayerServerRpc(currentPlayerIndex);
                TurnPlayerClientRpc();
                DrawNextTileClientRpc();
                boardManager.HighlightAvailablePositionsClientRpc();

                while (gameState != GameState.TurnCompleted)
                {
                    yield return null;
                }
                Debug.Log("GameManager Server: Turn completed");

                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                Debug.Log("GameManager: Current player index: " + currentPlayerIndex);

                yield return null;
            }
        }

        [ServerRpc (RequireOwnership = false)]
        private void SetCurrentPlayerServerRpc(int currentPlayerIndex)
        {
            currentPlayer = players[currentPlayerIndex];
            Debug.Log("Server: Setting current player: " + currentPlayerIndex);
            currentPlayerClientId.Value = currentPlayer.OwnerClientId;
        }

        [ClientRpc]
        private void TurnPlayerClientRpc()
        {
            gameState = GameState.Idle;
            Debug.Log("Game state is now idle");
            if (currentPlayerClientId.Value == NetworkManager.Singleton.LocalClientId)
            {
                EnablePlayerControls();
            }
            else
            {
                DisablePlayerControls();
                Debug.Log("Waiting for other playes...");
            }
        }

        private void EnablePlayerControls()
        {
            uiManager.HideEndTurnButton();
            uiManager.ShowPreview();
            uiManager.EnableRotation();
        }

        private void DisablePlayerControls()
        {
            uiManager.HidePreview();
            uiManager.HideEndTurnButton();
        }

        [ClientRpc]
        private void PlaceStarterClientRpc()
        {
            Vector2Int centerPos = Vector2Int.zero;
            boardManager.PlaceTileClientRpc(centerPos, 0, true);
        }

        [ClientRpc]
        private void DrawNextTileClientRpc(bool isStarter = false)
        {
            boardManager.DrawNextTile(isStarter);
            gameState = GameState.Idle;
        }

        private void SkipTurn()
        {
            Debug.Log($"GameManager: Player {currentPlayerClientId} skipped their turn.");
            SkipTurnServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SkipTurnServerRpc()
        {
            foreach (var meepleGO in uiManager.InstantiatedGrayMeeples)
            {
                Meeple meeple = meepleGO.GetComponent<Meeple>();
                meeple.OnGrayMeepleClicked -= OnMeepleSelected;
                meepleManager.RemoveMeepleServerRpc(meeple.MeepleData.MeepleID);
            }
            uiManager.ClearMeepleOptionsServerRpc();
            CalculateCompletedFeaturesServerRpc();
        }

        /// <summary>
        /// Ends the game.
        /// </summary>
        private void EndGame()
        {
            gameState = GameState.Finished;
            DisableFurtherPlacement();
            Debug.Log("GameManager: Ending the game.");
            foreach (NetworkedPlayer player in players) // Doar serverul are acces la players, gaseste solutie pentru clienti
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
            Debug.Log("GameManager: Entered OnTileSelected");

            if (currentPlayerClientId.Value != NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("GameManager: Unauthorized tile placement attempt.");
                return;
            }

            if (gameState == GameState.PlacingTile || gameState == GameState.PlacingMeeple)
            {
                Debug.Log($"GameManager: GameState is {gameState}");
                return;
            }
            Debug.Log($"GameManager: Tile selected at position: {position}");
            int rotationState = uiManager.GetPreviewRotationState();
            PlaceTile(position, rotationState);
        }

        /// <summary>
        /// ServerRpc to handle tile placement requested by a client.
        /// Validates and places the tile server-side.
        /// </summary>
        private void PlaceTile(Vector2Int position, int rotationState)
        {
            if (currentPlayerClientId.Value != NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("GameManager: Unauthorized tile placement attempt.");
                return;
            }
            boardManager.PlaceTileServerRpc(position, rotationState);
        }

        private void HandleTilePlaced(Tile placedTile)
        {
            gameState = GameState.PlacingMeeple;
            Debug.Log("Game state is now in PlacingMeeple phase");
            Debug.Log($"GameManager: Last placed tile: {placedTile.name} at {placedTile.GridPosition}");
            if (placedTile != null)
            {
                if (currentPlayerClientId.Value == NetworkManager.Singleton.LocalClientId)
                {
                    uiManager.ResetPreviewRotationState();
                    uiManager.HidePreview();
                    uiManager.DisableRotation();
                    uiManager.ShowEndTurnButton();
                }
                InitiateMeeplePlacement(placedTile);
            }
            else
            {
                Debug.LogError("GameManager: Placed tile is null.");
                gameState = GameState.Idle;
            }
        }

        private void InitiateMeeplePlacement(Tile placedTile)
        {
            if (IsServer)
            {
                if (currentPlayer.MeepleCount.Value <= 0)
                {
                    Debug.Log($"GameManager: Player {currentPlayer.PlayerColorEnum} has no meeples left to place.");
                    CalculateCompletedFeaturesServerRpc();
                    return;
                }
            }

            Debug.Log($"GameManager: Initiating meeple placement on placed tile {placedTile.name} with grid position {placedTile.GridPosition}");

            if (gameState == GameState.TurnCompleted)
            {
                Debug.LogWarning("GameManager: Turn already completed! Can't place a meeple!");
                return;
            }

            if (IsServer)
            {
                Debug.Log("GameManager: Delegating meeple placement computation to the server.");
                ComputeMeeplePlacementOptionsServerRpc(placedTile.GridPosition);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ComputeMeeplePlacementOptionsServerRpc(Vector2Int placedTilePosition)
        {
            Tile placedTile = boardManager.GetTileAtPosition(placedTilePosition);
            if (placedTile == null)
            {
                Debug.LogError($"GameManager: No tile found at position {placedTilePosition}");
                return;
            }

            FeatureType[] featureTypes;
            int[] edgeIndexes;
            int[] meepleIDs;

            // Temporary lists for computation
            List<FeatureType> tempFeatureTypes = new List<FeatureType>();
            List<int> tempEdgeIndexes = new List<int>();
            List<int> tempMeepleIDs = new List<int>();

            List<(FeatureType, int)> featuresAndEdgeIndexes = placedTile.GetAllFeatures();

            foreach (var featureAndEdgeIndex in featuresAndEdgeIndexes)
            {
                FeatureType featureType = featureAndEdgeIndex.Item1;
                int edgeIndex = featureAndEdgeIndex.Item2;

                if (meepleManager.CanPlaceMeeple(placedTile, featureType, edgeIndex))
                {
                    MeepleData candidateMeepleData = meepleManager.PlaceMeeple(placedTile, featureType, edgeIndex);

                    tempFeatureTypes.Add(featureType);
                    tempEdgeIndexes.Add(edgeIndex);
                    tempMeepleIDs.Add(candidateMeepleData.MeepleID);
                }
            }

            // Convert temporary lists to arrays for serialization
            featureTypes = tempFeatureTypes.ToArray();
            edgeIndexes = tempEdgeIndexes.ToArray();
            meepleIDs = tempMeepleIDs.ToArray();

            // Notify clients
            NotifyClientsOfMeeplePlacementOptionsClientRpc(placedTilePosition, featureTypes, edgeIndexes, meepleIDs);
        }

        [ClientRpc]
        private void NotifyClientsOfMeeplePlacementOptionsClientRpc(
            Vector2Int placedTilePosition,
            FeatureType[] featureTypes,
            int[] edgeIndexes,
            int[] meepleIDs)
        {
            Debug.Log($"GameManager: Received meeple placement options for tile at {placedTilePosition}");

            if (featureTypes.Length > 0)
            {
                List<(FeatureType, int, MeepleData)> availableFeaturesForMeeples = new List<(FeatureType, int, MeepleData)>();

                for (int i = 0; i < featureTypes.Length; i++)
                {
                    MeepleData reconstructedMeepleData = new MeepleData(
                        PlayerColor.GRAY,
                        Converters.ConvertFeatureTypeToMeepleType(featureTypes[i]),
                        meepleIDs[i]
                    );

                    availableFeaturesForMeeples.Add((featureTypes[i], edgeIndexes[i], reconstructedMeepleData));
                }

                Vector3 tileWorldPosition = new Vector3(placedTilePosition.x, placedTilePosition.y, 0);
                uiManager.FindMeepleOptions(tileWorldPosition, availableFeaturesForMeeples);

                List<GameObject> instantiatedGrayMeeples = uiManager.InstantiatedGrayMeeples;

                foreach (var grayMeepleGO in instantiatedGrayMeeples)
                {
                    Meeple grayMeeple = grayMeepleGO.GetComponent<Meeple>();
                    grayMeeple.OnGrayMeepleClicked += OnMeepleSelected;
                    Debug.Log("GameManager: Gray meeple click event subscribed.");
                }
            }
            else
            {
                if (IsServer)
                {
                    Debug.Log("GameManager: No meeple placement options available.");
                    CalculateCompletedFeaturesServerRpc();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void HasSelectedMeepleServerRpc(int meepleID, ulong clientID)
        {
            PlayerColor playerColor = currentPlayer.PlayerColorEnum.Value;
            Debug.Log($"GameManager: Meeple will be of player color {playerColor}");
            Vector3 position = uiManager.MeepleObjectsPositionsServer[meepleID];
            meepleManager.UpdateMeepleData(meepleID, playerColor);
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientID }
                }
            };
            HasSelectedMeepleClientRpc(meepleID, position, playerColor, clientRpcParams);
        }

        [ClientRpc]
        private void HasSelectedMeepleClientRpc(int meepleID, Vector3 position, PlayerColor playerColor, ClientRpcParams clientRpcParams = default)
        {
            GameObject selectedMeepleGameObject = uiManager.FindGameObjectAtPosition(position);
            Meeple selectedMeeple = selectedMeepleGameObject.GetComponent<Meeple>();
            Debug.Log($"GameManager: Selected meeple with ID {selectedMeeple.MeepleData.MeepleID} found.");

            MeepleData selectedMeepleData = selectedMeeple.MeepleData;
            if (selectedMeepleData == null)
            {
                Debug.LogError("GameManager: MeepleData is null.");
                return;
            }

            MeepleType meepleType = selectedMeepleData.GetMeepleType();
            selectedMeeple.OnGrayMeepleClicked -= OnMeepleSelected;
            //uiManager.InstantiatedGrayMeeples.Remove(selectedMeepleGameObject);
            Debug.Log("GameManager: Before UpdateMeepleVisualServerRpc");
            uiManager.UpdatePlacedMeepleVisualServerRpc(meepleID, selectedMeepleGameObject.transform.position, meepleType, playerColor);
        }

        private void RemoveGrayMeeples()
        {
            Debug.Log("GameManager: After UpdateMeepleVisualServerRpc");
            foreach (var meepleGO in uiManager.InstantiatedGrayMeeples)
            {
                Meeple meeple = meepleGO.GetComponent<Meeple>();
                meeple.OnGrayMeepleClicked -= OnMeepleSelected;
                meepleManager.RemoveMeepleServerRpc(meeple.MeepleData.MeepleID);
            }
            uiManager.ClearMeepleOptionsServerRpc();
            if (IsServer)
            {
                ModifyPlayerMeepleCountServerRpc(1, true);
                CalculateCompletedFeaturesServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ModifyPlayerMeepleCountServerRpc(int quantity, bool isRemoval)
        {
            if (isRemoval)
            {
                currentPlayer.MeepleCount.Value -= quantity;
            }
            else
            {
                currentPlayer.MeepleCount.Value += quantity;
            }
        }

        private void OnMeepleSelected(Meeple selectedMeeple)
        {
            if (currentPlayerClientId.Value != NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("GameManager: It's not your turn.");
                return;
            }

            Debug.Log("GameManager: Selected a gray meeple");
            HasSelectedMeepleServerRpc(selectedMeeple.MeepleData.MeepleID, currentPlayerClientId.Value);
        }

        [ServerRpc(RequireOwnership = false)]
        private void CalculateCompletedFeaturesServerRpc()
        {
            Dictionary<TileFeatureKey, MeepleData> tileFeatureMeepleMap = meepleManager.TileFeatureMeepleMap;
            Debug.Log($"GameManager: TileFeatureMeepleMap size: {tileFeatureMeepleMap.Count}");
            List<MeepleData> meeplesFlaggedForRemoval = new List<MeepleData>();
            foreach (TileFeatureKey key in tileFeatureMeepleMap.Keys)
            {
                Tile tile = key.tile;
                FeatureType featureType = key.featureType;
                int featureIndex = key.featureIndex;
                MeepleData meepleData = tileFeatureMeepleMap[key];
                bool isMeepleFlaggedForRemoval = meeplesFlaggedForRemoval.Any(m => m.MeepleID == meepleData.MeepleID);
                if (!isMeepleFlaggedForRemoval)
                {
                    if (boardManager.IsFeatureComplete(tile, featureType, featureIndex))
                    {
                        Debug.Log($"The tile contains a completed {featureType}");
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
            Debug.Log("GameManager: Entered RemovePlayerMeeples");
            foreach (MeepleData meepleData in meeplesFlaggedForRemoval)
            {
                PlayerColor playerColor = meepleData.GetPlayerColor();
                Debug.Log($"GameManager: Player color of the current meeple is {playerColor}");
                Debug.Log($"GameManager: Number of players: {players.Count}");
                NetworkedPlayer player = players.FirstOrDefault(p => p.PlayerColorEnum.Value == playerColor);
                player.MeepleCount.Value++;
                meepleManager.RemoveMeepleServerRpc(meepleData.MeepleID);
                uiManager.RemoveUIMeepleServerRpc(meepleData.MeepleID);
            }
            CompleteTurnServerRpc();
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
                HashSet<Tile> connectedCities = boardManager.GetConnectedTiles(tile, featureType, featureIndex);
                int bonusPoints = 0;
                foreach (Tile connectedCity in connectedCities)
                {
                    if (connectedCity.GetSpecialFeatures() == FeatureType.SHIELD)
                    {
                        bonusPoints += 2;
                    }
                }
                if (endGame)
                {
                    return connectedCities.Count + bonusPoints / 2;
                }
                return connectedCities.Count * 2 + bonusPoints;
            }
            else throw new InvalidOperationException("Invalid feature type.");
        }

        private void DisableFurtherPlacement()
        {
            Debug.Log("GameManager: Disabling further tile placements.");
            // Hide or disable the preview image
            if (uiManager != null)
            {
                uiManager.HidePreview();
                uiManager.DisableRotation();
            }
        }

        /// <summary>
        /// Handler method called when the preview image needs to be updated.
        /// </summary>
        /// <param name="sprite">The sprite to display in the preview UI.</param>
        private void HandlePreviewImageUpdate(Sprite sprite)
        {
            if (uiManager != null)
            {
                uiManager.UpdatePreview(sprite);
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

        [ServerRpc(RequireOwnership = false)]
        private void SetGameStateServerRpc(GameState state)
        {
            gameState = state;
        }

        [ServerRpc(RequireOwnership = false)]
        private void CompleteTurnServerRpc()
        {
            CompleteTurnClientRpc();
        }

        /// <summary>
        /// Call this method to mark the current turn as completed.
        /// </summary>
        [ClientRpc]
        private void CompleteTurnClientRpc()
        {
            uiManager.HideEndTurnButton();
            Debug.Log("Game state is now idle after turn completion");
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
