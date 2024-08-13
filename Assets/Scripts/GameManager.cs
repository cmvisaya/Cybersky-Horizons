using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject pauseMenu;
    [SerializeField] private Button quitBtn, closeBtn, confirmBtn;
    [SerializeField] private Slider volumeSlider, sfxSlider, bgmSlider;
    public int selectedCharacterCode, teamId, numTeams;
    public string displayName;
    public Dictionary<int, int> charCodes = new Dictionary<int, int>(); //First int is network client id
    public enum GameState {
        TITLESCREEN, //Exits game
        IN_SELECTION_MENU, //Go back to title (gates can be marked as in_selection_menu)
        IN_ONLINE_MATCH, //Disable the quit button (do this for being in lobby as well)
        IN_SINGLEPLAYER_LEVEL, //Go back to Gates
    }

    public float levelDifficulty = 0.0f;
    public int latestSinglePlayerKills = 0;
    public float spLevelElapsedTime = 0f;
    public bool inSpLevel, inLevelClear;

    public int totalDf = 0;

    public GameState currentState = GameState.TITLESCREEN;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //Default team values
        numTeams = 2;
        teamId = 0;

        quitBtn.onClick.AddListener(() => {
            HandleQuitButton();
        });
        closeBtn.onClick.AddListener(() => {
            TogglePauseMenu();
        });
        confirmBtn.onClick.AddListener(() => {
            TogglePauseMenu();
        });

        pauseMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(pauseMenu.activeSelf) {
            //Handle quit button text/activation
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
            //Handle Volume Sliders
            AudioManager.Instance.volumeMult = volumeSlider.value;
            AudioManager.Instance.sfxMult = sfxSlider.value;
            AudioManager.Instance.bgmMult = bgmSlider.value;
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePauseMenu();

        if (inSpLevel) {
            spLevelElapsedTime += Time.deltaTime;
        }

        if (inLevelClear) {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void TogglePauseMenu() {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
    }

    public void LoadScene(int id, GameState state) {
        currentState = state;
        LoadScene(id);
    }

    public void LoadScene(int id) {
        SceneManager.LoadScene(id);
    }

    public void SetTeam(int newTeam) {
        teamId = newTeam;
    }

    public void ChangeTeam() {
        teamId = (teamId + 1) % numTeams;
    }

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
