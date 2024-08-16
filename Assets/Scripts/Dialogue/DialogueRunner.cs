using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to Dialogue Runer gameobject
 * Runs attached dialogueEvent on start
 */

public class DialogueRunner : MonoBehaviour
{
    public DialogueEvent myEvent;  // Reference to the DialogueEvent that will be triggered
    public float delayTime;  // Delay before starting the dialogue event

    // Start is called before the first frame update
    void Start()
    {
        // Start the coroutine to handle the delay and trigger the dialogue event
        StartCoroutine(myEvent.StartWithDelayTime(delayTime));
    }
}
