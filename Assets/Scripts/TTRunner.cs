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
    [SerializeField] private int maxHp;
    [SerializeField] private TextMeshProUGUI winText;
    public Transform redSpawn, blueSpawn;

    private void Awake() {
        if(winText.gameObject.activeSelf) {
            winText.text = "";
            winText.gameObject.SetActive(false);
        }
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
