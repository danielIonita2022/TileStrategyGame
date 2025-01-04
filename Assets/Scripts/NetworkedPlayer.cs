// Assets/Scripts/NetworkedPlayer.cs
using Assets.Scripts;
using Unity.Netcode;
using UnityEngine;

public class NetworkedPlayer : NetworkBehaviour
{
    // Networked Variables
    public NetworkVariable<PlayerColor> PlayerColorEnum;
    public NetworkVariable<int> Score;
    public NetworkVariable<int> MeepleCount;
    public NetworkVariable<string> PlayerName;

    // Reference to GameManager
    private GameManager gameManager;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            gameManager = GameManager.Instance;

            // Initialize Networked Variables
            PlayerColorEnum = new NetworkVariable<PlayerColor>(
                PlayerColor.GRAY, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

            Score = new NetworkVariable<int>(
                0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

            MeepleCount = new NetworkVariable<int>(
                6, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

            PlayerName = new NetworkVariable<string>(
                "Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
            SetPlayerNameServerRpc("Player " + OwnerClientId);
            AssignColorServerRpc();
        }

        PlayerColorEnum.OnValueChanged += OnColorChanged;
    }

    public override void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        PlayerColorEnum.OnValueChanged -= OnColorChanged;
    }

    /// <summary>
    /// Handles updates when the player's color changes.
    /// </summary>
    private void OnColorChanged(PlayerColor oldColor, PlayerColor newColor)
    {
        // Update UI or visual elements based on the new color
        // Example:
        // UIController.Instance.UpdatePlayerColorUI(newColor);
        Debug.Log($"Player {OwnerClientId} color changed from {oldColor} to {newColor}");
    }

    /// <summary>
    /// ServerRpc to request color assignment from the server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void AssignColorServerRpc(ServerRpcParams rpcParams = default)
    {
        // Ensure GameManager is available
        if (gameManager == null)
        {
            Debug.LogError("NetworkedPlayer: GameManager instance not found.");
            return;
        }

        // Assign the next available color
        PlayerColorEnum.Value = gameManager.GetNextAvailableColor();
    }

    /// <summary>
    /// ServerRpc to set the player's name.
    /// </summary>
    [ServerRpc]
    public void SetPlayerNameServerRpc(string name)
    {
        PlayerName.Value = name;
    }

    /// <summary>
    /// Method to add score to the player. Can be called by the server.
    /// </summary>
    /// <param name="points">Points to add.</param>
    public void AddScore(int points)
    {
        if (IsServer)
        {
            Score.Value += points;
        }
    }
}
