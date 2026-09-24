using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Restringe la interacción a contacto o alcance directo de la mano en el espacio de trabajo,
    /// bloqueando completamente la selección y el resaltado desde el puntero láser o rayo lejano.
    ///
    /// Principio de IHC (Don Norman - Affordance y Mapeo Natural):
    /// Herramientas tangibles como la linterna, estetoscopio, sellos y extremidades del civil
    /// deben ser tomadas directamente con las manos en el mundo físico virtual, tal como
    /// en la vida real, eliminando la sensación antinatural de objetos flotando al final de un rayo.
    /// </summary>
    [DisallowMultipleComponent]
    public class DirectInteractionOnlyFilter : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        [Header("Distancia Máxima de Agarre Directo")]
        [Tooltip("Distancia máxima en metros entre la mano y el objeto para permitir interactuar.")]
        [SerializeField] private float maxDistance = 0.42f;

        [Header("Comportamiento Físico")]
        [SerializeField] private bool configureGrabInteractable = true;

        private XRBaseInteractable interactable;

        public bool canProcess => isActiveAndEnabled;

        public float MaxDistance
        {
            get => maxDistance;
            set => maxDistance = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (configureGrabInteractable && interactable is XRGrabInteractable grab)
            {
                grab.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractableFarAttachMode.Near;
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = true;
                grab.smoothPosition = false;
                grab.smoothRotation = false;
            }
        }

        private void OnEnable()
        {
            if (interactable == null) interactable = GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.selectFilters.Add(this);
                interactable.hoverFilters.Add(this);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectFilters.Remove(this);
                interactable.hoverFilters.Remove(this);
            }
        }

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable _)
        {
            return IsDirectInteractionAllowed(interactor);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable _)
        {
            return IsDirectInteractionAllowed(interactor);
        }

        private bool IsDirectInteractionAllowed(IXRInteractor interactor)
        {
            if (interactor == null) return false;

            // 1. Si es un NearFarInteractor en modo región lejana (rayo láser), rechazar siempre.
            if (interactor is NearFarInteractor nearFar)
            {
                if (nearFar.selectionRegion.Value == NearFarInteractor.Region.Far)
                {
                    return false;
                }
            }

            // 2. Si el interactor es un XRRayInteractor puro, rechazar.
            if (interactor is XRRayInteractor)
            {
                return false;
            }

            // 3. Comprobar distancia física entre la mano/controlador y el objeto.
            float dist = Vector3.Distance(interactor.transform.position, transform.position);
            return dist <= maxDistance;
        }
    }
}
