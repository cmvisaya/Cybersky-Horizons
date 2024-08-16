using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script that is attached to all objects that can be shot in singleplayer mode
 * Contains the same functionality as the online variant without the server-client interactions
 */

public class OfflineShootable : MonoBehaviour
{
    [SerializeField] private int health, maxHealth;
    public AudioClip killSound;
    public RawImage vignette;
    private bool invuln = false;

    private void Start() {
        health = maxHealth;
        if (vignette != null) vignette.CrossFadeAlpha(0f, 0f, false);
    }

    private void Update() {
        if (vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) health / maxHealth), 0, false);
    }

    public void SetHealth(int newHealth) {
        maxHealth = newHealth;
        health = newHealth;
    }

    public int GetHealth() {
        return health;
    }

    public bool TakeDamage(int damage) { //bool is for if they died
        Debug.Log("Entity with tag " + tag + " took damage.  ");
        bool died = false;
        health -= damage;
        //UpdateVignetteClientRpc(health);
        if(health <= 0) {
            string tag = gameObject.GetComponent<Collider>().tag;
            HandleObjectDeath(tag);
            died = true;
        }
        return died;
    }

    private IEnumerator GrantInvuln(float timeInvuln) {
        invuln = true;
        yield return new WaitForSeconds(timeInvuln);
        invuln = false;
    }

    private void UpdateVignette(int passHealth) {
        if (vignette != null) vignette.CrossFadeAlpha(1.0f - ((float) passHealth / maxHealth), 0, false);
    }

    //CREATE NEW TAG FOR OBJECTIVE
    private void HandleObjectDeath(string tag) {
        switch (tag) {
            case "Player":
                gameObject.GetComponent<OfflinePlayerController>().Respawn();
                UpdateVignette(maxHealth);
                break;
            case "Enemy":
                Destroy(gameObject);
                GameManager.Instance.latestSinglePlayerKills++;
                PlaySound();
                break;
        }
    }

    private void PlaySound() {
        GameObject.Find("AudioManager").GetComponent<AudioManager>().PlaySoundEffect(killSound, 5f);
    }
}
