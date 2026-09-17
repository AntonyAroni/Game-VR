using System.Collections.Generic;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Catálogo de gestos clínicos por defecto con sus umbrales ya calibrados.
    /// Principio de Responsabilidad Única (SRP): sólo provee datos de configuración inicial.
    /// Justificación IHC (Mapeo Natural): cada postura reproduce el gesto que un
    /// oficial de control usaría físicamente para dar la orden sin hablar.
    /// </summary>
    public static class HandGestureCatalog
    {
        /// <summary>
        /// Construye la lista de gestos reconocidos por defecto, ordenada de más
        /// específica a más genérica (el reconocedor acepta la primera coincidencia).
        /// </summary>
        public static List<HandGestureDefinition> CreateDefaultDefinitions()
        {
            return new List<HandGestureDefinition>
            {
                // Pulgar arriba: veredicto de aprobación. Dwell largo por ser acción irreversible.
                new HandGestureDefinition(
                    HandGestureType.ThumbUp, "Pulgar Arriba",
                    thumb: new Vector2(0f, 0.40f),
                    index: new Vector2(0.65f, 1f),
                    middle: new Vector2(0.65f, 1f),
                    ring: new Vector2(0.60f, 1f),
                    little: new Vector2(0.55f, 1f),
                    requiredOrientation: GestureOrientation.ThumbUp,
                    minDot: 0.60f, hold: 1.1f, cooldown: 2.0f),

                // Pulgar abajo: veredicto de cuarentena. Mismo dwell largo de confirmación.
                new HandGestureDefinition(
                    HandGestureType.ThumbDown, "Pulgar Abajo",
                    thumb: new Vector2(0f, 0.40f),
                    index: new Vector2(0.65f, 1f),
                    middle: new Vector2(0.65f, 1f),
                    ring: new Vector2(0.60f, 1f),
                    little: new Vector2(0.55f, 1f),
                    requiredOrientation: GestureOrientation.ThumbDown,
                    minDot: 0.60f, hold: 1.1f, cooldown: 2.0f),

                // Índice señalando al civil: "descubra el torso".
                new HandGestureDefinition(
                    HandGestureType.PointIndex, "Índice Señalando",
                    thumb: new Vector2(0f, 1f),
                    index: new Vector2(0f, 0.30f),
                    middle: new Vector2(0.60f, 1f),
                    ring: new Vector2(0.55f, 1f),
                    little: new Vector2(0.55f, 1f),
                    requiredOrientation: GestureOrientation.IndexForward,
                    minDot: 0.35f, hold: 0.7f, cooldown: 1.5f),

                // Palma abierta hacia el cielo: "levante los brazos".
                new HandGestureDefinition(
                    HandGestureType.OpenPalmUp, "Palma Arriba",
                    thumb: new Vector2(0f, 0.50f),
                    index: new Vector2(0f, 0.30f),
                    middle: new Vector2(0f, 0.30f),
                    ring: new Vector2(0f, 0.35f),
                    little: new Vector2(0f, 0.40f),
                    requiredOrientation: GestureOrientation.PalmUp,
                    minDot: 0.55f, hold: 0.6f, cooldown: 1.5f),

                // Palma abierta hacia el suelo: "baje los brazos".
                new HandGestureDefinition(
                    HandGestureType.OpenPalmDown, "Palma Abajo",
                    thumb: new Vector2(0f, 0.50f),
                    index: new Vector2(0f, 0.30f),
                    middle: new Vector2(0f, 0.30f),
                    ring: new Vector2(0f, 0.35f),
                    little: new Vector2(0f, 0.40f),
                    requiredOrientation: GestureOrientation.PalmDown,
                    minDot: 0.55f, hold: 0.6f, cooldown: 1.5f),

                // Palma frontal hacia el civil: gesto universal de "alto".
                new HandGestureDefinition(
                    HandGestureType.OpenPalmForward, "Palma al Frente (Alto)",
                    thumb: new Vector2(0f, 0.50f),
                    index: new Vector2(0f, 0.30f),
                    middle: new Vector2(0f, 0.30f),
                    ring: new Vector2(0f, 0.35f),
                    little: new Vector2(0f, 0.40f),
                    requiredOrientation: GestureOrientation.PalmAwayFromFace,
                    minDot: 0.55f, hold: 0.7f, cooldown: 1.5f),

                // Pinza índice-pulgar: conmutador fino (luz UV).
                new HandGestureDefinition(
                    HandGestureType.PinchIndex, "Pinza Índice",
                    thumb: new Vector2(0f, 1f),
                    index: new Vector2(0f, 1f),
                    middle: new Vector2(0.45f, 1f),
                    ring: new Vector2(0.40f, 1f),
                    little: new Vector2(0.40f, 1f),
                    requiredOrientation: GestureOrientation.Ignore,
                    minDot: 0.5f, hold: 0.8f, cooldown: 1.8f,
                    pinch: new Vector2(0.80f, 1f)),

                // Puño cerrado: reservado para "cancelar/retener".
                new HandGestureDefinition(
                    HandGestureType.Fist, "Puño Cerrado",
                    thumb: new Vector2(0.25f, 1f),
                    index: new Vector2(0.75f, 1f),
                    middle: new Vector2(0.75f, 1f),
                    ring: new Vector2(0.70f, 1f),
                    little: new Vector2(0.65f, 1f),
                    requiredOrientation: GestureOrientation.Ignore,
                    minDot: 0.5f, hold: 0.8f, cooldown: 1.8f)
            };
        }

        /// <summary>
        /// Enlaces gesto → comando clínico por defecto.
        /// La pinza queda sin asignar porque XRI 3.x ya la reserva para seleccionar objetos,
        /// y el puño porque se confunde con la postura de agarre de herramientas.
        /// </summary>
        public static List<GestureCommandBinding> CreateDefaultBindings()
        {
            return new List<GestureCommandBinding>
            {
                new GestureCommandBinding(HandGestureType.OpenPalmUp, GestureCommandId.RaiseArms, HandSide.Both),
                new GestureCommandBinding(HandGestureType.OpenPalmDown, GestureCommandId.LowerArms, HandSide.Both),
                new GestureCommandBinding(HandGestureType.PointIndex, GestureCommandId.ToggleExposeTorso, HandSide.Both),
                new GestureCommandBinding(HandGestureType.ThumbUp, GestureCommandId.ApproveSafeZone, HandSide.Right),
                new GestureCommandBinding(HandGestureType.ThumbDown, GestureCommandId.SendToQuarantine, HandSide.Right),
                new GestureCommandBinding(HandGestureType.OpenPalmForward, GestureCommandId.HoldCivilian, HandSide.Both)
            };
        }
    }
}
