#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RageHunters.Editor
{
    /// <summary>
    /// Build automation for Android APK/AAB generation.
    /// Invoke from command line or Unity Editor menu.
    /// </summary>
    public static class BuildAndroid
    {
        private const string BUILD_PATH_APK = "Build/Android/RageHunters.apk";
        private const string BUILD_PATH_AAB = "Build/Android/RageHunters.aab";

        private static readonly string[] GAME_SCENES = new string[]
        {
            "Assets/Game/Scenes/OfficeComplexScene.unity",
            "Assets/Scenes/SampleScene.unity"
        };

        /// <summary>
        /// Builds a development APK with script debugging enabled.
        /// Can be called from CLI: -executeMethod RageHunters.Editor.BuildAndroid.BuildDevelopmentAPK
        /// </summary>
        [MenuItem("Build/Android/Development APK")]
        public static void BuildDevelopmentAPK()
        {
            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                locationPathName = BUILD_PATH_APK,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            ExecuteBuild(options, "Development APK");
        }

        /// <summary>
        /// Builds a release APK.
        /// </summary>
        [MenuItem("Build/Android/Release APK")]
        public static void BuildReleaseAPK()
        {
            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                locationPathName = BUILD_PATH_APK,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            ExecuteBuild(options, "Release APK");
        }

        /// <summary>
        /// Builds an Android App Bundle for store distribution.
        /// </summary>
        [MenuItem("Build/Android/Release AAB")]
        public static void BuildReleaseAAB()
        {
            EditorUserBuildSettings.buildAppBundle = true;

            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                locationPathName = BUILD_PATH_AAB,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            ExecuteBuild(options, "Release AAB");
            EditorUserBuildSettings.buildAppBundle = false;
        }

        private static string[] GetScenes()
        {
            // Use configured scenes if they exist, otherwise fall back to build settings
            var buildScenes = EditorBuildSettings.scenes;
            if (buildScenes != null && buildScenes.Length > 0)
            {
                var paths = new System.Collections.Generic.List<string>();
                foreach (var scene in buildScenes)
                {
                    if (scene.enabled)
                        paths.Add(scene.path);
                }
                if (paths.Count > 0) return paths.ToArray();
            }

            return GAME_SCENES;
        }

        private static void ExecuteBuild(BuildPlayerOptions options, string buildName)
        {
            Debug.Log($"[Build] Starting {buildName} build...");

            string dir = System.IO.Path.GetDirectoryName(options.locationPathName);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"[Build] {buildName} succeeded! Size: {summary.totalSize / (1024 * 1024)} MB, Time: {summary.totalTime}");
                    break;
                case BuildResult.Failed:
                    Debug.LogError($"[Build] {buildName} FAILED with {summary.totalErrors} error(s).");
                    // Exit with error code for CI
                    if (Application.isBatchMode)
                        EditorApplication.Exit(1);
                    break;
                case BuildResult.Cancelled:
                    Debug.LogWarning($"[Build] {buildName} was cancelled.");
                    break;
            }
        }
    }
}
#endif
