using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script that is attached to the Mode Select Manager in the mode selection scene.
 * Adds functionality to UI buttons for redirecting to various scenes when pressed.
 */

public class ModeSelectManager : MonoBehaviour
{
    // Buttons for selecting different game modes and going back
    [SerializeField] private Button onlineBtn, localBtn, backBtn;

    // Scene codes for the online and local modes
    [SerializeField] private int onlineSceneCode, localSceneCode;

    // Awake is called when the script instance is being loaded
    private void Awake() {
        // Assigns a function to be called when the online mode button is pressed
        onlineBtn.onClick.AddListener(() => {
            // Load the scene associated with online mode
            GameManager.Instance.LoadScene(onlineSceneCode);

            // Play a sound effect
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        // Assigns a function to be called when the local mode button is pressed
        localBtn.onClick.AddListener(() => { 
            // Load the scene associated with local mode and set the game state to IN_ONLINE_MATCH
            GameManager.Instance.LoadScene(localSceneCode, GameManager.GameState.IN_ONLINE_MATCH);

            // Stop the background music and play a sound effect
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });

        // Assigns a function to be called when the back button is pressed
        backBtn.onClick.AddListener(() => { 
            // Load the title screen scene and set the game state to TITLESCREEN
            GameManager.Instance.LoadScene(0, GameManager.GameState.TITLESCREEN);
        });
    }
}
