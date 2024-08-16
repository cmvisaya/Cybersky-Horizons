using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to TouchDialogue game objects
 * Runs attached dialogueEvent when player comes into contact with the trigger attached to the gameobject of which this script's instance is a component
 */

public class TouchDialogue : MonoBehaviour
{
    // Serialized field to assign the DialogueEvent from the Unity Inspector
    [SerializeField] private DialogueEvent dialogueEvent;

    // Boolean to determine whether to destroy an object on touch
    [SerializeField] private bool destroyOnTouch;

    // The GameObject to be destroyed if destroyOnTouch is true
    [SerializeField] private GameObject destroyedOnTouch;

    // This method is called when another collider enters the trigger collider attached to this object
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the tag "Player"
        if(other.tag == "Player") 
        {
            // If a DialogueEvent is assigned, trigger the dialogue
            if (dialogueEvent != null) 
            {
                dialogueEvent.TriggerDialogue();
            }
            
            // If destroyOnTouch is true, destroy the specified GameObject
            if (destroyOnTouch) 
            {
                Destroy(destroyedOnTouch);
            }
        }
    }
}
