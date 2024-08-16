using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

/*
 * Script that is attached to Dialogue Manager game object
 * Handles the running of given DialogueEvent instances. This includes dialogue system output, proper rich text display, player choice.
 */

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;  // Singleton instance of DialogueManager

    public RawImage characterIcon;  // UI element for displaying the character's icon
    public TextMeshProUGUI characterName;  // UI element for displaying the character's name
    public TextMeshProUGUI dialogueArea;  // UI element for displaying the dialogue text
    public Button continueBtn;  // Button to continue to the next line of dialogue
    public Button choiceBtn1;  // Button for the first choice response
    public Button choiceBtn2;  // Button for the second choice response
    public GameObject choiceBoxes;  // UI element containing choice buttons

    private Queue<DialogueLine> lines;  // Queue to manage the lines of dialogue
    private List<DialogueEvent> branches;  // List of dialogue events for branching choices

    private bool inTyping, inChoice, stoleControl, skipRichTag = false;  // Flags for dialogue state

    private DialogueLine currentLine;  // Currently active line of dialogue
    private string currentLineText;  // Text of the currently active line

    private int endEventId;  // ID for the end event to trigger after the dialogue

    public bool isDialogueActive = false;  // Indicates if a dialogue is currently active

    public float typingSpeed = 0.2f;  // Speed at which the dialogue is typed out

    public Animator animator;  // Animator for controlling dialogue animations
    public AudioClip typeSound;  // Sound effect for typing

    private void Awake()
    {
        // Setup button listeners for choices
        choiceBtn1.onClick.AddListener(() => {
            RunChoice(0);
        });

        choiceBtn2.onClick.AddListener(() => {
            RunChoice(1);
        });

        // Singleton pattern to ensure only one instance of DialogueManager
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  // Preserve this object across scenes

        lines = new Queue<DialogueLine>();  // Initialize the dialogue lines queue
        typingSpeed = 1 / typingSpeed;  // Inverse of typing speed for correct coroutine timing
        choiceBoxes.SetActive(false);  // Hide choice boxes initially
    }

    private void Update()
    {
        // Handle input for advancing dialogue
        if(Input.GetButtonDown("Fire1") && isDialogueActive) {
            DisplayNextDialogueLine();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        endEventId = dialogue.endEventId;
        stoleControl = dialogue.stealsControl;
        inChoice = false;
        choiceBoxes.SetActive(false);

        isDialogueActive = true;

        if (stoleControl) Cursor.lockState = CursorLockMode.None;  // Unlock cursor if dialogue steals control

        if(dialogue.dialogueLines.Count > 0) animator.Play("show");  // Play show animation if there are dialogue lines

        lines.Clear();

        // Enqueue all dialogue lines
        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        DisplayNextDialogueLine();  // Start displaying the first line of dialogue
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
        
                currentLine = lines.Dequeue();  // Get the next line of dialogue
                currentLineText = currentLine.line;
        
                // Update UI with current line's character info
                characterIcon.texture = currentLine.character.icon;
                characterName.text = currentLine.character.name;
        
                StopAllCoroutines();  // Stop any ongoing typing coroutines
        
                StartCoroutine(TypeSentence(currentLine));  // Start typing out the current line
            } else {
                StopAllCoroutines();
                dialogueArea.text = currentLineText;  // Display the full line if typing is interrupted
                inTyping = false;
                HandleChoice(currentLine);  // Handle choices if available
            }
        }
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        float waitTime = typingSpeed;
        inTyping = true;
        dialogueArea.text = "";
        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            if(skipRichTag) {
                waitTime = typingSpeed;
                skipRichTag = false;
            }

            if(letter == '<') {
                waitTime = 0f;  // Skip time if inside a rich text tag
            }
            else if(letter == '>') {
                skipRichTag = true;
            }

            dialogueArea.text += letter;  // Append each letter to the dialogue area
            AudioManager.Instance.PlaySoundEffect(typeSound, 1.5f);  // Play typing sound
            yield return new WaitForSeconds(waitTime);  // Wait for the specified typing speed
        }
        inTyping = false;
        if(!stoleControl) {
            yield return new WaitForSeconds(2f);
            DisplayNextDialogueLine();  // Automatically proceed to next line if control was not stolen
        }
        HandleChoice(dialogueLine);  // Handle any choices if present
    }

    void HandleChoice(DialogueLine dialogueLine) {
        if(dialogueLine.choice.branches.Count > 0) {
            inChoice = true;
            branches = dialogueLine.choice.branches;
            choiceBoxes.SetActive(true);  // Show choice boxes if choices are available
        }
    }

    void RunChoice(int choice) {
        branches[choice].TriggerDialogue();  // Trigger the dialogue event corresponding to the chosen option
    }

    void EndDialogue()
    {
        if (stoleControl) {
            GameObject player = GameObject.Find("Player");
            if (player != null) {
                var playerController = player.GetComponentInChildren<OfflinePlayerController>();
                var weaponController = player.GetComponentInChildren<OfflineWeaponController>();
                if (playerController != null) playerController.hasControl = true;
                if (weaponController != null) weaponController.hasControl = true;
            }
        }
        isDialogueActive = false;
        animator.Play("hide");  // Play hide animation
        StartCoroutine(EnactEndEvent());  // Trigger end event based on endEventId
    }

    private IEnumerator EnactEndEvent() {
        switch (endEventId) {
            case 1:
                yield return new WaitForSeconds(0.5f);
                GameManager.Instance.LoadScene(4, GameManager.GameState.IN_ONLINE_MATCH);  // Load specific scene
                break;
            case 2:
                GameObject.Find("Player").GetComponentInChildren<OfflineWeaponController>().ResetBullets();  // Reset player bullets
                GameObject.Find("Player").GetComponentInChildren<OfflinePlayerController>().respawnLocation = GameObject.Find("ObstacleRespawn").transform;  // Set respawn location
                GameManager.Instance.spLevelElapsedTime = 0f;
                GameManager.Instance.inSpLevel = true;
                GameManager.Instance.levelDifficulty = 2.0f;
                break;
            case 3:
                yield return new WaitForSeconds(0.5f);
                LevelClear lccomp = GameObject.Find("LevelClear").GetComponent<LevelClear>();
                lccomp.level.dfCollected = 100;  // Update level status
                lccomp.ClearLevel();  // Clear the level
                //GameManager.Instance.LoadScene(6, GameManager.GameState.IN_SELECTION_MENU);
                break;
        }

        yield return null;
    }
}
