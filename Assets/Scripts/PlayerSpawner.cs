using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour {

    public static PlayerSpawner Instance;
    [SerializeField] private GameObject[] playerPrefabList;

    private void Awake() {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SpawnPlayer() {
        if (OwnerClientId == 0) SpawnPlayerServerRpc();
        else SpawnPlayerClientRpc(OwnerClientId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc() {
        GameObject newPlayer;
        newPlayer = (GameObject)Instantiate(playerPrefabList[GameManager.Instance.selectedCharacterCode]);

        NetworkObject netObj = newPlayer.GetComponent<NetworkObject>();
        newPlayer.SetActive(true);

        Debug.Log("HELLLLLLLLPPPPP " + OwnerClientId);
        netObj.SpawnAsPlayerObject(OwnerClientId, true);
    }

    [ClientRpc]
    public void SpawnPlayerClientRpc(ulong ownderId) {
        if(OwnerClientId != ownderId) return;
        GameObject newPlayer;
        newPlayer = (GameObject)Instantiate(playerPrefabList[GameManager.Instance.selectedCharacterCode]);

        NetworkObject netObj = newPlayer.GetComponent<NetworkObject>();
        newPlayer.SetActive(true);

        Debug.Log("WHAT THE HELLLLL " + OwnerClientId);
        netObj.SpawnAsPlayerObject(OwnerClientId, true);
    }
}
