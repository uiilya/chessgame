using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public List<AudioClip> deckDealClips;
    public List<AudioClip> pieceImpactClips;
    public List<AudioClip> pieceImpactClip;
    public List<AudioClip> pieceMoveClips;

    [Header("Settings")]
    public float sfxVolume = 1.0f;
    [Range(0f, 0.5f)]
    public float pitchVariance = 0.1f; // Randomizes pitch slightly for realism

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
    }

    public void PlayDeckDeal() => PlayRandom(deckDealClips);
    public void PlayPieceImpact() => PlayRandom(pieceImpactClips);
    public void PlayPieceMove() => PlayRandom(pieceMoveClips);
    public void PlayPieceImpactOne() => PlayRandom(pieceImpactClip);

    private void PlayRandom(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0) return;
        
        // Pick a random clip
        AudioClip clip = clips[Random.Range(0, clips.Count)];
        
        // Randomize pitch (e.g., 0.9 to 1.1) to make repeated sounds less annoying
        source.pitch = 1.0f + Random.Range(-pitchVariance, pitchVariance);
        source.PlayOneShot(clip, sfxVolume);
    }
}