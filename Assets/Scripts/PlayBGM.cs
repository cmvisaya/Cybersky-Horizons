using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayBGM : MonoBehaviour
{
    [SerializeField] private AudioClip track;
    [SerializeField] private float volume = 1f;

    // Start is called before the first frame update
    private void Start()
    {
        //if(!AudioManager.Instance.BGMPlaying())
        AudioManager.Instance.PlayBGM(track, volume);
    }
}
