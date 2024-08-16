using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/*
 * Script that is attached to the GameManager object.
 * Handles all architecture and stores relevent game data that persists across scenes.
 * The pause menu is also stored here (could be its own script).
 * Some singleplayer stats are also stored here (could be its own script).
 * Team selection is also done here (again, could be moved to a networking script).
 */

public class GameManager : MonoBehaviour
{
    // Singleton instance of the GameManager
    public static GameManager Instance;

    // Reference to the pause menu UI element
    public GameObject pauseMenu;

    // References to buttons in the pause menu
    [SerializeField] private Button quitBtn, closeBtn, confirmBtn;

    // References to sliders for adjusting audio settings in the pause menu
    [SerializeField] private Slider volumeSlider, sfxSlider, bgmSlider;

    // Selected character code, team ID, and the number of teams
    public int selectedCharacterCode, teamId, numTeams;

    // Player's display name
    public string displayName;

    // Dictionary to store character codes associated with network client IDs
    public Dictionary<int, int> charCodes = new Dictionary<int, int>();

    // Enum representing different game states
    public enum GameState {
        TITLESCREEN,            // Goes to title Screen
        IN_SELECTION_MENU,      // In character selection menu, can return to title screen
        IN_ONLINE_MATCH,        // In an online match, quit button is disabled
        IN_SINGLEPLAYER_LEVEL,  // In a single-player level, can return to the mode select menu
    }

    // Variables to track single-player level statistics
    public float levelDifficulty = 0.0f;
    public int latestSinglePlayerKills = 0;
    public float spLevelElapsedTime = 0f;

    // Flags to indicate whether the player is in a single-player level or in a level clear state
    public bool inSpLevel, inLevelClear;

    // Total difficulty factor, possibly related to game settings or difficulty adjustments
    public int totalDf = 0;

    // Current state of the game
    public GameState currentState = GameState.TITLESCREEN;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Implement singleton pattern to ensure only one instance of GameManager exists
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Set default team values
        numTeams = 2;
        teamId = 0;

        // Add listeners to buttons for handling quit, close, and confirm actions in the pause menu
        quitBtn.onClick.AddListener(() => {
            HandleQuitButton();
        });
        closeBtn.onClick.AddListener(() => {
            TogglePauseMenu();
        });
        confirmBtn.onClick.AddListener(() => {
            TogglePauseMenu();
        });

        // Initially set the pause menu to inactive
        pauseMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // If the pause menu is active, handle various UI updates
        if(pauseMenu.activeSelf) {
            // Manage the visibility and text of the quit button based on the current game state
            if(currentState != GameState.IN_ONLINE_MATCH) {
                quitBtn.gameObject.SetActive(true);
                TextMeshProUGUI btn = quitBtn.GetComponentInChildren<TextMeshProUGUI>();
                switch (currentState) {
                    case GameState.TITLESCREEN:
                        btn.text = "Close Game";
                        break;
                    case GameState.IN_SELECTION_MENU:
                        btn.text = "Return to Title Screen";
                        break;
                    case GameState.IN_SINGLEPLAYER_LEVEL:
                        btn.text = "Return to <i>The Gates</i>";
                        break;
                }
            } else {
                quitBtn.gameObject.SetActive(false);
            }

            // Update audio settings based on slider values in the pause menu
            AudioManager.Instance.volumeMult = volumeSlider.value;
            AudioManager.Instance.sfxMult = sfxSlider.value;
            AudioManager.Instance.bgmMult = bgmSlider.value;

            // Unlock the cursor when the pause menu is active
            Cursor.lockState = CursorLockMode.None;
        }

        // Toggle the pause menu when the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePauseMenu();

        // Update the elapsed time if the player is in a single-player level
        if (inSpLevel) {
            spLevelElapsedTime += Time.deltaTime;
        }

        // Unlock the cursor if the player is in a level clear state
        if (inLevelClear) {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // Toggles the visibility of the pause menu
    private void TogglePauseMenu() {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
    }

    // Loads a scene by its ID and sets the game state
    public void LoadScene(int id, GameState state) {
        currentState = state;
        LoadScene(id);
    }

    // Loads a scene by its ID without changing the game state
    public void LoadScene(int id) {
        SceneManager.LoadScene(id);
    }

    // Sets the player's team ID to a new value
    public void SetTeam(int newTeam) {
        teamId = newTeam;
    }

    // Cycles through the available teams, updating the team ID
    public void ChangeTeam() {
        teamId = (teamId + 1) % numTeams;
    }

    // Handles the quit button functionality based on the current game state
    public void HandleQuitButton() {
        switch (currentState) {
            case GameState.TITLESCREEN:
                Application.Quit();
                break;
            case GameState.IN_SELECTION_MENU:
                LoadScene(0, GameState.TITLESCREEN);
                break;
            case GameState.IN_SINGLEPLAYER_LEVEL:
                LoadScene(6, GameState.IN_SELECTION_MENU);
                break;
        }
    }
}
