using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class TTRunner : NetworkBehaviour
{

    [SerializeField] private Slider redCentral, redOpposite, blueCentral, blueOpposite;
    [SerializeField] private Shootable rchp, rohp, bchp, bohp;
    private bool[] objectivesDestroyed = new bool[4];
    private int firstDestroyed = -1;
    private bool timerActive = false;
    [SerializeField] private float secondsPerGame;
    private NetworkVariable<float> secondsLeft = new NetworkVariable<float>();
    [SerializeField] private int maxHp;
    [SerializeField] private TextMeshProUGUI winText, timerText;
    public Transform redSpawn, blueSpawn;
    private float endGameWaitTime = 5f;
    [SerializeField] private GameObject ppc;
    private string kdText;

    private void Awake() {
        if(winText.gameObject.activeSelf) {
            winText.text = "";
            winText.gameObject.SetActive(false);
        }
        secondsLeft.Value = secondsPerGame + 1;
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    public void Init() {
        if(winText.gameObject.activeSelf) {
            winText.text = "";
            winText.gameObject.SetActive(false);
        }
        secondsLeft.Value = secondsPerGame + 1;

        redCentral.maxValue = maxHp;
        redOpposite.maxValue = maxHp;
        blueCentral.maxValue = maxHp;
        blueOpposite.maxValue = maxHp;
        rchp.SetHealth(maxHp);
        rohp.SetHealth(maxHp);
        bchp.SetHealth(maxHp);
        bohp.SetHealth(maxHp);

        timerActive = true;
    }

    private void Update() {
        PropogateHealthClientRpc(rchp.GetHealth(), rohp.GetHealth(), bchp.GetHealth(), bohp.GetHealth());
        HandleTimer();
    }

    [ClientRpc]
    private void PropogateHealthClientRpc(int rc, int ro, int bc, int bo) {
        redCentral.value = rc;
        redOpposite.value = ro;
        blueCentral.value = bc;
        blueOpposite.value = bo;
    }

    public void ResetTimer() {
        ResetTimerServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ResetTimerServerRpc() {
        // Debug.Log(TestRelay.Instance.clientsConnected.Value + " | " + TestRelay.Instance.clientsExpected);
        // if (TestRelay.Instance.clientsConnected.Value == TestRelay.Instance.clientsExpected) {
        //     UnfreezePlayers();
        // } else {
        //     FreezePlayers();
        // }

        secondsLeft.Value = secondsPerGame + 1;
    }

    private void HandleTimer() {
        if (timerActive && secondsLeft.Value > 0 && timerText.transform.parent.gameObject.activeSelf) {
            if(IsServer) secondsLeft.Value -= Time.deltaTime;
            int seconds = (int) (secondsLeft.Value % 60);
            if (seconds < 10) timerText.text = "" + (int) (secondsLeft.Value / 60) + ":0" + (int) (secondsLeft.Value % 60);
            else timerText.text = "" + (int) (secondsLeft.Value / 60) + ":" + (int) (secondsLeft.Value % 60);
        }

        if(secondsLeft.Value < 1) {
            FreezePlayers();
            StartCoroutine(HandleWin());
        }
    }

    private void FreezePlayers() {
        PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
        foreach(PlayerController player in players) {
            player.hasControl = false;
            player.gameObject.GetComponent<WeaponController>().hasControl = false;
            timerActive = false;
        }
    }

    private void UnfreezePlayers() {
        PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
        foreach(PlayerController player in players) {
            player.hasControl = true;
            player.gameObject.GetComponent<WeaponController>().hasControl = true;
            timerActive = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetKDTextServerRpc(string text) {
        WeaponController[] players = Object.FindObjectsOfType<WeaponController>();
        kdText = "";
        foreach(WeaponController player in players) {
            Shootable myShootable = player.GetComponent<Shootable>();
            string playerLine = player.transform.parent.gameObject.name + ": " + myShootable.kills + "/" + myShootable.deaths + "\n";
            kdText += playerLine;
        }
        SetKDTextClientRpc(text + "\n\n" + kdText);
    }

    [ClientRpc]
    private void SetKDTextClientRpc(string text) {
        winText.gameObject.SetActive(true); 
        winText.text = text;
    }

    private void DespawnAll() {
        PlayerSpawner[] spawners = Object.FindObjectsOfType<PlayerSpawner>();
        //Debug.Log(spawners.Length);
        foreach(PlayerSpawner spawner in spawners) {
            try {
                spawner.gameObject.GetComponent<NetworkObject>().Despawn(true);
            } catch (SpawnStateException e) {
                Debug.Log(e);
            }
        }
    }

    public void KillObjective(int id) {
        //Debug.Log("Killed objective: " + id);
        Shootable destroyed = null;
        if(firstDestroyed < 0) {
            firstDestroyed = id;
        }
        switch(id) {
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

    [ClientRpc]
    private void DisplayWinnerClientRpc(string text) {
        FreezePlayers();
        SetKDTextServerRpc(text);
        timerText.text = "TIME!";
        timerActive = false;
    }

    private IEnumerator HandleWin() {
        bool blueWon;
        bool redWon;
        if(timerActive) {
            blueWon = objectivesDestroyed[0] && objectivesDestroyed[1];
            redWon = objectivesDestroyed[2] && objectivesDestroyed[3];

            //Do Game Logic Here (rn technically gives blue advantage if they both win at the same time)
            if(blueWon) {
                DisplayWinnerClientRpc("Blue wins!");
                yield return new WaitForSeconds(endGameWaitTime);
                EndGameServerRpc();
            }
            else if (redWon) { 
                DisplayWinnerClientRpc("Red wins!");
                yield return new WaitForSeconds(endGameWaitTime);
                EndGameServerRpc();
            }

            //if(destroyed != null) destroyed.gameObject.GetComponent<NetworkSpawnable>().Kill();
        } else {
            int redDestroyed = 0;
            int blueDestroyed = 0;
            for(int i = 0; i < 2; i++) {
                if(objectivesDestroyed[i]) redDestroyed++;
            }
            for(int i = 2; i < 4; i++) {
                if(objectivesDestroyed[i]) blueDestroyed++;
            }
            blueWon = redDestroyed > blueDestroyed || firstDestroyed == 0 || firstDestroyed == 1;
            redWon = blueDestroyed > redDestroyed || firstDestroyed == 2 || firstDestroyed == 3;
            if(blueWon) { 
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

    [ServerRpc(RequireOwnership = false)]
    private void EndGameServerRpc() {
        //DespawnOnServerRpc();
        DespawnAll();

        NetworkManagerUI.Instance.DeleteLobby();
        TransitionSceneClientRpc();
        //NetworkManagerUI.Instance.ResetLobbyData();
        //ReopenLobbyClientRpc();
    }

    [ClientRpc]
    private void TransitionSceneClientRpc() {
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
        NetworkManager.Singleton.SceneManager.LoadScene("OfflineTester", LoadSceneMode.Single);
    }
}
