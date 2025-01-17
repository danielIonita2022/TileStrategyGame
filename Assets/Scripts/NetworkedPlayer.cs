// Assets/Scripts/NetworkedPlayer.cs
using Assets.Scripts;
using Unity.Netcode;
using UnityEngine;

public class NetworkedPlayer : NetworkBehaviour
{
    // Networked Variables
    public NetworkVariable<PlayerColor> PlayerColorEnum = new NetworkVariable<PlayerColor>();
    public NetworkVariable<int> Score = new NetworkVariable<int>();
    public NetworkVariable<int> MeepleCount = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        PlayerColorEnum.OnValueChanged += OnColorChanged;
        Score.OnValueChanged += OnScoreChanged;
        MeepleCount.OnValueChanged += OnMeepleCountChanged;
    }

    public override void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        PlayerColorEnum.OnValueChanged -= OnColorChanged;
        Score.OnValueChanged -= OnScoreChanged;
        MeepleCount.OnValueChanged -= OnMeepleCountChanged;
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

    private void OnScoreChanged(int oldScore, int newScore)
    {
        // Update UI or visual elements based on the new color
        // Example:
        // UIController.Instance.UpdatePlayerColorUI(newColor);
        Debug.Log($"Player {OwnerClientId} score changed {oldScore} to {newScore}");
    }

    private void OnMeepleCountChanged(int oldCount, int newCount)
    {
        Debug.Log($"Player {OwnerClientId} meeple count changed {oldCount} to {newCount}");
    }
    public void AddScore(int points)
    {
        if (IsServer)
        {
            Score.Value += points;
        }
    }
}
