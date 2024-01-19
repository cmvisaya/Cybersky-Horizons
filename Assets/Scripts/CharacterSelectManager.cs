using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectManager : MonoBehaviour
{
    [SerializeField] private Button lockInBtn;
    [SerializeField] private Button[] charButtons;
    [SerializeField] private string[] charNames;
    [SerializeField] private GameObject[] charModelPrefabs;
    [SerializeField] private int currCharCode, nextSceneCode;
    public TextMeshProUGUI charName;
    public GameObject infoTooltip;

    private void Awake() {
        lockInBtn.onClick.AddListener(() => {
            LockIn();
        });
        charButtons[0].onClick.AddListener(() => {
            SelectCharacter(0);
        });
        charButtons[1].onClick.AddListener(() => {
            SelectCharacter(1);
        });
        charButtons[2].onClick.AddListener(() => {
            SelectCharacter(2);
        });
    }

    private void Start() {
        DeactivateAllPrefabs();
    }

    private void SelectCharacter(int id) {
        DeactivateAllPrefabs();
        charModelPrefabs[id].SetActive(true);
        charName.text = charNames[id];
        infoTooltip.SetActive(true);
        currCharCode = id;
    }

    private void DeactivateAllPrefabs() {
        foreach(GameObject charModel in charModelPrefabs) {
            charModel.SetActive(false);
        }
        charName.text = "";
        infoTooltip.SetActive(false);
    }

    private void LockIn() {
        GameManager.Instance.selectedCharacterCode = currCharCode;
        GameManager.Instance.LoadScene(nextSceneCode);
    }
}
