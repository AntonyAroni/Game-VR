using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Paquete de retroalimentación que el reconocedor publica en cada evaluación.
    /// Principio de Responsabilidad Única (SRP): sólo transporta el estado visualizable
    /// de una mano (gesto en curso, progreso y anclaje espacial de la palma).
    ///
    /// Justificación IHC: el indicador se dibuja sobre la propia mano del usuario, así que
    /// la capa visual necesita la pose de la palma, no sólo el porcentaje de confirmación.
    /// </summary>
    public struct HandGestureFeedback
    {
        /// <summary>Mano descrita por esta retroalimentación.</summary>
        public HandSide Side;

        /// <summary>Gesto candidato en curso, o <see cref="HandGestureType.None"/> si no hay ninguno.</summary>
        public HandGestureType Gesture;

        /// <summary>Nombre legible del gesto candidato.</summary>
        public string Label;

        /// <summary>Avance de confirmación del dwell, de 0 a 1.</summary>
        public float Progress;

        /// <summary>Indica si el seguimiento articular de esta mano está activo.</summary>
        public bool IsHandTracked;

        /// <summary>Indica si la mano sostiene un objeto (sus posturas no se interpretan como órdenes).</summary>
        public bool IsHoldingObject;

        /// <summary>Posición de la palma en espacio de mundo (anclaje del indicador).</summary>
        public Vector3 PalmPosition;

        /// <summary>Normal de la palma en espacio de mundo.</summary>
        public Vector3 PalmNormal;

        /// <summary>Dirección muñeca → nudillos en espacio de mundo.</summary>
        public Vector3 PalmForward;
    }
}
