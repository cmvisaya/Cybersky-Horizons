using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Shootable : NetworkBehaviour
{
    [SerializeField] private int health, maxHealth, objectiveId;
    public int teamId = -1;
    public AudioClip killSound;
    public RawImage vignette;

    private void Start() {
        maxHealth = health;
        if (vignette != null) vignette.CrossFadeAlpha(0f, 0f, false);
    }

    private void Update() {
        //if (vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) health / maxHealth), 0, false);
    }

    public void SetHealth(int newHealth) {
        health = newHealth;
    }

    public int GetHealth() {
        return health;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage) {
        TakeDamageServerRpc(damage, -1, 1000000000);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterTeamId, ulong clientWhoShot) {
        Debug.Log("Entity with tag " + tag + " took damage. Owner: " + OwnerClientId + " | Shooter: " + clientWhoShot);
        if (health > 0 && (teamId == -1 || teamId != shooterTeamId)) health -= damage;
        UpdateVignetteClientRpc(health);
        if(health <= 0) {
            string tag = gameObject.GetComponent<Collider>().tag;
            HandleObjectDeath(tag, clientWhoShot);
        }
    }

    [ClientRpc]
    private void UpdateVignetteClientRpc(int passHealth) {
        if (IsOwner && vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) passHealth / maxHealth), 0, false);
    }

    //CREATE NEW TAG FOR OBJECTIVE
    private void HandleObjectDeath(string tag, ulong clientWhoShot) {
        switch (tag) {
            case "Player":
                health = 100;
                UpdateVignetteClientRpc(maxHealth);
                FeedbackToShooterClientRpc(clientWhoShot, "Killed " + transform.parent.gameObject.name);
                gameObject.GetComponent<PlayerController>().Respawn();
                break;
            case "Enemy":
                gameObject.GetComponent<NetworkSpawnable>().Kill();
                //Destroy(gameObject);
                break;
            case "Objective":
                FeedbackToShooterClientRpc(clientWhoShot, "Destroyed Objective!");
                GameObject.Find("TTRunner").GetComponent<TTRunner>().KillObjective(objectiveId);
                break;
        }
    }

    [ClientRpc]
    private void FeedbackToShooterClientRpc(ulong clientWhoShot, string message) {
        if(clientWhoShot == NetworkManager.Singleton.LocalClientId) {
            PlaySound(clientWhoShot);
            DemonstrateKill(clientWhoShot, message);
        }
    }

    private void PlaySound(ulong clientWhoShot) {
        Debug.Log(OwnerClientId + " | " + clientWhoShot);
        GameObject.Find("AudioManager").GetComponent<AudioManager>().PlaySoundEffect(killSound, 5f);
    }

    public void DemonstrateKill(ulong clientWhoShot, string message) {
        Debug.Log(OwnerClientId + " | " + clientWhoShot);
        GameObject.Find("Controller").GetComponent<WeaponController>().DisplayHUDNotif(message);
    }
}
