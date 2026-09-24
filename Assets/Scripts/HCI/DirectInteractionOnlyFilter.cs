using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Configura y calibra las herramientas del puesto para agarre natural y directo.
    /// Principio de IHC (Don Norman - Affordance y Mapeo Natural):
    /// - Permite coger los objetos tanto por pellizco (gesto de pinch con dedos índice y pulgar)
    ///   como por cierre directo de la mano (puño/palma).
    /// - Garantiza que al agarrar por pellizco, el objeto viaje y se acople de inmediato
    ///   en la mano del evaluador (farAttachMode = Near), en lugar de flotar a distancia
    ///   como si fuera un puntero láser o varita mágica.
    /// - Restringe el alcance a la distancia ergonómica de la mesa (1.35 m), evitando agarres
    ///   accidentales de objetos o puertas lejanas.
    /// - Al soltar o abrir la mano, libera el objeto con gravedad física natural.
    /// </summary>
    [DisallowMultipleComponent]
    public class DirectInteractionOnlyFilter : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        [Header("Distancia Máxima de Alcance en Mesa")]
        [Tooltip("Distancia máxima en metros entre la mano/cámara y el objeto en el mostrador.")]
        [SerializeField] private float maxDistance = 1.35f;

        [Header("Comportamiento Físico")]
        [SerializeField] private bool configureGrabInteractable = true;

        private XRBaseInteractable interactable;

        public bool canProcess => isActiveAndEnabled;

        public float MaxDistance
        {
            get => maxDistance;
            set => maxDistance = Mathf.Max(0.1f, value);
        }

        private void Awake()
        {
            ConfigureInteractable();
        }

        private void Start()
        {
            ConfigureInteractable();
        }

        public void ConfigureInteractable()
        {
            if (interactable == null) interactable = GetComponent<XRBaseInteractable>();
            if (configureGrabInteractable && interactable is XRGrabInteractable grab)
            {
                // Al coger por pellizco, acoplar de inmediato a la mano (Near attach)
                // para que el objeto no quede flotando al final del rayo.
                grab.farAttachMode = InteractableFarAttachMode.Near;
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
                interactable.selectFilters.Remove(this);
                interactable.selectFilters.Add(this);
                interactable.hoverFilters.Remove(this);
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

            // 1. Permitir NearFarInteractor tanto en Near (palma) como en Far (gesto de pellizco/pinch).
            // Comprobar únicamente que el objeto esté dentro del alcance ergonómico del mostrador.
            float dist = Vector3.Distance(interactor.transform.position, transform.position);
            return dist <= maxDistance;
        }
    }
}
