using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Grip
{
    /// <summary>
    /// Ancla de agarre en la palma para un interactor de mano articulada.
    /// Principio de Responsabilidad Única (SRP): sólo decide dónde se sujetan los objetos
    /// (palma o pinza) y cuándo el cierre de la mano cuenta como agarre.
    ///
    /// Diagnóstico que resuelve: el rig de manos de XRI ancla todo agarre al punto de pinza
    /// entre pulgar e índice ("Pinch Grab Pose"), así que las herramientas quedaban colgando
    /// de las yemas. Esta ancla sigue el centro real de la palma leído de XR Hands y la usa
    /// para los objetos que declaran un <see cref="PalmGripProfile"/>; el resto (partes del
    /// cuerpo del civil, agarres lejanos) conserva intacto el comportamiento de pinza.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-120)] // Antes del XRInteractionManager (-105): decide el ancla del fotograma.
    public class PalmGripAnchor : MonoBehaviour
    {
        [Header("Geometría de la Palma")]
        [Tooltip("Separación de la superficie de la palma respecto al centro articular (grosor de la piel).")]
        [SerializeField] private float palmSurfaceOffset = 0.012f;
        [Tooltip("Desplazamiento de la sonda de agarre hacia los dedos.")]
        [SerializeField] private float graspProbeForward = 0.035f;
        [Tooltip("Desplazamiento de la sonda de agarre hacia fuera de la palma.")]
        [SerializeField] private float graspProbeOut = 0.03f;

        [Header("Agarre por Cierre de Mano")]
        [SerializeField] private bool enableGraspToGrab = true;
        [Tooltip("Curvatura media de los cuatro dedos para considerar la mano cerrada.")]
        [Range(0f, 1f)]
        [SerializeField] private float graspCloseThreshold = 0.52f;
        [Tooltip("Curvatura media por debajo de la cual la mano se considera abierta (histéresis).")]
        [Range(0f, 1f)]
        [SerializeField] private float graspOpenThreshold = 0.36f;
        [Tooltip("Distancia máxima entre la sonda y un objeto para que cerrar la mano lo agarre.")]
        [SerializeField] private float graspReach = 0.11f;

        private static readonly Dictionary<IXRSelectInteractor, PalmGripAnchor> Registry = new Dictionary<IXRSelectInteractor, PalmGripAnchor>();
        private static readonly List<XRHandSubsystem> SubsystemBuffer = new List<XRHandSubsystem>();

        private NearFarInteractor interactor;
        private InteractionAttachController attachController;
        private InteractionCasterBase nearCaster;
        private Transform pinchPose;
        private Transform originalCastOrigin;
        private Transform palmPose;
        private Transform graspProbe;
        private HandGraspSelectReader graspReader;
        private IXRInputButtonReader previousBypass;
        private XRHandSubsystem handSubsystem;

        private HandSide side = HandSide.Right;
        private bool isRightHand = true;
        private bool initialized;
        private bool hooked;
        private bool palmEngaged;
        private bool handTracked;
        private bool graspClosed;
        private bool graspEngaged;
        private bool reportedHolding;

        /// <summary>Mano servida por esta ancla.</summary>
        public HandSide Side => side;

        /// <summary>Indica si es la mano izquierda (necesario para asientos especulares).</summary>
        public bool IsLeftHand => !isRightHand;

        /// <summary>Indica si el interactor sigue ahora mismo a la palma en lugar de a la pinza.</summary>
        public bool IsPalmEngaged => palmEngaged;

        /// <summary>Indica si el cierre de la mano está produciendo un agarre activo.</summary>
        public bool IsGraspEngaged => enableGraspToGrab && graspEngaged;

        /// <summary>Indica si el seguimiento articular de esta mano está disponible.</summary>
        public bool IsHandTracked => handTracked;

        /// <summary>Busca el ancla registrada para un interactor concreto.</summary>
        public static bool TryGet(IXRSelectInteractor selectInteractor, out PalmGripAnchor anchor)
        {
            anchor = null;
            if (selectInteractor == null) return false;
            return Registry.TryGetValue(selectInteractor, out anchor) && anchor != null && anchor.isActiveAndEnabled;
        }

        /// <summary>
        /// Enlaza el ancla con el interactor de la mano. Puede llamarse sobre un GameObject
        /// inactivo: el enganche real se completa en cuanto la mano se activa.
        /// </summary>
        public void Initialize(NearFarInteractor handInteractor, HandSide handSide)
        {
            if (handInteractor == null) return;

            interactor = handInteractor;
            side = handSide;
            isRightHand = handSide != HandSide.Left;

            attachController = interactor.GetComponent<InteractionAttachController>();
            nearCaster = interactor.nearInteractionCaster as InteractionCasterBase;
            pinchPose = attachController != null ? attachController.transformToFollow : null;
            if (nearCaster != null) originalCastOrigin = nearCaster.castOrigin;

            if (attachController == null)
            {
                Debug.LogWarning($"[PalmGripAnchor] El interactor '{interactor.name}' no tiene InteractionAttachController: se mantiene el agarre por pinza.");
            }

            initialized = true;
            if (isActiveAndEnabled) Hook();
        }

        private void OnEnable()
        {
            if (initialized) Hook();
        }

        private void OnDisable()
        {
            Unhook();
        }

        private void OnDestroy()
        {
            if (palmPose != null) Destroy(palmPose.gameObject);
            if (graspProbe != null) Destroy(graspProbe.gameObject);
        }

        private void Hook()
        {
            if (hooked || interactor == null) return;

            EnsurePoseTransforms();
            Registry[interactor] = this;

            if (nearCaster != null && graspProbe != null)
            {
                // La detección cercana pasa a centrarse en la mano, no en las yemas: con el radio
                // de 10 cm del caster sigue cubriendo el punto de pinza para los agarres finos.
                nearCaster.castOrigin = graspProbe;
            }

            XRInputButtonReader selectInput = interactor.selectInput;
            if (selectInput != null)
            {
                previousBypass = selectInput.bypass;
                graspReader = new HandGraspSelectReader(selectInput, this);
                selectInput.bypass = graspReader;
            }

            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
            hooked = true;
        }

        private void Unhook()
        {
            if (!hooked) return;

            SetPalmEngaged(false);
            UnsubscribeSubsystem();

            if (interactor != null)
            {
                interactor.selectEntered.RemoveListener(OnSelectEntered);
                interactor.selectExited.RemoveListener(OnSelectExited);

                XRInputButtonReader selectInput = interactor.selectInput;
                if (selectInput != null && ReferenceEquals(selectInput.bypass, graspReader))
                {
                    selectInput.bypass = previousBypass;
                }

                Registry.Remove(interactor);
            }

            if (nearCaster != null) nearCaster.castOrigin = originalCastOrigin;

            graspClosed = false;
            graspEngaged = false;
            ReportHolding(false);
            hooked = false;
        }

        /// <summary>
        /// Crea las poses de palma y de sonda bajo el Camera Offset del XR Origin, que es el
        /// espacio local en el que XR Hands publica las articulaciones (el mismo que usan los
        /// TrackedPoseDriver de las manos). Así basta con copiar la pose articular como pose local.
        /// </summary>
        private void EnsurePoseTransforms()
        {
            if (palmPose != null && graspProbe != null) return;

            Transform trackingSpace = ResolveTrackingSpace();
            string handName = isRightHand ? "Right" : "Left";

            if (palmPose == null)
            {
                palmPose = new GameObject($"Palm Grab Pose ({handName})").transform;
                palmPose.SetParent(trackingSpace, false);
                if (pinchPose != null) palmPose.SetPositionAndRotation(pinchPose.position, pinchPose.rotation);
            }

            if (graspProbe == null)
            {
                graspProbe = new GameObject($"Grasp Probe ({handName})").transform;
                graspProbe.SetParent(trackingSpace, false);
                if (pinchPose != null) graspProbe.SetPositionAndRotation(pinchPose.position, pinchPose.rotation);
            }
        }

        private Transform ResolveTrackingSpace()
        {
            var origin = FindAnyObjectByType<XROrigin>(FindObjectsInactive.Exclude);
            if (origin != null && origin.CameraFloorOffsetObject != null) return origin.CameraFloorOffsetObject.transform;
            if (origin != null) return origin.transform;
            return pinchPose != null ? pinchPose.parent : transform;
        }

        private void Update()
        {
            if (!hooked || interactor == null) return;

            TryEnsureSubsystem();
            UpdateGraspState();
            SetPalmEngaged(ShouldUsePalm());
        }

        private void TryEnsureSubsystem()
        {
            if (handSubsystem != null && handSubsystem.running) return;

            UnsubscribeSubsystem();
            SubsystemManager.GetSubsystems(SubsystemBuffer);
            for (int i = 0; i < SubsystemBuffer.Count; ++i)
            {
                if (SubsystemBuffer[i] != null && SubsystemBuffer[i].running)
                {
                    handSubsystem = SubsystemBuffer[i];
                    handSubsystem.updatedHands += OnUpdatedHands;
                    break;
                }
            }

            if (handSubsystem == null) handTracked = false;
        }

        private void UnsubscribeSubsystem()
        {
            if (handSubsystem != null) handSubsystem.updatedHands -= OnUpdatedHands;
            handSubsystem = null;
        }

        /// <summary>
        /// Actualiza las poses tanto en la fase dinámica como justo antes de renderizar,
        /// para que el objeto sostenido no arrastre ni un fotograma de latencia respecto a la mano.
        /// </summary>
        private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType updateType)
        {
            XRHand hand = isRightHand ? subsystem.rightHand : subsystem.leftHand;
            if (!HandPalmFrame.TryGetPalmFrame(hand, isRightHand, out Vector3 palm, out Vector3 fingers, out Vector3 normal))
            {
                handTracked = false;
                return;
            }

            handTracked = true;
            Quaternion palmRotation = Quaternion.LookRotation(fingers, normal);

            if (palmPose != null)
            {
                palmPose.localPosition = palm + normal * palmSurfaceOffset;
                palmPose.localRotation = palmRotation;
            }

            if (graspProbe != null)
            {
                graspProbe.localPosition = palm + fingers * graspProbeForward + normal * graspProbeOut;
                graspProbe.localRotation = palmRotation;
            }
        }

        /// <summary>
        /// Histéresis de cierre de mano. El agarre sólo se activa en el instante de cerrar si hay
        /// un objeto al alcance: cerrar el puño en el aire y luego tocar algo no lo coge, igual
        /// que en la vida real hay que abrir la mano para volver a agarrar.
        /// </summary>
        private void UpdateGraspState()
        {
            if (!enableGraspToGrab || !handTracked || handSubsystem == null)
            {
                graspClosed = false;
                graspEngaged = false;
                return;
            }

            XRHand hand = isRightHand ? handSubsystem.rightHand : handSubsystem.leftHand;
            if (!TryGetMeanFingerCurl(hand, out float curl))
            {
                graspClosed = false;
                graspEngaged = false;
                return;
            }

            if (!graspClosed && curl >= graspCloseThreshold)
            {
                graspClosed = true;
                graspEngaged = interactor.hasSelection || HasReachableHoverTarget(out _);
            }
            else if (graspClosed && curl <= graspOpenThreshold)
            {
                graspClosed = false;
                graspEngaged = false;
            }
        }

        private static bool TryGetMeanFingerCurl(XRHand hand, out float meanCurl)
        {
            meanCurl = 0f;
            if (!hand.isTracked) return false;

            if (!hand.CalculateFingerShape(XRHandFingerID.Index, XRFingerShapeTypes.FullCurl).TryGetFullCurl(out float index)) return false;
            if (!hand.CalculateFingerShape(XRHandFingerID.Middle, XRFingerShapeTypes.FullCurl).TryGetFullCurl(out float middle)) return false;
            if (!hand.CalculateFingerShape(XRHandFingerID.Ring, XRFingerShapeTypes.FullCurl).TryGetFullCurl(out float ring)) return false;
            if (!hand.CalculateFingerShape(XRHandFingerID.Little, XRFingerShapeTypes.FullCurl).TryGetFullCurl(out float little)) return false;

            meanCurl = (index + middle + ring + little) * 0.25f;
            return true;
        }

        /// <summary>
        /// Decide el ancla del fotograma: la palma si se sostiene un objeto asentado por su perfil
        /// o si el objetivo cercano más próximo tiene perfil; la pinza en cualquier otro caso.
        /// Engancharse a la palma ya durante el hover evita el salto de un fotograma al agarrar.
        /// </summary>
        private bool ShouldUsePalm()
        {
            if (palmPose == null) return false;

            // Con un objeto asentado no se exige tracking: ante una pérdida breve la palma conserva
            // su última pose, mientras que volver a la pinza haría saltar el objeto a las yemas.
            if (interactor.hasSelection)
            {
                var selected = interactor.interactablesSelected;
                for (int i = 0; i < selected.Count; ++i)
                {
                    if (PalmGripProfile.TryGet(selected[i], out PalmGripProfile profile) && profile.IsSeatedFor(interactor))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (!handTracked) return false;

            return HasReachableHoverTarget(out IXRInteractable closest)
                && PalmGripProfile.TryGet(closest, out _);
        }

        /// <summary>Busca el objeto sobrevolado más cercano a la sonda dentro del alcance de agarre.</summary>
        private bool HasReachableHoverTarget(out IXRInteractable closest)
        {
            closest = null;
            if (graspProbe == null) return false;

            Vector3 probe = graspProbe.position;
            float bestSqr = graspReach * graspReach;
            var hovered = interactor.interactablesHovered;

            for (int i = 0; i < hovered.Count; ++i)
            {
                IXRInteractable candidate = hovered[i];
                if (candidate == null) continue;

                var colliders = candidate.colliders;
                for (int c = 0; c < colliders.Count; ++c)
                {
                    Collider col = colliders[c];
                    if (col == null || !col.enabled) continue;

                    // Se usa la caja envolvente: segura con MeshCollider cóncavos y sin coste físico.
                    float sqr = (col.bounds.ClosestPoint(probe) - probe).sqrMagnitude;
                    if (sqr <= bestSqr)
                    {
                        bestSqr = sqr;
                        closest = candidate;
                    }
                }
            }

            return closest != null;
        }

        private void SetPalmEngaged(bool engage)
        {
            if (attachController == null || palmPose == null || pinchPose == null)
            {
                palmEngaged = false;
                return;
            }

            if (palmEngaged == engage) return;

            palmEngaged = engage;
            attachController.transformToFollow = engage ? palmPose : pinchPose;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            SetPalmEngaged(ShouldUsePalm());
            ReportHolding(true);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (interactor != null && interactor.hasSelection) return;

            ReportHolding(false);
        }

        /// <summary>
        /// Publica en el EventBus si la mano sostiene algo, para que el módulo de gestos no
        /// interprete como orden la postura de la mano alrededor de una herramienta
        /// (p. ej. un puño con el pulgar arriba sujetando la linterna).
        /// </summary>
        private void ReportHolding(bool holding)
        {
            if (reportedHolding == holding) return;
            reportedHolding = holding;
            EventBus.TriggerHandGrabStateChanged(side, holding);
        }
    }
}
