using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/*
 * Script that is attached to all objects spawned on the network
 * Handles proper spawning and despawning of network objects
 */

public class NetworkSpawnable : MonoBehaviour
{
    // This method is called to spawn the network object on the server and synchronize it across all clients
    public void Alive() {
        // Spawns the NetworkObject attached to this GameObject, making it visible and interactable on all clients
        transform.GetComponent<NetworkObject>().Spawn(true);
    }

    // This method is called to despawn (or "kill") the network object, removing it from all clients
    public void Kill() {
        try {
            // Despawns the NetworkObject, removing it from all clients
            transform.GetComponent<NetworkObject>().Despawn(true);
        } catch (NotServerException e) {
            // Catches and logs an exception if this method is called by a non-server client
            Debug.Log(e);
        }
    }
}
