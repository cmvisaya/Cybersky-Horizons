using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour {

    public static PlayerSpawner Instance;
    [SerializeField] private GameObject[] playerPrefabList;
 
    [ServerRpc(RequireOwnership=false)] //server owns this object but client can request a spawn

    private void Awake() {
        Instance = this;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc() {
        GameObject newPlayer;
        newPlayer=(GameObject)Instantiate(playerPrefabList[GameManager.Instance.selectedCharacterCode]);

        NetworkObject netObj = newPlayer.GetComponent<NetworkObject>();
        newPlayer.SetActive(true);
        netObj.SpawnAsPlayerObject(OwnerClientId,true);
    }
}
