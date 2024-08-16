using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script that is attached to the TitleScreenManager in the Title Screen scene.
 * Adds functionality to UI buttons for redirecting to next scene when pressed.
 */

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] private Button startBtn;
    [SerializeField] private int nextSceneCode;

    private void Awake() {
        startBtn.onClick.AddListener(() => {
            GameManager.Instance.LoadScene(nextSceneCode, GameManager.GameState.IN_SELECTION_MENU);
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });
    }
}
