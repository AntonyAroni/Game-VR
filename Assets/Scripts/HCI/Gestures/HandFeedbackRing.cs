using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Indicador anular de confirmación anclado a la palma de una mano concreta.
    /// Principio de Responsabilidad Única (SRP): sólo dibuja y anima el anillo de dwell.
    ///
    /// Justificación IHC (Visibilidad sin invasión): el usuario ya está mirando su mano
    /// cuando gesticula, así que la retroalimentación aparece justo ahí y desaparece por
    /// completo al terminar, dejando el campo visual libre para inspeccionar al civil.
    /// </summary>
    public class HandFeedbackRing : MonoBehaviour
    {
        private const float HoverOffset = 0.055f;
        private const float PositionSmoothing = 14f;
        private const float FillSmoothing = 18f;
        private const float FadeSpeed = 6f;
        private const float FlashDuration = 1.1f;

        private static readonly Color PendingColor = new Color(0f, 0.63f, 1f, 0.95f);
        private static readonly Color ReadyColor = new Color(0.15f, 1f, 0.45f, 0.95f);
        private static readonly Color AcceptedColor = new Color(0.25f, 1f, 0.55f, 1f);
        private static readonly Color RejectedColor = new Color(1f, 0.55f, 0.28f, 1f);

        private Transform headAnchor;
        private CanvasGroup canvasGroup;
        private Image ringFill;
        private TextMeshProUGUI percentLabel;
        private TextMeshProUGUI commandLabel;

        private Vector3 targetPosition;
        private Vector3 targetNormal = Vector3.up;
        private float targetFill;
        private float displayedFill;
        private bool hasValidPose;
        private float flashExpireTime = -1f;
        private bool flashAccepted;

        /// <summary>Construye la jerarquía visual y fija la referencia de cabeza para el encaramiento.</summary>
        public void Initialize(Transform head)
        {
            headAnchor = head;
            BuildVisuals();
        }

        /// <summary>Actualiza el destino de animación con la última lectura del reconocedor.</summary>
        public void ApplyFeedback(in HandGestureFeedback feedback)
        {
            hasValidPose = feedback.IsHandTracked;
            if (hasValidPose)
            {
                targetPosition = feedback.PalmPosition;
                targetNormal = feedback.PalmNormal.sqrMagnitude > 1e-6f ? feedback.PalmNormal : Vector3.up;
            }

            targetFill = feedback.Progress;

            if (commandLabel != null && flashExpireTime < 0f)
            {
                commandLabel.text = feedback.Progress > 0.02f ? feedback.Label : string.Empty;
                commandLabel.color = new Color(0.86f, 0.94f, 1f, 1f);
            }
        }

        /// <summary>
        /// Destella el anillo al resolverse una orden: verde si se ejecutó,
        /// ámbar si el gesto se entendió pero no era aplicable en ese estado.
        /// </summary>
        public void FlashConfirmation(string label, bool accepted)
        {
            flashAccepted = accepted;
            flashExpireTime = Time.unscaledTime + FlashDuration;
            targetFill = 1f;
            displayedFill = 1f;

            if (commandLabel != null)
            {
                commandLabel.text = accepted ? label : $"{label} · ahora no";
                commandLabel.color = accepted ? AcceptedColor : RejectedColor;
            }
        }

        /// <summary>Fija la referencia de cabeza cuando la cámara aparece después del arranque.</summary>
        public void SetHeadAnchor(Transform head) => headAnchor = head;

        private void LateUpdate()
        {
            if (canvasGroup == null) return;

            float deltaTime = Time.deltaTime;
            bool flashing = flashExpireTime > 0f && Time.unscaledTime < flashExpireTime;
            if (!flashing && flashExpireTime > 0f)
            {
                flashExpireTime = -1f;
                if (commandLabel != null) commandLabel.text = string.Empty;
            }

            // El anillo sólo existe mientras hay algo que comunicar.
            bool shouldShow = hasValidPose && (targetFill > 0.02f || flashing);
            float targetAlpha = shouldShow ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, FadeSpeed * deltaTime);

            if (canvasGroup.alpha <= 0.001f)
            {
                if (canvasGroup.gameObject.activeSelf) canvasGroup.gameObject.SetActive(false);
                displayedFill = targetFill;
                return;
            }

            if (!canvasGroup.gameObject.activeSelf) canvasGroup.gameObject.SetActive(true);

            FollowPalm(deltaTime);
            AnimateFill(deltaTime, flashing);
        }

        private void FollowPalm(float deltaTime)
        {
            Vector3 desiredPosition = targetPosition + targetNormal * HoverOffset;

            float t = 1f - Mathf.Exp(-PositionSmoothing * deltaTime);
            Transform widget = canvasGroup.transform;
            widget.position = Vector3.Lerp(widget.position, desiredPosition, t);

            if (headAnchor != null)
            {
                // Encarado siempre al jugador: el anillo se lee igual sea cual sea el giro de la muñeca.
                widget.rotation = Quaternion.LookRotation(widget.position - headAnchor.position, Vector3.up);
            }
        }

        private void AnimateFill(float deltaTime, bool flashing)
        {
            float t = 1f - Mathf.Exp(-FillSmoothing * deltaTime);
            displayedFill = Mathf.Lerp(displayedFill, targetFill, t);

            if (ringFill != null)
            {
                ringFill.fillAmount = displayedFill;
                ringFill.color = flashing
                    ? (flashAccepted ? AcceptedColor : RejectedColor)
                    : Color.Lerp(PendingColor, ReadyColor, displayedFill);
            }

            if (percentLabel != null)
            {
                percentLabel.text = flashing
                    ? (flashAccepted ? "OK" : "—")
                    : $"{Mathf.RoundToInt(displayedFill * 100f)}%";
            }
        }

        private void BuildVisuals()
        {
            var canvasObj = new GameObject("FeedbackRing_Canvas");
            canvasObj.transform.SetParent(transform, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200f, 200f);
            canvasRect.localScale = Vector3.one * 0.0005f; // ~10 cm de diámetro

            Sprite ringSprite = RingSpriteFactory.GetRingSprite();

            var trackObj = new GameObject("RingTrack");
            trackObj.transform.SetParent(canvasObj.transform, false);
            var ringTrack = trackObj.AddComponent<Image>();
            ringTrack.sprite = ringSprite;
            ringTrack.color = new Color(0.85f, 0.95f, 1f, 0.16f);
            ringTrack.raycastTarget = false;
            StretchToParent(trackObj.GetComponent<RectTransform>());

            var fillObj = new GameObject("RingFill");
            fillObj.transform.SetParent(canvasObj.transform, false);
            ringFill = fillObj.AddComponent<Image>();
            ringFill.sprite = ringSprite;
            ringFill.color = PendingColor;
            ringFill.raycastTarget = false;
            ringFill.type = Image.Type.Filled;
            ringFill.fillMethod = Image.FillMethod.Radial360;
            ringFill.fillOrigin = (int)Image.Origin360.Top;
            ringFill.fillClockwise = true;
            ringFill.fillAmount = 0f;
            StretchToParent(fillObj.GetComponent<RectTransform>());

            percentLabel = CreateLabel(canvasObj.transform, "PercentLabel", Vector2.zero, new Vector2(150f, 60f), 42f);
            percentLabel.color = new Color(0.92f, 0.97f, 1f);

            commandLabel = CreateLabel(canvasObj.transform, "CommandLabel", new Vector2(0f, -132f), new Vector2(420f, 48f), 26f);
            commandLabel.text = string.Empty;

            canvasObj.SetActive(false);
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var labelObj = new GameObject(name);
            labelObj.transform.SetParent(parent, false);

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var rect = labelObj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }
    }
}
