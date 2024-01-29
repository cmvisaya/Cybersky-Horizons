using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource source, bgmSource;

    public AudioListener al;
    public float volumeMult;

    [SerializeField] AudioClip[] globalClips;

    [SerializeField] AudioClip[] standardizedSFX;

    private void Awake() {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update() {
        source.volume = volumeMult;
        bgmSource.volume = volumeMult;
    }

    public void PlaySoundEffect(int clipId, float volume) {
        source.PlayOneShot(standardizedSFX[clipId], volume);
    }

    public void PlaySoundEffect(AudioClip clip, float volume) {
        source.PlayOneShot(clip, volume);
    }

    public void PlayBGM(AudioClip clip) {
        PlayBGM(clip, 1f);
    }

    public void PlayBGM(AudioClip clip, float vol) {
        bgmSource.clip = clip;
        bgmSource.volume = vol * volumeMult;
        bgmSource.Play();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayGlobalSoundEffectServerRpc(int id, Vector3 position, float volume) {
        //Debug.Log("SFX run at OwnerId: " + OwnerClientId);
        if(id >= 0 && id < globalClips.Length) {
            //PlaySoundEffectAtLocationServerRpc(id, position, volume);
            PlaySoundEffectAtLocationClientRpc(id, position, volume);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySoundEffectAtLocationServerRpc(int id, Vector3 position, float volume) {
        //Debug.Log("Play at Server");
        AudioSource.PlayClipAtPoint(globalClips[id], position, volume);
    }

    [ClientRpc]
    private void PlaySoundEffectAtLocationClientRpc(int id, Vector3 position, float volume) {
        //Debug.Log("Play at Client");
        AudioSource.PlayClipAtPoint(globalClips[id], position, volume * volumeMult);
    }
}
