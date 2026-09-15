using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Gestiona la retroalimentación háptica (vibración de mandos) en Realidad Virtual.
    /// Principio de IHC: Feedback Táctil. Transmite sensaciones mecánicas al usuario (latidos, clic de botones, impacto del sello).
    /// Compatible nativamente con XRI 3.x (HapticImpulsePlayer) y OpenXR InputDevices.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        [Header("Calibración Háptica Global")]
        [Range(0f, 1f)] [SerializeField] private float globalIntensityModifier = 1.0f;

        [Header("XRI 3.x Haptic Players (Opcional - Se auto-detectan)")]
        [SerializeField] private HapticImpulsePlayer leftHapticPlayer;
        [SerializeField] private HapticImpulsePlayer rightHapticPlayer;

        private void Awake()
        {
            FindHapticPlayersIfNull();
        }

        private void FindHapticPlayersIfNull()
        {
            if (leftHapticPlayer != null && rightHapticPlayer != null) return;

            var players = FindObjectsByType<HapticImpulsePlayer>();
            foreach (var p in players)
            {
                string objName = p.gameObject.name.ToLower();
                if (objName.Contains("left") && leftHapticPlayer == null) leftHapticPlayer = p;
                else if (objName.Contains("right") && rightHapticPlayer == null) rightHapticPlayer = p;
            }
        }

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
                TriggerLeftHaptic(finalAmplitude, durationSeconds);
            }

            if (hand == HandSide.Right || hand == HandSide.Both)
            {
                TriggerRightHaptic(finalAmplitude, durationSeconds);
            }
        }

        private void TriggerLeftHaptic(float amplitude, float duration)
        {
            if (leftHapticPlayer != null)
            {
                leftHapticPlayer.SendHapticImpulse(amplitude, duration);
                return;
            }

            // Fallback a InputDevice
            TriggerDeviceHaptic(XRNode.LeftHand, amplitude, duration);
        }

        private void TriggerRightHaptic(float amplitude, float duration)
        {
            if (rightHapticPlayer != null)
            {
                rightHapticPlayer.SendHapticImpulse(amplitude, duration);
                return;
            }

            // Fallback a InputDevice
            TriggerDeviceHaptic(XRNode.RightHand, amplitude, duration);
        }

        private void TriggerDeviceHaptic(XRNode node, float amplitude, float duration)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid)
            {
                device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
