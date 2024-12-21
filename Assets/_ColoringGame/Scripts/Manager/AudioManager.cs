using System;
using Unity.VisualScripting;
using UnityEngine;

namespace ColorSwipeGame
{
    public class AudioManager : GenericSingleton<AudioManager>
    {
        [SerializeField] private bool _isMute = false;
        [SerializeField] private AudioSO _englishAudio;
        [SerializeField] private AudioSO _hindiAudio;
        [SerializeField] private AudioSO _tamilAudio;
        [SerializeField] private string _audioLocalization;

        private AudioSource _soundEffect;
        private AudioSource _soundMusic;
        private AudioSource _paintSFX;

        private AudioSO _currentMainAudio;

        protected override void Awake()
        {
            base.Awake();
            SetLanguage();
            _soundMusic = GetComponents<AudioSource>()[0];
            _soundEffect = GetComponents<AudioSource>()[1];
            _paintSFX = GetComponents<AudioSource>()[2];
            ChangeBrushSound_Paint();
            PlayBackgroundMusic();
        }

        private void SetLanguage()
        {
            _audioLocalization = PlayerPrefs.GetString("PlayschoolLanguageAudio", _audioLocalization);
            _currentMainAudio = _audioLocalization switch
            {
                "English" => _englishAudio,
                "Hindi" => _hindiAudio,
                "Tamil" => _tamilAudio,
                _ => _englishAudio,
            };
        }

        public void ChangeBrushSound_Erase()
        {
            _paintSFX.clip = GetSoundClip(Sounds.Erase);    
        }

        public void ChangeBrushSound_Paint()
        {
            _paintSFX.clip = GetSoundClip(Sounds.PaintBrush);
        }

        public void PlayClickSound()
        {
            PlaySound(Sounds.ButtonClick);
        }

        public void PlayCameraButtonSound() => PlaySound(Sounds.CameraShutter);

        public void PlayIntroAudio() => PlaySound(Sounds.GameIntro);
        public void PlayGameStartAudio()
        {
            if (_soundEffect.isPlaying)
            {
                _soundEffect.Stop();
            }
            PlaySound(Sounds.GameStart);
        }

        public void PlayCheeringAudio()
        {
            if (!_soundEffect.isPlaying)
            {
                PlaySound(Sounds.Cheer);
            }
        }

        public void PlayBackgroundMusic()
        {
            var backgroundMusic = GetSoundClip(Sounds.BG_MUSIC);
            if (backgroundMusic != null)
            {
                _soundMusic.clip = backgroundMusic;
                _soundMusic.Play();
            }
        }

        private bool _playOnceFlag = false;
        public void PlayGameEndAudio()
        {
            if (!_playOnceFlag)
            {
                _playOnceFlag = true;
                PlaySound(Sounds.GameEnd);
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
            if (_paintSFX.isPlaying)
            {
                _paintSFX.Stop();
            }
            _paintSFX.Play();
        }
        public void StopPaintingSound()
        {
            //_paintSFX.enabled = false;
            _paintSFX.Stop();

        }

        private AudioClip GetSoundClip(Sounds sound)
        {
            AudioInfo item = Array.Find(_currentMainAudio.Audios, i => i.soundtype == sound);
            if (item != null)
            {
                int randomIndex = UnityEngine.Random.Range(0, item.soundclips.Length);
                return item.soundclips[randomIndex];
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
        public AudioClip[] soundclips;
    }

    public enum Sounds
    {
        None,
        ButtonClick,
        BG_MUSIC,
        Interactive,
        PaintBrush,
        CameraShutter,
        GameIntro,
        GameStart,
        Cheer,
        GameEnd,
        Erase,
    }
}