using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Networking.Transport.Relay;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    private Allocation allocation;
    private string joinCode;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRelay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void InitializeRelay()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services Initialized");

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in as player {AuthenticationService.Instance.PlayerId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    public async Task<bool> HostRelayAsync()
    {
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(4);


            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component not found on NetworkManager.");
                return false;
            }

            transport.SetRelayServerData(new RelayServerData(allocation, "udp"));
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Relay Hosted. Join Code: {joinCode}");

            NetworkManager.Singleton.StartHost();

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay Hosting Failed: {e.Message}");
            return false;
        }
    }

    public async Task<bool> JoinRelayAsync(string code)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: code);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component not found on NetworkManager.");
                return false;
            }

            transport.SetRelayServerData(new RelayServerData(joinAllocation, "udp"));

            NetworkManager.Singleton.StartClient();

            Debug.Log("Successfully joined Relay session.");
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay Joining Failed: {e.Message}");
            return false;
        }
    }
    public string GetJoinCode()
    {
        return joinCode;
    }

}
