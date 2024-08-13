using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject[] playerPrefabList;

    private GameObject myGo;

    private int charCode;
    private int teamId;
    private string displayName;

    public bool playerSpawned = false;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            charCode = gm.selectedCharacterCode;
            teamId = gm.teamId;
            displayName = gm.displayName;
            //Debug.Log(charCode + " | " + OwnerClientId);
            SpawnPlayerServerRpc(charCode, teamId, displayName, OwnerClientId);
            RefreshNames();
        }
    }

    public override void OnNetworkDespawn()
    {
        Cursor.lockState = CursorLockMode.None;
        if (myGo != null)
        {
            myGo.GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void Update()
    {
        //RefreshNames();
    }

    private void RefreshNames()
    {
        //Debug.Log("Refreshing for: " + OwnerClientId);
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (myGo != null)
                {
                    SetStatsServerRpc(new NetworkObjectReference(myGo));
                    SetStatsClientRpc(new NetworkObjectReference(myGo));
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(int charCode, int teamId, string displayName, ulong clientId)
    {
        //if (playerSpawned) return;

        Debug.Log($"SpawnPlayerServerRpc - CharCode: {charCode}, OwnerClientId: {clientId}");

        myGo = Instantiate(playerPrefabList[charCode]);
        NetworkObject netObj = myGo.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.SpawnWithOwnership(clientId, false);
            SetStatsClientRpc(new NetworkObjectReference(myGo), teamId, displayName);
            myGo.transform.parent = transform;
            playerSpawned = true;
            Debug.Log("Player spawned successfully.");
        }
        else
        {
            Debug.LogError("NetworkObject component not found on the player prefab.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetStatsServerRpc(NetworkObjectReference player)
    {
        //Debug.Log("Called from server rpc");
        if (IsOwner) SetStatsClientRpc(player, teamId, displayName);
    }

    [ClientRpc]
    private void SetStatsClientRpc(NetworkObjectReference player)
    {
        //Debug.Log("Called from client rpc");
        if (IsOwner) SetStatsClientRpc(player, teamId, displayName);
    }

    [ClientRpc]
    private void SetStatsClientRpc(NetworkObjectReference player, int teamId, string newName)
    {
        //Debug.Log("Spawner SetNameClientRpc: " + newName);
        GameObject pobj = ((GameObject)player);
        pobj.name = newName;
        pobj.GetComponentInChildren<NameTag>().SetName(newName);
        pobj.GetComponentInChildren<NameTag>().SetTeam(teamId);
        pobj.GetComponentInChildren<Shootable>().teamId = teamId;
        pobj.GetComponentInChildren<WeaponController>().teamId = teamId;
        if(!playerSpawned) pobj.GetComponentInChildren<PlayerController>().Respawn(teamId);
        playerSpawned = true;
    }
}