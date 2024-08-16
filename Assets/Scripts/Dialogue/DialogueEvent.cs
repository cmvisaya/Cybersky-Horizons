using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/*
 * Script that is attached to DialogueEvent game objects and define the full behavior of any given dialogue event.
 * The script component of the game object is then attached in "dialogue runner" scripts
 */

[System.Serializable]
public class DialogueCharacter
{
    public string name;  // Name of the character speaking
    public Texture2D icon;  // Icon representing the character
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;  // Character who is speaking
    [TextArea(3, 10)]
    public string line;  // The line of dialogue spoken by the character
    public Choice choice;  // Optional choice associated with this dialogue line
}

[System.Serializable]
public class Choice
{
    public List<string> responses = new List<string>();  // List of possible responses
    public List<DialogueEvent> branches = new List<DialogueEvent>();  // Dialogue events triggered by each response
}

[System.Serializable]
public class Dialogue
{
    public int endEventId;  // Identifier for the end event
    public float delayTime;  // Delay time before starting the dialogue
    public bool stealsControl;  // Determines if the dialogue steals control from the player
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();  // List of dialogue lines
}

public class DialogueEvent : MonoBehaviour
{
    public Dialogue dialogue;  // Dialogue data for this event

    // Trigger the dialogue event
    public void TriggerDialogue()
    {
        // If the dialogue steals control, disable player control
        if (dialogue.stealsControl) {
            GameObject player = GameObject.Find("Player");
            if (player != null) {
                var playerController = player.GetComponentInChildren<OfflinePlayerController>();
                var weaponController = player.GetComponentInChildren<OfflineWeaponController>();
                if (playerController != null) playerController.hasControl = false;
                if (weaponController != null) weaponController.hasControl = false;
            }
        }

        // Start the dialogue through the DialogueManager
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void Start() // Debug for now
    {
        // Start the dialogue with a delay if specified
        //StartCoroutine(StartWithDelayTime(dialogue.delayTime));
    }

    // Coroutine to start the dialogue with a delay
    public IEnumerator StartWithDelayTime(float dt) {
        yield return new WaitForSeconds(dt);
        TriggerDialogue();
    }
}
