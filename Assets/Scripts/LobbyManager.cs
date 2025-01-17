using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using System;
using Assets.Scripts;

public class LobbyManager : MonoBehaviour
{
    [Header("Lobby Settings")]
    public int MaxPlayers = 4;
    public string LobbyName = "MyLobby";

    [SerializeField] private UIManager uiManager;

    [Header("Prefabs")]
    //public GameObject gameManagerPrefab;

    private Lobby currentLobby;

    private void Start()
    {
        if (RelayManager.Instance == null)
        {
            Debug.LogError("LobbyManager: RelayManager instance not found.");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("LobbyManager: UIManager instance not found.");
            return;
        }

        uiManager = UIManager.Instance;

        uiManager.HostButton.onClick.AddListener(OnHostClicked);
        uiManager.JoinButton.onClick.AddListener(OnJoinClicked);
        uiManager.StartGameButton.onClick.AddListener(OnStartGameClicked);

        uiManager.StartGameButton.gameObject.SetActive(false); // Visible only to host
    }

    private string GenerateRandomUserName()
    {
        string username = "Player#";
        System.Random rnd = new System.Random();
        username += rnd.Next(1000, 9999);
        return username;
    }

    public async void OnHostClicked()
    {
        uiManager.HostButton.interactable = false;
        uiManager.LobbyStatusText.text = "Hosting Lobby...";

        bool relayHosted = await RelayManager.Instance.HostRelayAsync();
        if (relayHosted)
        {
            string username = uiManager.UserNameField.text;
            if (string.IsNullOrEmpty(username))
            {
                username = GenerateRandomUserName();
            }
            Player hostPlayer = InitializePlayer(AuthenticationService.Instance.PlayerId, username);

            string relayJoinCode = RelayManager.Instance.GetJoinCode();

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = hostPlayer,
                Data = new Dictionary<string, DataObject>()
            };
            options.Data.Add("RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode));

            try
            {
                currentLobby = await LobbyService.Instance.CreateLobbyAsync(LobbyName, MaxPlayers, options);
                Debug.Log($"Lobby created with ID: {currentLobby.Id}");

                uiManager.LobbyStatusText.text = $"Lobby Hosted!\nLobby Code: {currentLobby.LobbyCode}\nRelay Join Code: {relayJoinCode}";
                Debug.Log($"Current Lobby size (host): {currentLobby.Players.Count}");

                uiManager.StartGameButton.gameObject.SetActive(true); // Host can start the game
            }
            catch (LobbyServiceException e)
            {
                uiManager.LobbyStatusText.text = $"Error Creating Lobby: {e.Message}";
                Debug.LogError($"Lobby Creation Error: {e.Message}");
                uiManager.HostButton.interactable = true;
            }
        }
        else
        {
            uiManager.LobbyStatusText.text = "Failed to Host Relay.";
            uiManager.HostButton.interactable = true;
        }
    }

    // Join a lobby using a lobby code
    private async void OnJoinClicked()
    {
        uiManager.JoinButton.interactable = false;
        uiManager.LobbyStatusText.text = "Joining Lobby...";

        string lobbyCode = uiManager.LobbyCodeInputField.text;
        if (string.IsNullOrEmpty(lobbyCode))
        {
            uiManager.LobbyStatusText.text = "Please enter a Lobby Code.";
            uiManager.JoinButton.interactable = true;
            return;
        }

        try
        {
            string username = uiManager.UserNameField.text;
            if (string.IsNullOrEmpty(username))
            {
                username = GenerateRandomUserName();
            }

            Player joiningPlayer = InitializePlayer(AuthenticationService.Instance.PlayerId, username);

            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions
            { Player = joiningPlayer};

            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);

            uiManager.LobbyStatusText.text = $"Joined Lobby: {currentLobby.Name}";
            Debug.Log($"Current Lobby size after joining lobby (client): {currentLobby.Players.Count}");

            // Retrieve Relay Join Code from Lobby Data
            if (currentLobby.Data.ContainsKey("RelayJoinCode"))
            {
                string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
                bool relayJoined = await RelayManager.Instance.JoinRelayAsync(relayJoinCode);
                if (relayJoined)
                {
                    Debug.Log($"Current Lobby size after joining relay (client): {currentLobby.Players.Count}");
                    uiManager.LobbyStatusText.text = "Joined Relay Session.";
                }
                else
                {
                    uiManager.LobbyStatusText.text = "Failed to Join Relay Session.";
                }
            }
            else
            {
                uiManager.LobbyStatusText.text = "Lobby does not contain Relay Join Code.";
            }
        }
        catch (LobbyServiceException e)
        {
            uiManager.LobbyStatusText.text = $"Error Joining Lobby: {e.Message}";
            Debug.LogError($"Lobby Join Error: {e.Message}");
        }
        finally
        {
            uiManager.JoinButton.interactable = true;
        }
    }

    // Start the game as the host
    private async void OnStartGameClicked()
    {
        uiManager.StartGameButton.interactable = false;
        uiManager.LobbyStatusText.text = "Starting Game...";

        // Assign colors to players
        await AssignPlayerColors();

        //if (NetworkManager.Singleton.IsHost)
        //{
        //    //GameObject gm = Instantiate(gameManagerPrefab);
        //    //gm.GetComponent<NetworkObject>().Spawn();
        //    //Debug.Log("LobbyManager: GameManager spawned.");
        //    foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        //    {
        //        if (obj.TryGetComponent<GameManager>(out GameManager gm))
        //        {
        //            Debug.Log($"Found GameManager already spawned with hashcode: {gm.GetHashCode()}!");
        //            if (!gm.NetworkObject.IsSpawned)
        //            {
        //                Debug.Log("GameManager is not spawned. Spawning...");
        //                gm.NetworkObject.Spawn();
                        
        //            }
                    
        //        }
        //    }
        //}

        // Transition to the Game
        uiManager.lobbyUI.SetActive(false);
        uiManager.gameUI.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.Log("GameManager not found in spawned objects.");
        }
    }

    private Player InitializePlayer(string playerId, string username)
    {
        return new Player(
            id: playerId,
            data: new Dictionary<string, PlayerDataObject>
            {
            { "Username", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, username) },
            { "Color", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "GRAY") } // Default color
            }
        );
    }

    private async Task AssignPlayerColors()
    {
        List<Player> players = currentLobby.Players;
        string[] colors = { "Red", "Blue", "Green", "Yellow" };
        for (int i = 0; i < players.Count; i++)
        {
            string color = colors[i % colors.Length];
            await UpdatePlayerDataAsync(players[i].Id, "Color", color);
        }
    }

    public async Task UpdatePlayerDataAsync(string playerId, string key, string value)
    {
        try
        {
            if (currentLobby == null)
            {
                Debug.LogError("UpdatePlayerData Error: currentLobby is null.");
                return;
            }

            if (currentLobby.Players == null)
            {
                Debug.LogError("UpdatePlayerData Error: currentLobby.Players is null.");
                return;
            }

            Debug.Log($"UpdatePlayerData: Current lobby has {currentLobby.Players.Count} players.");

            Player player = currentLobby.Players.Find(p => p.Id == playerId);
            if (player == null)
            {
                Debug.LogError($"UpdatePlayerData Error: No player found with ID {playerId}.");
                return;
            }

            if (player.Data == null)
            {
                Debug.LogWarning($"UpdatePlayerData Error: player.Data is null for player ID {playerId}.");
                player.Data = new Dictionary<string, PlayerDataObject>();
            }

            player.Data[key] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value);
            Debug.Log($"UpdatePlayerData: Updated player {playerId} data with key '{key}' and value '{value}'.");

            if (LobbyService.Instance == null)
            {
                Debug.LogError("UpdatePlayerData Error: LobbyService.Instance is null.");
                return;
            }

            if (string.IsNullOrEmpty(currentLobby.Id))
            {
                Debug.LogError("UpdatePlayerData Error: currentLobby.Id is null or empty.");
                return;
            }

            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> { { key, new DataObject(DataObject.VisibilityOptions.Member, value) } }
            });

            Debug.Log("UpdatePlayerData: Lobby updated successfully.");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to Update Player Data: {e.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unexpected error in UpdatePlayerData: {ex.Message}");
        }
    }

}
