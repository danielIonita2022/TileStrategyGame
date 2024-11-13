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

        private void Start()
        {
            boardManager = BoardManager.Instance;
            boardManager.OnNoMoreTilePlacements += DisableFurtherPlacement;
            boardManager.OnHighlightTileCreated += HandleHighlightTileCreated;
            boardManager.OnPreviewImageUpdate += HandlePreviewImageUpdate;
            meepleManager = MeepleManager.Instance;
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
            }
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
            // Optionally, display a "Game Over" message or UI
            // You can also disable input or other relevant components
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
