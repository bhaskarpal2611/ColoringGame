using UnityEngine;

namespace DrawingGame
{
    public class TGM_AudioManager : MonoBehaviour
    {
        public static TGM_AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public VoiceLineData winAudioData;
        [SerializeField] private AudioSource voiceSource;

        [Header("Placeholder")]
        [SerializeField] private AudioClip placeholderClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (voiceSource == null)
                voiceSource = gameObject.AddComponent<AudioSource>();
        }

        public AudioSource PlayVoiceLine(string voiceClipName)
        {
            if (voiceSource == null)
                voiceSource = gameObject.AddComponent<AudioSource>();

            AudioClip loadedClip = RuntimeAudioLoader.Instance != null ? RuntimeAudioLoader.Instance.GetClip(voiceClipName) : null;
            AudioClip clipToPlay = loadedClip ?? placeholderClip;

            if (clipToPlay == null)
            {
                Debug.LogError("TGM_AudioManager: No voice clip assigned and no placeholder available.");
                return voiceSource;
            }

            voiceSource.volume = 1.5f;
            voiceSource.loop = false; // never let a scene-serialized Loop flag make voice lines repeat
            voiceSource.clip = clipToPlay;
            voiceSource.Play();
            return voiceSource;
        }

        public bool IsPlaying =>
            voiceSource != null && voiceSource.isPlaying;

        public void StopVoice()
        {
            if (voiceSource != null && voiceSource.isPlaying)
                voiceSource.Stop();
        }
    }
}
