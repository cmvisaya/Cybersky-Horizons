using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to ONLINE killwall gameobjects.
 * Kill walls must have a collider set as trigger
 * Assumes players have less than 1000 HP
 */

public class KillWall : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Player") {
            other.GetComponent<Shootable>().TakeDamageServerRpc(1000);
        }
    }
}
