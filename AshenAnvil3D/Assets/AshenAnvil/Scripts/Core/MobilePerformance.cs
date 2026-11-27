using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AshenAnvil
{
    /// <summary>Frame rate, quality tier and render scale tuned for Android phones.</summary>
    public static class MobilePerformance
    {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
            Layers.ConfigureCollisionMatrix();
            Apply(PlayerProfile.Instance.Data.settings);
        }

        /// <summary>0 = Low, 1 = Medium, 2 = High.</summary>
        public static void Apply(GameSettings s)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = s.targetFps;

            int levels = QualitySettings.names.Length;
            if (levels > 0)
            {
                int idx = s.qualityLevel == 0 ? 0 : s.qualityLevel == 1 ? levels / 2 : levels - 1;
                QualitySettings.SetQualityLevel(idx, true);
            }

            QualitySettings.shadowDistance = s.qualityLevel == 0 ? 0f : s.qualityLevel == 1 ? 25f : 40f;
            QualitySettings.antiAliasing = s.qualityLevel == 2 ? 2 : 0;

#if !UNITY_EDITOR
            // Render scale is changed on the pipeline asset; skipped in the editor so the asset stays clean.
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = s.qualityLevel == 0 ? 0.7f : s.qualityLevel == 1 ? 0.85f : 1f;
                urp.shadowDistance = QualitySettings.shadowDistance;
            }
#endif
        }
    }
}
