using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * Script that is attached to LevelClear game object
 * Runs level clear UI and proper transition when player finishes a singleplayer level
 */

// Serializable class to hold level-specific data
[System.Serializable]
public class Level {
    public string levelClearText; // Text to display when the level is cleared
    public int dfCollected; // Amount of DF (currency or points) collected in the level
}

// Serializable class to hold UI elements for the level clear screen
[System.Serializable]
public class LevelUI {
    public GameObject clearMenu; // The menu that appears when the level is cleared
    public TextMeshProUGUI levelClearText; // Text element to display level clear message
    public TextMeshProUGUI levelDifficultyText; // Text element to display level difficulty
    public TextMeshProUGUI timeToCompleteText; // Text element to display completion time
    public TextMeshProUGUI enemiesKilledText; // Text element to display number of enemies killed
    public TextMeshProUGUI dfObtainedText; // Text element to display DF obtained
    public Button confirmBtn; // Button to confirm and proceed to the next screen
}

public class LevelClear : MonoBehaviour
{
    public Level level; // Level-specific data
    [SerializeField] private LevelUI levelUI; // UI elements for level clear screen
    [SerializeField] private AudioClip levelClearSound; // Sound effect for level clear

    private void Awake() {
        // Initialize UI and button listener
        levelUI.clearMenu.SetActive(false); // Hide the level clear menu initially
        levelUI.confirmBtn.onClick.AddListener(() => {
            GameManager.Instance.inLevelClear = false; // Set level clear flag to false
            AudioManager.Instance.PlaySoundEffect(0, 2f); // Play confirmation sound effect
            GameManager.Instance.LoadScene(6, GameManager.GameState.IN_SELECTION_MENU); // Load the selection menu scene
        });
    }

    // Method to be called when the level is cleared
    public void ClearLevel() {
        // Reset UI texts to default values
        levelUI.levelClearText.text = "";
        levelUI.levelDifficultyText.text = "Difficulty: ";
        levelUI.timeToCompleteText.text = "Time to Complete: ";
        levelUI.enemiesKilledText.text = "Enemies Killed: ";
        levelUI.dfObtainedText.text = "DF Obtained: ";
        
        // Run level clear process
        RunLevelClear();
    }

    // Method to update the UI with level clear details
    private void RunLevelClear() {
        levelUI.clearMenu.SetActive(true); // Show the level clear menu
        GameManager.Instance.inLevelClear = true; // Set level clear flag to true

        // Play level clear sound if assigned
        if (levelClearSound != null) { 
            AudioManager.Instance.PlaySoundEffect(levelClearSound, 1f);
        }

        // Update UI elements with level data
        levelUI.levelClearText.text = "" + level.levelClearText;
        levelUI.levelDifficultyText.text = "Difficulty: " + GameManager.Instance.levelDifficulty.ToString("F1");

        // Format and display the time taken to complete the level
        int timeInSeconds = (int) GameManager.Instance.spLevelElapsedTime;
        int minutes = timeInSeconds / 60;
        int seconds = timeInSeconds % 60;
        levelUI.timeToCompleteText.text = "Time to Complete: " + minutes + ":" + seconds;

        // Display the number of enemies killed and DF collected
        levelUI.enemiesKilledText.text = "Enemies Killed: " + GameManager.Instance.latestSinglePlayerKills;
        levelUI.dfObtainedText.text = "DF Obtained: " + level.dfCollected;

        // Update total DF collected in the game manager
        GameManager.Instance.totalDf += level.dfCollected;
    }
}
