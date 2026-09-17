using UnityEngine;
using UnityEngine.InputSystem;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Inyector de gestos por teclado para iterar el módulo de manos sin ponerse el visor.
    /// Principio de Responsabilidad Única (SRP): sólo traduce teclas a gestos sintéticos;
    /// el reconocimiento articular real sigue viviendo en <see cref="HandGestureRecognizer"/>.
    ///
    /// Se autodestruye fuera del Editor siguiendo la misma política de aislamiento que
    /// <c>XRSimulatorDesktopEnhancer</c>, para que la APK de Meta Quest nunca acepte
    /// órdenes sintéticas de teclado.
    /// </summary>
    public class HandGestureKeyboardSimulator : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private HandGestureRecognizer recognizer;

        [Header("Mano Simulada")]
        [Tooltip("Mantener Shift Izquierdo emite el gesto con la mano izquierda en lugar de la derecha.")]
        [SerializeField] private bool leftShiftSelectsLeftHand = true;

        private void Awake()
        {
#if !UNITY_EDITOR
            Destroy(this);
            return;
#else
            if (recognizer == null && !TryGetComponent(out recognizer))
            {
                recognizer = FindAnyObjectByType<HandGestureRecognizer>();
            }
#endif
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (recognizer == null) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            HandSide hand = leftShiftSelectsLeftHand && keyboard.leftShiftKey.isPressed
                ? HandSide.Left
                : HandSide.Right;

            if (keyboard.digit1Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.OpenPalmUp);
            else if (keyboard.digit2Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.OpenPalmDown);
            else if (keyboard.digit3Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.PointIndex);
            else if (keyboard.digit4Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.ThumbUp);
            else if (keyboard.digit5Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.ThumbDown);
            else if (keyboard.digit6Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.OpenPalmForward);
            else if (keyboard.digit7Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.PinchIndex);
            else if (keyboard.digit8Key.wasPressedThisFrame) recognizer.InjectGesture(hand, HandGestureType.Fist);
        }
#endif
    }
}
