using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class TestRelay : MonoBehaviour
{

    public static TestRelay Instance;
    
    [SerializeField] private GameObject displayName;

    private void Awake() {
        Instance = this;
    }

    public async Task<string> CreateRelay() {
        try {
            if (displayName != null) displayName.SetActive(false);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            PlayerSpawner.Instance.SpawnPlayer();

            return joinCode;

        } catch (RelayServiceException e) {
            Debug.Log(e);
            return null;
        }
    }

    public async void JoinRelay(string joinCode) {
        try {
            if (displayName != null) displayName.SetActive(false);
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

            PlayerSpawner.Instance.SpawnPlayer();
            
        } catch (RelayServiceException e) {
            Debug.Log(e);
        }
    }
}
