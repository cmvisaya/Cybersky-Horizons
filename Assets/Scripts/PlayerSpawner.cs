using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class PlayerSpawner : NetworkBehaviour {

    [SerializeField] private GameObject[] playerPrefabList;

    private Transform myGo;

    public bool playerSpawned = false;

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            // Only the owner (host or local client) sets the character code
            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            int charCode = gm.selectedCharacterCode;
            int teamId = gm.teamId;
            string displayName = gm.displayName;
            Debug.Log(charCode + " | " + OwnerClientId);
            SpawnPlayerServerRpc(charCode, teamId, displayName, OwnerClientId);
        }
    }

    public override void OnNetworkDespawn() {
        Cursor.lockState = CursorLockMode.None;
        if(myGo != null) {
            myGo.GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(int charCode, int teamId, string displayName, ulong clientId) {
        if (playerSpawned) return;

        Debug.Log($"SpawnPlayerServerRpc - CharCode: {charCode}, OwnerClientId: {clientId}");

        Transform go = Instantiate(playerPrefabList[charCode]).transform;
        myGo = go;
        NetworkObject netObj = go.GetComponent<NetworkObject>();

        if (netObj != null) {
            netObj.SpawnWithOwnership(clientId, false);
            go.parent = transform;
            go.gameObject.GetComponentInChildren<WeaponController>().teamId = teamId;
            go.gameObject.GetComponentInChildren<Shootable>().teamId = teamId;
            go.gameObject.GetComponentInChildren<NameTag>().SetTeam(teamId);
            go.gameObject.GetComponentInChildren<NameTag>().SetName(displayName);
            playerSpawned = true;
            Debug.Log("Player spawned successfully.");
        } else {
            Debug.LogError("NetworkObject component not found on the player prefab.");
        }
    }
}
