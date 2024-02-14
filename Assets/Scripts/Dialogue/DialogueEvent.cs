using System.Collections.Generic;
using System.Collections;
using UnityEngine;
 
[System.Serializable]
public class DialogueCharacter
{
    public string name;
    public Texture2D icon;
}
 
[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;
    public Choice choice;
}

[System.Serializable]
public class Choice
{
    public List<string> responses = new List<string>();
    public List<DialogueEvent> branches = new List<DialogueEvent>();
}
 
[System.Serializable]
public class Dialogue
{
    public int endEventId;
    public float delayTime;
    public bool stealsControl;
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}
 
public class DialogueEvent : MonoBehaviour
{
    public Dialogue dialogue;
 
    public void TriggerDialogue()
    {
        if (dialogue.stealsControl) {
            GameObject player = GameObject.Find("Player");
            player.GetComponentInChildren<OfflinePlayerController>().hasControl = false;
            player.GetComponentInChildren<OfflineWeaponController>().hasControl = false;
        }
        DialogueManager.Instance.StartDialogue(dialogue);
    }
 
    private void Start() //Debug for now
    {
        //StartCoroutine(StartWithDelayTime(dialogue.delayTime));
    }

    public IEnumerator StartWithDelayTime(float dt) {
        yield return new WaitForSeconds(dt);
        TriggerDialogue();
    }
}
 