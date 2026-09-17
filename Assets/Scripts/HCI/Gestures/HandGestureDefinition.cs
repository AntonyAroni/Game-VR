using System;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Definición declarativa y editable desde el Inspector de un gesto estático.
    /// Principio Abierto/Cerrado (OCP): añadir un gesto nuevo no obliga a tocar el
    /// reconocedor; basta con declarar otro rango de curvaturas y su orientación.
    /// </summary>
    [Serializable]
    public class HandGestureDefinition
    {
        [Header("Identidad del Gesto")]
        [SerializeField] private HandGestureType type = HandGestureType.None;
        [SerializeField] private string displayName = "Gesto";
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private HandSide allowedHand = HandSide.Both;

        [Header("Rangos de Curvatura Permitidos (0 = extendido, 1 = flexionado)")]
        [SerializeField] private Vector2 thumbCurlRange = new Vector2(0f, 1f);
        [SerializeField] private Vector2 indexCurlRange = new Vector2(0f, 1f);
        [SerializeField] private Vector2 middleCurlRange = new Vector2(0f, 1f);
        [SerializeField] private Vector2 ringCurlRange = new Vector2(0f, 1f);
        [SerializeField] private Vector2 littleCurlRange = new Vector2(0f, 1f);
        [SerializeField] private Vector2 indexPinchRange = new Vector2(0f, 1f);

        [Header("Restricción de Orientación (Anti Falsos Positivos)")]
        [SerializeField] private GestureOrientation orientation = GestureOrientation.Ignore;
        [Range(0.1f, 1f)]
        [SerializeField] private float orientationMinDot = 0.55f;

        [Header("Temporización (Dwell + Cooldown)")]
        [Tooltip("Tiempo que la mano debe sostener la postura antes de confirmar el comando.")]
        [SerializeField] private float holdSeconds = 0.6f;
        [Tooltip("Tiempo mínimo entre dos disparos del mismo gesto.")]
        [SerializeField] private float cooldownSeconds = 1.2f;

        public HandGestureType Type => type;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? type.ToString() : displayName;
        public bool IsEnabled => isEnabled;
        public HandSide AllowedHand => allowedHand;
        public float HoldSeconds => Mathf.Max(0.05f, holdSeconds);
        public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);

        public HandGestureDefinition() { }

        /// <summary>
        /// Constructor de fábrica usado por el catálogo de gestos por defecto.
        /// </summary>
        public HandGestureDefinition(
            HandGestureType gestureType,
            string label,
            Vector2 thumb, Vector2 index, Vector2 middle, Vector2 ring, Vector2 little,
            GestureOrientation requiredOrientation,
            float minDot,
            float hold,
            float cooldown,
            Vector2? pinch = null,
            HandSide hand = HandSide.Both)
        {
            type = gestureType;
            displayName = label;
            thumbCurlRange = thumb;
            indexCurlRange = index;
            middleCurlRange = middle;
            ringCurlRange = ring;
            littleCurlRange = little;
            indexPinchRange = pinch ?? new Vector2(0f, 1f);
            orientation = requiredOrientation;
            orientationMinDot = minDot;
            holdSeconds = hold;
            cooldownSeconds = cooldown;
            allowedHand = hand;
            isEnabled = true;
        }

        /// <summary>
        /// Evalúa si la muestra actual de la mano satisface forma y orientación del gesto.
        /// </summary>
        public bool Matches(in HandGestureSample sample)
        {
            if (!isEnabled || type == HandGestureType.None) return false;
            if (!IsHandAllowed(sample.Side)) return false;

            if (!InRange(sample.ThumbCurl, thumbCurlRange)) return false;
            if (!InRange(sample.IndexCurl, indexCurlRange)) return false;
            if (!InRange(sample.MiddleCurl, middleCurlRange)) return false;
            if (!InRange(sample.RingCurl, ringCurlRange)) return false;
            if (!InRange(sample.LittleCurl, littleCurlRange)) return false;
            if (!InRange(sample.IndexPinch, indexPinchRange)) return false;

            if (orientation == GestureOrientation.Ignore) return true;
            if (!sample.TryResolveOrientation(orientation, out Vector3 handAxis, out Vector3 reference)) return true;

            if (handAxis.sqrMagnitude < 1e-6f || reference.sqrMagnitude < 1e-6f) return false;
            return Vector3.Dot(handAxis.normalized, reference.normalized) >= orientationMinDot;
        }

        /// <summary>Comprueba si el gesto admite la mano que lo está ejecutando.</summary>
        public bool IsHandAllowed(HandSide side)
            => allowedHand == HandSide.Both || allowedHand == side;

        private static bool InRange(float value, Vector2 range)
            => value >= range.x && value <= range.y;
    }
}
