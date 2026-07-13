using UnityEngine;

/// <summary>
/// Minimal audio manager stub for the tutorial voice-over system.
/// Wire a real AudioSource here if you want VO playback; otherwise it silently no-ops.
/// </summary>
public class TGM_AudioManager : MonoBehaviour
{
    public static TGM_AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void PlayVoiceLine(string key)
    {
        // No-op stub — assign AudioClips and implement playback here when ready.
        Debug.Log($"[TGM_AudioManager] PlayVoiceLine: {key}");
    }

    public void StopVoice()
    {
        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }
}
