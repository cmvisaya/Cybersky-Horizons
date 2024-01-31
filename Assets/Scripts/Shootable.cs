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
    private bool invuln = false;

    private void Start() {
        health = maxHealth;
        if (vignette != null) vignette.CrossFadeAlpha(0f, 0f, false);
    }

    private void Update() {
        //if (vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) health / maxHealth), 0, false);
    }

    public void SetHealth(int newHealth) {
        maxHealth = newHealth;
        health = newHealth;
    }

    public int GetHealth() {
        return health;
    }

    public void Reset() {
        health = maxHealth;
        UpdateVignetteClientRpc(maxHealth);
        Debug.Log("Reset function complete");
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage) {
        TakeDamageServerRpc(damage, -1, 1000000000, "");
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterTeamId, ulong clientWhoShot, string shooterName) {
        Debug.Log("Entity with tag " + tag + " took damage. Owner: " + OwnerClientId + " | Shooter: " + clientWhoShot + " " + shooterName);
        if (health > 0 && (teamId == -1 || teamId != shooterTeamId) && !invuln) health -= damage;
        UpdateVignetteClientRpc(health);
        if(health <= 0) {
            string tag = gameObject.GetComponent<Collider>().tag;
            StartCoroutine(GrantInvuln(3f));
            HandleObjectDeath(tag, clientWhoShot, shooterName);
        }
    }

    private IEnumerator GrantInvuln(float timeInvuln) {
        Debug.Log("Invuln start on " + transform.parent.gameObject.name);
        invuln = true;
        yield return new WaitForSeconds(timeInvuln);
        invuln = false;
        Debug.Log("Invuln end on " + transform.parent.gameObject.name);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc() {
        Reset();
    }

    [ClientRpc]
    private void UpdateVignetteClientRpc(int passHealth) {
        if (IsOwner && vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) passHealth / maxHealth), 0, false);
    }

    //CREATE NEW TAG FOR OBJECTIVE
    private void HandleObjectDeath(string tag, ulong clientWhoShot, string shooterName) {
        switch (tag) {
            case "Player":
                FeedbackToShooterClientRpc(clientWhoShot, "Killed " + transform.parent.gameObject.name);
                DemonstrateKilledByClientRpc("Killed by " + shooterName);
                gameObject.GetComponent<PlayerController>().Respawn();
                UpdateVignetteClientRpc(maxHealth);
                ResetHealthServerRpc();
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
        GameObject.Find("Controller").GetComponent<WeaponController>().kills++;
    }

    [ClientRpc]
    private void DemonstrateKilledByClientRpc(string message) {
        if (IsOwner) {
            GameObject.Find("Controller").GetComponent<WeaponController>().DisplayHUDNotif(message);
            GameObject.Find("Controller").GetComponent<WeaponController>().deaths++;
        }
    }
}
