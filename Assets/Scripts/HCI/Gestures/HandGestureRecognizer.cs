using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Reconocedor de gestos estáticos a partir del seguimiento articular de manos (XR Hands 1.9).
    /// Principio de Responsabilidad Única (SRP): sólo traduce articulaciones a gestos con
    /// confirmación temporal (dwell); desconoce por completo qué orden clínica produce cada gesto.
    ///
    /// Justificación IHC:
    /// - Restricciones: el dwell (mantener la postura) y el cooldown impiden disparos accidentales.
    /// - Feedback: publica el progreso de confirmación continuo para que el HUD lo dibuje.
    /// - Mapeo natural: valida además la orientación de la mano, no sólo la forma de los dedos.
    /// </summary>
    public class HandGestureRecognizer : MonoBehaviour
    {
        [Header("Estado del Módulo")]
        [Tooltip("Permite apagar el vocabulario gestual sin destruir el módulo (p. ej. durante un tutorial).")]
        [SerializeField] private bool moduleEnabled = true;

        [Header("Muestreo")]
        [Tooltip("Intervalo de evaluación en segundos. Evita analizar la mano en cada fotograma.")]
        [SerializeField] private float detectionInterval = 0.06f;

        [Header("Zona de Comando Ergonómica")]
        [Tooltip("Exige que la mano esté dentro del campo de trabajo frente al oficial para aceptar gestos.")]
        [SerializeField] private bool requireHandInCommandZone = true;
        [Tooltip("Distancia máxima entre la cabeza y la palma para considerar el gesto intencional.")]
        [SerializeField] private float maxCommandDistance = 0.95f;
        [Tooltip("Coseno mínimo entre la mirada y la dirección a la mano (0 = 90 grados, 1 = centrada).")]
        [Range(-1f, 1f)]
        [SerializeField] private float minCommandZoneDot = 0.15f;

        [Header("Vocabulario Gestual")]
        [SerializeField] private List<HandGestureDefinition> definitions = new List<HandGestureDefinition>();

        /// <summary>
        /// Estado visualizable de una mano en cada evaluación: gesto candidato, progreso de
        /// confirmación y pose de la palma, para anclar la retroalimentación sobre la mano real.
        /// </summary>
        public event Action<HandGestureFeedback> GestureFeedbackUpdated;

        /// <summary>Gesto confirmado tras completar su dwell: (mano, gesto, posición de la palma).</summary>
        public event Action<HandSide, HandGestureType, Vector3> GesturePerformed;

        /// <summary>Indica si el módulo está aceptando gestos en este momento.</summary>
        public bool ModuleEnabled
        {
            get => moduleEnabled;
            set
            {
                moduleEnabled = value;
                if (!moduleEnabled) ResetAllDwell();
            }
        }

        /// <summary>Vocabulario activo, expuesto para que el HUD liste los gestos disponibles.</summary>
        public IReadOnlyList<HandGestureDefinition> Definitions => definitions;

        /// <summary>Indica si hay un subsistema de manos activo (visor con hand tracking o simulador).</summary>
        public bool IsHandTrackingAvailable => handSubsystem != null && handSubsystem.running;

        private static readonly List<XRHandSubsystem> SubsystemBuffer = new List<XRHandSubsystem>();

        private XRHandSubsystem handSubsystem;
        private XROrigin xrOrigin;
        private Transform trackingSpace;
        private bool leftHandHolding;
        private bool rightHandHolding;
        private Transform headTransform;

        private readonly HandDwellState leftState = new HandDwellState();
        private readonly HandDwellState rightState = new HandDwellState();
        private float lastEvaluationTime = -1f;

        private void Reset()
        {
            RestoreDefaultDefinitions();
        }

        /// <summary>
        /// Repuebla el vocabulario con el catálogo calibrado por defecto.
        /// Se invoca desde el instalador del Editor para que la escena guarde los gestos.
        /// </summary>
        public void RestoreDefaultDefinitions()
        {
            definitions = HandGestureCatalog.CreateDefaultDefinitions();
        }

        private void Awake()
        {
            if (definitions == null || definitions.Count == 0)
            {
                RestoreDefaultDefinitions();
            }
        }

        private void OnEnable()
        {
            EventBus.OnHandGrabStateChanged += HandleGrabStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnHandGrabStateChanged -= HandleGrabStateChanged;
        }

        private void Update()
        {
            if (!moduleEnabled) return;

            float now = Time.unscaledTime;
            if (now - lastEvaluationTime < detectionInterval) return;
            lastEvaluationTime = now;

            if (!TryEnsureSubsystem()) return;

            EvaluateHand(handSubsystem.leftHand, HandSide.Left, leftState, now);
            EvaluateHand(handSubsystem.rightHand, HandSide.Right, rightState, now);
        }

        /// <summary>
        /// Inyecta un gesto de forma programática. Lo usa el simulador de escritorio (teclado)
        /// cuando no hay seguimiento articular real disponible durante las pruebas en PC.
        /// </summary>
        public void InjectGesture(HandSide side, HandGestureType gesture)
        {
            if (!moduleEnabled || gesture == HandGestureType.None) return;

            Vector3 origin = headTransform != null
                ? headTransform.position + headTransform.forward * 0.35f + headTransform.right * (side == HandSide.Left ? -0.18f : 0.18f)
                : transform.position;

            // Muestra sintética para que el anillo de confirmación aparezca donde estaría la palma.
            var syntheticSample = new HandGestureSample
            {
                Side = side,
                PalmPosition = origin,
                PalmNormal = headTransform != null ? -headTransform.forward : Vector3.up,
                PalmForward = Vector3.up
            };

            PublishFeedback(side, gesture, gesture.ToString(), 1f, true, syntheticSample);
            GesturePerformed?.Invoke(side, gesture, origin);
            EventBus.TriggerHandGesturePerformed(side, gesture.ToString());
        }

        private bool TryEnsureSubsystem()
        {
            if (handSubsystem != null && handSubsystem.running)
            {
                CacheRigTransforms();
                return true;
            }

            handSubsystem = null;
            SubsystemManager.GetSubsystems(SubsystemBuffer);
            for (int i = 0; i < SubsystemBuffer.Count; ++i)
            {
                if (SubsystemBuffer[i] != null && SubsystemBuffer[i].running)
                {
                    handSubsystem = SubsystemBuffer[i];
                    break;
                }
            }

            if (handSubsystem == null)
            {
                ResetAllDwell();
                return false;
            }

            CacheRigTransforms();
            return true;
        }

        private void CacheRigTransforms()
        {
            if (xrOrigin == null)
            {
                xrOrigin = FindAnyObjectByType<XROrigin>(FindObjectsInactive.Exclude);
            }

            if (xrOrigin != null)
            {
                if (xrOrigin.CameraFloorOffsetObject != null) trackingSpace = xrOrigin.CameraFloorOffsetObject.transform;
                else if (xrOrigin.Origin != null) trackingSpace = xrOrigin.Origin.transform;
                if (xrOrigin.Camera != null) headTransform = xrOrigin.Camera.transform;
            }

            if (headTransform == null && Camera.main != null)
            {
                headTransform = Camera.main.transform;
            }
        }

        private void EvaluateHand(XRHand hand, HandSide side, HandDwellState state, float now)
        {
            if (!TryBuildSample(hand, side, out HandGestureSample sample))
            {
                ClearDwell(side, state);
                PublishFeedback(side, HandGestureType.None, string.Empty, 0f, false, sample);
                return;
            }

            // Una mano que sostiene una herramienta no está dando órdenes: el puño alrededor de la
            // linterna con el pulgar arriba no debe confundirse con un veredicto de aprobación.
            if (IsHandHolding(side) || !IsInCommandZone(sample))
            {
                ClearDwell(side, state);
                PublishFeedback(side, HandGestureType.None, string.Empty, 0f, true, sample);
                return;
            }

            HandGestureDefinition match = FindMatchingDefinition(sample);
            if (match == null)
            {
                ClearDwell(side, state);
                PublishFeedback(side, HandGestureType.None, string.Empty, 0f, true, sample);
                return;
            }

            if (match.Type != state.Candidate)
            {
                state.Candidate = match.Type;
                state.HoldStartTime = now;
                if (match.Type != state.LastFired) state.AwaitingRelease = false;
            }

            // Bloqueo de repetición: el mismo gesto no vuelve a dispararse hasta deshacerlo.
            if (state.AwaitingRelease && match.Type == state.LastFired)
            {
                PublishFeedback(side, match.Type, match.DisplayName, 1f, true, sample);
                return;
            }

            float progress = Mathf.Clamp01((now - state.HoldStartTime) / match.HoldSeconds);
            PublishFeedback(side, match.Type, match.DisplayName, progress, true, sample);

            if (progress < 1f) return;
            if (now - state.LastFireTime < match.CooldownSeconds) return;

            state.LastFireTime = now;
            state.LastFired = match.Type;
            state.AwaitingRelease = true;

            GesturePerformed?.Invoke(side, match.Type, sample.PalmPosition);
            EventBus.TriggerHandGesturePerformed(side, match.Type.ToString());
        }

        private bool IsHandHolding(HandSide side) => side == HandSide.Left ? leftHandHolding : rightHandHolding;

        private void HandleGrabStateChanged(HandSide side, bool isHolding)
        {
            if (side == HandSide.Left) leftHandHolding = isHolding;
            else if (side == HandSide.Right) rightHandHolding = isHolding;
        }

        private HandGestureDefinition FindMatchingDefinition(in HandGestureSample sample)
        {
            for (int i = 0; i < definitions.Count; ++i)
            {
                HandGestureDefinition def = definitions[i];
                if (def != null && def.Matches(sample)) return def;
            }
            return null;
        }

        /// <summary>
        /// Restricción ergonómica: sólo se aceptan gestos hechos en el volumen de trabajo
        /// frente al oficial, evitando que un brazo caído en reposo emita órdenes.
        /// </summary>
        private bool IsInCommandZone(in HandGestureSample sample)
        {
            if (!requireHandInCommandZone || headTransform == null) return true;

            Vector3 toHand = sample.PalmPosition - headTransform.position;
            if (toHand.sqrMagnitude > maxCommandDistance * maxCommandDistance) return false;

            return Vector3.Dot(headTransform.forward, toHand.normalized) >= minCommandZoneDot;
        }

        private bool TryBuildSample(XRHand hand, HandSide side, out HandGestureSample sample)
        {
            sample = default;
            if (!hand.isTracked) return false;

            XRFingerShape thumbShape = hand.CalculateFingerShape(XRHandFingerID.Thumb, XRFingerShapeTypes.FullCurl);
            XRFingerShape indexShape = hand.CalculateFingerShape(XRHandFingerID.Index, XRFingerShapeTypes.FullCurl | XRFingerShapeTypes.Pinch);
            XRFingerShape middleShape = hand.CalculateFingerShape(XRHandFingerID.Middle, XRFingerShapeTypes.FullCurl);
            XRFingerShape ringShape = hand.CalculateFingerShape(XRHandFingerID.Ring, XRFingerShapeTypes.FullCurl);
            XRFingerShape littleShape = hand.CalculateFingerShape(XRHandFingerID.Little, XRFingerShapeTypes.FullCurl);

            if (!thumbShape.TryGetFullCurl(out float thumbCurl)) return false;
            if (!indexShape.TryGetFullCurl(out float indexCurl)) return false;
            if (!middleShape.TryGetFullCurl(out float middleCurl)) return false;
            if (!ringShape.TryGetFullCurl(out float ringCurl)) return false;
            if (!littleShape.TryGetFullCurl(out float littleCurl)) return false;
            if (!indexShape.TryGetPinch(out float indexPinch)) indexPinch = 0f;

            if (!HandPalmFrame.TryGetPalmFrame(hand, side == HandSide.Right,
                    out Vector3 palmLocalPosition, out Vector3 palmForward, out Vector3 palmNormal))
            {
                return false;
            }

            if (!hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose indexProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out Pose thumbProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip)) return false;
            if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip)) return false;

            sample = new HandGestureSample
            {
                Side = side,
                ThumbCurl = thumbCurl,
                IndexCurl = indexCurl,
                MiddleCurl = middleCurl,
                RingCurl = ringCurl,
                LittleCurl = littleCurl,
                IndexPinch = indexPinch,
                PalmNormal = ToWorldDirection(palmNormal),
                PalmForward = ToWorldDirection(palmForward),
                ThumbDirection = ToWorldDirection(thumbTip.position - thumbProximal.position),
                IndexDirection = ToWorldDirection(indexTip.position - indexProximal.position),
                HeadForward = headTransform != null ? headTransform.forward : Vector3.forward,
                PalmPosition = ToWorldPosition(palmLocalPosition)
            };

            return true;
        }

        /// <summary>
        /// Las poses articulares llegan en espacio de seguimiento, cuyo marco local es el Camera
        /// Offset del XR Origin (no el Origin raíz): en modo Device ese offset eleva la escena
        /// la altura de ojos configurada, y sin él las palmas quedarían por debajo del suelo.
        /// </summary>
        private Vector3 ToWorldDirection(Vector3 trackingSpaceDirection)
        {
            if (trackingSpaceDirection.sqrMagnitude < 1e-8f) return Vector3.zero;
            Vector3 dir = trackingSpace != null
                ? trackingSpace.rotation * trackingSpaceDirection
                : trackingSpaceDirection;
            return dir.normalized;
        }

        private Vector3 ToWorldPosition(Vector3 trackingSpacePosition)
            => trackingSpace != null ? trackingSpace.TransformPoint(trackingSpacePosition) : trackingSpacePosition;

        /// <summary>
        /// Publica el estado visualizable de la mano. El evento del <see cref="EventBus"/> sólo
        /// se emite cuando el gesto o el progreso cambian de forma apreciable, para no inundar
        /// a los suscriptores externos con una señal continua a 16 Hz.
        /// </summary>
        private void PublishFeedback(HandSide side, HandGestureType gesture, string label, float progress, bool isTracked, in HandGestureSample sample)
        {
            GestureFeedbackUpdated?.Invoke(new HandGestureFeedback
            {
                Side = side,
                Gesture = gesture,
                Label = label,
                Progress = progress,
                IsHandTracked = isTracked,
                IsHoldingObject = IsHandHolding(side),
                PalmPosition = sample.PalmPosition,
                PalmNormal = sample.PalmNormal,
                PalmForward = sample.PalmForward
            });

            HandDwellState state = side == HandSide.Left ? leftState : rightState;
            if (state.LastPublishedGesture == gesture && Mathf.Abs(state.LastPublishedProgress - progress) < 0.05f) return;

            state.LastPublishedGesture = gesture;
            state.LastPublishedProgress = progress;
            EventBus.TriggerHandGestureProgress(side, gesture.ToString(), progress);
        }

        private void ClearDwell(HandSide side, HandDwellState state)
        {
            state.Candidate = HandGestureType.None;
            state.AwaitingRelease = false;
        }

        private void ResetAllDwell()
        {
            ClearDwell(HandSide.Left, leftState);
            ClearDwell(HandSide.Right, rightState);
        }

        /// <summary>
        /// Estado de confirmación temporal por mano. Clase interna para mutarla por referencia
        /// sin reservar memoria en cada evaluación.
        /// </summary>
        private class HandDwellState
        {
            public HandGestureType Candidate = HandGestureType.None;
            public HandGestureType LastFired = HandGestureType.None;
            public float HoldStartTime;
            public float LastFireTime = -100f;
            public bool AwaitingRelease;
            public HandGestureType LastPublishedGesture = HandGestureType.None;
            public float LastPublishedProgress = -1f;
        }
    }
}
