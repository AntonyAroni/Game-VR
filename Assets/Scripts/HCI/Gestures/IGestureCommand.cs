using System;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Identificador de las órdenes clínicas que el oficial puede emitir con las manos.
    /// </summary>
    public enum GestureCommandId
    {
        None,
        /// <summary>Ordena al civil elevar los brazos para examinar axilas y costados.</summary>
        RaiseArms,
        /// <summary>Ordena al civil bajar los brazos a postura de reposo.</summary>
        LowerArms,
        /// <summary>Alterna entre brazos arriba y abajo con un mismo gesto.</summary>
        ToggleRaiseArms,
        /// <summary>Ordena descubrir / cubrir el torso para inspección dermatológica.</summary>
        ToggleExposeTorso,
        /// <summary>Emite veredicto de paso a zona segura.</summary>
        ApproveSafeZone,
        /// <summary>Emite veredicto de envío a cuarentena.</summary>
        SendToQuarantine,
        /// <summary>Detiene al civil en el sitio (gesto universal de "alto").</summary>
        HoldCivilian,
        /// <summary>Conmuta la linterna clínica entre luz blanca y lámpara de Wood UV.</summary>
        ToggleUvFlashlight
    }

    /// <summary>
    /// Contrato base de toda orden ejecutable mediante un gesto manual.
    /// Principio Abierto/Cerrado (OCP) y Sustitución de Liskov (LSP): el despachador
    /// sólo conoce esta interfaz, de modo que se pueden añadir órdenes nuevas
    /// sin modificar ni el reconocedor de gestos ni el propio despachador.
    /// </summary>
    public interface IGestureCommand
    {
        /// <summary>Identificador estable de la orden.</summary>
        GestureCommandId Id { get; }

        /// <summary>Nombre legible mostrado en el HUD de retroalimentación.</summary>
        string DisplayName { get; }

        /// <summary>Indica si en este instante existen las referencias de escena necesarias.</summary>
        bool CanExecute(GestureCommandContext context);

        /// <summary>Ejecuta la orden. Devuelve <c>true</c> si realmente produjo un cambio de estado.</summary>
        bool Execute(GestureCommandContext context, HandSide hand);
    }

    /// <summary>
    /// Enlace configurable desde el Inspector entre un gesto y la orden que dispara.
    /// Permite reasignar el vocabulario gestual sin recompilar.
    /// </summary>
    [Serializable]
    public class GestureCommandBinding
    {
        [SerializeField] private HandGestureType gesture = HandGestureType.None;
        [SerializeField] private GestureCommandId command = GestureCommandId.None;
        [SerializeField] private HandSide hand = HandSide.Both;
        [SerializeField] private bool isEnabled = true;

        public HandGestureType Gesture => gesture;
        public GestureCommandId Command => command;
        public HandSide Hand => hand;
        public bool IsEnabled => isEnabled;

        public GestureCommandBinding() { }

        public GestureCommandBinding(HandGestureType gestureType, GestureCommandId commandId, HandSide allowedHand)
        {
            gesture = gestureType;
            command = commandId;
            hand = allowedHand;
            isEnabled = true;
        }

        /// <summary>Comprueba si este enlace responde al gesto detectado en la mano indicada.</summary>
        public bool Handles(HandGestureType detectedGesture, HandSide detectedHand)
            => isEnabled && gesture == detectedGesture && (hand == HandSide.Both || hand == detectedHand);
    }
}
