using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.HCI.Grip;

namespace ZombieCheckpoint.Editor
{
    /// <summary>
    /// Instalador del agarre natural con las manos en la escena abierta.
    /// Añade el servicio <see cref="NaturalHandGripSystem"/>, que en tiempo de ejecución
    /// ancla los agarres a la palma y asienta cada herramienta según su perfil,
    /// sin modificar los prefabs del rig ni reconstruir la cabina.
    /// </summary>
    public static class NaturalHandGripInstaller
    {
        private const string ModuleRootName = "[NATURAL_HAND_GRIP]";

        [MenuItem("ZombieCheckpoint/Instalar Agarre Natural de Manos")]
        public static void InstallInOpenScene()
        {
            GameObject root = Install(GameObject.Find("Checkpoint_Booth"));

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[NaturalHandGrip] Agarre natural instalado: las herramientas se sujetan con la palma " +
                      "y pueden agarrarse cerrando la mano además de con la pinza.");
        }

        /// <summary>Crea o repara el servicio bajo el padre indicado y devuelve su raíz.</summary>
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

            root.GetOrAddComponent<NaturalHandGripSystem>();
            return root;
        }
    }
}
