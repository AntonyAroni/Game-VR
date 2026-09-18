using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ZombieCheckpoint.HCI.Grip
{
    /// <summary>
    /// Describe cómo se asienta un objeto en la palma: qué punto toca la mano y hacia qué
    /// dirección anatómica apunta cada eje del objeto.
    /// Principio de Responsabilidad Única (SRP): sólo calcula el attach del objeto por mano.
    ///
    /// Justificación IHC (Affordance y Mapeo Natural): una linterna se empuña con el haz
    /// saliendo por el lado del pulgar; un sello, con la almohadilla asomando bajo el meñique;
    /// un pasaporte, apoyado en la palma con el texto hacia los dedos, listo para leerse
    /// al girar la mano hacia la cara.
    ///
    /// Se registra como filtro de selección de XRI porque es el único punto en el que se conoce
    /// qué mano está a punto de agarrar, justo antes de que el agarre calcule sus desfases.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PalmGripProfile : MonoBehaviour, IXRSelectFilter
    {
        [Header("Punto de Contacto")]
        [Tooltip("Transform opcional que marca el centro del agarre (p. ej. la campana del estetoscopio).")]
        [SerializeField] private Transform gripPointOverride;
        [Tooltip("Centro del agarre en espacio local del objeto (se ignora si hay transform de agarre).")]
        [SerializeField] private Vector3 gripCenterLocal = Vector3.zero;
        [Tooltip("Distancia en metros entre el centro del agarre y la superficie de la palma (radio del mango).")]
        [SerializeField] private float gripRadius = 0.02f;

        [Header("Orientación Anatómica")]
        [SerializeField] private Vector3 primaryObjectAxis = Vector3.up;
        [SerializeField] private HandAxis primaryHandAxis = HandAxis.ThumbSide;
        [SerializeField] private Vector3 secondaryObjectAxis = Vector3.forward;
        [SerializeField] private HandAxis secondaryHandAxis = HandAxis.FingersForward;

        [Header("Seguimiento")]
        [Tooltip("Instantaneous elimina el retardo y la flotación del VelocityTracking: el objeto va pegado a la mano.")]
        [SerializeField] private bool forceInstantMovement = true;

        private static readonly Dictionary<IXRInteractable, PalmGripProfile> Registry = new Dictionary<IXRInteractable, PalmGripProfile>();

        private XRGrabInteractable grab;
        private Transform attachPoint;
        private Transform originalAttach;
        private IXRSelectInteractor seatedInteractor;

        /// <inheritdoc />
        public bool canProcess => isActiveAndEnabled;

        /// <summary>Busca el perfil asociado a un interactable sin usar GetComponent.</summary>
        public static bool TryGet(IXRInteractable interactable, out PalmGripProfile profile)
        {
            profile = null;
            if (interactable == null) return false;
            return Registry.TryGetValue(interactable, out profile) && profile != null && profile.isActiveAndEnabled;
        }

        /// <summary>Indica si el attach actual se calculó para el interactor indicado.</summary>
        public bool IsSeatedFor(IXRSelectInteractor selectInteractor)
            => selectInteractor != null && ReferenceEquals(seatedInteractor, selectInteractor);

        /// <summary>
        /// Configura el perfil desde código (presets). Los ejes se expresan en espacio local
        /// del objeto y el radio en metros de mundo.
        /// </summary>
        public void Configure(
            Transform gripPoint,
            Vector3 centerLocal,
            float radiusMeters,
            Vector3 primaryAxisLocal, HandAxis primaryAxisHand,
            Vector3 secondaryAxisLocal, HandAxis secondaryAxisHand)
        {
            gripPointOverride = gripPoint;
            gripCenterLocal = centerLocal;
            gripRadius = Mathf.Max(0f, radiusMeters);
            primaryObjectAxis = primaryAxisLocal;
            primaryHandAxis = primaryAxisHand;
            secondaryObjectAxis = secondaryAxisLocal;
            secondaryHandAxis = secondaryAxisHand;
        }

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            originalAttach = grab.attachTransform;

            attachPoint = new GameObject("PalmGrip_Attach").transform;
            attachPoint.SetParent(transform, false);
            ResetToDefaultAttach();

            grab.attachTransform = attachPoint;
            grab.useDynamicAttach = false;

            if (forceInstantMovement)
            {
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.smoothPosition = false;
                grab.smoothRotation = false;
            }
        }

        private void OnEnable()
        {
            if (grab == null) return;
            Registry[grab] = this;
            grab.selectFilters.Add(this);
        }

        private void OnDisable()
        {
            if (grab == null) return;
            Registry.Remove(grab);
            grab.selectFilters.Remove(this);
            seatedInteractor = null;
        }

        /// <summary>
        /// Se invoca justo antes de una selección. Nunca rechaza: sólo prepara el attach.
        /// Si la mano está en modo palma se asienta el objeto para esa mano concreta;
        /// en cualquier otro caso (mandos, agarre lejano) se restaura el pivote original.
        /// </summary>
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // Sólo cuando la mano intenta agarrar ahora mismo y está libre. XRI también evalúa
            // filtros durante el hover, y recolocar el attach entonces movería un objeto ya sostenido.
            if (interactor == null || !interactor.isSelectActive || interactor.hasSelection) return true;
            if (grab.isSelected && grab.selectMode == InteractableSelectMode.Multiple) return true;

            if (PalmGripAnchor.TryGet(interactor, out PalmGripAnchor anchor) && anchor.IsPalmEngaged)
            {
                SeatFor(anchor.IsLeftHand);
                seatedInteractor = interactor;
            }
            else
            {
                ResetToDefaultAttach();
                seatedInteractor = null;
            }

            return true;
        }

        /// <summary>
        /// Coloca el attach en espacio de mundo para que, al alinearse con el ancla de palma,
        /// cada eje del objeto apunte a su dirección anatómica y el centro del agarre quede a
        /// <see cref="gripRadius"/> de la palma. Trabajar en mundo evita deformaciones por escala.
        /// </summary>
        private void SeatFor(bool isLeftHand)
        {
            Vector3 objectPrimary = transform.TransformDirection(primaryObjectAxis);
            Vector3 objectSecondary = transform.TransformDirection(secondaryObjectAxis);
            Vector3 handPrimary = HandAxisUtility.ToAnchorSpace(primaryHandAxis, isLeftHand);
            Vector3 handSecondary = HandAxisUtility.ToAnchorSpace(secondaryHandAxis, isLeftHand);

            if (!TryBuildFrame(objectPrimary, objectSecondary, out Quaternion objectFrame) ||
                !TryBuildFrame(handPrimary, handSecondary, out Quaternion handFrame))
            {
                ResetToDefaultAttach();
                return;
            }

            // R cumple R·ejeMano = ejeObjeto: el attach coincide con el ancla cuando el objeto está asentado.
            Quaternion attachRotation = objectFrame * Quaternion.Inverse(handFrame);

            Vector3 gripCenter = gripPointOverride != null
                ? gripPointOverride.position
                : transform.TransformPoint(gripCenterLocal);

            Vector3 palmOut = attachRotation * Vector3.up;
            attachPoint.SetPositionAndRotation(gripCenter - palmOut * gripRadius, attachRotation);
        }

        private static bool TryBuildFrame(Vector3 forward, Vector3 up, out Quaternion frame)
        {
            frame = Quaternion.identity;
            if (forward.sqrMagnitude < 1e-8f) return false;

            forward.Normalize();
            if (up.sqrMagnitude < 1e-8f || Mathf.Abs(Vector3.Dot(forward, up.normalized)) > 0.97f)
            {
                // Ejes casi paralelos: se elige cualquier perpendicular estable para no degenerar.
                up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.9f ? Vector3.up : Vector3.forward;
            }

            frame = Quaternion.LookRotation(forward, up);
            return true;
        }

        private void ResetToDefaultAttach()
        {
            if (attachPoint == null) return;

            if (originalAttach != null)
            {
                attachPoint.SetPositionAndRotation(originalAttach.position, originalAttach.rotation);
            }
            else
            {
                attachPoint.localPosition = Vector3.zero;
                attachPoint.localRotation = Quaternion.identity;
            }
        }

        private void OnValidate()
        {
            gripRadius = Mathf.Max(0f, gripRadius);
            if (primaryObjectAxis == Vector3.zero) primaryObjectAxis = Vector3.up;
        }
    }
}
