using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkSpawnable : MonoBehaviour
{
    public void Alive() {
        transform.GetComponent<NetworkObject>().Spawn(true);
    }

    public void Kill() {
        try {
            transform.GetComponent<NetworkObject>().Despawn(true);
        } catch (NotServerException e) {
            Debug.Log(e);
        }
    }
}