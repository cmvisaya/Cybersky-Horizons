using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Shootable : NetworkBehaviour
{
    public int health;

    //CHANGE THESE TO USE CLIENTRPC INSTEAD OF SERVER RPCS
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage) {
        health -= damage;
        if(health <= 0) {
            string tag = gameObject.GetComponent<Collider>().tag;
            Debug.Log("Entity with tag " + tag + " took damage. Owner: " + OwnerClientId);
            health = 100;
            HandleObjectDeath(tag);
        }
    }

    private void HandleObjectDeath(string tag) {
        switch (tag) {
            case "Player":
                Debug.Log("shottED");
                gameObject.GetComponent<PlayerController>().Respawn();
                break;
            case "Enemy":
                gameObject.GetComponent<NetworkSpawnable>().Kill();
                //Destroy(gameObject);
                break;
        }
    }
}
