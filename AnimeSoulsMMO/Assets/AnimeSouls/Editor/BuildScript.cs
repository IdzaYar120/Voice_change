using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AnimeSouls.EditorTools
{
    /// <summary>Command-line / CI builds. Usage: -executeMethod AnimeSouls.EditorTools.BuildScript.BuildWindows</summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/AnimeSouls/Scenes/Main.unity";

        [MenuItem("Anime Souls/Build Windows (.exe)")]
        public static void BuildWindows()
        {
            AnimeSoulsSetup.Setup();
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[AnimeSouls] Main scene was not created, aborting build.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var output = Path.Combine("build", "StandaloneWindows64", "AnimeSouls.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[AnimeSouls] Build {report.summary.result}: {output} ({report.summary.totalSize / (1024 * 1024)} MB)");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
