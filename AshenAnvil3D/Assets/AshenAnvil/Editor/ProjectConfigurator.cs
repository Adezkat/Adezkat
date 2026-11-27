using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AshenAnvil.EditorTools
{
    /// <summary>Physics layers, Android player settings and the mobile URP pipeline asset.</summary>
    public static class ProjectConfigurator
    {
        static readonly string[] RequiredLayers = { "Player", "Enemy", "Pickup", "Projectile", "Destructible", "Environment" };

        public static void ConfigureLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            int slot = 8;
            foreach (var name in RequiredLayers)
            {
                if (LayerMask.NameToLayer(name) >= 0) continue;
                while (slot < 32 && !string.IsNullOrEmpty(layers.GetArrayElementAtIndex(slot).stringValue)) slot++;
                if (slot >= 32) { Debug.LogError("No free layer for " + name); break; }
                layers.GetArrayElementAtIndex(slot).stringValue = name;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Layers.Refresh();
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Ashen Anvil Studio";
            PlayerSettings.productName = "Ashen Anvil Legends";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ashenanvil.legends");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.MTRendering = true;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(GeneratorPaths.Icon);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            QualitySettings.vSyncCount = 0;
        }

        public static UniversalRenderPipelineAsset ConfigureRenderPipeline()
        {
            GeneratorPaths.Ensure(GeneratorPaths.Settings);
            string rendererPath = GeneratorPaths.Settings + "/URP_MobileRenderer.asset";
            string pipelinePath = GeneratorPaths.Settings + "/URP_Mobile.asset";

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    try { ResourceReloader.ReloadAllNullIn(renderer, "Packages/com.unity.render-pipelines.universal"); }
                    catch (System.Exception e) { Debug.LogWarning("URP renderer resources: " + e.Message); }
                    AssetDatabase.CreateAsset(renderer, rendererPath);
                }
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            // Mobile-friendly defaults.
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 35f;
            pipeline.shadowCascadeCount = 1;
            pipeline.useSRPBatcher = true;
            pipeline.supportsDynamicBatching = false;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
            return pipeline;
        }
    }
}
