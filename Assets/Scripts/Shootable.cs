using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Shootable : NetworkBehaviour
{
    [SerializeField] private int health, objectiveId;
    public int teamId = -1;

    public void SetHealth(int newHealth) {
        health = newHealth;
    }

    public int GetHealth() {
        return health;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage) {
        TakeDamageServerRpc(damage, -1);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterTeamId) {
        Debug.Log("Entity with tag " + tag + " took damage. Owner: " + OwnerClientId);
        if (health > 0 && (teamId == -1 || teamId != shooterTeamId)) health -= damage;
        if(health <= 0) {
            string tag = gameObject.GetComponent<Collider>().tag;
            HandleObjectDeath(tag);
        }
    }

    //CREATE NEW TAG FOR OBJECTIVE
    private void HandleObjectDeath(string tag) {
        switch (tag) {
            case "Player":
                health = 100;
                gameObject.GetComponent<PlayerController>().Respawn();
                break;
            case "Enemy":
                gameObject.GetComponent<NetworkSpawnable>().Kill();
                //Destroy(gameObject);
                break;
            case "Objective":
                GameObject.Find("TTRunner").GetComponent<TTRunner>().KillObjective(objectiveId);
                break;
        }
    }
}
