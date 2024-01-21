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

    public bool playerSpawned = false;

    public override void OnNetworkSpawn() {
    if (IsOwner) {
        // Only the owner (host or local client) sets the character code
        int charCode = GameObject.Find("GameManager").GetComponent<GameManager>().selectedCharacterCode;
        int teamId = GameObject.Find("GameManager").GetComponent<GameManager>().teamId;
        Debug.Log(charCode + " | " + OwnerClientId);
        SpawnPlayerServerRpc(charCode, teamId, OwnerClientId);
    }
}

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(int charCode, int teamId, ulong clientId) {
        if (playerSpawned) return;

        Debug.Log($"SpawnPlayerServerRpc - CharCode: {charCode}, OwnerClientId: {clientId}");

        Transform go = Instantiate(playerPrefabList[charCode]).transform;
        NetworkObject netObj = go.GetComponent<NetworkObject>();

        if (netObj != null) {
            netObj.SpawnWithOwnership(clientId, false);
            go.parent = transform;
            go.gameObject.GetComponentInChildren<WeaponController>().teamId = teamId;
            go.gameObject.GetComponentInChildren<Shootable>().teamId = teamId;
            playerSpawned = true;
            Debug.Log("Player spawned successfully.");
        } else {
            Debug.LogError("NetworkObject component not found on the player prefab.");
        }
    }
}
