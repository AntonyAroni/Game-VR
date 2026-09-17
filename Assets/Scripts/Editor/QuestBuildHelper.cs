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

            // Asegurar que la escena activa tenga XROrigin configurado en Floor
            var origin = GameObject.Find("XR Origin Hands (XR Rig)") ?? GameObject.Find("XR Origin (XR Rig)");
            if (origin != null)
            {
                var xrOrigin = origin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    xrOrigin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
                    xrOrigin.CameraYOffset = 1.65f;
                    EditorUtility.SetDirty(origin);
                }
            }

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

    /// <summary>
    /// Preprocesador automático de escena para compilaciones de Meta Quest (Android).
    /// Elimina el simulador de ratón/teclado de la escena del build para que el visor físico
    /// Meta Quest tome el control absoluto del tracking 6DOF y la altura de ojos sobre el suelo,
    /// evitando que la cámara quede fijada al suelo (0, 0, 0) viendo solo los zapatos.
    /// </summary>
    public class QuestBuildSceneProcessor : UnityEditor.Build.IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, UnityEditor.Build.Reporting.BuildReport report)
        {
            if (report != null && report.summary.platformGroup == BuildTargetGroup.Android)
            {
                // 1. Eliminar XR Device Simulator del build de Quest
                var sim = GameObject.Find("XR Device Simulator");
                if (sim != null)
                {
                    Object.DestroyImmediate(sim);
                    Debug.Log("[QuestBuildSceneProcessor] ✅ XR Device Simulator eliminado del build de Quest.");
                }

                // 2. Garantizar que XROrigin use modo Floor y altura ergonómica
                var origin = GameObject.Find("XR Origin Hands (XR Rig)") ?? GameObject.Find("XR Origin (XR Rig)");
                if (origin != null)
                {
                    var xrOrigin = origin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
                    if (xrOrigin != null)
                    {
                        xrOrigin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
                        xrOrigin.CameraYOffset = 1.65f;
                        Debug.Log("[QuestBuildSceneProcessor] ✅ XROrigin configurado en modo Floor con altura 1.65m.");
                    }
                }
            }
        }
    }
}
