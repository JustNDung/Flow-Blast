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
            _soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            _musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
            _vibrateEnabled = PlayerPrefs.GetInt("VibrateEnabled", 1) == 1;

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
            _musicSource.mute = !_musicEnabled;
            _sfxSource.mute = !_soundEnabled;
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

        public void Vibrate()
        {
            if (!_vibrateEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
