using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZombieCheckpoint.Editor
{
    /// <summary>
    /// Ensures that the primary project scene (CheckpointBoothScene) is always loaded
    /// and designated as the default Play Mode scene, preventing Unity from falling back to template scenes.
    /// </summary>
    [InitializeOnLoad]
    public static class DefaultSceneAutoLoader
    {
        private const string PrimaryScenePath = "Assets/Scenes/CheckpointBoothScene.unity";

        static DefaultSceneAutoLoader()
        {
            EditorApplication.delayCall += OnEditorLoaded;
        }

        private static void OnEditorLoaded()
        {
            // Set CheckpointBoothScene as the guaranteed PlayMode start scene
            var primarySceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(PrimaryScenePath);
            if (primarySceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = primarySceneAsset;
            }

            // If the active scene is the template SampleScene or untitled, switch to CheckpointBoothScene
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path == "Assets/Scenes/SampleScene.unity" || string.IsNullOrEmpty(activeScene.path))
            {
                Debug.Log($"[DefaultSceneAutoLoader] Redirigiendo automáticamente a la escena principal: {PrimaryScenePath}");
                EditorSceneManager.OpenScene(PrimaryScenePath, OpenSceneMode.Single);
            }
        }
    }
}
