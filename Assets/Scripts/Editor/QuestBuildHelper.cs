using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ZombieCheckpoint.Editor
{
    public static class QuestBuildHelper
    {
        [MenuItem("Zombie Checkpoint/1. Configure Quest Settings")]
        public static void ConfigureQuestSettings()
        {
            // Switch build target to Android
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[QuestBuildHelper] Switching active build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // Set IL2CPP and ARM64 required by Meta Quest
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Ensure CheckpointBoothScene is in build settings
            var scenePath = "Assets/Scenes/CheckpointBoothScene.unity";
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            Debug.Log("[QuestBuildHelper] Quest 2 settings applied successfully! Target: Android (ARM64, IL2CPP). Scene: " + scenePath);
        }

        [MenuItem("Zombie Checkpoint/2. Build and Run to Quest 2")]
        public static void BuildAndRunToQuest()
        {
            ConfigureQuestSettings();

            var buildPath = "Builds/ZombieCheckpoint.apk";
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/CheckpointBoothScene.unity" },
                locationPathName = buildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.AutoRunPlayer
            };

            Debug.Log($"[QuestBuildHelper] Starting Build and Run to: {buildPath}...");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log($"[QuestBuildHelper] Build result: {report.summary.result} (Total time: {report.summary.totalTime.TotalSeconds:F1}s)");
        }
    }
}
