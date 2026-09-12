using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    [Serializable]
    public class InspectionRecord
    {
        public int subjectIndex;
        public float inspectionDurationSeconds;
        public int symptomsDiscoveredCount;
        public VerdictType playerVerdict;
        public bool wasTrulyInfected;
        public bool wasVerdictCorrect;
    }

    /// <summary>
    /// Registrador de métricas de rendimiento y usabilidad para evaluación formal en IHC.
    /// Mide: Carga cognitiva indirecta (tiempo por decisión), tasa de error (falsos positivos/negativos)
    /// y eficacia del descubrimiento de síntomas mediante affordances.
    /// </summary>
    public class UsabilityMetricsTracker : MonoBehaviour
    {
        [Header("Estadísticas en Tiempo Real")]
        [SerializeField] private int totalInspections = 0;
        [SerializeField] private int correctVerdicts = 0;
        [SerializeField] private int falsePositives = 0; // Sano enviado a cuarentena
        [SerializeField] private int falseNegatives = 0; // Infectado dejado pasar a zona segura
        [SerializeField] private float averageInspectionTime = 0f;

        [Header("Uso de Herramientas Multimodales")]
        [SerializeField] private int flashlightUses = 0;
        [SerializeField] private int stethoscopeUses = 0;
        [SerializeField] private int documentUses = 0;

        private float currentInspectionStartTime;
        private int currentSymptomsDiscovered;
        private readonly List<InspectionRecord> sessionHistory = new List<InspectionRecord>();

        public IReadOnlyList<InspectionRecord> SessionHistory => sessionHistory;
        public int TotalInspections => totalInspections;
        public int CorrectVerdicts => correctVerdicts;
        public float AverageInspectionTime => averageInspectionTime;

        private void OnEnable()
        {
            EventBus.OnSurvivorArrived += HandleSurvivorArrived;
            EventBus.OnSymptomDiscovered += HandleSymptomDiscovered;
            EventBus.OnEvaluationResult += HandleEvaluationResult;
            EventBus.OnToolApplied += HandleToolApplied;
        }

        private void OnDisable()
        {
            EventBus.OnSurvivorArrived -= HandleSurvivorArrived;
            EventBus.OnSymptomDiscovered -= HandleSymptomDiscovered;
            EventBus.OnEvaluationResult -= HandleEvaluationResult;
            EventBus.OnToolApplied -= HandleToolApplied;
        }

        private void HandleToolApplied(string toolName, GameObject target)
        {
            if (string.IsNullOrEmpty(toolName)) return;

            string lower = toolName.ToLower();
            if (lower.Contains("flashlight")) flashlightUses++;
            else if (lower.Contains("stethoscope")) stethoscopeUses++;
            else if (lower.Contains("doc")) documentUses++;
        }

        private void HandleSurvivorArrived(object survivor)
        {
            currentInspectionStartTime = Time.time;
            currentSymptomsDiscovered = 0;
        }

        private void HandleSymptomDiscovered(string symptomName, string feedback)
        {
            currentSymptomsDiscovered++;
        }

        private void HandleEvaluationResult(bool isCorrect, string explanation)
        {
            float duration = Time.time - currentInspectionStartTime;
            totalInspections++;

            if (isCorrect)
            {
                correctVerdicts++;
            }

            // Actualizar promedio
            averageInspectionTime = ((averageInspectionTime * (totalInspections - 1)) + duration) / totalInspections;

            InspectionRecord record = new InspectionRecord
            {
                subjectIndex = totalInspections,
                inspectionDurationSeconds = duration,
                symptomsDiscoveredCount = currentSymptomsDiscovered,
                wasVerdictCorrect = isCorrect
            };

            sessionHistory.Add(record);
            Debug.Log($"[IHC Metrics] Sujeto #{totalInspections} procesado en {duration:F2}s. Correcto: {isCorrect}. Síntomas hallados: {currentSymptomsDiscovered}");
        }

        public void RegisterVerdictDetails(bool wasTrulyInfected, VerdictType verdict)
        {
            if (verdict == VerdictType.SendToQuarantine && !wasTrulyInfected)
            {
                falsePositives++;
            }
            else if (verdict == VerdictType.ApprovedSafeZone && wasTrulyInfected)
            {
                falseNegatives++;
            }
        }

        /// <summary>
        /// Genera el informe cuantitativo diegético de usabilidad y rendimiento para la pantalla del monitor.
        /// </summary>
        public string GenerateShiftReport()
        {
            float accuracy = totalInspections > 0 ? ((float)correctVerdicts / totalInspections) * 100f : 0f;
            string grade = accuracy >= 80f ? "<color=#00ff66>EXCELENTE (Apto para el Servicio)</color>"
                         : accuracy >= 60f ? "<color=#ffcc00>ACEPTABLE (Riesgo Moderado)</color>"
                         : "<color=#ff3333>CRÍTICO (Fuga de Infección)</color>";

            return $"<color=#33ccff><b>=== REPORTE DE TURNO (MÉTRICAS IHC) ===</b></color>\n" +
                   $"------------------------------------\n" +
                   $"• <b>Sujetos Evaluados:</b> {totalInspections}\n" +
                   $"• <b>Precisión Global:</b> {accuracy:F1}% ({correctVerdicts}/{totalInspections})\n" +
                   $"• <b>Tiempo Medio de Decisión:</b> {averageInspectionTime:F1} s\n" +
                   $"• <b>Falsos Positivos:</b> {falsePositives} (Sanos aislados)\n" +
                   $"• <b>Falsos Negativos:</b> {falseNegatives} (Infectados admitidos)\n" +
                   $"• <b>Uso de Herramientas:</b> Linterna: {flashlightUses} | Fonendoscopio: {stethoscopeUses}\n" +
                   $"• <b>Calificación Clínica:</b> {grade}\n\n" +
                   $"<i>Iniciando siguiente bloque de evaluación...</i>";
        }
    }
}
