using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/*
 * Script that is attached to AudioManager Gameobject.
 * Handles all audio, background music, and sound effects.
 * Also contains code for playing audio over the network (server/client architecture)
 */
public class AudioManager : NetworkBehaviour
{
    // Singleton instance of the AudioManager, accessible globally
    public static AudioManager Instance { get; private set; }
    
    // References to the main audio source and background music (BGM) audio source
    [SerializeField] private AudioSource source, bgmSource;

    // Audio listener reference, and volume multipliers for global volume, sound effects (SFX), and BGM
    public AudioListener al;
    public float volumeMult, sfxMult, bgmMult;

    // Array of global audio clips that can be accessed and played from any client/server
    [SerializeField] AudioClip[] globalClips;

    // Array of standardized sound effects for use in the game
    [SerializeField] AudioClip[] standardizedSFX;

    // Unity's Awake method to initialize the singleton instance and prevent duplicate managers
    private void Awake() {
        if (Instance != null)
        {
            // If an instance already exists, destroy the new one to enforce the singleton pattern
            Destroy(gameObject);
            return;
        }

        // Set the static instance to this object and ensure it persists across scene loads
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Unity's Update method, called once per frame, to adjust the volume of audio sources
    private void Update() {
        // Adjust the volume of the sound effects and BGM based on the respective multipliers
        source.volume = volumeMult * sfxMult;
        bgmSource.volume = volumeMult * bgmMult;
    }

    // Stops the currently playing sound effect
    public void StopSFX() {
        source.Stop();
    }

    // Plays a sound effect from the standardizedSFX array using the specified clip ID and volume
    public void PlaySoundEffect(int clipId, float volume) {
        source.PlayOneShot(standardizedSFX[clipId], volume);
    }

    // Plays a given sound effect AudioClip with the specified volume
    public void PlaySoundEffect(AudioClip clip, float volume) {
        source.PlayOneShot(clip, volume);
    }

    // Plays the specified background music clip with a default volume of 1
    public void PlayBGM(AudioClip clip) {
        PlayBGM(clip, 1f);
    }

    // Plays the specified background music clip with the given volume
    public void PlayBGM(AudioClip clip, float vol) {
        bgmSource.clip = clip;
        bgmSource.volume = vol * volumeMult;
        bgmSource.Play();
    }

    // Returns true if background music is currently playing, otherwise false
    public bool BGMPlaying() {
        return bgmSource.isPlaying;
    }

    // Stops the currently playing background music
    public void StopBGM() {
        bgmSource.Stop();
    }
    
    // Server-side method to play a global sound effect at a specific location for all clients
    // The sound effect is identified by its ID, with position and volume specified
    [ServerRpc(RequireOwnership = false)]
    public void PlayGlobalSoundEffectServerRpc(int id, Vector3 position, float volume) {
        // Ensure the ID is within the valid range of globalClips array
        if(id >= 0 && id < globalClips.Length) {
            // Invoke the client-side method to play the sound effect at the specified location for all clients
            PlaySoundEffectAtLocationClientRpc(id, position, volume);
        }
    }

    // Server-side method to play a sound effect at a specific location on the server
    [ServerRpc(RequireOwnership = false)]
    private void PlaySoundEffectAtLocationServerRpc(int id, Vector3 position, float volume) {
        // Play the specified clip at the given position and volume on the server
        AudioSource.PlayClipAtPoint(globalClips[id], position, volume);
    }

    // Client-side method to play a sound effect at a specific location for all clients
    [ClientRpc]
    private void PlaySoundEffectAtLocationClientRpc(int id, Vector3 position, float volume) {
        // Play the specified clip at the given position and volume, adjusted by the multipliers, on each client
        AudioSource.PlayClipAtPoint(globalClips[id], position, volume * volumeMult * sfxMult);
    }
}
