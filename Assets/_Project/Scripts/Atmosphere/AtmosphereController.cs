using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using DeadlineRush.Core;

namespace DeadlineRush.Atmosphere
{
    /// <summary>
    /// M11 — two independent axes:
    /// 1) WorldPhase drives Global Light 2D (color + intensity, flicker in Phase 3).
    /// 2) Stress drives URP Volume (Vignette / Film Grain / Chromatic Aberration).
    /// Listens only to EventBus. Never writes RunState.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AtmosphereController : MonoBehaviour
    {
        public static AtmosphereController Instance { get; private set; }

        [SerializeField] AtmosphereConfig config;
        [SerializeField] Light2D globalLight2D;
        [SerializeField] Light fallbackLight;
        [SerializeField] Volume globalVolume;
        [SerializeField] Camera targetCamera;
        [Tooltip("Optional full-screen dimmer if the scene still uses a 3D URP renderer (Light2D needs a 2D Renderer).")]
        [SerializeField] SpriteRenderer overlayDimmer;

        WorldPhase _phase = WorldPhase.Phase1_18_20;
        int _stress = 10;
        float _baseIntensity = 1f;
        Color _baseColor = Color.white;
        float _brightness = 1f;
        bool _isRefreshingBindings;

        Vignette _vignette;
        FilmGrain _grain;
        ChromaticAberration _chroma;
        ColorAdjustments _colorAdj;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            RefreshSceneBindings();
            EnsureVolumeOverrides();
            CacheLight();
        }

        void OnEnable()
        {
            EventBus.OnTimeChanged += HandleTimeChanged;
            EventBus.OnStatsChanged += HandleStatsChanged;
            EventBus.OnGameEventTriggered += HandleGameEvent;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            EventBus.OnTimeChanged -= HandleTimeChanged;
            EventBus.OnStatsChanged -= HandleStatsChanged;
            EventBus.OnGameEventTriggered -= HandleGameEvent;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void Start()
        {
            PullBrightness();
            if (GameManager.Instance != null && GameManager.Instance.CurrentRun != null)
            {
                _phase = GameConfig.GetPhase(GameManager.Instance.CurrentRun.minute);
                _stress = GameManager.Instance.CurrentRun.stress;
            }
            ApplyPhase(_phase, instant: true);
            ApplyStress(_stress);
        }

        void Update()
        {
            if (PullBrightness())
            {
                ApplyPhase(_phase, instant: true);
                ApplyStress(_stress);
            }
            ApplyFlickerIfNeeded();
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshSceneBindings();
            ApplyPhase(_phase, instant: true);
            ApplyStress(_stress);
        }

        void RefreshSceneBindings()
        {
            if (_isRefreshingBindings) return;
            _isRefreshingBindings = true;

            var sceneCamera = Camera.main;
            if (sceneCamera != null)
                targetCamera = sceneCamera;

            EnsureVolumeOverrides();
            CacheLight();
            _isRefreshingBindings = false;
        }

        bool PullBrightness()
        {
            var mem = GameManager.Instance != null ? GameManager.Instance.Memory : null;
            float brightness = mem != null ? Mathf.Clamp(mem.brightness, 0.15f, 1.4f) : 1f;
            if (Mathf.Approximately(_brightness, brightness)) return false;
            _brightness = brightness;
            return true;
        }

        void CacheLight()
        {
            if (globalLight2D == null)
                globalLight2D = FindAnyObjectByType<Light2D>();
            if (fallbackLight == null)
                fallbackLight = FindAnyObjectByType<Light>();
        }

        void EnsureVolumeOverrides()
        {
            if (globalVolume == null)
                globalVolume = GetComponent<Volume>();
            if (globalVolume == null)
                globalVolume = gameObject.AddComponent<Volume>();

            globalVolume.isGlobal = true;
            globalVolume.priority = 10f;
            if (globalVolume.profile == null)
                globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var profile = globalVolume.profile;
            _vignette = GetOrAdd<Vignette>(profile);
            _grain = GetOrAdd<FilmGrain>(profile);
            _chroma = GetOrAdd<ChromaticAberration>(profile);
            _colorAdj = GetOrAdd<ColorAdjustments>(profile);

            if (targetCamera != null &&
                targetCamera.TryGetComponent<UniversalAdditionalCameraData>(out var camData))
            {
                camData.renderPostProcessing = true;
            }
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T comp))
                comp = profile.Add<T>(true);
            return comp;
        }

        void HandleTimeChanged(int minute, WorldPhase phase)
        {
            _phase = phase;
            ApplyPhase(phase, instant: false);
        }

        void HandleStatsChanged(int energy, int stress, int progress)
        {
            _stress = stress;
            ApplyStress(stress);
        }

        void HandleGameEvent(string eventId)
        {
            if (eventId == "E01" || eventId == "E06")
            {
                // brief extra grain spike on world glitches
                if (_grain != null)
                {
                    _grain.intensity.overrideState = true;
                    _grain.intensity.value = Mathf.Min(1f, _grain.intensity.value + 0.2f);
                }
            }
        }

        void ApplyPhase(WorldPhase phase, bool instant)
        {
            if (config == null) return;
            var look = config.GetPhaseLook(phase);
            if (look == null) return;

            _baseColor = look.lightColor;
            _baseIntensity = look.intensity;
            ApplyLight(_baseColor, _baseIntensity * _brightness);

            if (targetCamera != null)
                targetCamera.backgroundColor = look.cameraBackground;

            if (AudioManager.Instance != null)
                AudioManager.Instance.CrossfadeToPhase(phase, instant ? 0.05f : -1f);
        }

        void ApplyFlickerIfNeeded()
        {
            if (config == null) return;
            float intensity = _baseIntensity * _brightness;

            if (_phase == WorldPhase.Phase3_22_23)
            {
                float n = Mathf.PerlinNoise(Time.unscaledTime * config.flickerSpeed, 0.37f);
                intensity *= 1f + (n - 0.5f) * 2f * config.flickerAmplitude;
            }
            else if (_phase == WorldPhase.Phase4_23_24)
            {
                float n = Mathf.PerlinNoise(Time.unscaledTime * 2.2f, 1.1f);
                intensity *= 0.92f + n * 0.12f;
            }

            ApplyLight(_baseColor, intensity);
        }

        void ApplyLight(Color color, float intensity)
        {
            if (globalLight2D != null)
            {
                globalLight2D.color = color;
                globalLight2D.intensity = Mathf.Max(0.02f, intensity);
            }

            if (fallbackLight != null)
            {
                fallbackLight.color = color;
                fallbackLight.intensity = Mathf.Max(0.02f, intensity);
            }

            if (overlayDimmer != null)
            {
                float darkness = Mathf.Clamp01(1f - intensity * 0.65f);
                var c = color;
                c.a = darkness * 0.55f;
                overlayDimmer.color = c;
            }
        }

        void ApplyStress(int stress)
        {
            if (config == null) return;
            var look = config.GetStressLook(stress);
            if (look == null) return;

            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _vignette.smoothness.overrideState = true;
                _vignette.color.overrideState = true;
                _vignette.rounded.overrideState = true;
                _vignette.intensity.value = look.vignette;
                _vignette.smoothness.value = 0.35f;
                _vignette.color.value = look.vignetteColor;
                _vignette.rounded.value = true;
            }

            if (_grain != null)
            {
                _grain.type.overrideState = true;
                _grain.intensity.overrideState = true;
                _grain.type.value = stress >= 81 ? FilmGrainLookup.Large01 : FilmGrainLookup.Medium1;
                _grain.intensity.value = look.filmGrain;
            }

            if (_chroma != null)
            {
                _chroma.intensity.overrideState = true;
                _chroma.intensity.value = look.chromaticAberration;
            }

            if (_colorAdj != null)
            {
                _colorAdj.saturation.overrideState = true;
                _colorAdj.postExposure.overrideState = true;
                _colorAdj.contrast.overrideState = true;
                _colorAdj.saturation.value = look.saturation;
                _colorAdj.postExposure.value = Mathf.Lerp(-0.15f, 0.15f, (_brightness - 0.15f) / 1.25f);
                _colorAdj.contrast.value = stress >= 81 ? 12f : (stress >= 61 ? 6f : 0f);
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.SetStressLayers(look.whisperLayer, look.heartbeatLayer);
        }
    }
}
