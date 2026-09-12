using System.Collections;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Survivors.Symptoms
{
    /// <summary>
    /// Síntoma cardíaco y respiratorio evaluable mediante auscultación (estetoscopio).
    /// Principio de IHC: Feedback Multimodal Sincronizado. Cada latido produce simultáneamente
    /// un sonido 3D posicionado en el pecho y un pulso háptico en el mando del visor.
    /// </summary>
    public class HeartbeatSymptom : MonoBehaviour, ISymptom
    {
        [Header("Configuración Cardíaca")]
        [SerializeField] private string symptomName = "Ritmo Cardíaco / Respiración";
        [SerializeField] private bool isPositiveForInfection = false;
        
        [Header("Parámetros de Simulación")]
        [SerializeField] private float normalBPM = 72f;
        [SerializeField] private float infectedBPM = 150f;
        [SerializeField] private string normalAudioClipId = "heartbeat_normal";
        [SerializeField] private string infectedAudioClipId = "heartbeat_infected";

        public string SymptomName => symptomName;
        public bool IsPositiveForInfection => isPositiveForInfection;
        public bool IsDiscovered { get; private set; }

        private Coroutine pulseRoutine;
        private bool isCurrentlyBeingAuscultated;
        private HandSide activeListeningHand = HandSide.Right;

        public void Initialize(bool isInfected)
        {
            isPositiveForInfection = isInfected;
            IsDiscovered = false;
            StopAuscultation();
        }

        public bool TryExamine(string toolName, out string feedbackMessage)
        {
            if (toolName != "Stethoscope")
            {
                feedbackMessage = "Se requiere un estetoscopio para auscultar el tórax.";
                return false;
            }

            IsDiscovered = true;
            if (isPositiveForInfection)
            {
                feedbackMessage = "¡Taquicardia severa y estertores pulmonares irregulares!";
                EventBus.TriggerSymptomDiscovered(symptomName, feedbackMessage);
            }
            else
            {
                feedbackMessage = "Ritmo sinusal regular (aprox. 72 lpm). Pulmones limpios.";
            }

            return true;
        }

        public void StartAuscultation(HandSide handHoldingTool)
        {
            if (isCurrentlyBeingAuscultated) return;
            isCurrentlyBeingAuscultated = true;
            activeListeningHand = handHoldingTool;
            pulseRoutine = StartCoroutine(HeartbeatLoop());
        }

        public void StopAuscultation()
        {
            isCurrentlyBeingAuscultated = false;
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
        }

        private IEnumerator HeartbeatLoop()
        {
            float targetBPM = isPositiveForInfection ? infectedBPM : normalBPM;
            float beatInterval = 60f / targetBPM;
            string clipId = isPositiveForInfection ? infectedAudioClipId : normalAudioClipId;

            while (isCurrentlyBeingAuscultated)
            {
                // Lub-dub: Primer pulso (sístole)
                EventBus.RequestSpatialAudio(clipId, transform.position, 1.0f);
                EventBus.RequestHapticImpulse(activeListeningHand, isPositiveForInfection ? 0.7f : 0.35f, 0.08f);

                yield return new WaitForSeconds(0.12f);

                // Segundo pulso (diástole)
                EventBus.RequestHapticImpulse(activeListeningHand, isPositiveForInfection ? 0.5f : 0.2f, 0.05f);

                yield return new WaitForSeconds(Mathf.Max(0.1f, beatInterval - 0.12f));
            }
        }

        private void OnDisable()
        {
            StopAuscultation();
        }
    }
}
