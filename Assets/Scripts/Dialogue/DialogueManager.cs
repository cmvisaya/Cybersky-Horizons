using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
 
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
 
    public RawImage characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;
 
    private Queue<DialogueLine> lines;

    private bool inTyping = false;
    private string currentLineText;

    private int endEventId;
    
    public bool isDialogueActive = false;
 
    public float typingSpeed = 0.2f;
 
    public Animator animator;
 
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
 
        lines = new Queue<DialogueLine>();
        typingSpeed = 1 / typingSpeed;
    }
 
    public void StartDialogue(Dialogue dialogue)
    {
        endEventId = dialogue.endEventId;

        isDialogueActive = true;
 
        animator.Play("show");
 
        lines.Clear();
 
        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }
 
        DisplayNextDialogueLine();
    }
 
    public void DisplayNextDialogueLine()
    {
        if(!inTyping) {
            if (lines.Count == 0)
            {
                EndDialogue();
                return;
            }
    
            DialogueLine currentLine = lines.Dequeue();
            currentLineText = currentLine.line;
    
            characterIcon.texture = currentLine.character.icon;
            characterName.text = currentLine.character.name;
    
            StopAllCoroutines();
    
            StartCoroutine(TypeSentence(currentLine));
        } else {
            StopAllCoroutines();
            dialogueArea.text = currentLineText;
            inTyping = false;
        }
    }
 
    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        inTyping = true;
        dialogueArea.text = "";
        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        inTyping = false;
    }
 
    void EndDialogue()
    {
        isDialogueActive = false;
        animator.Play("hide");
        StartCoroutine(EnactEndEvent());
    }

    private IEnumerator EnactEndEvent() {
        switch (endEventId) {
            case 1:
                yield return new WaitForSeconds(2f);
                GameManager.Instance.LoadScene(4);
                break;
        }

        yield return null;
    }


}