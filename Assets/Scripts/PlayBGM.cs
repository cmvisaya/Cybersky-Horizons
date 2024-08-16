using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script that is attached to 'Jukebox' objects, or anything that is meant to change the background music
 * Audioclip played is set in the inspector
 */

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
