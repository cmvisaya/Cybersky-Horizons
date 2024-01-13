using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shootable : MonoBehaviour
{
    public int health;

    public void TakeDamage(int damage) {
        health -= damage;
        if(health <= 0) {
            string tag = gameObject.GetComponent<Collider>().tag;
            HandleObjectDeath(tag);
        }
    }

    private void HandleObjectDeath(string tag) {
        switch (tag) {
            case "Player":
                gameObject.GetComponent<PlayerController>().Respawn();
                break;
            case "Enemy":
                gameObject.GetComponent<NetworkSpawnable>().Kill();
                //Destroy(gameObject);
                break;
        }
    }
}
