using UnityEngine;
using UnityEngine.XR.Hands;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Cálculo del marco anatómico de la palma a partir del esqueleto de XR Hands.
    /// Principio de Responsabilidad Única (SRP): sólo deriva centro, dirección de los dedos
    /// y normal de la palma; lo comparten el reconocedor de gestos y el agarre natural.
    ///
    /// Todos los resultados están en espacio de seguimiento (el mismo espacio local que usan
    /// los <c>TrackedPoseDriver</c> de las manos bajo el Camera Offset del XR Origin).
    /// </summary>
    public static class HandPalmFrame
    {
        /// <summary>
        /// Obtiene el marco de la palma: centro, dirección muñeca → nudillos y normal saliente
        /// de la palma. La normal se deriva con producto vectorial y se invierte en la mano
        /// derecha por la simetría anatómica especular entre ambas manos.
        /// </summary>
        public static bool TryGetPalmFrame(
            XRHand hand,
            bool isRightHand,
            out Vector3 palmPosition,
            out Vector3 fingersDirection,
            out Vector3 palmNormal)
        {
            palmPosition = Vector3.zero;
            fingersDirection = Vector3.forward;
            palmNormal = Vector3.up;

            if (!hand.isTracked) return false;
            if (!hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist)) return false;
            if (!hand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out Pose middleProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose indexProximal)) return false;
            if (!hand.GetJoint(XRHandJointID.LittleProximal).TryGetPose(out Pose littleProximal)) return false;

            Vector3 forward = middleProximal.position - wrist.position;
            Vector3 across = littleProximal.position - indexProximal.position;
            if (forward.sqrMagnitude < 1e-8f || across.sqrMagnitude < 1e-8f) return false;

            fingersDirection = forward.normalized;
            palmNormal = Vector3.Cross(fingersDirection, across.normalized) * (isRightHand ? -1f : 1f);
            if (palmNormal.sqrMagnitude < 1e-8f) return false;
            palmNormal.Normalize();

            // La articulación Palm no la publican todos los proveedores: el punto medio
            // muñeca-nudillos es un respaldo anatómicamente fiel.
            palmPosition = hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm)
                ? palm.position
                : Vector3.Lerp(wrist.position, middleProximal.position, 0.5f);

            return true;
        }
    }
}
