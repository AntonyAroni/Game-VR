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

        /// <summary>Progreso de confirmación del gesto en curso: (mano, gesto, 0..1).</summary>
        public event Action<HandSide, HandGestureType, float> GestureProgressChanged;

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
        private Transform originTransform;
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
                ? headTransform.position + headTransform.forward * 0.35f
                : transform.position;

            GestureProgressChanged?.Invoke(side, gesture, 1f);
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
                if (xrOrigin.Origin != null) originTransform = xrOrigin.Origin.transform;
                if (xrOrigin.Camera != null) headTransform = xrOrigin.Camera.transform;
            }

            if (headTransform == null && Camera.main != null)
            {
                headTransform = Camera.main.transform;
            }
        }

        private void EvaluateHand(XRHand hand, HandSide side, HandDwellState state, float now)
        {
            if (!TryBuildSample(hand, side, out HandGestureSample sample) || !IsInCommandZone(sample))
            {
                ClearDwell(side, state);
                return;
            }

            HandGestureDefinition match = FindMatchingDefinition(sample);
            if (match == null)
            {
                ClearDwell(side, state);
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
                GestureProgressChanged?.Invoke(side, match.Type, 1f);
                return;
            }

            float progress = Mathf.Clamp01((now - state.HoldStartTime) / match.HoldSeconds);
            GestureProgressChanged?.Invoke(side, match.Type, progress);
            EventBus.TriggerHandGestureProgress(side, match.Type.ToString(), progress);

            if (progress < 1f) return;
            if (now - state.LastFireTime < match.CooldownSeconds) return;

            state.LastFireTime = now;
            state.LastFired = match.Type;
            state.AwaitingRelease = true;

            GesturePerformed?.Invoke(side, match.Type, sample.PalmPosition);
            EventBus.TriggerHandGesturePerformed(side, match.Type.ToString());
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

            if (!hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist)) return false;
            if (!hand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out Pose middleProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose indexProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.LittleProximal).TryGetPose(out Pose littleProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out Pose thumbProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip)) return false;
            if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip)) return false;

            // La articulación Palm no la publican todos los proveedores: la muñeca actúa de respaldo.
            Vector3 palmLocalPosition = hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm)
                ? palm.position
                : wrist.position;

            Vector3 palmForward = middleProximal.position - wrist.position;
            Vector3 acrossPalm = littleProximal.position - indexProximal.position;
            if (palmForward.sqrMagnitude < 1e-8f || acrossPalm.sqrMagnitude < 1e-8f) return false;

            palmForward.Normalize();
            acrossPalm.Normalize();

            // El producto vectorial invierte su signo entre manos por la simetría anatómica especular.
            Vector3 palmNormal = Vector3.Cross(palmForward, acrossPalm) * (side == HandSide.Right ? -1f : 1f);

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
        /// Las poses articulares llegan en espacio de seguimiento del XROrigin; hay que llevarlas
        /// a espacio de mundo antes de compararlas contra arriba, abajo o la dirección de mirada.
        /// </summary>
        private Vector3 ToWorldDirection(Vector3 trackingSpaceDirection)
        {
            if (trackingSpaceDirection.sqrMagnitude < 1e-8f) return Vector3.zero;
            Vector3 dir = originTransform != null
                ? originTransform.rotation * trackingSpaceDirection
                : trackingSpaceDirection;
            return dir.normalized;
        }

        private Vector3 ToWorldPosition(Vector3 trackingSpacePosition)
            => originTransform != null ? originTransform.TransformPoint(trackingSpacePosition) : trackingSpacePosition;

        private void ClearDwell(HandSide side, HandDwellState state)
        {
            if (state.Candidate != HandGestureType.None)
            {
                GestureProgressChanged?.Invoke(side, HandGestureType.None, 0f);
                EventBus.TriggerHandGestureProgress(side, HandGestureType.None.ToString(), 0f);
            }

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
        }
    }
}
