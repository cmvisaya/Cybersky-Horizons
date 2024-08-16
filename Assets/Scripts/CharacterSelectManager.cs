using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * Script that is attached to the Character Select Manager object.
 * Handles the entire Character Select scene.
 * This includes button functionality, displaying selected character select stats, and progressing to the appropriate scene with the appropriate character.
 */

public class CharacterSelectManager : MonoBehaviour
{
    // References to UI buttons for locking in a character, going back, and displaying character info
    [SerializeField] private Button lockInBtn, backBtn, infoBtn;

    // Array of buttons for selecting characters
    [SerializeField] private Button[] charButtons;

    // Array of character names corresponding to the character buttons
    [SerializeField] private string[] charNames;

    // Array of character model prefabs to be displayed when a character is selected
    [SerializeField] private GameObject[] charModelPrefabs;

    // Current selected character's code (ID) and the scene code for the next scene to load
    [SerializeField] private int currCharCode, nextSceneCode;

    // UI text component to display the name of the selected character
    public TextMeshProUGUI charName;

    // UI sliders for displaying various stats of the selected character
    [SerializeField] private Slider speed, damage, range, fireRate, magSize;

    // UI text components for displaying the character's class name and the tooltip information
    [SerializeField] private TextMeshProUGUI className, infoName;

    // Tooltip GameObject that shows additional information about the selected character
    public GameObject infoTooltip;

    // Unity's Awake method, called when the script instance is being loaded
    private void Awake() {
        // Add listener to the lock-in button to trigger the LockIn method when clicked
        lockInBtn.onClick.AddListener(() => {
            LockIn();
        });

        // Add listener to the back button to load a specific scene when clicked
        backBtn.onClick.AddListener(() => {
            GameManager.Instance.LoadScene(3);
        });

        // Add listener to the info button to toggle the info tooltip and play a sound effect when clicked
        infoBtn.onClick.AddListener(() => {
            infoTooltip.SetActive(!infoTooltip.activeSelf);
            AudioManager.Instance.PlaySoundEffect(1, 2f);
            infoName.text = infoTooltip.activeSelf ? "Hide Stats" : "Show Stats";
        });

        // Add listeners to each character button to trigger the SelectCharacter method when clicked
        charButtons[0].onClick.AddListener(() => {
            SelectCharacter(0);
        });
        charButtons[1].onClick.AddListener(() => {
            SelectCharacter(1);
        });
        charButtons[2].onClick.AddListener(() => {
            SelectCharacter(2);
        });
        charButtons[3].onClick.AddListener(() => {
            SelectCharacter(3);
        });
        charButtons[4].onClick.AddListener(() => {
            SelectCharacter(4);
        });
        charButtons[5].onClick.AddListener(() => {
            SelectCharacter(5);
        });
        charButtons[6].onClick.AddListener(() => {
            SelectCharacter(6);
        });
        charButtons[7].onClick.AddListener(() => {
            SelectCharacter(7);
        });
        charButtons[8].onClick.AddListener(() => {
            SelectCharacter(8);
        });
        charButtons[9].onClick.AddListener(() => {
            SelectCharacter(9);
        });
    }

    // Unity's Start method, called before the first frame update
    private void Start() {
        // Deactivate all character model prefabs to ensure none are visible at the start
        DeactivateAllPrefabs();
    }

    // Method to handle character selection, triggered by the character buttons
    private void SelectCharacter(int id) {
        // Play a sound effect when a character is selected
        AudioManager.Instance.PlaySoundEffect(1, 2f);

        // Deactivate all character model prefabs to show only the selected one
        DeactivateAllPrefabs();

        // Activate the selected character model prefab and update the character name display
        charModelPrefabs[id].SetActive(true);
        charName.text = charNames[id];

        // Update the current character code and set the info stats for the selected character
        currCharCode = id;
        SetInfoStats(id);
    }

    // Method to set the information stats of the selected character
    private void SetInfoStats(int id) {
        // Retrieve the character stats from the selected character model's CharSelectStats component
        CharSelectStats stats = charModelPrefabs[id].GetComponent<CharSelectStats>();

        // Update the class name and the UI sliders to reflect the selected character's stats
        className.text = "Class: " + stats.className;
        speed.value = stats.speed;
        damage.value = stats.damage;
        range.value = stats.range;
        fireRate.value = stats.fireRate;
        magSize.value = stats.magSize;
    }

    // Method to deactivate all character model prefabs, ensuring none are visible
    private void DeactivateAllPrefabs() {
        foreach(GameObject charModel in charModelPrefabs) {
            charModel.SetActive(false);
        }

        // Clear the character name display when all prefabs are deactivated
        charName.text = "";
    }

    // Method to handle the lock-in process for the selected character
    private void LockIn() {
        // Play a sound effect when the character is locked in
        AudioManager.Instance.PlaySoundEffect(0, 2f);

        // Store the selected character code in the GameManager and load the next scene
        GameManager.Instance.selectedCharacterCode = currCharCode;
        GameManager.Instance.LoadScene(nextSceneCode, GameManager.GameState.IN_ONLINE_MATCH);
    }
}
