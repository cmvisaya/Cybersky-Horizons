using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TTRunner : MonoBehaviour
{

    [SerializeField] private Slider redCentral, redOpposite, blueCentral, blueOpposite;
    [SerializeField] private Shootable rchp, rohp, bchp, bohp;
    private bool[] objectivesDestroyed = new bool[4];
    private bool timerActive = true;
    [SerializeField] private float secondsLeft, secondsPerGame;
    [SerializeField] private int maxHp;
    [SerializeField] private TextMeshProUGUI winText, timerText;
    public Transform redSpawn, blueSpawn;

    private void Awake() {
        if(winText.gameObject.activeSelf) {
            winText.text = "";
            winText.gameObject.SetActive(false);
        }
        secondsLeft = secondsPerGame + 1;
    }

    // Start is called before the first frame update
    void Start()
    {
        redCentral.maxValue = maxHp;
        redOpposite.maxValue = maxHp;
        blueCentral.maxValue = maxHp;
        blueOpposite.maxValue = maxHp;
        rchp.SetHealth(maxHp);
        rohp.SetHealth(maxHp);
        bchp.SetHealth(maxHp);
        bohp.SetHealth(maxHp);
    }

    private void Update() {
        redCentral.value = rchp.GetHealth();
        redOpposite.value = rohp.GetHealth();
        blueCentral.value = bchp.GetHealth();
        blueOpposite.value = bohp.GetHealth();

        HandleTimer();
    }

    private void HandleTimer() {
        if (timerActive && secondsLeft > 0 && timerText.transform.parent.gameObject.activeSelf) {
            secondsLeft -= Time.deltaTime;
            int seconds = (int) (secondsLeft % 60);
            if (seconds < 10) timerText.text = "" + (int) (secondsLeft / 60) + ":0" + (int) (secondsLeft % 60);
            else timerText.text = "" + (int) (secondsLeft / 60) + ":" + (int) (secondsLeft % 60);
        }

        if(secondsLeft < 1) {
            PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
            foreach(PlayerController player in players) {
                player.hasControl = false;
                player.gameObject.GetComponent<WeaponController>().hasControl = false;
            }
        }
    }

    public void KillObjective(int id) {
        Debug.Log("Killed objective: " + id);
        Shootable destroyed = null;
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
        bool blueWon = objectivesDestroyed[0] && objectivesDestroyed[1];
        bool redWon = objectivesDestroyed[2] && objectivesDestroyed[3];
        //Do Game Logic Here
        if(blueWon) { winText.gameObject.SetActive(true); winText.text = "Blue wins!"; }
        else if (redWon) { winText.gameObject.SetActive(true); winText.text = "Red wins!"; }
        //if(destroyed != null) destroyed.gameObject.GetComponent<NetworkSpawnable>().Kill();
    }
}
