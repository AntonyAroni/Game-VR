using UnityEngine;

namespace ZombieCheckpoint.HCI.Grip
{
    /// <summary>
    /// Direcciones anatómicas de la mano usadas para describir cómo se asienta un objeto.
    /// Se expresan igual para ambas manos (el pulgar siempre es "ThumbSide"), de modo que
    /// un mismo perfil de agarre produce un asiento correcto en la mano izquierda y en la derecha.
    /// </summary>
    public enum HandAxis
    {
        /// <summary>De la muñeca hacia las puntas de los dedos extendidos.</summary>
        FingersForward,

        /// <summary>De los nudillos hacia la muñeca.</summary>
        TowardWrist,

        /// <summary>Saliendo de la palma (hacia el objeto sostenido).</summary>
        PalmOut,

        /// <summary>Hacia el dorso de la mano.</summary>
        PalmIn,

        /// <summary>Hacia el pulgar y el índice (el "cañón" de un puño cerrado).</summary>
        ThumbSide,

        /// <summary>Hacia el meñique.</summary>
        PinkySide
    }

    /// <summary>
    /// Conversión de ejes anatómicos al marco del ancla de palma.
    /// El marco del ancla es: Z = dedos, Y = normal de la palma, X = Y × Z.
    /// Como las manos son imágenes especulares, X apunta al pulgar en la mano derecha
    /// y al meñique en la izquierda; esta utilidad oculta esa asimetría.
    /// </summary>
    public static class HandAxisUtility
    {
        /// <summary>Devuelve el vector unitario del eje anatómico en el marco local del ancla.</summary>
        public static Vector3 ToAnchorSpace(HandAxis axis, bool isLeftHand)
        {
            float thumbSign = isLeftHand ? -1f : 1f;
            switch (axis)
            {
                case HandAxis.FingersForward: return Vector3.forward;
                case HandAxis.TowardWrist: return Vector3.back;
                case HandAxis.PalmOut: return Vector3.up;
                case HandAxis.PalmIn: return Vector3.down;
                case HandAxis.ThumbSide: return Vector3.right * thumbSign;
                case HandAxis.PinkySide: return Vector3.left * thumbSign;
                default: return Vector3.forward;
            }
        }
    }
}
