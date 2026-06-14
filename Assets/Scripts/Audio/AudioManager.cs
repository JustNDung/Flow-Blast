using UI;
using UnityEngine;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (Application.isPlaying)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Sound Effects")]
        [SerializeField] private AudioClip _absorbSound;
        [SerializeField] private AudioClip _winSound;
        [SerializeField] private AudioClip _loseSound;
        [SerializeField] private AudioClip _boxCompleteSound;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private bool _soundEnabled = true;
        private bool _musicEnabled = true;
        private bool _vibrateEnabled = true;

        public bool SoundEnabled => _soundEnabled;
        public bool MusicEnabled => _musicEnabled;
        public bool VibrateEnabled => _vibrateEnabled;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadSettings();
            GenerateProceduralSounds();
            SubscribeToMessages();
        }

        private void OnDestroy()
        {
            UnsubscribeFromMessages();
        }

        private void SubscribeToMessages()
        {
            MessageDispatcher.MessageDispatcher.Subscribe<GameStateMessage>(OnGameStateChanged);
        }

        private void UnsubscribeFromMessages()
        {
            MessageDispatcher.MessageDispatcher.Unsubscribe<GameStateMessage>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateMessage message)
        {
            if (message.Result == GameResult.Win)
                PlayWinSound();
            else
                PlayLoseSound();
        }

        /// <summary>
        /// Generate simple procedural sounds if no AudioClips are assigned.
        /// </summary>
        private void GenerateProceduralSounds()
        {
            if (_absorbSound == null)
                _absorbSound = GenerateToneClip(440f, 0.08f, 0.3f); // short A4 note
            if (_winSound == null)
                _winSound = GenerateWinClip();
            if (_loseSound == null)
                _loseSound = GenerateToneClip(220f, 0.5f, 0.5f); // low A3 note
            if (_boxCompleteSound == null)
                _boxCompleteSound = GenerateToneClip(660f, 0.15f, 0.4f); // higher E5 note
        }

        private static AudioClip GenerateToneClip(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            AudioClip clip = AudioClip.Create("ProceduralTone", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)sampleCount); // fade out
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateWinClip()
        {
            int sampleRate = 44100;
            float duration = 0.6f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            AudioClip clip = AudioClip.Create("ProceduralWin", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];
            float[] freqs = { 523f, 659f, 784f, 1047f }; // C5, E5, G5, C6 ascending
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float noteIndex = t / duration * freqs.Length;
                int freqIndex = Mathf.Clamp(Mathf.FloorToInt(noteIndex), 0, freqs.Length - 1);
                float envelope = 1f - (i / (float)sampleCount) * 0.5f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freqs[freqIndex] * t) * 0.4f * envelope;
            }
            clip.SetData(samples, 0);
            return clip;
        }

        private void InitializeAudioSources()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        private void LoadSettings()
        {
            // Only load if previously saved, otherwise default to enabled
            _soundEnabled = PlayerPrefs.HasKey("SoundEnabled") ? PlayerPrefs.GetInt("SoundEnabled") == 1 : true;
            _musicEnabled = PlayerPrefs.HasKey("MusicEnabled") ? PlayerPrefs.GetInt("MusicEnabled") == 1 : true;
            _vibrateEnabled = PlayerPrefs.HasKey("VibrateEnabled") ? PlayerPrefs.GetInt("VibrateEnabled") == 1 : true;

            ApplySettings();
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("SoundEnabled", _soundEnabled ? 1 : 0);
            PlayerPrefs.SetInt("MusicEnabled", _musicEnabled ? 1 : 0);
            PlayerPrefs.SetInt("VibrateEnabled", _vibrateEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplySettings()
        {
            if (_musicSource != null)
                _musicSource.mute = !_musicEnabled;
            if (_sfxSource != null)
                _sfxSource.mute = !_soundEnabled;
        }

        /// <summary>
        /// Reset all settings to enabled. Useful for debugging or first launch.
        /// </summary>
        public void ResetToDefaults()
        {
            _soundEnabled = true;
            _musicEnabled = true;
            _vibrateEnabled = true;
            ApplySettings();
            SaveSettings();
        }

        public void SetSound(bool enabled)
        {
            _soundEnabled = enabled;
            ApplySettings();
            SaveSettings();

            MessageDispatcher.MessageDispatcher.Publish(new SoundSettingChangedMessage(enabled));
        }

        public void SetMusic(bool enabled)
        {
            _musicEnabled = enabled;
            ApplySettings();
            SaveSettings();

            MessageDispatcher.MessageDispatcher.Publish(new MusicSettingChangedMessage(enabled));
        }

        public void SetVibrate(bool enabled)
        {
            _vibrateEnabled = enabled;
            SaveSettings();

            MessageDispatcher.MessageDispatcher.Publish(new VibrateSettingChangedMessage(enabled));
        }

        public void PlayMusic(AudioClip clip, float volume = 1f)
        {
            if (!_musicEnabled || clip == null) return;

            _musicSource.clip = clip;
            _musicSource.volume = volume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (!_soundEnabled || clip == null) return;

            _sfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Play the sound played when items are being absorbed.
        /// </summary>
        public void PlayAbsorbSound()
        {
            PlaySound(_absorbSound, 0.5f);
        }

        /// <summary>
        /// Play the box completion sound.
        /// </summary>
        public void PlayBoxCompleteSound()
        {
            PlaySound(_boxCompleteSound, 0.6f);
        }

        /// <summary>
        /// Play victory fanfare.
        /// </summary>
        public void PlayWinSound()
        {
            PlaySound(_winSound, 0.7f);
        }

        /// <summary>
        /// Play defeat sound.
        /// </summary>
        public void PlayLoseSound()
        {
            PlaySound(_loseSound, 0.7f);
        }

        /// <summary>
        /// Trigger haptic feedback during absorption.
        /// </summary>
        public void VibrateLight()
        {
            if (!_vibrateEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        public void Vibrate()
        {
            if (!_vibrateEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
