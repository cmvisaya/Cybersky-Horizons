using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to kill requirement gameobjects in singleplayer.
 * Attach instances of enemies in required kills array such that an event is run when all of them are killed.
 */

public class KillRequirement : MonoBehaviour
{

    public OfflineShootable[] requiredKills;
    public int fulfilledEventId;

    // Update is called once per frame
    void Update()
    {
        foreach (OfflineShootable enemy in requiredKills) {
            if (enemy != null) {
                return;
            }
        }

        RunKilledEvent();
        Destroy(gameObject);
    }

    private void RunKilledEvent() {
        switch (fulfilledEventId) {
            case 1:
                GameObject.Find("TargetDialogue").GetComponent<DialogueEvent>().TriggerDialogue();
                Destroy(GameObject.Find("DeleteTouch6"));
                break;
        }
    }
}
