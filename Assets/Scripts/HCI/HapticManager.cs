using UnityEngine;
using UnityEngine.XR;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Gestiona la retroalimentación háptica (vibración de mandos) en Realidad Virtual.
    /// Principio de IHC: Feedback Táctil. Transmite sensaciones mecánicas al usuario (latidos, clic de botones, impacto del sello).
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        [Header("Calibración Háptica Global")]
        [Range(0f, 1f)] [SerializeField] private float globalIntensityModifier = 1.0f;

        private void OnEnable()
        {
            EventBus.OnHapticImpulseRequested += HandleHapticImpulse;
        }

        private void OnDisable()
        {
            EventBus.OnHapticImpulseRequested -= HandleHapticImpulse;
        }

        private void HandleHapticImpulse(HandSide hand, float amplitude, float durationSeconds)
        {
            float finalAmplitude = Mathf.Clamp01(amplitude * globalIntensityModifier);

            if (hand == HandSide.Left || hand == HandSide.Both)
            {
                TriggerDeviceHaptic(XRNode.LeftHand, finalAmplitude, durationSeconds);
            }

            if (hand == HandSide.Right || hand == HandSide.Both)
            {
                TriggerDeviceHaptic(XRNode.RightHand, finalAmplitude, durationSeconds);
            }
        }

        private void TriggerDeviceHaptic(XRNode node, float amplitude, float duration)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid)
            {
                // Canal 0 es el actuador háptico principal en Meta Quest Touch controllers
                device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
