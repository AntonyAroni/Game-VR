using System;
using System.Text;
using TMPro;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Gestiona la lista de verificación médica y tareas clínicas diegéticas (Checklist).
    /// Principio de IHC (Don Norman - Reducción de Carga Cognitiva y Conocimiento en el Mundo):
    /// - Alivia la memoria de trabajo del evaluador mostrando claramente qué pasos del examen clínico
    ///   han sido realizados y cuáles quedan pendientes.
    /// - Retroalimentación multimodal: marcado con casilla [✔], texto tachado suave en verde
    ///   y sonido procedural de lápiz sobre papel ("pencil_check").
    /// - Doble affordance: Portapapeles físico diegético en el mostrador + consulta rápida en la muñeca (reloj).
    /// </summary>
    [DisallowMultipleComponent]
    public class InspectionChecklistController : MonoBehaviour
    {
        public static InspectionChecklistController Instance { get; private set; }

        [Header("Referencias Visuales")]
        [SerializeField] private TextMeshProUGUI checklistText;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Configuración de Tareas")]
        [SerializeField] private string[] taskDescriptions = new string[]
        {
            "Auscultar corazón (tórax)",
            "Evaluar ojos y luz UV",
            "Examinar brazos y marcas",
            "Revisar torso (descubrir polo)",
            "Sellar pasaporte (Aprobado/Cuarentena)"
        };

        private readonly bool[] taskCompleted = new bool[5];
        private readonly StringBuilder summaryBuilder = new StringBuilder(256);

        public bool IsTaskComplete(int index) => (index >= 0 && index < taskCompleted.Length) && taskCompleted[index];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureVisualComponents();
        }

        private void OnEnable()
        {
            EventBus.OnSurvivorArrived += HandleSurvivorArrived;
            EventBus.OnHeartbeatAuscultationStateChanged += HandleHeartbeatStateChanged;
            EventBus.OnToolApplied += HandleToolApplied;
            EventBus.OnSymptomDiscovered += HandleSymptomDiscovered;
            EventBus.OnHandCommandExecuted += HandleHandCommandExecuted;
            EventBus.OnVerdictSubmitted += HandleVerdictSubmitted;

            UpdateVisualDisplay();
        }

        private void OnDisable()
        {
            EventBus.OnSurvivorArrived -= HandleSurvivorArrived;
            EventBus.OnHeartbeatAuscultationStateChanged -= HandleHeartbeatStateChanged;
            EventBus.OnToolApplied -= HandleToolApplied;
            EventBus.OnSymptomDiscovered -= HandleSymptomDiscovered;
            EventBus.OnHandCommandExecuted -= HandleHandCommandExecuted;
            EventBus.OnVerdictSubmitted -= HandleVerdictSubmitted;

            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            UpdateVisualDisplay();
        }

        /// <summary>
        /// Reinicia las casillas de triaje para la llegada de un nuevo civil.
        /// </summary>
        public void ResetChecklist()
        {
            for (int i = 0; i < taskCompleted.Length; i++)
            {
                taskCompleted[i] = false;
            }
            UpdateVisualDisplay();
        }

        /// <summary>
        /// Marca una tarea como realizada y genera retroalimentación sonora de trazo de lápiz.
        /// </summary>
        public void MarkTaskCompleted(int index)
        {
            if (index < 0 || index >= taskCompleted.Length) return;
            if (taskCompleted[index]) return;

            taskCompleted[index] = true;

            // Feedback auditivo: sonido realista de lápiz marcando la casilla
            EventBus.RequestSpatialAudio("pencil_check", transform.position, 0.9f);

            UpdateVisualDisplay();
            Debug.Log($"[Checklist] Tarea {index + 1} completada: {taskDescriptions[index]}");
        }

        private void HandleSurvivorArrived(object _)
        {
            ResetChecklist();
        }

        private void HandleHeartbeatStateChanged(bool isAuscultating, float bpm, bool isAbnormal)
        {
            if (isAuscultating)
            {
                MarkTaskCompleted(0); // Tarea 1: Auscultar corazón
            }
        }

        private void HandleToolApplied(string toolName, GameObject target)
        {
            if (string.IsNullOrEmpty(toolName)) return;

            if (toolName.Equals("Stethoscope", StringComparison.OrdinalIgnoreCase))
            {
                MarkTaskCompleted(0); // Auscultar
            }
            else if (toolName.Equals("Flashlight", StringComparison.OrdinalIgnoreCase))
            {
                MarkTaskCompleted(1); // Ojos y luz UV
            }
        }

        private void HandleSymptomDiscovered(string symptomName, string feedback)
        {
            if (string.IsNullOrEmpty(symptomName)) return;

            string nameUpper = symptomName.ToUpperInvariant();
            if (nameUpper.Contains("CORAZÓN") || nameUpper.Contains("LATIDO"))
            {
                MarkTaskCompleted(0);
            }
            else if (nameUpper.Contains("PUPIL") || nameUpper.Contains("OJO") || nameUpper.Contains("UV"))
            {
                MarkTaskCompleted(1);
            }
            else if (nameUpper.Contains("MORDEDURA") || nameUpper.Contains("BRAZO") || nameUpper.Contains("BITE"))
            {
                MarkTaskCompleted(2);
            }
            else if (nameUpper.Contains("ERUPCIÓN") || nameUpper.Contains("TORSO") || nameUpper.Contains("RASH"))
            {
                MarkTaskCompleted(3);
            }
        }

        private void HandleHandCommandExecuted(string commandLabel, bool wasAccepted)
        {
            if (!wasAccepted || string.IsNullOrEmpty(commandLabel)) return;

            string labelLower = commandLabel.ToLowerInvariant();
            if (labelLower.Contains("brazo"))
            {
                MarkTaskCompleted(2); // Examinar brazos
            }
            else if (labelLower.Contains("torso") || labelLower.Contains("pecho"))
            {
                MarkTaskCompleted(3); // Revisar torso
            }
        }

        private void HandleVerdictSubmitted(VerdictType verdict)
        {
            if (verdict != VerdictType.None)
            {
                MarkTaskCompleted(4); // Sellar pasaporte
            }
        }

        /// <summary>
        /// Genera el texto resumen formateado para el HUD de muñeca / HandCheatSheet.
        /// </summary>
        public string GetSummaryText()
        {
            summaryBuilder.Clear();
            for (int i = 0; i < taskCompleted.Length; i++)
            {
                if (taskCompleted[i])
                {
                    summaryBuilder.Append("<color=#4CAF50>✔</color> <color=#9E9E9E><s>")
                                  .Append(taskDescriptions[i])
                                  .Append("</s></color>\n");
                }
                else
                {
                    summaryBuilder.Append("<color=#FFB300>○</color> ")
                                  .Append(taskDescriptions[i])
                                  .Append('\n');
                }
            }
            return summaryBuilder.ToString();
        }

        private void UpdateVisualDisplay()
        {
            if (checklistText == null) return;

            summaryBuilder.Clear();
            for (int i = 0; i < taskCompleted.Length; i++)
            {
                if (taskCompleted[i])
                {
                    summaryBuilder.Append("<color=#2E7D32><b>[✔]</b></color> <color=#424242><s>")
                                  .Append(i + 1).Append(". ").Append(taskDescriptions[i])
                                  .Append("</s></color>\n");
                }
                else
                {
                    summaryBuilder.Append("<color=#C62828><b>[  ]</b></color> <color=#1A1A1A><b>")
                                  .Append(i + 1).Append(". ").Append(taskDescriptions[i])
                                  .Append("</b></color>\n");
                }
            }

            checklistText.text = summaryBuilder.ToString();
        }

        private void EnsureVisualComponents()
        {
            if (checklistText != null) return;

            // Buscar Canvas en los hijos
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Checklist_Canvas");
                canvasObj.transform.SetParent(transform, false);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                var rect = canvas.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 520);
                rect.localScale = new Vector3(0.00065f, 0.00065f, 0.00065f);
                rect.localPosition = new Vector3(0, 0.005f, 0);
                rect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            if (titleText == null)
            {
                var titleObj = new GameObject("Title_TMP");
                titleObj.transform.SetParent(canvas.transform, false);
                titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = "PROTOCOLO SANITARIO";
                titleText.fontSize = 26;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                titleText.fontStyle = FontStyles.Bold;

                var titleRect = titleText.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 0.85f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;
            }

            if (checklistText == null)
            {
                var bodyObj = new GameObject("Tasks_TMP");
                bodyObj.transform.SetParent(canvas.transform, false);
                checklistText = bodyObj.AddComponent<TextMeshProUGUI>();
                checklistText.fontSize = 19;
                checklistText.alignment = TextAlignmentOptions.TopLeft;
                checklistText.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                checklistText.lineSpacing = 16f;

                var bodyRect = checklistText.GetComponent<RectTransform>();
                bodyRect.anchorMin = new Vector2(0.05f, 0.05f);
                bodyRect.anchorMax = new Vector2(0.95f, 0.85f);
                bodyRect.offsetMin = Vector2.zero;
                bodyRect.offsetMax = Vector2.zero;
            }
        }
    }
}
