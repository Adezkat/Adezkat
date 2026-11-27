using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AshenAnvil.EditorTools
{
    /// <summary>
    /// One-click project generator. Runs automatically the first time the project is opened
    /// (when the generated scenes are missing) and can be re-run from the menu at any time.
    /// </summary>
    [InitializeOnLoad]
    public static class AshenAnvilSetup
    {
        static AshenAnvilSetup()
        {
            EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(GeneratorPaths.GameScene) && File.Exists(GeneratorPaths.MainMenuScene)) return;
            if (SessionState.GetBool("AshenAnvil.AutoRunAttempted", false)) return;
            SessionState.SetBool("AshenAnvil.AutoRunAttempted", true);
            Debug.Log("[Ashen Anvil] First launch detected - generating project assets...");
            RebuildAll();
        }

        [MenuItem("Ashen Anvil/Rebuild Project Assets", priority = 0)]
        public static void RebuildAll()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(GeneratorPaths.Database);
            if (db == null)
            {
                Debug.LogError($"[Ashen Anvil] GameDatabase not found at {GeneratorPaths.Database}");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                Step("Configuring layers", 0.05f);
                ProjectConfigurator.ConfigureLayers();
                Step("Configuring Android player settings", 0.1f);
                ProjectConfigurator.ConfigurePlayerSettings();
                Step("Creating mobile URP pipeline", 0.2f);
                ProjectConfigurator.ConfigureRenderPipeline();

                Step("Authoring animations", 0.3f);
                db.characterAnimator = AnimationBuilder.Build();
                EditorUtility.SetDirty(db);

                Step("Configuring model importers", 0.35f);
                PrefabBuilder.ConfigureModelImporters();
                Step("Building materials", 0.4f);
                PrefabBuilder.BuildMaterials(db);
                Step("Building weapon prefabs", 0.5f);
                PrefabBuilder.BuildWeaponPrefabs(db);
                Step("Building prop prefabs", 0.6f);
                PrefabBuilder.BuildPropPrefabs(db);
                Step("Baking hero & monster prefabs", 0.7f);
                PrefabBuilder.BuildCharacterPrefabs(db);

                Step("Generating scenes", 0.85f);
                SceneBuilder.BuildAll();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(GeneratorPaths.MainMenuScene);
                Debug.Log("[Ashen Anvil] Project ready. Press Play in the MainMenu scene, or use Ashen Anvil > Build Android APK.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                PrefabBuilder.EndMaterialPersistence();
                EditorUtility.ClearProgressBar();
            }
        }

        static void Step(string label, float progress)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Ashen Anvil", label, progress);
            Debug.Log("[Ashen Anvil] " + label);
        }

        [MenuItem("Ashen Anvil/Open Main Menu Scene", priority = 20)]
        static void OpenMenu() => OpenScene(GeneratorPaths.MainMenuScene);

        [MenuItem("Ashen Anvil/Open Battle Scene", priority = 21)]
        static void OpenGame() => OpenScene(GeneratorPaths.GameScene);

        static void OpenScene(string path)
        {
            if (!File.Exists(path)) { RebuildAll(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }

        [MenuItem("Ashen Anvil/Delete Save Data", priority = 40)]
        static void DeleteSave()
        {
            SaveSystem.Delete();
            Debug.Log("[Ashen Anvil] Save data deleted.");
        }
    }
}
