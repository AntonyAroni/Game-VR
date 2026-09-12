using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Survivors.Symptoms
{
    /// <summary>
    /// Síntoma ocular de respuesta pupilar a la luz (Pupil Light Reflex).
    /// Principio de IHC: Feedback Multimodal e Interacción Diegética.
    /// Al apuntar el haz de la linterna hacia los ojos/cabeza del superviviente:
    /// - Ciudadano Sano: Reflejo pupilar normal (las pupilas responden y se contraen).
    /// - Sujeto Infectado: Midriasis fija o respuesta fotomotora perezosa/nula (pupilas dilatadas e inmóviles).
    /// </summary>
    public class PupilSymptom : MonoBehaviour, ISymptom
    {
        [Header("Configuración del Síntoma")]
        [SerializeField] private string symptomName = "Reflejo Pupilar";
        [SerializeField] private bool isPositiveForInfection = false;

        public string SymptomName => symptomName;
        public bool IsPositiveForInfection => isPositiveForInfection;
        public bool IsDiscovered { get; private set; }

        private float lastExamineTime = -10f;
        [SerializeField] private float examineCooldown = 2.0f;

        public void Initialize(bool isAbnormal)
        {
            isPositiveForInfection = isAbnormal;
            IsDiscovered = false;
            lastExamineTime = -10f;
        }

        public bool TryExamine(string toolName, out string feedbackMessage)
        {
            if (toolName != "Flashlight")
            {
                feedbackMessage = "Se requiere una fuente de luz (Linterna) para evaluar el reflejo pupilar.";
                return false;
            }

            if (Time.time - lastExamineTime < examineCooldown)
            {
                feedbackMessage = "";
                return false;
            }
            lastExamineTime = Time.time;

            // Feedback auditivo espacializado de escaneo médico clínico
            EventBus.RequestSpatialAudio("pupil_scan_beep", transform.position, 1.0f);

            if (isPositiveForInfection)
            {
                DiscoverSymptom("¡Midriasis bilateral arreactiva confirmada con linterna!");
                feedbackMessage = "¡ANOMALÍA OCULAR: Pupilas totalmente dilatadas e inmóviles ante la luz!";
                return true;
            }
            else
            {
                EventBus.RequestHapticImpulse(HandSide.Both, 0.2f, 0.08f);
                feedbackMessage = "Reflejo fotomotor normal: Las pupilas se contraen simétricamente ante la luz.";
                Debug.Log("[Inspección Pupilar] Reflejo normal: Pupilas reactivas a la luz.");
                return true;
            }
        }

        private void DiscoverSymptom(string message)
        {
            if (IsDiscovered) return;
            IsDiscovered = true;
            EventBus.TriggerSymptomDiscovered(symptomName, message);
            EventBus.RequestHapticImpulse(HandSide.Both, 0.55f, 0.2f);
        }
    }
}
