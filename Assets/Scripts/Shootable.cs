using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/*
 * Script attached to all game objects that can be shot.
 * Handles damage dealing, death on server and client, 
 * displaying vignettes and hit arrows.
 */
public class Shootable : NetworkBehaviour
{
    [SerializeField] private int health, maxHealth, objectiveId; // Health parameters and objective ID
    public int teamId = -1; // Team ID for the object
    public AudioClip killSound; // Sound played on kill
    public RawImage vignette; // UI element for vignette effect
    public GameObject hitArrow, uiParent, pivot; // UI elements for hit arrows and their position
    private bool invuln = false; // Flag to indicate if the object is invulnerable
    public int kills, deaths = 0; // Kills and deaths count

    // Initialize health and vignette at start
    private void Start() {
        health = maxHealth;
        if (vignette != null) vignette.CrossFadeAlpha(0f, 0f, false);
    }

    // Update vignette and handle hit arrows input
    private void Update() {
        // Uncomment if vignette should reflect health
        // if (vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) health / maxHealth), 0, false);

        if(Input.GetKeyDown(KeyCode.Q) && hitArrow != null) { 
            GameObject myArrow = Instantiate(hitArrow, uiParent.transform);
            myArrow.GetComponent<Hitarrow>().Init(new Vector3(0,0,0), transform.position, pivot);
        }
    }

    // Set new health value
    public void SetHealth(int newHealth) {
        maxHealth = newHealth;
        health = newHealth;
    }

    // Get current health value
    public int GetHealth() {
        return health;
    }

    // Reset health and update vignette on clients
    public void Reset() {
        health = maxHealth;
        UpdateVignetteClientRpc(maxHealth);
        Debug.Log("Reset function complete");
    }

    // Server RPC to take damage with default parameters
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage) {
        TakeDamageServerRpc(damage, -1, 1000000000, "", new Vector3(0,0,0));
    }

    // Server RPC to take damage with detailed parameters
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterTeamId, ulong clientWhoShot, string shooterName, Vector3 shooterPosition) {
        Debug.Log("Entity with tag " + tag + " took damage. Owner: " + OwnerClientId + " | Shooter: " + clientWhoShot + " " + shooterName);

        // Check if the object can take damage
        if (health > 0 && (teamId == -1 || teamId != shooterTeamId) && !invuln) health -= damage;

        UpdateVignetteClientRpc(health); // Update vignette on clients
        UpdateHitArrowClientRpc(shooterPosition); // Show hit arrow on clients

        // Handle death if health is zero or less
        if(health <= 0) {
            string tag = gameObject.GetComponent<Collider>().tag;
            StartCoroutine(GrantInvuln(3f)); // Grant temporary invulnerability
            HandleObjectDeath(tag, clientWhoShot, shooterName); // Handle the death of the object
        }
    }

    // Coroutine to grant temporary invulnerability
    private IEnumerator GrantInvuln(float timeInvuln) {
        Debug.Log("Invuln start on " + transform.parent.gameObject.name);
        invuln = true;
        yield return new WaitForSeconds(timeInvuln);
        invuln = false;
        Debug.Log("Invuln end on " + transform.parent.gameObject.name);
    }

    // Server RPC to reset health
    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc() {
        Reset();
    }

    // Client RPC to update vignette based on health
    [ClientRpc]
    private void UpdateVignetteClientRpc(int passHealth) {
        if (IsOwner && vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) passHealth / maxHealth), 0, false);
    }

    // Client RPC to display hit arrow
    [ClientRpc]
    private void UpdateHitArrowClientRpc(Vector3 shooterPosition) {
        if (IsOwner && hitArrow != null) {
            GameObject myArrow = Instantiate(hitArrow, uiParent.transform);
            myArrow.GetComponent<Hitarrow>().Init(shooterPosition, transform.position, pivot);
        }
    }

    // Handle the object's death based on its tag
    private void HandleObjectDeath(string tag, ulong clientWhoShot, string shooterName) {
        switch (tag) {
            case "Player":
                FeedbackToShooterClientRpc(clientWhoShot, "Killed " + transform.parent.gameObject.name, true, shooterName);
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
                FeedbackToShooterClientRpc(clientWhoShot, "Destroyed Objective!", false, shooterName);
                GameObject.Find("TTRunner").GetComponent<TTRunner>().KillObjective(objectiveId);
                break;
        }
    }

    // Client RPC to provide feedback to the shooter
    [ClientRpc]
    private void FeedbackToShooterClientRpc(ulong clientWhoShot, string message, bool addKill, string shooterName) {
        if(clientWhoShot == NetworkManager.Singleton.LocalClientId) {
            PlaySound(clientWhoShot);
            DemonstrateKillClientRpc(clientWhoShot, message, addKill, shooterName);
        }
    }

    // Server RPC to increment the number of kills
    [ServerRpc(RequireOwnership = false)]
    private void IncrementKillsServerRpc(ulong clientWhoShot) {
        kills++;
        Debug.Log("KILLS " + OwnerClientId + ": " + kills);
    }

    // Server RPC to increment the number of deaths
    [ServerRpc(RequireOwnership = false)]
    public void IncrementDeathsServerRpc() {
        deaths++; 
        Debug.Log("DEATHS " + OwnerClientId + ": " + deaths);
    }

    // Play sound effect
    private void PlaySound(ulong clientWhoShot) {
        //Debug.Log(OwnerClientId + " | " + clientWhoShot);
        GameObject.Find("AudioManager").GetComponent<AudioManager>().PlaySoundEffect(killSound, 5f);
    }

    // Client RPC to display kill message
    [ClientRpc]
    public void DemonstrateKillClientRpc(ulong clientWhoShot, string message, bool addKill, string shooterName) {
        if(clientWhoShot == NetworkManager.Singleton.LocalClientId) {
            GameObject.Find("Controller").GetComponent<WeaponController>().DisplayHUDNotif(message);
            if (addKill) GameObject.Find(shooterName).GetComponentInChildren<Shootable>().IncrementKillsServerRpc(clientWhoShot);
        }
    }

    // Client RPC to display death message
    [ClientRpc]
    private void DemonstrateKilledByClientRpc(string message) {
        if (IsOwner) {
            GameObject.Find("Controller").GetComponent<WeaponController>().DisplayHUDNotif(message);
        }
    }
}
