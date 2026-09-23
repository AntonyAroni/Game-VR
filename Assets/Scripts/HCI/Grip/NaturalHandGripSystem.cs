using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Grip
{
    /// <summary>
    /// Servicio de escena que activa el agarre natural con las manos.
    /// Principio de Responsabilidad Única (SRP): sólo instala anclas de palma en los
    /// interactores de mano y perfiles de asiento en las herramientas, sin tocar prefabs.
    ///
    /// Funciona sobre la escena existente sin reconstruirla: localiza el rig por su
    /// <see cref="XRInputModalityManager"/> y escucha el registro de interactables para
    /// cubrir también los objetos que aparezcan más tarde.
    /// </summary>
    [DisallowMultipleComponent]
    public class NaturalHandGripSystem : MonoBehaviour
    {
        [Header("Dependencias (se resuelven solas si quedan vacías)")]
        [SerializeField] private XRInputModalityManager modalityManager;
        [SerializeField] private XRInteractionManager interactionManager;

        [Header("Configuración")]
        [Tooltip("Añade automáticamente perfiles de agarre en la palma a linterna, sello, estetoscopio y pasaporte.")]
        [SerializeField] private bool autoConfigureTools = true;

        private void Start()
        {
            InstallHandAnchors();

            if (autoConfigureTools)
            {
                ConfigureExistingInteractables();
                SubscribeToRegistrations();
            }
        }

        private void OnDestroy()
        {
            if (interactionManager != null)
            {
                interactionManager.interactableRegistered -= OnInteractableRegistered;
            }
        }

        /// <summary>
        /// Instala un <see cref="PalmGripAnchor"/> en el interactor Near-Far de cada mano articulada.
        /// Los mandos no se tocan: su pose de agarre ya es la del mando físico.
        /// </summary>
        private void InstallHandAnchors()
        {
            if (modalityManager == null)
            {
                modalityManager = FindAnyObjectByType<XRInputModalityManager>(FindObjectsInactive.Include);
            }

            int installed = 0;
            if (modalityManager != null)
            {
                installed += InstallAnchor(modalityManager.leftHand, HandSide.Left) ? 1 : 0;
                installed += InstallAnchor(modalityManager.rightHand, HandSide.Right) ? 1 : 0;
            }

            if (installed == 0)
            {
                installed = InstallAnchorsDirect();
            }

            if (installed == 0)
            {
                Debug.LogWarning("[NaturalHandGrip] No se encontraron interactores de mano articulada: el agarre natural queda inactivo.");
            }
            else
            {
                Debug.Log($"[NaturalHandGrip] ✅ {installed} ancla(s) de agarre en palma instaladas correctamente.");
            }
        }

        private static bool InstallAnchor(GameObject handRoot, HandSide side)
        {
            if (handRoot == null) return false;

            var handInteractor = handRoot.GetComponentInChildren<NearFarInteractor>(true);
            if (handInteractor == null) return false;

            var anchor = handInteractor.gameObject.GetOrAddComponent<PalmGripAnchor>();
            anchor.Initialize(handInteractor, side);
            return true;
        }

        /// <summary>
        /// Localiza directamente todos los interactores Near-Far en la escena y les instala
        /// el PalmGripAnchor asignando la lateralidad correspondiente (izq/der).
        /// </summary>
        private static int InstallAnchorsDirect()
        {
            int installed = 0;
            var interactors = FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include);
            foreach (var candidate in interactors)
            {
                HandSide side = HandSide.Right;
                string objName = candidate.gameObject.name.ToLowerInvariant();
                string parentName = candidate.transform.parent != null ? candidate.transform.parent.name.ToLowerInvariant() : "";

                if (candidate.handedness == InteractorHandedness.Left || objName.Contains("left") || parentName.Contains("left"))
                {
                    side = HandSide.Left;
                }

                var anchor = candidate.gameObject.GetOrAddComponent<PalmGripAnchor>();
                anchor.Initialize(candidate, side);
                installed++;
            }
            return installed;
        }

        private static void ConfigureExistingInteractables()
        {
            var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                PalmGripPresets.TryApply(grab);
            }
        }

        private void SubscribeToRegistrations()
        {
            if (interactionManager == null)
            {
                interactionManager = FindAnyObjectByType<XRInteractionManager>(FindObjectsInactive.Exclude);
            }

            if (interactionManager != null)
            {
                interactionManager.interactableRegistered += OnInteractableRegistered;
            }
        }

        private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            if (args.interactableObject is XRGrabInteractable grab)
            {
                PalmGripPresets.TryApply(grab);
            }
        }
    }
}
