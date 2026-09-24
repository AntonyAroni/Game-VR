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

            // Identificador y nombre formal del proyecto para Meta Quest
            PlayerSettings.productName = "Zombie Checkpoint VR";
            PlayerSettings.applicationIdentifier = "com.ihc.zombiecheckpoint";

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

        [MenuItem("Zombie Checkpoint/3. Build Quest APK Only")]
        public static void BuildQuestApkOnly()
        {
            ConfigureQuestSettings();

            if (!System.IO.Directory.Exists("Builds"))
            {
                System.IO.Directory.CreateDirectory("Builds");
            }

            var buildPath = "Builds/ZombieCheckpoint.apk";
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/CheckpointBoothScene.unity" },
                locationPathName = buildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[QuestBuildHelper] Starting standalone APK build to: {buildPath}...");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log($"[QuestBuildHelper] Build result: {report.summary.result} (Total time: {report.summary.totalTime.TotalSeconds:F1}s)");
        }
    }

    /// <summary>
    /// Preprocesador automático de escena para compilaciones de Meta Quest (Android).
    /// Elimina el simulador de ratón/teclado de la escena del build para que el visor físico
    /// Meta Quest tome el control absoluto del tracking 6DOF y la altura de ojos sobre el suelo,
    /// y asegura el arranque incondicional del gestor de manos articuladas.
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

                // 3. Garantizar que OpenXRHandSubsystemManager quede activado para el build
                var handSubsystemManager = Object.FindAnyObjectByType<UnityEngine.XR.Hands.OpenXR.OpenXRHandSubsystemManager>(FindObjectsInactive.Include);
                if (handSubsystemManager != null)
                {
                    handSubsystemManager.enabled = true;
                    Debug.Log("[QuestBuildSceneProcessor] ✅ OpenXRHandSubsystemManager habilitado para el build.");
                }
            }
        }
    }

    /// <summary>
    /// Post-procesador de Gradle que inyecta los metadatos de alto rendimiento de Hand Tracking
    /// (60Hz, V2.0 y soporte obligatorio de manos) en el AndroidManifest.xml de Meta Quest.
    /// </summary>
    public class QuestPostGenerateGradleProject : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 99;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = System.IO.Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!System.IO.File.Exists(manifestPath))
            {
                manifestPath = System.IO.Path.Combine(path, "..", "unityLibrary", "src", "main", "AndroidManifest.xml");
            }

            if (!System.IO.File.Exists(manifestPath)) return;

            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.Load(manifestPath);
                var manifestNode = doc.SelectSingleNode("/manifest");
                var appNode = doc.SelectSingleNode("/manifest/application");
                if (manifestNode == null || appNode == null) return;

                const string androidNs = "http://schemas.android.com/apk/res/android";

                // 1. Permiso de Hand Tracking de Meta Quest
                EnsurePermission(doc, manifestNode, androidNs, "com.oculus.permission.HAND_TRACKING");

                // 2. Requisito de software de Hand Tracking
                EnsureFeature(doc, manifestNode, androidNs, "oculus.software.handtracking", "true");

                // 3. Metadatos de alta frecuencia y Hand Tracking v2.0
                EnsureMetaData(doc, appNode, androidNs, "com.oculus.handtracking.frequency", "HIGH");
                EnsureMetaData(doc, appNode, androidNs, "com.oculus.handtracking.version", "V2.0");

                doc.Save(manifestPath);
                Debug.Log("[QuestPostGenerateGradleProject] ✅ AndroidManifest configurado: Hand Tracking V2.0 (HIGH freq, Hands Required).");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[QuestPostGenerateGradleProject] Advertencia configurando manifest: " + ex.Message);
            }
        }

        private static void EnsurePermission(System.Xml.XmlDocument doc, System.Xml.XmlNode manifestNode, string ns, string permissionName)
        {
            foreach (System.Xml.XmlNode node in manifestNode.SelectNodes("uses-permission"))
            {
                if (node.Attributes?["android:name"]?.Value == permissionName) return;
            }

            var elem = doc.CreateElement("uses-permission");
            var attr = doc.CreateAttribute("android", "name", ns);
            attr.Value = permissionName;
            elem.Attributes.Append(attr);
            manifestNode.AppendChild(elem);
        }

        private static void EnsureFeature(System.Xml.XmlDocument doc, System.Xml.XmlNode manifestNode, string ns, string featureName, string required)
        {
            foreach (System.Xml.XmlNode node in manifestNode.SelectNodes("uses-feature"))
            {
                if (node.Attributes?["android:name"]?.Value == featureName)
                {
                    if (node.Attributes["android:required"] != null)
                        node.Attributes["android:required"].Value = required;
                    return;
                }
            }

            var elem = doc.CreateElement("uses-feature");
            var nameAttr = doc.CreateAttribute("android", "name", ns);
            nameAttr.Value = featureName;
            elem.Attributes.Append(nameAttr);

            var reqAttr = doc.CreateAttribute("android", "required", ns);
            reqAttr.Value = required;
            elem.Attributes.Append(reqAttr);

            manifestNode.AppendChild(elem);
        }

        private static void EnsureMetaData(System.Xml.XmlDocument doc, System.Xml.XmlNode appNode, string ns, string name, string value)
        {
            foreach (System.Xml.XmlNode node in appNode.SelectNodes("meta-data"))
            {
                if (node.Attributes?["android:name"]?.Value == name)
                {
                    if (node.Attributes["android:value"] != null)
                        node.Attributes["android:value"].Value = value;
                    return;
                }
            }

            var elem = doc.CreateElement("meta-data");
            var nameAttr = doc.CreateAttribute("android", "name", ns);
            nameAttr.Value = name;
            elem.Attributes.Append(nameAttr);

            var valAttr = doc.CreateAttribute("android", "value", ns);
            valAttr.Value = value;
            elem.Attributes.Append(valAttr);

            appNode.AppendChild(elem);
        }
    }
}
