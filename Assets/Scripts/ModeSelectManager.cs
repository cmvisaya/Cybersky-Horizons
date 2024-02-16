using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModeSelectManager : MonoBehaviour
{
    [SerializeField] private Button onlineBtn, localBtn, backBtn;
    [SerializeField] private int onlineSceneCode, localSceneCode;

    private void Awake() {
        onlineBtn.onClick.AddListener(() => {
            GameManager.Instance.LoadScene(onlineSceneCode);
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });
        localBtn.onClick.AddListener(() => { 
            GameManager.Instance.LoadScene(localSceneCode);
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlaySoundEffect(0, 2f);
        });
        backBtn.onClick.AddListener(() => { 
            GameManager.Instance.LoadScene(0);
        });
    }
}
