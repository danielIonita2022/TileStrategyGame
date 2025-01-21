using UnityEngine;
using Unity.Netcode;

public class NetworkManagerSetup : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}

