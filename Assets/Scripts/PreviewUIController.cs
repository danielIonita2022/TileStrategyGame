using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class PreviewUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private Image previewImage;
        private Button rotateLeftButton;
        private Button rotateRightButton;
        private Button skipMeeplePlacementButton;

        private int previewRotationState = 0; // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°
        private float[] rotationAngles = { 0f, 90f, 180f, 270f };

        [SerializeField] private GameObject meeplePrefab;
        [SerializeField] private Transform meepleParent; // Parent transform for instantiating meeples

        private List<Meeple> instantiatedGrayMeeples = new List<Meeple>();
        private List<Meeple> instantiatedPlayerMeeples = new List<Meeple>();

        public List<Meeple> InstantiatedGrayMeeples => instantiatedGrayMeeples;
        public List<Meeple> InstantiatedPlayerMeeples => instantiatedPlayerMeeples;

        public Action OnMeepleSkipped { get; internal set; }

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found on " + gameObject.name);
                return;
            }

            var root = uiDocument.rootVisualElement;
            previewImage = root.Q<Image>("PreviewImage");
            rotateLeftButton = root.Q<Button>("RotateLeftButton");
            rotateRightButton = root.Q<Button>("RotateRightButton");
            skipMeeplePlacementButton = root.Q<Button>("SkipMeeplePlacement");
            if (previewImage == null)
            {
                Debug.LogError("PreviewImage not found in UXML.");
            }
            if (rotateLeftButton != null)
                rotateLeftButton.clicked += RotateLeft;
            else
                Debug.LogError("RotateLeftButton not found in UXML.");

            if (rotateRightButton != null)
                rotateRightButton.clicked += RotateRight;
            else
                Debug.LogError("RotateRightButton not found in UXML.");

            if (skipMeeplePlacementButton != null)
            {
                skipMeeplePlacementButton.clicked += SkipMeeplePlacement;
                HideEndTurnButton();
            }
            else
                Debug.LogError("SkipMeeplePlacement button not found in UXML.");
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            if (rotateLeftButton != null)
                rotateLeftButton.clicked -= RotateLeft;

            if (rotateRightButton != null)
                rotateRightButton.clicked -= RotateRight;
        }

        public static Vector3 GetOffset(int edgeIndex, float tileSize = 8f, float meepleOffset = 0.85f)
        {
            Vector3 offset = Vector3.zero;
            switch (edgeIndex)
            {
                case 0:
                    offset = Vector3.zero;
                    break;
                case 1: // North
                    offset = new Vector3(0, tileSize / 2 - meepleOffset, 0);
                    break;
                case 2: // East
                    offset = new Vector3(tileSize / 2 - meepleOffset, 0, 0);
                    break;
                case 3: // South
                    offset = new Vector3(0, -tileSize / 2 + meepleOffset, 0);
                    break;
                case 4: // West
                    offset = new Vector3(-tileSize / 2 + meepleOffset, 0, 0);
                    break;
                default:
                    Debug.LogError("Invalid edge index.");
                    break;
            }
            return offset;
        }

        /// <summary>
        /// Displays meeple options based on available features.
        /// </summary>
        public void DisplayMeepleOptions(Vector3 tileWorldPosition, List<(FeatureType, int, MeepleData)> availableFeaturesForMeeples)
        {
            ClearMeepleOptions();

            foreach (var featureAndEdgeIndexAndMeepleData in availableFeaturesForMeeples)
            {
                FeatureType feature = featureAndEdgeIndexAndMeepleData.Item1;
                int edgeIndex = featureAndEdgeIndexAndMeepleData.Item2;
                MeepleData meepleData = featureAndEdgeIndexAndMeepleData.Item3;
                Vector3 offset = GetOffset(edgeIndex);
                Vector3 meeplePosition = tileWorldPosition + offset;
                // Instantiate the single meeple prefab
                GameObject meepleGO = Instantiate(meeplePrefab, meeplePosition, Quaternion.identity, meepleParent);
                meepleGO.transform.position = meeplePosition;
                BoxCollider collider = meepleGO.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.center = Vector3.zero; // Assuming the collider is at the origin of the prefab
                }
                else
                {
                    Debug.LogError("MeeplePrefab does not have a BoxCollider component.");
                }
                Meeple meeple = meepleGO.GetComponent<Meeple>();
                if (meeple != null)
                {
                    MeepleType meepleType = Converters.ConvertFeatureTypeToMeepleType(feature);
                    meeple.UpdateMeepleVisual(meepleType, PlayerColor.GRAY);
                    meeple.MeepleData = meepleData;
                    instantiatedGrayMeeples.Add(meeple);
                    Debug.Log($"PreviewUIController: Meeple position: {meeplePosition}.");
                }
                else
                {
                    Debug.LogError("PreviewUIController: MeeplePrefab does not have a Meeple script attached.");
                }
            }

            // Optionally, make the meeple options panel visible
            meepleParent.gameObject.SetActive(true);
        }

        public void AddInstantiatedPlayerMeeple(Meeple meeple)
        {
            InstantiatedPlayerMeeples.Add(meeple);
        }

        public void RemoveUIMeeple(int meepleID)
        {
            Meeple meepleToRemove = InstantiatedPlayerMeeples.Find(meeple => meeple.MeepleData.MeepleID == meepleID);
            if (meepleToRemove != null)
            {
                meepleToRemove.SpriteRenderer = null;
                Destroy(meepleToRemove.gameObject);
            }
        }

        public void RemoveUIMeeple(Meeple meeple)
        {
            InstantiatedGrayMeeples.Remove(meeple);
            InstantiatedPlayerMeeples.Remove(meeple);
        }

        /// <summary>
        /// Clears all current meeple options.
        /// </summary>
        public void ClearMeepleOptions()
        {
            foreach (var meeple in instantiatedGrayMeeples)
            {
                if (meeple != null)
                {
                    meeple.SpriteRenderer = null;
                    Destroy(meeple.gameObject);
                }
            }
            instantiatedGrayMeeples.Clear();
        }


        /// <summary>
        /// Updates the preview image with the selected tile's sprite.
        /// </summary>
        public void UpdatePreview(Sprite tileSprite)
        {
            if (previewImage != null)
            {
                previewImage.sprite = tileSprite;
                previewImage.transform.rotation = Quaternion.Euler(0, 0, 0);
                Debug.Log("Updated preview image.");
            }
            else
            {
                Debug.LogError("PreviewImage is not assigned in PreviewUIController.");
            }
        }

        // Optionally, hide the preview image
        public void HidePreview()
        {
            if (previewImage != null)
            {
                previewImage.style.display = DisplayStyle.None;
                rotateLeftButton.style.display = DisplayStyle.None;
                rotateRightButton.style.display = DisplayStyle.None;
            }
        }

        // Optionally, show the preview image
        public void ShowPreview()
        {
            if (previewImage != null)
            {
                previewImage.style.display = DisplayStyle.Flex;
                rotateLeftButton.style.display = DisplayStyle.Flex;
                rotateRightButton.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Rotates the preview tile to the left (counter-clockwise).
        /// </summary>
        private void RotateLeft()
        {
            previewRotationState = (previewRotationState + 3) % 4; // Equivalent to -1 mod 4
            ApplyRotation();
        }

        /// <summary>
        /// Rotates the preview tile to the right (clockwise).
        /// </summary>
        private void RotateRight()
        {
            previewRotationState = (previewRotationState + 1) % 4;
            ApplyRotation();
        }

        /// <summary>
        /// Applies the current rotation state to the preview image.
        /// </summary>
        private void ApplyRotation()
        {
            if (previewImage != null)
            {
                previewImage.transform.rotation = Quaternion.Euler(0, 0, rotationAngles[previewRotationState]);
                Debug.Log($"Preview rotated to {rotationAngles[previewRotationState]} degrees.");
            }
        }

        /// <summary>
        /// Retrieves the current rotation state of the preview tile.
        /// </summary>
        public int GetPreviewRotationState()
        {
            return previewRotationState;
        }

        public void ResetPreviewRotationState()
        {
            previewRotationState = 0;
        }

        /// <summary>
        /// Disables rotation controls when no tiles can be placed.
        /// </summary>
        public void DisableRotation()
        {
            if (rotateLeftButton != null)
                rotateLeftButton.SetEnabled(false);
            if (rotateRightButton != null)
                rotateRightButton.SetEnabled(false);

            Debug.Log("Rotation controls disabled.");
        }

        /// <summary>
        /// Enables rotation controls when tile placements are available.
        /// </summary>
        public void EnableRotation()
        {
            if (rotateLeftButton != null)
                rotateLeftButton.SetEnabled(true);
            if (rotateRightButton != null)
                rotateRightButton.SetEnabled(true);

            Debug.Log("Rotation controls enabled.");
        }

        private void SkipMeeplePlacement()
        {
            Debug.Log("SkipMeeplePlacement button clicked.");
            OnMeepleSkipped?.Invoke();
        }

        public void ShowEndTurnButton()
        {
            if (skipMeeplePlacementButton != null)
            {
                skipMeeplePlacementButton.style.display = DisplayStyle.Flex;
            }
        }

        public void HideEndTurnButton()
        {
            if (skipMeeplePlacementButton != null)
            {
                skipMeeplePlacementButton.style.display = DisplayStyle.None;
            }
        }
    }
}