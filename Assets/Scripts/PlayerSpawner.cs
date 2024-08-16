using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

/*
 * Script that is attached to player spawner prefabs and used by network manager as object that is spawned for each client
 * Handles spawning correct character for each client
 */

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject[] playerPrefabList; // List of player prefabs for different characters

    private GameObject myGo; // Reference to the instantiated player GameObject

    private int charCode; // Character code for the selected player
    private int teamId; // Team ID for the player
    private string displayName; // Display name for the player

    public bool playerSpawned = false; // Flag to indicate if the player has been spawned

    // Called when the network object is spawned
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Retrieve game manager and player settings
            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            charCode = gm.selectedCharacterCode;
            teamId = gm.teamId;
            displayName = gm.displayName;
            // Spawn the player on the server
            SpawnPlayerServerRpc(charCode, teamId, displayName, OwnerClientId);
            RefreshNames(); // Refresh player names
        }
    }

    // Called when the network object is despawned
    public override void OnNetworkDespawn()
    {
        Cursor.lockState = CursorLockMode.None; // Unlock the cursor
        if (myGo != null)
        {
            myGo.GetComponent<NetworkObject>().Despawn(true); // Despawn the player object
        }
    }

    // Update method (currently not used)
    private void Update()
    {
        //RefreshNames();
    }

    // Refresh the names and stats for all players
    private void RefreshNames()
    {
        // Only the server should handle name refreshing
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (myGo != null)
                {
                    // Update stats for the player
                    SetStatsServerRpc(new NetworkObjectReference(myGo));
                    SetStatsClientRpc(new NetworkObjectReference(myGo));
                }
            }
        }
    }

    // Server RPC to spawn a player on the server
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(int charCode, int teamId, string displayName, ulong clientId)
    {
        //Debug.Log($"SpawnPlayerServerRpc - CharCode: {charCode}, OwnerClientId: {clientId}");

        // Instantiate the player prefab and get its NetworkObject
        myGo = Instantiate(playerPrefabList[charCode]);
        NetworkObject netObj = myGo.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            // Spawn the player object with ownership
            netObj.SpawnWithOwnership(clientId, false);
            SetStatsClientRpc(new NetworkObjectReference(myGo), teamId, displayName);
            myGo.transform.parent = transform; // Set parent to the spawner
            playerSpawned = true; // Mark player as spawned
            //Debug.Log("Player spawned successfully.");
        }
        else
        {
            // Log error if NetworkObject is not found
            Debug.LogError("NetworkObject component not found on the player prefab.");
        }
    }

    // Server RPC to set player stats
    [ServerRpc(RequireOwnership = false)]
    private void SetStatsServerRpc(NetworkObjectReference player)
    {
        //Debug.Log("Called from server rpc");
        if (IsOwner) SetStatsClientRpc(player, teamId, displayName);
    }

    // Client RPC to set player stats
    [ClientRpc]
    private void SetStatsClientRpc(NetworkObjectReference player)
    {
        //Debug.Log("Called from client rpc");
        if (IsOwner) SetStatsClientRpc(player, teamId, displayName);
    }

    // Client RPC to set player stats (with parameters)
    [ClientRpc]
    private void SetStatsClientRpc(NetworkObjectReference player, int teamId, string newName)
    {
        //Debug.Log("Spawner SetNameClientRpc: " + newName);
        GameObject pobj = ((GameObject)player);
        pobj.name = newName; // Set the player's name
        pobj.GetComponentInChildren<NameTag>().SetName(newName); // Update the name tag
        pobj.GetComponentInChildren<NameTag>().SetTeam(teamId); // Update the team ID on the name tag
        pobj.GetComponentInChildren<Shootable>().teamId = teamId; // Set the team ID on the Shootable component
        pobj.GetComponentInChildren<WeaponController>().teamId = teamId; // Set the team ID on the WeaponController component

        // Respawn player if not already spawned
        if (!playerSpawned) pobj.GetComponentInChildren<PlayerController>().Respawn(teamId);
        playerSpawned = true; // Mark player as spawned
    }
}
