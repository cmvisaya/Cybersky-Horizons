using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    private AudioSource source;

    public AudioListener al;

    private void Start() {
        Instance = this;
        source = GetComponent<AudioSource>();
    }
    public void PlaySoundEffect(AudioClip clip, float volume) {
        source.PlayOneShot(clip, volume);
    }

    public void PlayGlobalSoundEffect(AudioClip clip, Vector3 position, float volume) {
        GameObject[] players = GameObject.FindGameObjectsWithTag("GAL");
        foreach (GameObject player in players) {
            player.GetComponent<GlobalAudioListener>().RunGlobalSound(clip, position, volume);
        }
    }

    public void PlaySoundEffectAtLocation(AudioClip clip, Vector3 position, float volume) {
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}
