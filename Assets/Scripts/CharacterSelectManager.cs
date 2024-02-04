using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectManager : MonoBehaviour
{
    [SerializeField] private Button lockInBtn, backBtn, infoBtn;
    [SerializeField] private Button[] charButtons;
    [SerializeField] private string[] charNames;
    [SerializeField] private GameObject[] charModelPrefabs;
    [SerializeField] private int currCharCode, nextSceneCode;
    public TextMeshProUGUI charName;
    [SerializeField] private Slider speed, damage, range, fireRate, magSize;
    [SerializeField] private TextMeshProUGUI className, infoName;
    public GameObject infoTooltip;

    private void Awake() {
        lockInBtn.onClick.AddListener(() => {
            LockIn();
        });
        backBtn.onClick.AddListener(() => {
            GameManager.Instance.LoadScene(0);
        });
        infoBtn.onClick.AddListener(() => {
            infoTooltip.SetActive(!infoTooltip.activeSelf);
            infoName.text = infoTooltip.activeSelf ? "Hide Stats" : "Show Stats";
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
        charButtons[3].onClick.AddListener(() => {
            SelectCharacter(3);
        });
        charButtons[4].onClick.AddListener(() => {
            SelectCharacter(4);
        });
        charButtons[5].onClick.AddListener(() => {
            SelectCharacter(5);
        });
        charButtons[6].onClick.AddListener(() => {
            SelectCharacter(6);
        });
        charButtons[7].onClick.AddListener(() => {
            SelectCharacter(7);
        });
        charButtons[8].onClick.AddListener(() => {
            SelectCharacter(8);
        });
        charButtons[9].onClick.AddListener(() => {
            SelectCharacter(9);
        });
    }

    private void Start() {
        DeactivateAllPrefabs();
    }

    private void SelectCharacter(int id) {
        AudioManager.Instance.PlaySoundEffect(1, 2f);
        DeactivateAllPrefabs();
        charModelPrefabs[id].SetActive(true);
        charName.text = charNames[id];
        //infoTooltip.SetActive(true);
        currCharCode = id;
        SetInfoStats(id);
    }

    private void SetInfoStats(int id) {
        CharSelectStats stats = charModelPrefabs[id].GetComponent<CharSelectStats>();
        className.text = "Class: " + stats.className;
        speed.value = stats.speed;
        damage.value = stats.damage;
        range.value = stats.range;
        fireRate.value = stats.fireRate;
        magSize.value = stats.magSize;
    }

    private void DeactivateAllPrefabs() {
        foreach(GameObject charModel in charModelPrefabs) {
            charModel.SetActive(false);
        }
        charName.text = "";
        //infoTooltip.SetActive(false);
    }

    private void LockIn() {
        AudioManager.Instance.PlaySoundEffect(0, 2f);
        GameManager.Instance.selectedCharacterCode = currCharCode;
        GameManager.Instance.LoadScene(nextSceneCode);
    }
}
