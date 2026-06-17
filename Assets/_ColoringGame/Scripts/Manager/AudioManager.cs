using System;
using Unity.VisualScripting;
using UnityEngine;

namespace ColorSwipeGame
{
    public class AudioManager : GenericSingleton<AudioManager>
    {
        [SerializeField] private bool _isMute = false;

        [Header("All Different Audio SO's")]
        [SerializeField] private AudioSO _englishAudio;
        [SerializeField] private AudioSO _hindiAudio;
        [SerializeField] private AudioSO _tamilAudio;
        [SerializeField] private AudioSO _frenchAudio;
        [SerializeField] private AudioSO _marathiAudio;
        [SerializeField] private AudioSO _bengaliAudio;
        [SerializeField] private AudioSO _malayalamAudio;
        [SerializeField] private AudioSO _punjabiAudio;
        [SerializeField] private AudioSO _bhojpuriAudio;
        [SerializeField] private AudioSO _gujaratiAudio;

        [Space]
        [SerializeField] private string _audioLocalization;

        [Header("SFX, BG etc.")]
        [SerializeField] private AudioClip _bgMusic;
        [SerializeField] private AudioClip _cameraSFX, _btnClick;
        [SerializeField] private AudioClip _colorSfx;


        private AudioSource _sfxSource;
        private AudioSource _bgSource;
        private AudioSource _paintSFX;

        private AudioSO _currentMainAudio;

        protected override void Awake()
        {
            base.Awake();
            SetLanguage();
            _bgSource = GetComponents<AudioSource>()[0];
            _sfxSource = GetComponents<AudioSource>()[1];
            _paintSFX = GetComponents<AudioSource>()[2];
            ChangeBrushSound_Paint();
            PlayBackgroundMusic();
        }

        private void SetLanguage()
        {
            _audioLocalization = PlayerPrefs.GetString("PlayschoolLanguageAudio");
            _currentMainAudio = _audioLocalization switch
            {
                "English" => _englishAudio,
                "Hindi" => _hindiAudio,
                "Tamil" => _tamilAudio,
                "French" => _frenchAudio,
                "Marathi" => _marathiAudio,
                "Bengali" => _bengaliAudio,
                "Malayalam" => _malayalamAudio,
                "Punjabi" => _punjabiAudio,
                "Bhojpuri" => _bhojpuriAudio,
                "Gujarati" => _gujaratiAudio,
                _ => _englishAudio,
            };

            if (_currentMainAudio == null) _currentMainAudio = _englishAudio;
        }

        public void ChangeBrushSound_Erase()
        {
            var clip = GetSoundClip(Sounds.Erase);

            if (clip != null)
                _paintSFX.clip = clip;
        }

        public void ChangeBrushSound_Paint()
        {
            var clip = GetSoundClip(Sounds.PaintBrush);

            if (clip != null)
            {
                _paintSFX.clip = clip;
            }
        }

        public void PlayClickSound()
        {
            if(_btnClick != null)
            {
                _sfxSource.PlayOneShot(_btnClick);
            }
        }

        public void PlayCameraButtonSound()
        {
            if(_cameraSFX != null)
            {
                _sfxSource.PlayOneShot(_cameraSFX);
            }
        }

        public void PlayIntroAudio()
        {
            if (RuntimeAudioLoader.Instance != null && AudioMapper.Instance != null)
                RuntimeAudioLoader.Instance.PlayRuntimeAudio(AudioMapper.Instance.GetRandomKeyFor(Sounds.GameIntro));
        }

        // Playing through Unity Events in Level Selection Manager's Level Load
        public void PlayGameStartAudio()
        {
            if (RuntimeAudioLoader.Instance != null && AudioMapper.Instance != null)
                RuntimeAudioLoader.Instance.PlayRuntimeAudio(AudioMapper.Instance.GetRandomKeyFor(Sounds.GameStart));
        }

        public void PlayCheeringAudio()
        {
            if (RuntimeAudioLoader.Instance != null && AudioMapper.Instance != null)
                RuntimeAudioLoader.Instance.PlayRuntimeAudio(AudioMapper.Instance.GetRandomKeyFor(Sounds.Cheer));
        }

        public void PlayBackgroundMusic()
        {
            //var backgroundMusic = GetSoundClip(Sounds.BG_MUSIC);
            //if (backgroundMusic != null)
            //{
            //    _soundMusic.clip = backgroundMusic;
            //    _soundMusic.Play();
            //}

            if(_bgMusic != null)
            {
                _bgSource.clip = _bgMusic;
                _bgSource.Play();
            }
        }

        private bool _playOnceFlag = false;
        public void PlayGameEndAudio()
        {
            if (!_playOnceFlag)
            {
                _playOnceFlag = true;
                //PlaySound(Sounds.GameEnd);
                if (RuntimeAudioLoader.Instance != null && AudioMapper.Instance != null)
                    RuntimeAudioLoader.Instance.PlayRuntimeAudio(AudioMapper.Instance.GetRandomKeyFor(Sounds.GameEnd));
            }
        }

        private void PlaySound(Sounds sound)
        {
            if (_isMute) return;
            AudioClip clip = GetSoundClip(sound);
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip);
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
            if (_colorSfx != null)
            {
                _paintSFX.clip = _colorSfx;
                _paintSFX.loop = true;
            }
            if (!_paintSFX.isPlaying)
                _paintSFX.Play();
        }

        // Called every frame while actively dragging (any movement)
        public void UpdatePaintingSound(float distance)
        {
            if (_colorSfx != null && _paintSFX.clip != _colorSfx)
            {
                _paintSFX.clip = _colorSfx;
                _paintSFX.loop = true;
            }

            if (!_paintSFX.isPlaying)
                _paintSFX.Play();
        }

        public void StopPaintingSound()
        {
            _paintSFX.pitch = 1f;
            _paintSFX.Stop();
        }

        private AudioClip GetSoundClip(Sounds sound)
        {
            if (_currentMainAudio == null) return null;

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
                _bgSource.volume = 0f;
                return;
            }
            else
            {
                _bgSource.volume = 0.5f;
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