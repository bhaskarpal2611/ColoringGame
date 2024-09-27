using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private bool _isMute = false;
    [SerializeField] private AudioInfo[] _audioClips;

    private AudioSource _soundEffect;
    private AudioSource _soundMusic;
    private AudioSource _paintSFX;

    private void Awake()
    {
        _soundMusic = GetComponents<AudioSource>()[0];
        _soundEffect = GetComponents<AudioSource>()[1];
        _paintSFX = GetComponents<AudioSource>()[2];
        _paintSFX.clip = GetSoundClip(Sounds.PaintBrush);

        PlayBackgroundMusic();
    }

    public void PlayClickSound() => PlaySound(Sounds.ButtonClick);

    public void PlayBackgroundMusic()
    {
        var backgroundMusic = GetSoundClip(Sounds.BG_MUSIC);
        if (backgroundMusic != null)
        {
            _soundMusic.clip = backgroundMusic;
            _soundMusic.Play();
        }
    }

    public void PlaySound(Sounds sound)
    {
        if (_isMute) return;
        AudioClip clip = GetSoundClip(sound);
        if (clip != null)
        {
            _soundEffect.PlayOneShot(clip);
        }
        else
        {
            Debug.Log("Audio Not Assigned");
        }
    }
    public void ToggleMute()
    {
        _isMute = !_isMute;
        SetMusicStatus();
    }

    public void PlayPaintingSound()
    {
        _paintSFX.enabled = true;
    }
    public void StopPaintingSound()
    {
        _paintSFX.enabled = false;
    }

    private AudioClip GetSoundClip(Sounds sound)
    {
        AudioInfo item = Array.Find(_audioClips, i => i.soundtype == sound);
        if (item != null)
        {
            return item.soundclip;
        }
        else
        {
            return null;
        }
    }
    private void SetMusicStatus()
    {
        if (_isMute)
        {
            _soundMusic.volume = 0f;
            return;
        }
        else
        {
            _soundMusic.volume = 0.5f;
        }
    }
}

[Serializable]
public class AudioInfo
{
    public Sounds soundtype;
    public Language Language;
    public AudioClip soundclip;
}

public enum Sounds
{
    None,
    ButtonClick,
    BG_MUSIC,
    Interactive,
    PaintBrush,
}
public enum Language
{
    None, English, Hindi, Marathi,
}