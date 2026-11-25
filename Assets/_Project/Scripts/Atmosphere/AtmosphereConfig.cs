using UnityEngine;
using DeadlineRush.Core;

namespace DeadlineRush.Atmosphere
{
    /// <summary>
    /// Data-driven atmosphere: 4 WorldPhase lights + 4 stress visual bands.
    /// Create via Assets → Create → Deadline Rush → Atmosphere Config.
    /// </summary>
    [CreateAssetMenu(fileName = "AtmosphereConfig", menuName = "Deadline Rush/Atmosphere Config")]
    public class AtmosphereConfig : ScriptableObject
    {
        [System.Serializable]
        public class PhaseLook
        {
            public WorldPhase phase = WorldPhase.Phase1_18_20;
            [ColorUsage(false, true)] public Color lightColor = Color.white;
            [Range(0f, 2f)] public float intensity = 1f;
            public Color cameraBackground = new Color(0.08f, 0.07f, 0.12f);
        }

        [System.Serializable]
        public class StressLook
        {
            [Range(0, 100)] public int minStress;
            [Range(0, 100)] public int maxStress;
            [Range(0f, 1f)] public float vignette;
            [Range(0f, 1f)] public float filmGrain;
            [Range(0f, 1f)] public float chromaticAberration;
            public Color vignetteColor = Color.black;
            [Range(-100f, 100f)] public float saturation;
            public bool whisperLayer;
            public bool heartbeatLayer;
        }

        [Header("Phase axis (time)")]
        public PhaseLook[] phases =
        {
            new PhaseLook
            {
                phase = WorldPhase.Phase1_18_20,
                lightColor = new Color(1f, 0.90f, 0.72f),
                intensity = 1.05f,
                cameraBackground = new Color(0.12f, 0.10f, 0.14f)
            },
            new PhaseLook
            {
                phase = WorldPhase.Phase2_20_22,
                lightColor = new Color(0.82f, 0.80f, 0.92f),
                intensity = 0.72f,
                cameraBackground = new Color(0.08f, 0.08f, 0.12f)
            },
            new PhaseLook
            {
                phase = WorldPhase.Phase3_22_23,
                lightColor = new Color(0.55f, 0.62f, 0.85f),
                intensity = 0.38f,
                cameraBackground = new Color(0.04f, 0.05f, 0.09f)
            },
            new PhaseLook
            {
                phase = WorldPhase.Phase4_23_24,
                lightColor = new Color(0.40f, 0.48f, 0.70f),
                intensity = 0.16f,
                cameraBackground = new Color(0.02f, 0.02f, 0.05f)
            }
        };

        [Header("Stress axis (psychology)")]
        public StressLook[] stressBands =
        {
            new StressLook { minStress = 0, maxStress = 30, vignette = 0.08f, filmGrain = 0.02f, chromaticAberration = 0f, saturation = 8f },
            new StressLook { minStress = 31, maxStress = 60, vignette = 0.18f, filmGrain = 0.10f, chromaticAberration = 0.04f, saturation = 0f },
            new StressLook
            {
                minStress = 61, maxStress = 80, vignette = 0.38f, filmGrain = 0.28f, chromaticAberration = 0.16f,
                vignetteColor = new Color(0.12f, 0.02f, 0.08f), saturation = -18f, whisperLayer = true
            },
            new StressLook
            {
                minStress = 81, maxStress = 100, vignette = 0.58f, filmGrain = 0.48f, chromaticAberration = 0.32f,
                vignetteColor = new Color(0.35f, 0.02f, 0.05f), saturation = -32f, whisperLayer = true, heartbeatLayer = true
            }
        };

        [Header("Phase 3 flicker")]
        public float flickerSpeed = 7.5f;
        public float flickerAmplitude = 0.28f;

        [Header("Crossfade")]
        public float musicCrossfadeSeconds = 1.6f;

        public PhaseLook GetPhaseLook(WorldPhase phase)
        {
            if (phases == null) return null;
            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i] != null && phases[i].phase == phase)
                    return phases[i];
            }
            return phases.Length > 0 ? phases[0] : null;
        }

        public StressLook GetStressLook(int stress)
        {
            if (stressBands == null) return null;
            for (int i = 0; i < stressBands.Length; i++)
            {
                var b = stressBands[i];
                if (b != null && stress >= b.minStress && stress <= b.maxStress)
                    return b;
            }
            return stressBands.Length > 0 ? stressBands[stressBands.Length - 1] : null;
        }
    }
}
