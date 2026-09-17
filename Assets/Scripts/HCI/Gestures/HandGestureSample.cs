using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Fotografía normalizada del estado de una mano en un instante concreto.
    /// Principio de Responsabilidad Única (SRP): sólo transporta datos ya normalizados
    /// (curvatura de dedos 0..1 y direcciones en espacio de mundo), nunca decide gestos.
    /// Se declara como <c>struct</c> para evitar asignaciones de memoria por fotograma.
    /// </summary>
    public struct HandGestureSample
    {
        /// <summary>Mano a la que pertenece la muestra.</summary>
        public HandSide Side;

        /// <summary>Curvatura total del pulgar (0 = extendido, 1 = totalmente flexionado).</summary>
        public float ThumbCurl;

        /// <summary>Curvatura total del índice.</summary>
        public float IndexCurl;

        /// <summary>Curvatura total del medio.</summary>
        public float MiddleCurl;

        /// <summary>Curvatura total del anular.</summary>
        public float RingCurl;

        /// <summary>Curvatura total del meñique.</summary>
        public float LittleCurl;

        /// <summary>Cercanía entre yema del índice y yema del pulgar (0 = separados, 1 = pinza cerrada).</summary>
        public float IndexPinch;

        /// <summary>Normal de la palma en espacio de mundo (hacia dónde "mira" la palma).</summary>
        public Vector3 PalmNormal;

        /// <summary>Dirección muñeca → nudillos en espacio de mundo (hacia dónde apuntan los dedos).</summary>
        public Vector3 PalmForward;

        /// <summary>Dirección del pulgar en espacio de mundo.</summary>
        public Vector3 ThumbDirection;

        /// <summary>Dirección del índice en espacio de mundo.</summary>
        public Vector3 IndexDirection;

        /// <summary>Dirección de la mirada del jugador en espacio de mundo (referencia egocéntrica).</summary>
        public Vector3 HeadForward;

        /// <summary>Posición de la palma en espacio de mundo (anclaje de retroalimentación visual y háptica).</summary>
        public Vector3 PalmPosition;

        /// <summary>
        /// Devuelve la dirección de mundo asociada a una restricción de orientación,
        /// junto a la dirección de referencia contra la que debe compararse.
        /// </summary>
        public readonly bool TryResolveOrientation(GestureOrientation orientation, out Vector3 handAxis, out Vector3 reference)
        {
            switch (orientation)
            {
                case GestureOrientation.PalmUp:
                    handAxis = PalmNormal; reference = Vector3.up; return true;
                case GestureOrientation.PalmDown:
                    handAxis = PalmNormal; reference = Vector3.down; return true;
                case GestureOrientation.PalmAwayFromFace:
                    handAxis = PalmNormal; reference = HeadForward; return true;
                case GestureOrientation.PalmTowardsFace:
                    handAxis = PalmNormal; reference = -HeadForward; return true;
                case GestureOrientation.ThumbUp:
                    handAxis = ThumbDirection; reference = Vector3.up; return true;
                case GestureOrientation.ThumbDown:
                    handAxis = ThumbDirection; reference = Vector3.down; return true;
                case GestureOrientation.IndexForward:
                    handAxis = IndexDirection; reference = HeadForward; return true;
                default:
                    handAxis = Vector3.zero; reference = Vector3.zero; return false;
            }
        }
    }
}
