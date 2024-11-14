using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private PreviewUIController previewUIController;
        private MeepleManager meepleManager;
        private List<Player> players = new List<Player>();
        private int currentPlayerIndex = 0;
        void Awake()
        {
            InitializePlayers();
            boardManager = BoardManager.Instance;
            boardManager.OnTilePlaced += HandleTilePlaced;
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
            players.Add(new Player("Bob", PlayerColor.BLUE, 2));
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
            bool hasPlacedTile = boardManager.PlaceTile(position, rotationState, highlightTile);
            if (hasPlacedTile)
            {
                previewUIController.ResetPreviewRotationState();
                previewUIController.HidePreview();
                previewUIController.DisableRotation();
                SwitchTurn();
            }
        }

        private void HandleTilePlaced(Tile tile)
        {
            //meepleManager.PlaceMeeple(tile)
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
