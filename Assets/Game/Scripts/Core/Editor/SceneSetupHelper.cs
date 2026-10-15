#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace RageHunters.Editor
{
    /// <summary>
    /// Utility to configure project build settings scenes and verify scene registry.
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Rage Hunters/Setup/Register Scenes in Build Settings")]
        public static void RegisterScenesInBuildSettings()
        {
            var desiredScenes = new string[]
            {
                "Assets/Game/Scenes/OfficeComplexScene.unity",
                "Assets/Scenes/SampleScene.unity"
            };

            var scenesList = new List<EditorBuildSettingsScene>();

            foreach (var scenePath in desiredScenes)
            {
                if (System.IO.File.Exists(scenePath))
                {
                    scenesList.Add(new EditorBuildSettingsScene(scenePath, true));
                    Debug.Log($"[SceneSetup] Registered scene: {scenePath}");
                }
            }

            EditorBuildSettings.scenes = scenesList.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneSetup] Build Settings updated with {scenesList.Count} scene(s).");
        }
    }
}
#endif
