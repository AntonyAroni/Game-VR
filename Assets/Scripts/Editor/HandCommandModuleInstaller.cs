using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.HCI.Gestures;

namespace ZombieCheckpoint.Editor
{
    /// <summary>
    /// Instalador del módulo de comandos por manos en la escena abierta.
    /// Permite añadir (o reparar) el módulo sin necesidad de reconstruir toda la cabina
    /// con <c>CheckpointSceneBuilder</c>, que es una operación destructiva.
    /// </summary>
    public static class HandCommandModuleInstaller
    {
        private const string ModuleRootName = "[HAND_COMMAND_MODULE]";

        [MenuItem("ZombieCheckpoint/Instalar Modulo de Comandos por Manos")]
        public static void InstallInOpenScene()
        {
            GameObject root = Install(GameObject.Find("Checkpoint_Booth"));

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[HandCommandModule] Módulo de comandos por manos instalado en la escena abierta. " +
                      "Gestos activos: palma arriba/abajo, índice señalando, pulgar arriba/abajo y palma al frente.");
        }

        /// <summary>
        /// Crea o repara el módulo bajo el padre indicado y devuelve su raíz.
        /// Reutilizable desde el constructor procedural de la escena.
        /// </summary>
        public static GameObject Install(GameObject parent)
        {
            GameObject root = GameObject.Find(ModuleRootName);
            if (root == null)
            {
                root = new GameObject(ModuleRootName);
            }

            if (parent != null && root.transform.parent != parent.transform)
            {
                root.transform.SetParent(parent.transform, false);
            }

            var recognizer = root.GetOrAddComponent<HandGestureRecognizer>();
            var dispatcher = root.GetOrAddComponent<HandCommandDispatcher>();
            root.GetOrAddComponent<HandCommandHud>();
            root.GetOrAddComponent<HandGestureKeyboardSimulator>();

            // Serializar el vocabulario y los enlaces en la escena para que sean visibles
            // y editables desde el Inspector sin necesidad de entrar en modo Play.
            if (recognizer.Definitions == null || recognizer.Definitions.Count == 0)
            {
                recognizer.RestoreDefaultDefinitions();
                EditorUtility.SetDirty(recognizer);
            }

            if (dispatcher.Bindings == null || dispatcher.Bindings.Count == 0)
            {
                dispatcher.RestoreDefaultBindings();
                EditorUtility.SetDirty(dispatcher);
            }

            // Cableado explícito de dependencias para no depender de búsquedas en tiempo de ejecución.
            SerializedObject dispatcherObj = new SerializedObject(dispatcher);
            dispatcherObj.FindProperty("recognizer").objectReferenceValue = recognizer;
            dispatcherObj.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject simulatorObj = new SerializedObject(root.GetComponent<HandGestureKeyboardSimulator>());
            simulatorObj.FindProperty("recognizer").objectReferenceValue = recognizer;
            simulatorObj.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject hudObj = new SerializedObject(root.GetComponent<HandCommandHud>());
            hudObj.FindProperty("dispatcher").objectReferenceValue = dispatcher;
            hudObj.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }
    }
}
