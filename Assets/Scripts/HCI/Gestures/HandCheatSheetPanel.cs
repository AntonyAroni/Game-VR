using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Chuleta de gestos bajo demanda, anclada a la palma como la esfera de un reloj.
    /// Principio de Responsabilidad Única (SRP): sólo muestra el listado de órdenes disponibles.
    ///
    /// Justificación IHC: el conocimiento vive "en el mundo" y no "en la cabeza" (Norman),
    /// pero sin ocupar permanentemente el campo visual: aparece únicamente cuando el
    /// usuario gira la palma hacia sí mismo, el gesto natural de consultar algo en la mano.
    /// </summary>
    public class HandCheatSheetPanel : MonoBehaviour
    {
        private const float HoverOffset = 0.045f;
        private const float PositionSmoothing = 12f;
        private const float FadeSpeed = 5f;

        private Transform headAnchor;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI contentLabel;

        private Vector3 targetPosition;
        private Vector3 targetNormal = Vector3.up;
        private bool isRequested;

        /// <summary>Construye el panel y fija la referencia de cabeza para el encaramiento.</summary>
        public void Initialize(Transform head)
        {
            headAnchor = head;
            BuildVisuals();
        }

        /// <summary>Fija la referencia de cabeza cuando la cámara aparece después del arranque.</summary>
        public void SetHeadAnchor(Transform head) => headAnchor = head;

        /// <summary>Define el texto del listado de gestos disponibles.</summary>
        public void SetContent(string content)
        {
            if (contentLabel != null) contentLabel.text = content;
        }

        /// <summary>Solicita mostrar la chuleta sobre la palma indicada.</summary>
        public void ShowAt(Vector3 palmPosition, Vector3 palmNormal)
        {
            isRequested = true;
            targetPosition = palmPosition;
            if (palmNormal.sqrMagnitude > 1e-6f) targetNormal = palmNormal;
        }

        /// <summary>Solicita ocultar la chuleta.</summary>
        public void Hide() => isRequested = false;

        private void LateUpdate()
        {
            if (canvasGroup == null) return;

            float deltaTime = Time.deltaTime;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, isRequested ? 1f : 0f, FadeSpeed * deltaTime);

            if (canvasGroup.alpha <= 0.001f)
            {
                if (canvasGroup.gameObject.activeSelf) canvasGroup.gameObject.SetActive(false);
                return;
            }

            if (!canvasGroup.gameObject.activeSelf) canvasGroup.gameObject.SetActive(true);

            Transform widget = canvasGroup.transform;
            Vector3 desiredPosition = targetPosition + targetNormal * HoverOffset;
            float t = 1f - Mathf.Exp(-PositionSmoothing * deltaTime);
            widget.position = Vector3.Lerp(widget.position, desiredPosition, t);

            if (headAnchor != null)
            {
                widget.rotation = Quaternion.LookRotation(widget.position - headAnchor.position, Vector3.up);
            }
        }

        private void BuildVisuals()
        {
            var canvasObj = new GameObject("CheatSheet_Canvas");
            canvasObj.transform.SetParent(transform, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(360f, 230f);
            canvasRect.localScale = Vector3.one * 0.0005f; // ~18 cm de ancho, legible en la mano

            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.07f, 0.09f, 0.82f);
            panelImage.raycastTarget = false;
            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var titleObj = new GameObject("TitleLabel");
            titleObj.transform.SetParent(canvasObj.transform, false);
            var titleLabel = titleObj.AddComponent<TextMeshProUGUI>();
            titleLabel.text = "ÓRDENES CON LA MANO";
            titleLabel.fontSize = 20f;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color = new Color(0.55f, 0.85f, 1f);
            titleLabel.raycastTarget = false;
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 92f);
            titleRect.sizeDelta = new Vector2(340f, 28f);

            var contentObj = new GameObject("ContentLabel");
            contentObj.transform.SetParent(canvasObj.transform, false);
            contentLabel = contentObj.AddComponent<TextMeshProUGUI>();
            contentLabel.fontSize = 18f;
            contentLabel.alignment = TextAlignmentOptions.TopLeft;
            contentLabel.color = new Color(0.88f, 0.92f, 0.95f);
            contentLabel.richText = true;
            contentLabel.raycastTarget = false;
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0f, -14f);
            contentRect.sizeDelta = new Vector2(330f, 180f);

            canvasObj.SetActive(false);
        }
    }
}
