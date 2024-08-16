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

/*
 * Script attached to the Test Relay game object.
 * Handles Unity Relay services for connecting multiple clients.
 */
public class TestRelay : NetworkBehaviour
{
    public static TestRelay Instance; // Singleton instance of TestRelay

    [SerializeField] private GameObject displayName; // UI element for displaying name

    public NetworkVariable<int> clientsConnected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); // Tracks the number of connected clients
    public int clientsExpected = 0; // Expected number of clients

    // Set the singleton instance on awake
    private void Awake() {
        Instance = this;
    }

    // Asynchronously creates a new relay allocation and returns a join code
    public async Task<string> CreateRelay(int expectedNum) {
        try {
            if (displayName != null) displayName.SetActive(false); // Hide display name UI element

            clientsExpected = expectedNum; // Set the expected number of clients

            // Create a new relay allocation with a maximum of 3 clients
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            // Get the join code for the created allocation
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode); // Log the join code

            // Set up relay server data
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            // Configure the UnityTransport component with relay server data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Start the network manager as the host
            NetworkManager.Singleton.StartHost();

            // Increment the number of connected clients
            clientsConnected.Value = clientsConnected.Value + 1;

            return joinCode; // Return the join code

        } catch (RelayServiceException e) {
            Debug.Log(e); // Log any exceptions that occur
            return null; // Return null if an exception occurs
        }
    }

    // Asynchronously joins a relay session using the provided join code
    public async void JoinRelay(string joinCode) {
        try {
            if (displayName != null) displayName.SetActive(false); // Hide display name UI element
            Debug.Log("Joining Relay with " + joinCode); // Log the join code
            // Join the relay allocation using the provided join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Set up relay server data
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            // Configure the UnityTransport component with relay server data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Start the network manager as a client
            NetworkManager.Singleton.StartClient();

            // Increment the number of connected clients
            clientsConnected.Value = clientsConnected.Value + 1;
            
        } catch (RelayServiceException e) {
            Debug.Log(e); // Log any exceptions that occur
        }
    }
}
