using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfflineKillWall : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Player") {
            other.GetComponent<OfflineShootable>().TakeDamage(10000);
        }
    }
}
