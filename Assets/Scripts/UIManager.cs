using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class UIManager : NetworkBehaviour
    {
        public static UIManager Instance;

        [Header("UI Panels")]
        public GameObject lobbyUI;
        public GameObject gameUI;

        [Header("Lobby UI Components")]
        public InputField LobbyCodeInputField;
        public Text LobbyStatusText;
        public Button HostButton;
        public Button JoinButton;
        public Button StartGameButton;

        [Header("Game UI Components")]
        public Image previewImage;
        public Button rotateLeftButton;
        public Button rotateRightButton;
        public Button skipMeeplePlacementButton;

        [Header("Background")]
        public Canvas backgroundCanvas;

        public TextMeshProUGUI FirstPlayerName;
        public TextMeshProUGUI SecondPlayerName;
        public TextMeshProUGUI ThirdPlayerName;
        public TextMeshProUGUI FourthPlayerName;
        public TextMeshProUGUI FirstPlayerScore;
        public TextMeshProUGUI SecondPlayerScore;
        public TextMeshProUGUI ThirdPlayerScore;
        public TextMeshProUGUI FourthPlayerScore;
        public TextMeshProUGUI FirstPlayerMeepleCount;
        public TextMeshProUGUI SecondPlayerMeepleCount;
        public TextMeshProUGUI ThirdPlayerMeepleCount;
        public TextMeshProUGUI FourthPlayerMeepleCount;

        private int previewRotationState = 0; // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°
        private float[] rotationAngles = { 0f, 90f, 180f, 270f };

        [SerializeField] private GameObject meeplePrefab;
        [SerializeField] private Transform meepleParent; // Parent transform for instantiating meeples

        private List<GameObject> instantiatedGrayMeeples = new List<GameObject>();
        private List<Meeple> instantiatedPlayerMeeples = new List<Meeple>();

        public List<GameObject> InstantiatedGrayMeeples => instantiatedGrayMeeples;

        public Dictionary<int, Vector3> MeepleObjectsPositionsServer = new Dictionary<int, Vector3>();
        public List<Meeple> InstantiatedPlayerMeeples => instantiatedPlayerMeeples;

        public Action OnMeepleSkipped { get; internal set; }
        public event Action OnMeeplePlaced;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                lobbyUI.SetActive(true);
                gameUI.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Multiple UIManager instances found. Destroying...");
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            rotateLeftButton.onClick.AddListener(RotateLeft);
            rotateRightButton.onClick.AddListener(RotateRight);
            skipMeeplePlacementButton.onClick.AddListener(SkipMeeplePlacement);
            HideEndTurnButton();
        }

        private void OnDisable()
        {
            rotateLeftButton.onClick.RemoveListener(RotateLeft);
            rotateRightButton.onClick.RemoveListener(RotateRight);
            skipMeeplePlacementButton.onClick.RemoveListener(SkipMeeplePlacement);
        }

        public string ConvertPlayerColorToPlayerName(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.RED:
                    return "RED PLAYER";
                case PlayerColor.BLUE:
                    return "BLUE PLAYER";
                case PlayerColor.YELLOW:
                    return "YELLOW PLAYER";
                case PlayerColor.GREEN:
                    return "GREEN PLAYER";
                default:
                    return "UNKNOWN PLAYER";
            }
        }

        [ClientRpc]
        public void UpdateHUDPlayerScoreClientRpc(int playerIndex, int newScore)
        {
            Debug.Log("UIManager: Entered UpdateHUDPlayerScore");
            switch (playerIndex)
            {
                case 0:
                    FirstPlayerScore.text = $"Score: {newScore}";
                    break;
                case 1:
                    SecondPlayerScore.text = $"Score: {newScore}";
                    break;
                case 2:
                    ThirdPlayerScore.text = $"Score: {newScore}";
                    break;
                case 3:
                    FourthPlayerScore.text = $"Score: {newScore}";
                    break;
            };
        }

        [ClientRpc]
        public void UpdateHUDPlayerMeepleCountClientRpc(int playerIndex, int newMeepleCount)
        {
            Debug.Log("UIManager: Entered UpdateHUDPlayerMeepleCount");
            switch (playerIndex)
            {
                case 0:
                    FirstPlayerMeepleCount.text = $"Meeples left: {newMeepleCount}";
                    break;
                case 1:
                    SecondPlayerMeepleCount.text = $"Meeples left: {newMeepleCount}";
                    break;
                case 2:
                    ThirdPlayerMeepleCount.text = $"Meeples left: {newMeepleCount}";
                    break;
                case 3:
                    FourthPlayerMeepleCount.text = $"Meeples left: {newMeepleCount}";
                    break;
            };
        }

        public void InitGameHUD(int numberOfPlayers, PlayerColor[] playerColors, PlayerColor currentPlayerColor)
        {
            backgroundCanvas.gameObject.SetActive(false);

            FirstPlayerName.gameObject.SetActive(false);
            FirstPlayerScore.gameObject.SetActive(false);
            FirstPlayerMeepleCount.gameObject.SetActive(false);
            SecondPlayerName.gameObject.SetActive(false);
            SecondPlayerScore.gameObject.SetActive(false);
            SecondPlayerMeepleCount.gameObject.SetActive(false);
            ThirdPlayerName.gameObject.SetActive(false);
            ThirdPlayerScore.gameObject.SetActive(false);
            ThirdPlayerMeepleCount.gameObject.SetActive(false);
            FourthPlayerName.gameObject.SetActive(false);
            FourthPlayerScore.gameObject.SetActive(false);
            FourthPlayerMeepleCount.gameObject.SetActive(false);

            string[] playerNames = { "RED PLAYER", "BLUE PLAYER", "YELLOW PLAYER", "GREEN PLAYER" };

            for (int i = 0; i < 4; i++)
            {
                bool isActive = i < numberOfPlayers;
                string playerName;
                if (isActive)
                {
                    PlayerColor color = playerColors[i];
                    if (color == currentPlayerColor)
                    {
                        playerName = "YOU";
                    }
                    else
                    {
                        playerName = playerNames[i];
                    }
                }
                else
                {
                    playerName = playerNames[i];
                }

                switch (i)
                {
                    case 0:
                        FirstPlayerName.text = playerName;
                        FirstPlayerName.gameObject.SetActive(isActive);
                        FirstPlayerScore.text = "Score: 0";
                        FirstPlayerScore.gameObject.SetActive(isActive);
                        FirstPlayerMeepleCount.text = "Meeples left: 6";
                        FirstPlayerMeepleCount.gameObject.SetActive(isActive);
                        break;

                    case 1:
                        SecondPlayerName.text = playerName;
                        SecondPlayerName.gameObject.SetActive(isActive);
                        SecondPlayerScore.text = "Score: 0";
                        SecondPlayerScore.gameObject.SetActive(isActive);
                        SecondPlayerMeepleCount.text = "Meeples left: 6";
                        SecondPlayerMeepleCount.gameObject.SetActive(isActive);
                        break;

                    case 2:
                        ThirdPlayerName.text = playerName;
                        ThirdPlayerName.gameObject.SetActive(isActive);
                        ThirdPlayerScore.text = "Score: 0";
                        ThirdPlayerScore.gameObject.SetActive(isActive);
                        ThirdPlayerMeepleCount.text = "Meeples left: 6";
                        ThirdPlayerMeepleCount.gameObject.SetActive(isActive);
                        break;

                    case 3:
                        FourthPlayerName.text = playerName;
                        FourthPlayerName.gameObject.SetActive(isActive);
                        FourthPlayerScore.text = "Score: 0";
                        FourthPlayerScore.gameObject.SetActive(isActive);
                        FourthPlayerMeepleCount.text = "Meeples left: 6";
                        FourthPlayerMeepleCount.gameObject.SetActive(isActive);
                        break;
                }
            }
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

        [ServerRpc(RequireOwnership = false)]
        public void UpdatePlacedMeepleVisualServerRpc(int meepleID, Vector3 position, MeepleType type, PlayerColor color)
        {
            Debug.Log("Meeple: Entered UpdateMeepleVisualServerRpc");
            UpdatePlacedMeepleVisualClientRpc(meepleID, position, type, color);
        }

        [ClientRpc]
        public void UpdatePlacedMeepleVisualClientRpc(int meepleID, Vector3 position, MeepleType type, PlayerColor color)
        {
            Debug.Log("Meeple: Entered UpdateMeepleVisualClientRpc");
            GameObject selectedMeepleGameObject = InstantiatedGrayMeeples
                    .FirstOrDefault(m => m.GetComponent<Meeple>().MeepleData.MeepleID == meepleID);
            if (selectedMeepleGameObject != null)
            {
                SpriteRenderer renderer = selectedMeepleGameObject.GetComponent<SpriteRenderer>();
                renderer.sprite = null;
                InstantiatedGrayMeeples.Remove(selectedMeepleGameObject);
                Destroy(selectedMeepleGameObject);
            }
            GameObject newInstantiatedMeepleGO = Instantiate(meeplePrefab, position, Quaternion.identity, meepleParent);
            UpdateMeepleSpriteRenderer(newInstantiatedMeepleGO, type, color, meepleID);
            OnMeeplePlaced?.Invoke();
        }

        public void UpdateMeepleSpriteRenderer(GameObject selectedMeeple, MeepleType type, PlayerColor color, int meepleID)
        {
            SpriteRenderer meepleSpriteRenderer = selectedMeeple.GetComponent<SpriteRenderer>();
            string colorString = Converters.ConvertPlayerColorToString(color);
            Debug.Log($"Meeple: Updating meeple to {color} {type}");
            switch (type)
            {
                case MeepleType.Knight:
                    meepleSpriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Meeples/{colorString}Knight");
                    break;
                case MeepleType.Bishop:
                    meepleSpriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Meeples/{colorString}Bishop");
                    break;
                case MeepleType.Road:
                    meepleSpriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Meeples/{colorString}Meeple");
                    break;
                default:
                    meepleSpriteRenderer.sprite = null;
                    break;
            }
            Meeple newMeeple = selectedMeeple.GetComponent<Meeple>();
            newMeeple.MeepleData = new MeepleData(color, type, meepleID);
            newMeeple.UpdateMeepleData(type, color);
        }

        [ServerRpc (RequireOwnership = false)]
        public void AddMeepleToMeepleDictServerRpc(int meepleID, Vector3 position)
        {
            Debug.Log($"UIManager: Added to the server dictionary the following: meeple ID {meepleID} with position {position}"); 
            MeepleObjectsPositionsServer.Add(meepleID, position);
        }

        public void DisplayMeepleOptions(Vector3 meeplePosition, FeatureType feature, int meepleDataID)
        {
            Debug.Log("UIManager: Displaying meeple options...");

            if (IsServer)
            {
                AddMeepleToMeepleDictServerRpc(meepleDataID, meeplePosition);
            }

            MeepleType meepleType = Converters.ConvertFeatureTypeToMeepleType(feature);
            Debug.Log($"UIManager: Displaying meeple options on client {NetworkManager.Singleton.LocalClientId}...");

            GameObject meepleGO = Instantiate(meeplePrefab, meeplePosition, Quaternion.identity, meepleParent);
            UpdateMeepleSpriteRenderer(meepleGO, meepleType, PlayerColor.GRAY, meepleDataID);

            InstantiatedGrayMeeples.Add(meepleGO);
        }

        /// <summary>
        /// Displays meeple options based on available features.
        /// </summary>
        public void FindMeepleOptions(Vector3 tileWorldPosition, List<(FeatureType, int, MeepleData)> availableFeaturesForMeeples)
        {
            //ClearMeepleOptionsServerRpc();

            foreach (var featureAndEdgeIndexAndMeepleData in availableFeaturesForMeeples)
            {
                FeatureType feature = featureAndEdgeIndexAndMeepleData.Item1;
                int edgeIndex = featureAndEdgeIndexAndMeepleData.Item2;
                MeepleData meepleData = featureAndEdgeIndexAndMeepleData.Item3;

                Vector3 offset = GetOffset(edgeIndex);
                Vector3 meeplePosition = tileWorldPosition + offset;

                DisplayMeepleOptions(meeplePosition, feature, meepleData.MeepleID);
            }

            // Optionally, make the meeple options panel visible
            meepleParent.gameObject.SetActive(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveUIMeepleServerRpc(int meepleID)
        {
            Vector3 position = MeepleObjectsPositionsServer[meepleID];
            MeepleObjectsPositionsServer.Remove(meepleID);
            RemoveUIMeepleClientRpc(position);
        }

        public GameObject FindGameObjectAtPosition(Vector3 position, float tolerance = 0.1f)
        {
            foreach (GameObject obj in FindObjectsOfType<GameObject>())
            {
                if (Vector3.Distance(obj.transform.position, position) <= tolerance)
                {
                    return obj;
                }
            }

            Debug.LogWarning($"No GameObject found at position {position}");
            return null;
        }

        [ClientRpc]
        public void RemoveUIMeepleClientRpc(Vector3 position)
        {
            GameObject meepleGO = FindGameObjectAtPosition(position);
            Meeple meepleToRemove = meepleGO.GetComponent<Meeple>();
            if (meepleToRemove != null)
            {
                meepleToRemove.SpriteRenderer = null;
                Destroy(meepleGO);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveInstantiatedMeeplesServerRpc(int meepleID)
        {
            MeepleObjectsPositionsServer.Remove(meepleID);
            RemoveInstantiatedMeeplesClientRpc(meepleID);
        }

        [ClientRpc]
        public void RemoveInstantiatedMeeplesClientRpc(int meepleID)
        {
            Debug.Log($"UIManager: Removed meeple with id {meepleID} from Game UI");
            Meeple meepleToRemove = InstantiatedPlayerMeeples.Find(meeple => meeple.MeepleData.MeepleID == meepleID);
            if (meepleToRemove != null)
            {
                GameObject meepleGO = meepleToRemove.gameObject;
                InstantiatedGrayMeeples.Remove(meepleGO);
                InstantiatedPlayerMeeples.Remove(meepleToRemove);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClearMeepleOptionsServerRpc()
        {
            ClearMeepleOptionsClientRpc();
        }

        /// <summary>
        /// Clears all current meeple options.
        /// </summary>
        [ClientRpc]
        public void ClearMeepleOptionsClientRpc()
        {
            foreach (var meepleGO in InstantiatedGrayMeeples)
            {
                if (meepleGO != null)
                {
                    Meeple meeple = meepleGO.GetComponent<Meeple>();
                    meeple.SpriteRenderer = null;
                    Destroy(meeple.gameObject);
                }
            }
            InstantiatedGrayMeeples.Clear();
        }


        /// <summary>
        /// Updates the preview image with the selected tile's sprite.
        /// </summary>
        public void UpdatePreview(Sprite tileSprite)
        {
            previewImage.sprite = tileSprite;
            previewImage.transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log("Updated preview image.");
        }

        // Replace the HidePreview method
        public void HidePreview()
        {
            previewImage.gameObject.SetActive(false);
            rotateLeftButton.gameObject.SetActive(false);
            rotateRightButton.gameObject.SetActive(false);
        }

        // Replace the ShowPreview method
        public void ShowPreview()
        {
            previewImage.gameObject.SetActive(true);
            rotateLeftButton.gameObject.SetActive(true);
            rotateRightButton.gameObject.SetActive(true);
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
            previewImage.transform.rotation = Quaternion.Euler(0, 0, rotationAngles[previewRotationState]);
            Debug.Log($"Preview rotated to {rotationAngles[previewRotationState]} degrees.");
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
            rotateLeftButton.enabled = false;
            rotateRightButton.enabled = false;
            Debug.Log("Rotation controls disabled.");
        }

        /// <summary>
        /// Enables rotation controls when tile placements are available.
        /// </summary>
        public void EnableRotation()
        {
            rotateLeftButton.enabled = true;
            rotateRightButton.enabled = true;

            Debug.Log("Rotation controls enabled.");
        }

        private void SkipMeeplePlacement()
        {
            Debug.Log("SkipMeeplePlacement button clicked.");
            OnMeepleSkipped?.Invoke();
        }

        // Replace the ShowEndTurnButton method
        public void ShowEndTurnButton()
        {
            skipMeeplePlacementButton.gameObject.SetActive(true);
        }

        // Replace the HideEndTurnButton method
        public void HideEndTurnButton()
        {
            skipMeeplePlacementButton.gameObject.SetActive(false);
        }
    }
}