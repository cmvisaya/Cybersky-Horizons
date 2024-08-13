using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class Level {
    public string levelClearText;
    public int dfCollected;
}

[System.Serializable]
public class LevelUI {
    public GameObject clearMenu;
    public TextMeshProUGUI levelClearText, levelDifficultyText, timeToCompleteText, enemiesKilledText, dfObtainedText;
    public Button confirmBtn;
}

public class LevelClear : MonoBehaviour
{
    public Level level;
    [SerializeField] private LevelUI levelUI;
    [SerializeField] private AudioClip levelClearSound;

    private void Awake() {
        levelUI.clearMenu.SetActive(false);
        levelUI.confirmBtn.onClick.AddListener(() => {
            GameManager.Instance.inLevelClear = false;
            AudioManager.Instance.PlaySoundEffect(0, 2f);
            GameManager.Instance.LoadScene(6, GameManager.GameState.IN_SELECTION_MENU);
        });
    }

    public void ClearLevel() {
        levelUI.levelClearText.text = "";
        levelUI.levelDifficultyText.text = "Difficulty: ";
        levelUI.timeToCompleteText.text = "Time to Complete: ";
        levelUI.enemiesKilledText.text = "Enemies Killed: ";
        levelUI.dfObtainedText.text = "DF Obtained: ";
        RunLevelClear();
    }

    private void RunLevelClear() {
        levelUI.clearMenu.SetActive(true);
        GameManager.Instance.inLevelClear = true;
        if (levelClearSound != null) { AudioManager.Instance.PlaySoundEffect(levelClearSound, 1f); }
        levelUI.levelClearText.text = "" + level.levelClearText;
        levelUI.levelDifficultyText.text = "Difficulty: " + GameManager.Instance.levelDifficulty.ToString("F1");
        int timeInSeconds = (int) GameManager.Instance.spLevelElapsedTime;
        int minutes = timeInSeconds / 60;
        int seconds = timeInSeconds % 60;
        levelUI.timeToCompleteText.text = "Time to Complete: " + minutes + ":" + seconds;
        levelUI.enemiesKilledText.text = "Enemies Killed: " + GameManager.Instance.latestSinglePlayerKills;
        levelUI.dfObtainedText.text = "DF Obtained: " + level.dfCollected;
        GameManager.Instance.totalDf += level.dfCollected;
    }
}
