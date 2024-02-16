using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchDialogue : MonoBehaviour
{
    [SerializeField] private DialogueEvent dialogueEvent;
    [SerializeField] private bool destroyOnTouch;
    [SerializeField] private GameObject destroyedOnTouch;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player") {
            if (dialogueEvent != null) dialogueEvent.TriggerDialogue();
            if (destroyOnTouch) Destroy(destroyedOnTouch);
        }
    }
}
