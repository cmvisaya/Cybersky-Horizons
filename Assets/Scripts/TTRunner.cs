using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

/*
 * Script attached to the TTRunner game object.
 * Manages game logic, objective tracking, timer, and win conditions.
 */
public class TTRunner : NetworkBehaviour
{
    [SerializeField] private Slider redCentral, redOpposite, blueCentral, blueOpposite; // Sliders for tracking objective health
    [SerializeField] private Shootable rchp, rohp, bchp, bohp; // References to Shootable components for objectives
    private bool[] objectivesDestroyed = new bool[4]; // Tracks which objectives have been destroyed
    private int firstDestroyed = -1; // Tracks the ID of the first destroyed objective
    private bool timerActive = false; // Flag to manage timer state
    [SerializeField] private float secondsPerGame; // Total game duration in seconds
    private NetworkVariable<float> secondsLeft = new NetworkVariable<float>(); // Time remaining in the game
    public NetworkVariable<int> maxHp = new NetworkVariable<int>(); // Maximum health value for objectives
    [SerializeField] private TextMeshProUGUI winText, timerText; // UI elements for displaying win messages and timer
    public Transform redSpawn, blueSpawn; // Spawn points for teams
    private float endGameWaitTime = 5f; // Time to wait before transitioning scenes after game end
    [SerializeField] private GameObject ppc; // Placeholder for player prefab (if used)
    private string kdText; // Text to display kills and deaths
    [SerializeField] AudioClip previousBgm; // Background music to play during scene transition

    private void Awake() {
        if (winText.gameObject.activeSelf) {
            winText.text = "";
            winText.gameObject.SetActive(false);
        }
        secondsLeft.Value = secondsPerGame + 1; // Initialize the timer
        maxHp.Value = 1350; // Set default maximum health
    }

    // Initialize game state and UI elements
    void Start()
    {
        Init();
    }

    // Reset game state and initialize UI elements
    public void Init() {
        if (winText.gameObject.activeSelf) {
            winText.text = "";
            winText.gameObject.SetActive(false);
        }
        secondsLeft.Value = secondsPerGame + 1;

        redCentral.maxValue = maxHp.Value;
        redOpposite.maxValue = maxHp.Value;
        blueCentral.maxValue = maxHp.Value;
        blueOpposite.maxValue = maxHp.Value;
        rchp.SetHealth(maxHp.Value);
        rohp.SetHealth(maxHp.Value);
        bchp.SetHealth(maxHp.Value);
        bohp.SetHealth(maxHp.Value);

        Debug.Log("Max HP: " + maxHp.Value);

        timerActive = true;
    }

    private void Update() {
        PropogateHealthClientRpc(rchp.GetHealth(), rohp.GetHealth(), bchp.GetHealth(), bohp.GetHealth());
        HandleTimer();
    }

    // ClientRPC to update health sliders for all clients
    [ClientRpc]
    private void PropogateHealthClientRpc(int rc, int ro, int bc, int bo) {
        redCentral.value = rc;
        redOpposite.value = ro;
        blueCentral.value = bc;
        blueOpposite.value = bo;
    }

    // Reset the game timer on the server
    public void ResetTimer() {
        ResetTimerServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ResetTimerServerRpc() {
        secondsLeft.Value = secondsPerGame + 1;
    }

    private void HandleTimer() {
        if (timerActive && secondsLeft.Value > 0 && timerText.transform.parent.gameObject.activeSelf) {
            if (IsServer) secondsLeft.Value -= Time.deltaTime;
            int seconds = (int) (secondsLeft.Value % 60);
            if (seconds < 10) timerText.text = "" + (int) (secondsLeft.Value / 60) + ":0" + (int) (secondsLeft.Value % 60);
            else timerText.text = "" + (int) (secondsLeft.Value / 60) + ":" + (int) (secondsLeft.Value % 60);
        }

        if (secondsLeft.Value < 1) {
            FreezePlayers();
            StartCoroutine(HandleWin());
        }
    }

    // Freeze all players' controls
    private void FreezePlayers() {
        PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players) {
            player.hasControl = false;
            player.gameObject.GetComponent<WeaponController>().hasControl = false;
            timerActive = false;
        }
    }

    // Unfreeze all players' controls
    private void UnfreezePlayers() {
        PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players) {
            player.hasControl = true;
            player.gameObject.GetComponent<WeaponController>().hasControl = true;
            timerActive = true;
        }
    }

    // ServerRPC to update the kills/deaths text
    [ServerRpc(RequireOwnership = false)]
    private void SetKDTextServerRpc(string text) {
        WeaponController[] players = Object.FindObjectsOfType<WeaponController>();
        kdText = "";
        foreach (WeaponController player in players) {
            Shootable myShootable = player.GetComponent<Shootable>();
            string playerLine = player.transform.parent.gameObject.name + ": " + myShootable.kills + "/" + myShootable.deaths + "\n";
            kdText += playerLine;
        }
        SetKDTextClientRpc(text + "\n\n" + kdText);
    }

    // ClientRPC to display the kills/deaths text
    [ClientRpc]
    private void SetKDTextClientRpc(string text) {
        winText.gameObject.SetActive(true); 
        winText.text = text;
    }

    // Despawn all game objects with a NetworkObject component
    private void DespawnAll() {
        PlayerSpawner[] spawners = Object.FindObjectsOfType<PlayerSpawner>();
        foreach (PlayerSpawner spawner in spawners) {
            try {
                spawner.gameObject.GetComponent<NetworkObject>().Despawn(true);
            } catch (SpawnStateException e) {
                Debug.Log(e);
            }
        }
    }

    // Handle objective destruction and game win logic
    public void KillObjective(int id) {
        Shootable destroyed = null;
        if (firstDestroyed < 0) {
            firstDestroyed = id;
        }
        switch (id) {
            case 0:
                destroyed = rchp;
                break;
            case 1:
                destroyed = rohp;
                break;
            case 2:
                destroyed = bchp;
                break;
            case 3:
                destroyed = bohp;
                break;
        }
        objectivesDestroyed[id] = true;
        StartCoroutine(HandleWin());
    }

    // ClientRPC to display the winner and handle end of game
    [ClientRpc]
    private void DisplayWinnerClientRpc(string text) {
        FreezePlayers();
        SetKDTextServerRpc(text);
        timerText.text = "TIME!";
        timerActive = false;
    }

    // Handle the end of the game, determining the winner and transitioning scenes
    private IEnumerator HandleWin() {
        bool blueWon;
        bool redWon;
        if (timerActive) {
            blueWon = objectivesDestroyed[0] && objectivesDestroyed[1];
            redWon = objectivesDestroyed[2] && objectivesDestroyed[3];

            if (blueWon) {
                DisplayWinnerClientRpc("Blue wins!");
                yield return new WaitForSeconds(endGameWaitTime);
                EndGameServerRpc();
            }
            else if (redWon) { 
                DisplayWinnerClientRpc("Red wins!");
                yield return new WaitForSeconds(endGameWaitTime);
                EndGameServerRpc();
            }
        } else {
            int redDestroyed = 0;
            int blueDestroyed = 0;
            for (int i = 0; i < 2; i++) {
                if (objectivesDestroyed[i]) redDestroyed++;
            }
            for (int i = 2; i < 4; i++) {
                if (objectivesDestroyed[i]) blueDestroyed++;
            }
            blueWon = redDestroyed > blueDestroyed || firstDestroyed == 0 || firstDestroyed == 1;
            redWon = blueDestroyed > redDestroyed || firstDestroyed == 2 || firstDestroyed == 3;
            if (blueWon) { 
                DisplayWinnerClientRpc("Blue wins!");
                yield return new WaitForSeconds(endGameWaitTime); 
                EndGameServerRpc();
            }
            else if (redWon) { 
                DisplayWinnerClientRpc("Red wins!");
                yield return new WaitForSeconds(endGameWaitTime); 
                EndGameServerRpc();
            }
            else { 
                DisplayWinnerClientRpc("It's a tie!");
                yield return new WaitForSeconds(endGameWaitTime); 
                EndGameServerRpc();
            }
        }

        yield return new WaitForSeconds(0f);
    }

    // ServerRPC to end the game, despawning all objects and transitioning scenes
    [ServerRpc(RequireOwnership = false)]
    private void EndGameServerRpc() {
        DespawnAll();

        NetworkManagerUI.Instance.DeleteLobby();
        TransitionSceneClientRpc();
    }

    // ClientRPC to transition to a new scene
    [ClientRpc]
    private void TransitionSceneClientRpc() {
        AudioManager.Instance.PlayBGM(previousBgm, 0.2f);
        NetworkManager.Singleton.SceneManager.LoadScene("OfflineTester", LoadSceneMode.Single);
    }

    [ClientRpc]
    private void ReopenLobbyClientRpc() {
        NetworkManagerUI.Instance.ReturnToLobby();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnOnServerRpc() {
        DespawnAll();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TransitionSceneServerRpc() {
        AudioManager.Instance.PlayBGM(previousBgm, 0.2f);
        NetworkManager.Singleton.SceneManager.LoadScene("OfflineTester", LoadSceneMode.Single);
    }
}
