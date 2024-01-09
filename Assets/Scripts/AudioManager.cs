using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource source;

    private void Start() {
        source = GetComponent<AudioSource>();
    }
    public void PlaySoundEffect(AudioClip clip, float volume) {
        source.PlayOneShot(clip, volume);
    }
}
