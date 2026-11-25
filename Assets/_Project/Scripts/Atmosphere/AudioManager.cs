using UnityEngine;
using DeadlineRush.Core;

namespace DeadlineRush.Atmosphere
{
    /// <summary>
    /// M11 — phase BGM crossfade, dorm ambience, stress whisper/heartbeat, SFX.
    /// Reads/writes volumes through MemoryData (via GameManager) — never RunState.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AtmosphereConfig config;

        [Header("BGM (index 0..3 = Phase 1..4)")]
        [SerializeField] AudioClip[] phaseMusic = new AudioClip[4];

        [Header("Ambience")]
        [SerializeField] AudioClip dormAmbience;
        [SerializeField] AudioClip whisperLoop;
        [SerializeField] AudioClip heartbeatLoop;

        [Header("SFX")]
        [SerializeField] AudioClip sfxKnock;
        [SerializeField] AudioClip sfxPhone;
        [SerializeField] AudioClip sfxComputer;
        [SerializeField] AudioClip sfxUiClick;
        [SerializeField] AudioClip sfxGlitch;
        [SerializeField] AudioClip sfxWhisperOneShot;

        AudioSource _bgmA;
        AudioSource _bgmB;
        AudioSource _ambience;
        AudioSource _whisper;
        AudioSource _heartbeat;
        AudioSource _sfx;

        bool _aIsPrimary = true;
        float _crossfade;
        float _crossfadeDuration = 1.6f;
        bool _crossfading;
        WorldPhase _currentPhase = WorldPhase.Phase1_18_20;
        float _musicVol = 0.8f;
        float _sfxVol = 0.8f;
        bool _volumeSettingsDirty;
        float _saveSettingsAt;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmA = CreateSource("BGM_A", loop: true);
            _bgmB = CreateSource("BGM_B", loop: true);
            _ambience = CreateSource("Ambience", loop: true);
            _whisper = CreateSource("Whisper", loop: true);
            _heartbeat = CreateSource("Heartbeat", loop: true);
            _sfx = CreateSource("SFX", loop: false);

            PullVolumes();
        }

        void OnEnable()
        {
            EventBus.OnTimeChanged += HandleTimeChanged;
            EventBus.OnAreaChanged += HandleAreaChanged;
            EventBus.OnAttemptOpened += HandleAttemptOpened;
            EventBus.OnGameEventTriggered += HandleGameEvent;
            EventBus.OnGameEnded += HandleGameEnded;
        }

        void OnDisable()
        {
            EventBus.OnTimeChanged -= HandleTimeChanged;
            EventBus.OnAreaChanged -= HandleAreaChanged;
            EventBus.OnAttemptOpened -= HandleAttemptOpened;
            EventBus.OnGameEventTriggered -= HandleGameEvent;
            EventBus.OnGameEnded -= HandleGameEnded;
        }

        void Start()
        {
            PlayClip(_ambience, dormAmbience, _sfxVol * 0.35f);
            var phase = GameManager.Instance != null && GameManager.Instance.CurrentRun != null
                ? GameConfig.GetPhase(GameManager.Instance.CurrentRun.minute)
                : WorldPhase.Phase1_18_20;
            CrossfadeToPhase(phase, 0.05f);
        }

        void Update()
        {
            PullVolumes();
            TickCrossfade();
            ApplyBusVolumes();
            if (_volumeSettingsDirty && Time.unscaledTime >= _saveSettingsAt)
                SaveVolumes();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveVolumes();
        }

        void OnApplicationQuit()
        {
            SaveVolumes();
        }

        AudioSource CreateSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
            return src;
        }

        void PullVolumes()
        {
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            if (mem == null) return;
            _musicVol = Mathf.Clamp01(mem.musicVolume);
            _sfxVol = Mathf.Clamp01(mem.sfxVolume);
        }

        /// <summary>Public API for Settings sliders (Agent B can also call this).</summary>
        public void SetMusicVolume(float linear)
        {
            _musicVol = Mathf.Clamp01(linear);
            if (GameManager.Instance != null && GameManager.Instance.Memory != null)
            {
                GameManager.Instance.Memory.musicVolume = _musicVol;
                MarkVolumeSettingsDirty();
            }
        }

        public void SetSfxVolume(float linear)
        {
            _sfxVol = Mathf.Clamp01(linear);
            if (GameManager.Instance != null && GameManager.Instance.Memory != null)
            {
                GameManager.Instance.Memory.sfxVolume = _sfxVol;
                MarkVolumeSettingsDirty();
            }
        }

        void MarkVolumeSettingsDirty()
        {
            _volumeSettingsDirty = true;
            _saveSettingsAt = Time.unscaledTime + 0.5f;
        }

        public void SaveVolumes()
        {
            if (GameManager.Instance != null && GameManager.Instance.Memory != null)
            {
                SaveService.Save(GameManager.Instance.Memory);
                _volumeSettingsDirty = false;
            }
        }

        void HandleTimeChanged(int minute, WorldPhase phase)
        {
            if (phase != _currentPhase || _bgmA.clip == null && _bgmB.clip == null)
                CrossfadeToPhase(phase);
        }

        void HandleGameEvent(string eventId) => PlayEventSting(eventId);

        public void CrossfadeToPhase(WorldPhase phase, float durationOverride = -1f)
        {
            _currentPhase = phase;
            int idx = Mathf.Clamp((int)phase - 1, 0, 3);
            AudioClip clip = (phaseMusic != null && idx < phaseMusic.Length) ? phaseMusic[idx] : null;
            if (clip == null) return;

            var incoming = _aIsPrimary ? _bgmB : _bgmA;
            var outgoing = _aIsPrimary ? _bgmA : _bgmB;
            if (incoming.clip == clip && incoming.isPlaying) return;

            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.Play();

            _crossfade = 0f;
            _crossfadeDuration = durationOverride > 0f
                ? durationOverride
                : (config != null ? config.musicCrossfadeSeconds : 1.6f);
            _crossfading = true;
            _aIsPrimary = !_aIsPrimary;

            if (outgoing.clip == null)
            {
                incoming.volume = _musicVol;
                _crossfading = false;
            }
        }

        void TickCrossfade()
        {
            if (!_crossfading) return;
            _crossfade += Time.unscaledDeltaTime / Mathf.Max(0.05f, _crossfadeDuration);
            float t = Mathf.Clamp01(_crossfade);
            var incoming = _aIsPrimary ? _bgmA : _bgmB;
            var outgoing = _aIsPrimary ? _bgmB : _bgmA;
            incoming.volume = _musicVol * t;
            outgoing.volume = _musicVol * (1f - t);
            if (t >= 1f)
            {
                outgoing.Stop();
                outgoing.clip = null;
                _crossfading = false;
            }
        }

        void ApplyBusVolumes()
        {
            if (!_crossfading)
            {
                var primary = _aIsPrimary ? _bgmA : _bgmB;
                primary.volume = _musicVol;
            }

            if (_ambience != null) _ambience.volume = _sfxVol * 0.35f;
            if (_whisper != null && _whisper.isPlaying) _whisper.volume = _sfxVol * 0.22f;
            if (_heartbeat != null && _heartbeat.isPlaying) _heartbeat.volume = _sfxVol * 0.30f;
        }

        public void SetStressLayers(bool whisper, bool heartbeat)
        {
            SetLoop(_whisper, whisperLoop, whisper, _sfxVol * 0.22f);
            SetLoop(_heartbeat, heartbeatLoop, heartbeat, _sfxVol * 0.30f);
        }

        public void StopStressLayers()
        {
            SetLoop(_whisper, whisperLoop, false, 0f);
            SetLoop(_heartbeat, heartbeatLoop, false, 0f);
        }

        static void SetLoop(AudioSource src, AudioClip clip, bool on, float volume)
        {
            if (src == null) return;
            if (on && clip != null)
            {
                if (src.clip != clip) src.clip = clip;
                src.volume = volume;
                if (!src.isPlaying) src.Play();
            }
            else if (src.isPlaying)
            {
                src.Stop();
            }
        }

        static void PlayClip(AudioSource src, AudioClip clip, float volume)
        {
            if (src == null || clip == null) return;
            src.clip = clip;
            src.volume = volume;
            src.loop = true;
            src.Play();
        }

        public void PlaySfx(AudioClip clip, float scale = 1f)
        {
            if (_sfx == null || clip == null) return;
            _sfx.PlayOneShot(clip, Mathf.Clamp01(_sfxVol * scale));
        }

        public void PlayUiClick() => PlaySfx(sfxUiClick);

        public void PlayEventSting(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            switch (eventId)
            {
                case "E01":
                    PlaySfx(sfxGlitch);
                    break;
                case "E02":
                    PlaySfx(sfxPhone);
                    break;
                case "E04":
                    PlaySfx(sfxKnock);
                    break;
                case "E05":
                    PlaySfx(sfxWhisperOneShot != null ? sfxWhisperOneShot : whisperLoop, 0.8f);
                    break;
                case "E06":
                    PlaySfx(sfxComputer);
                    break;
            }
        }

        void HandleAreaChanged(AreaId area)
        {
            if (area == AreaId.SecretArea)
                PlaySfx(sfxGlitch, 0.7f);
        }

        void HandleAttemptOpened(int _)
        {
            PlaySfx(sfxComputer, 0.6f);
        }

        void HandleGameEnded(EndingType ending)
        {
            StopStressLayers();
            if (_ambience != null) _ambience.Stop();
            if (ending == EndingType.True || ending == EndingType.Secret)
                PlaySfx(sfxGlitch, 0.5f);
        }
    }
}
