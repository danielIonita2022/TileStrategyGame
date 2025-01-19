// Assets/Scripts/NetworkedPlayer.cs
using Assets.Scripts;
using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkedPlayer : NetworkBehaviour
{
    // Networked Variables
    public NetworkVariable<PlayerColor> PlayerColorEnum = new NetworkVariable<PlayerColor>();
    public NetworkVariable<int> Score = new NetworkVariable<int>();
    public NetworkVariable<int> MeepleCount = new NetworkVariable<int>();

    public Action<int, int> OnUIPlayerScoreChanged;
    public Action<int, int> OnUIMeepleCountChanged;

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
        Debug.Log($"Player {OwnerClientId} color changed from {oldColor} to {newColor}");
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        Debug.Log($"Player {OwnerClientId} score changed {oldScore} to {newScore}");
    }

    private void OnMeepleCountChanged(int oldCount, int newCount)
    {
        Debug.Log($"Player {OwnerClientId} meeple count changed {oldCount} to {newCount}");
        OnUIMeepleCountChanged?.Invoke((int)OwnerClientId, newCount);
    }
    public void AddScore(int points)
    {
        if (IsServer)
        {
            Debug.Log($"Server: Adding {points} points to Player {OwnerClientId}");
            Score.Value += points;
            OnUIPlayerScoreChanged?.Invoke((int)OwnerClientId, Score.Value);
        }
    }
}
