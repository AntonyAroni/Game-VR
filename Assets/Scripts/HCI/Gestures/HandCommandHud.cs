using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Panel diegético flotante que hace visible el vocabulario gestual y el progreso de confirmación.
    /// Principio de Responsabilidad Única (SRP): sólo dibuja retroalimentación, nunca ejecuta órdenes.
    ///
    /// Justificación IHC (Don Norman):
    /// - Visibilidad: el usuario siempre sabe qué gestos existen y cuál está reconociendo el sistema.
    /// - Feedback: la barra de dwell muestra cuánto falta para confirmar, evitando la sensación
    ///   de sistema mudo característica de las interfaces sin botones.
    /// </summary>
    public class HandCommandHud : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private HandCommandDispatcher dispatcher;

        [Header("Anclaje Espacial")]
        [Tooltip("Transform de referencia (cámara del visor). Si queda vacío se resuelve en tiempo de ejecución.")]
        [SerializeField] private Transform headAnchor;
        [SerializeField] private float distanceFromHead = 0.85f;
        [SerializeField] private float verticalOffset = -0.32f;
        [SerializeField] private float followSmoothing = 6f;

        [Header("Presentación")]
        [SerializeField] private bool showCheatSheet = true;
        [SerializeField] private float feedbackHoldSeconds = 2.2f;

        private Canvas canvas;
        private RectTransform panelRect;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI cheatSheetLabel;
        private TextMeshProUGUI feedbackLabel;
        private Image progressFill;

        private readonly StringBuilder builder = new StringBuilder(256);
        private float feedbackExpireTime = -1f;
        private bool cheatSheetDirty = true;

        private void Awake()
        {
            if (dispatcher == null && !TryGetComponent(out dispatcher))
            {
                dispatcher = FindAnyObjectByType<HandCommandDispatcher>();
            }

            BuildCanvas();
        }

        private void OnEnable()
        {
            if (dispatcher != null)
            {
                dispatcher.CommandExecuted += HandleCommandExecuted;
                if (dispatcher.Recognizer != null)
                {
                    dispatcher.Recognizer.GestureProgressChanged += HandleGestureProgress;
                }
            }
        }

        private void OnDisable()
        {
            if (dispatcher != null)
            {
                dispatcher.CommandExecuted -= HandleCommandExecuted;
                if (dispatcher.Recognizer != null)
                {
                    dispatcher.Recognizer.GestureProgressChanged -= HandleGestureProgress;
                }
            }
        }

        private void LateUpdate()
        {
            if (canvas == null) return;

            if (headAnchor == null)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                headAnchor = cam.transform;
            }

            if (cheatSheetDirty)
            {
                RefreshCheatSheet();
                cheatSheetDirty = false;
            }

            Vector3 targetPosition = headAnchor.position
                                   + headAnchor.forward * distanceFromHead
                                   + Vector3.up * verticalOffset;

            float t = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
            canvas.transform.position = Vector3.Lerp(canvas.transform.position, targetPosition, t);
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - headAnchor.position, Vector3.up);

            if (feedbackLabel != null && feedbackExpireTime > 0f && Time.unscaledTime > feedbackExpireTime)
            {
                feedbackLabel.text = string.Empty;
                feedbackExpireTime = -1f;
            }
        }

        private void HandleGestureProgress(HandSide hand, HandGestureType gesture, float progress)
        {
            if (progressFill == null || titleLabel == null) return;

            progressFill.fillAmount = progress;

            if (gesture == HandGestureType.None)
            {
                titleLabel.text = "MANOS LIBRES · esperando gesto";
                progressFill.color = new Color(0.35f, 0.45f, 0.5f, 0.9f);
                return;
            }

            string label = ResolveGestureLabel(gesture);
            string handName = hand == HandSide.Left ? "Izq" : "Der";
            titleLabel.text = $"{handName} · {label}  {Mathf.RoundToInt(progress * 100f)}%";
            progressFill.color = Color.Lerp(new Color(0f, 0.63f, 1f, 0.95f), new Color(0.15f, 1f, 0.45f, 0.95f), progress);
        }

        private void HandleCommandExecuted(string commandLabel, bool accepted)
        {
            if (feedbackLabel == null) return;

            feedbackLabel.color = accepted ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.55f, 0.3f);
            feedbackLabel.text = accepted ? $"✔ {commandLabel}" : $"✖ {commandLabel} (no aplicable ahora)";
            feedbackExpireTime = Time.unscaledTime + feedbackHoldSeconds;
        }

        private string ResolveGestureLabel(HandGestureType gesture)
        {
            var recognizer = dispatcher != null ? dispatcher.Recognizer : null;
            if (recognizer == null) return gesture.ToString();

            var definitions = recognizer.Definitions;
            for (int i = 0; i < definitions.Count; ++i)
            {
                if (definitions[i] != null && definitions[i].Type == gesture) return definitions[i].DisplayName;
            }
            return gesture.ToString();
        }

        private void RefreshCheatSheet()
        {
            if (cheatSheetLabel == null) return;

            if (!showCheatSheet || dispatcher == null)
            {
                cheatSheetLabel.text = string.Empty;
                return;
            }

            builder.Clear();
            var bindings = dispatcher.Bindings;
            for (int i = 0; i < bindings.Count; ++i)
            {
                GestureCommandBinding binding = bindings[i];
                if (binding == null || !binding.IsEnabled) continue;

                string handTag = binding.Hand switch
                {
                    HandSide.Left => " (izq)",
                    HandSide.Right => " (der)",
                    _ => string.Empty
                };

                builder.Append("• <b>").Append(ResolveGestureLabel(binding.Gesture)).Append(handTag)
                       .Append("</b> → ").Append(DescribeCommand(binding.Command)).Append('\n');
            }

            cheatSheetLabel.text = builder.ToString();
        }

        private static string DescribeCommand(GestureCommandId command) => command switch
        {
            GestureCommandId.RaiseArms => "Levantar brazos",
            GestureCommandId.LowerArms => "Bajar brazos",
            GestureCommandId.ToggleRaiseArms => "Alternar brazos",
            GestureCommandId.ToggleExposeTorso => "Descubrir torso",
            GestureCommandId.ApproveSafeZone => "APROBAR",
            GestureCommandId.SendToQuarantine => "CUARENTENA",
            GestureCommandId.HoldCivilian => "Ordenar alto",
            GestureCommandId.ToggleUvFlashlight => "Luz UV",
            _ => "—"
        };

        /// <summary>
        /// Construye el panel en espacio de mundo de forma procedural para no depender
        /// de prefabs externos ni de que el usuario cablee referencias en el Inspector.
        /// </summary>
        private void BuildCanvas()
        {
            var canvasObj = new GameObject("HandCommand_HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 220f;

            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(420f, 260f);
            canvasRect.localScale = Vector3.one * 0.0011f;

            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.06f, 0.08f, 0.72f);
            panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            titleLabel = CreateLabel(canvasObj.transform, "TitleLabel", new Vector2(0f, 104f), new Vector2(400f, 34f), 24f, FontStyles.Bold);
            titleLabel.color = new Color(0.75f, 0.92f, 1f);
            titleLabel.text = "MANOS LIBRES · esperando gesto";

            // Barra de progreso de confirmación (dwell).
            var barBackObj = new GameObject("ProgressBack");
            barBackObj.transform.SetParent(canvasObj.transform, false);
            var barBack = barBackObj.AddComponent<Image>();
            barBack.color = new Color(1f, 1f, 1f, 0.12f);
            var barBackRect = barBackObj.GetComponent<RectTransform>();
            barBackRect.anchoredPosition = new Vector2(0f, 76f);
            barBackRect.sizeDelta = new Vector2(380f, 12f);

            var barFillObj = new GameObject("ProgressFill");
            barFillObj.transform.SetParent(barBackObj.transform, false);
            progressFill = barFillObj.AddComponent<Image>();
            progressFill.color = new Color(0f, 0.63f, 1f, 0.95f);
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;
            var barFillRect = barFillObj.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;

            cheatSheetLabel = CreateLabel(canvasObj.transform, "CheatSheetLabel", new Vector2(0f, -4f), new Vector2(392f, 140f), 17f, FontStyles.Normal);
            cheatSheetLabel.color = new Color(0.86f, 0.9f, 0.92f);
            cheatSheetLabel.alignment = TextAlignmentOptions.TopLeft;

            feedbackLabel = CreateLabel(canvasObj.transform, "FeedbackLabel", new Vector2(0f, -104f), new Vector2(392f, 32f), 21f, FontStyles.Bold);
            feedbackLabel.text = string.Empty;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, FontStyles style)
        {
            var labelObj = new GameObject(name);
            labelObj.transform.SetParent(parent, false);

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.richText = true;

            var rect = labelObj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }
    }
}
