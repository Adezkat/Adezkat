using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AshenAnvil.EditorTools
{
    /// <summary>
    /// Android build entry points.
    /// Editor: Ashen Anvil > Build Android APK.
    /// CI:     Unity -batchmode -quit -projectPath . -executeMethod AshenAnvil.EditorTools.AndroidBuilder.BuildApkCommandLine
    /// </summary>
    public static class AndroidBuilder
    {
        const string OutputDir = "Builds/Android";

        [MenuItem("Ashen Anvil/Build Android APK", priority = 10)]
        public static void BuildApkMenu() => Build(false, true);

        [MenuItem("Ashen Anvil/Build Android App Bundle (AAB)", priority = 11)]
        public static void BuildAabMenu() => Build(true, true);

        public static void BuildApkCommandLine()
        {
            var report = Build(GetArg("-aab") != null, false);
            EditorApplication.Exit(report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static BuildReport Build(bool appBundle, bool interactive)
        {
            if (!File.Exists(GeneratorPaths.GameScene) || !File.Exists(GeneratorPaths.MainMenuScene))
                AshenAnvilSetup.RebuildAll();
            else
                ProjectConfigurator.ConfigurePlayerSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[Ashen Anvil] Android Build Support is not installed. Add it through Unity Hub > Installs > Add modules.");
                return null;
            }

            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory(OutputDir);
            string file = Path.Combine(OutputDir, "AshenAnvilLegends" + (appBundle ? ".aab" : ".apk"));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = file,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = GetArg("-development") != null ? BuildOptions.Development : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Ashen Anvil] Build succeeded: {file} ({summary.totalSize / (1024f * 1024f):0.0} MB) in {summary.totalTime:mm\\:ss}");
                if (interactive) EditorUtility.RevealInFinder(file);
            }
            else Debug.LogError($"[Ashen Anvil] Build {summary.result} with {summary.totalErrors} error(s).");
            return report;
        }

        static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            if (i < 0) return null;
            return i + 1 < args.Length ? args[i + 1] : string.Empty;
        }
    }
}
