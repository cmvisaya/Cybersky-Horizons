using System.Collections.Generic;
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
}
 
[System.Serializable]
public class Dialogue
{
    public int endEventId;
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}
 
public class DialogueEvent : MonoBehaviour
{
    public Dialogue dialogue;
 
    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }
 
    private void Start() //Debug for now
    {
        TriggerDialogue();
    }
}
 