using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to OfflineKillwall game object
 * Kills player when they come in contact with the collider of this script's game object
 */

public class OfflineKillWall : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Player") {
            other.GetComponent<OfflineShootable>().TakeDamage(10000);
        }
    }
}
