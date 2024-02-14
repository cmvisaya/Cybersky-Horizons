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
    public Button continueBtn;
    public Button choiceBtn1;
    public Button choiceBtn2;
    public GameObject choiceBoxes;
 
    private Queue<DialogueLine> lines;
    private List<DialogueEvent> branches;

    private bool inTyping = false;
    private bool inChoice = false;
    private bool stoleControl = false;

    private DialogueLine currentLine;
    private string currentLineText;

    private int endEventId;
    
    public bool isDialogueActive = false;
 
    public float typingSpeed = 0.2f;
 
    public Animator animator;
 
    private void Awake()
    {
        continueBtn.onClick.AddListener(() => {
            DisplayNextDialogueLine();
        });

        choiceBtn1.onClick.AddListener(() => {
            RunChoice(0);
        });

        choiceBtn2.onClick.AddListener(() => {
            RunChoice(1);
        });

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
 
        lines = new Queue<DialogueLine>();
        typingSpeed = 1 / typingSpeed;
        choiceBoxes.SetActive(false);
    }
 
    public void StartDialogue(Dialogue dialogue)
    {
        endEventId = dialogue.endEventId;
        stoleControl = dialogue.stealsControl;
        inChoice = false;
        choiceBoxes.SetActive(false);

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
        if(!inChoice) {
            if(!inTyping) {
                if (lines.Count == 0)
                {
                    EndDialogue();
                    return;
                }
        
                currentLine = lines.Dequeue();
                currentLineText = currentLine.line;
        
                characterIcon.texture = currentLine.character.icon;
                characterName.text = currentLine.character.name;
        
                StopAllCoroutines();
        
                StartCoroutine(TypeSentence(currentLine));
            } else {
                StopAllCoroutines();
                dialogueArea.text = currentLineText;
                inTyping = false;
                HandleChoice(currentLine);
            }
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
        HandleChoice(dialogueLine);
    }

    void HandleChoice(DialogueLine dialogueLine) {
        if(dialogueLine.choice.branches.Count > 0) {
            inChoice = true;
            branches = dialogueLine.choice.branches;
            choiceBoxes.SetActive(true);
        }
    }

    void RunChoice(int choice) {
        branches[choice].TriggerDialogue();
    }
 
    void EndDialogue()
    {
        if (stoleControl) {
            GameObject player = GameObject.Find("Player");
            player.GetComponentInChildren<OfflinePlayerController>().hasControl = true;
            player.GetComponentInChildren<OfflineWeaponController>().hasControl = true;
        }
        isDialogueActive = false;
        animator.Play("hide");
        StartCoroutine(EnactEndEvent());
    }

    private IEnumerator EnactEndEvent() {
        switch (endEventId) {
            case 1:
                yield return new WaitForSeconds(0.5f);
                GameManager.Instance.LoadScene(4);
                break;
        }

        yield return null;
    }


}