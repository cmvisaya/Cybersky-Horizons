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
    [SerializeField] private Slider volumeSlider;
    public int selectedCharacterCode, teamId, numTeams;
    public string displayName;
    public Dictionary<int, int> charCodes = new Dictionary<int, int>(); //First int is network client id

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
            Application.Quit();
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
        if(pauseMenu.activeSelf) { AudioManager.Instance.volumeMult = volumeSlider.value; Cursor.lockState = CursorLockMode.None; }
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePauseMenu();
    }

    private void TogglePauseMenu() {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
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
}
