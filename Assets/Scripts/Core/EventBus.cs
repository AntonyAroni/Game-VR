using System;
using UnityEngine;

namespace ZombieCheckpoint.Core
{
    public enum HandSide
    {
        Left,
        Right,
        Both
    }

    /// <summary>
    /// Bus central de eventos desacoplados (Publish-Subscribe) para todo el sistema de inspección.
    /// Principio de Inversión de Dependencias (DIP): Los sistemas se comunican mediante señales sin conocerse entre sí.
    /// </summary>
    public static class EventBus
    {
        // --- Eventos de Flujo de Supervivientes ---
        public static event Action<object> OnSurvivorArrived;
        public static event Action OnSurvivorDeparted;

        public static void TriggerSurvivorArrived(object survivor) => OnSurvivorArrived?.Invoke(survivor);
        public static void TriggerSurvivorDeparted() => OnSurvivorDeparted?.Invoke();

        // --- Eventos de Detección de Síntomas (IHC Feedback) ---
        public static event Action<string, string> OnSymptomDiscovered; // (symptomName, feedbackMessage)
        public static void TriggerSymptomDiscovered(string symptomName, string feedbackMessage) 
            => OnSymptomDiscovered?.Invoke(symptomName, feedbackMessage);

        // --- Eventos de Herramientas ---
        public static event Action<string, GameObject> OnToolApplied; // (toolName, targetBodyPart)
        public static void TriggerToolApplied(string toolName, GameObject targetBodyPart) 
            => OnToolApplied?.Invoke(toolName, targetBodyPart);

        // --- Eventos de Veredicto y Evaluación ---
        public static event Action<VerdictType> OnVerdictSubmitted;
        public static event Action<bool, string> OnEvaluationResult; // (isCorrect, explanation)

        public static void TriggerVerdictSubmitted(VerdictType verdict) => OnVerdictSubmitted?.Invoke(verdict);
        public static void TriggerEvaluationResult(bool isCorrect, string explanation) 
            => OnEvaluationResult?.Invoke(isCorrect, explanation);

        // --- Eventos de Retroalimentación Háptica ---
        public static event Action<HandSide, float, float> OnHapticImpulseRequested; // (hand, amplitude, durationSeconds)
        public static void RequestHapticImpulse(HandSide hand, float amplitude, float durationSeconds) 
            => OnHapticImpulseRequested?.Invoke(hand, amplitude, durationSeconds);

        // --- Eventos de Audio Espacial ---
        public static event Action<string, Vector3, float> OnSpatialAudioRequested; // (clipId, worldPosition, volume)
        public static void RequestSpatialAudio(string clipId, Vector3 worldPosition, float volume = 1.0f)
            => OnSpatialAudioRequested?.Invoke(clipId, worldPosition, volume);

        // --- Eventos de Auscultación y Signos Vitales (ECG) ---
        public static event Action<bool, float, bool> OnHeartbeatAuscultationStateChanged; // (isAuscultating, targetBpm, isAbnormal)
        public static void TriggerHeartbeatAuscultation(bool isAuscultating, float targetBpm, bool isAbnormal)
            => OnHeartbeatAuscultationStateChanged?.Invoke(isAuscultating, targetBpm, isAbnormal);

        // --- Eventos de Linterna y Modo Forense UV ---
        public static event Action<bool> OnFlashlightModeChanged; // (isUvActive)
        public static void TriggerFlashlightModeChanged(bool isUvActive)
            => OnFlashlightModeChanged?.Invoke(isUvActive);

        // --- Eventos de Comandos Gestuales (Módulo de Manos Libres) ---
        public static event Action<HandSide, string, float> OnHandGestureProgress; // (hand, gestureName, progress01)
        public static event Action<HandSide, string> OnHandGesturePerformed;       // (hand, gestureName)
        public static event Action<string, bool> OnHandCommandExecuted;            // (commandLabel, wasAccepted)

        public static void TriggerHandGestureProgress(HandSide hand, string gestureName, float progress01)
            => OnHandGestureProgress?.Invoke(hand, gestureName, progress01);

        public static void TriggerHandGesturePerformed(HandSide hand, string gestureName)
            => OnHandGesturePerformed?.Invoke(hand, gestureName);

        public static void TriggerHandCommandExecuted(string commandLabel, bool wasAccepted)
            => OnHandCommandExecuted?.Invoke(commandLabel, wasAccepted);

        /// <summary>
        /// Limpia todas las suscripciones para evitar fugas de memoria al recargar escenas.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnSurvivorArrived = null;
            OnSurvivorDeparted = null;
            OnSymptomDiscovered = null;
            OnToolApplied = null;
            OnVerdictSubmitted = null;
            OnEvaluationResult = null;
            OnHapticImpulseRequested = null;
            OnSpatialAudioRequested = null;
            OnHeartbeatAuscultationStateChanged = null;
            OnFlashlightModeChanged = null;
            OnHandGestureProgress = null;
            OnHandGesturePerformed = null;
            OnHandCommandExecuted = null;
        }
    }
}
