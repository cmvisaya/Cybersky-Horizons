using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * UNUSED: Attempt at playing sounds globally across all clients. This is instead handled in the AudioManager script.
 */

public class GlobalAudioListener : MonoBehaviour
{
    public void RunGlobalSound(AudioClip clip, Vector3 position, float volume) {
        //AudioManager.Instance.PlaySoundEffectAtLocation(clip, position, volume);
    }
}
