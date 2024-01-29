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

public class TestRelay : NetworkBehaviour
{

    public static TestRelay Instance;
    
    [SerializeField] private GameObject displayName;

    public NetworkVariable<int> clientsConnected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int clientsExpected = 0;

    private void Awake() {
        Instance = this;
    }

    public async Task<string> CreateRelay(int expectedNum) {
        try {
            if (displayName != null) displayName.SetActive(false);

            clientsExpected = expectedNum;

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            clientsConnected.Value = clientsConnected.Value + 1;

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

            clientsConnected.Value = clientsConnected.Value + 1;
            
        } catch (RelayServiceException e) {
            Debug.Log(e);
        }
    }
}
