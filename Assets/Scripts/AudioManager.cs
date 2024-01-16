using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager Instance { get; private set; }
    private AudioSource source;

    public AudioListener al;

    [SerializeField] AudioClip[] globalClips;

    private void Start() {
        Instance = this;
        source = GetComponent<AudioSource>();
    }
    public void PlaySoundEffect(AudioClip clip, float volume) {
        source.PlayOneShot(clip, volume);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayGlobalSoundEffectServerRpc(int id, Vector3 position, float volume) {
        Debug.Log("SFX run at OwnerId: " + OwnerClientId);
        if(id >= 0 && id < globalClips.Length) {
            //PlaySoundEffectAtLocationServerRpc(id, position, volume);
            PlaySoundEffectAtLocationClientRpc(id, position, volume);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySoundEffectAtLocationServerRpc(int id, Vector3 position, float volume) {
        Debug.Log("Play at Server");
        AudioSource.PlayClipAtPoint(globalClips[id], position, volume);
    }

    [ClientRpc]
    private void PlaySoundEffectAtLocationClientRpc(int id, Vector3 position, float volume) {
        Debug.Log("Play at Client");
        AudioSource.PlayClipAtPoint(globalClips[id], position, volume);
    }
}
