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
        AudioManager.Instance.PlayBGM(track, volume);
    }
}
