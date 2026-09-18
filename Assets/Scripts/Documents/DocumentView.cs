using System.Collections;
using TMPro;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Documents
{
    /// <summary>
    /// Renderizador visual del documento sobre un Canvas diegético en el espacio 3D.
    /// Principio SRP: Se encarga exclusivamente de la presentación visual del documento.
    /// Resuelve automáticamente los componentes de texto y sellos gráficos en tiempo de ejecución.
    /// Incorpora animación de impacto y rotación orgánica de tinta (metáfora de Papers, Please).
    /// </summary>
    public class DocumentView : MonoBehaviour
    {
        [Header("Campos de Texto (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI mainDocumentText;
        [SerializeField] private TextMeshProUGUI approvedStampGraphic;
        [SerializeField] private TextMeshProUGUI quarantineStampGraphic;
        [SerializeField] private TextMeshProUGUI uvWatermarkGraphic;

        private DocumentData currentData;
        private float uvFadeAlpha = 0f;

        private void Awake()
        {
            AutoResolveReferences();
        }

        private void AutoResolveReferences()
        {
            if (mainDocumentText == null)
            {
                var textObj = transform.Find("DocumentCanvas/DocText");
                if (textObj != null) mainDocumentText = textObj.GetComponent<TextMeshProUGUI>();
            }

            if (approvedStampGraphic == null)
            {
                var appObj = transform.Find("DocumentCanvas/Stamp_Approved_Vis");
                if (appObj != null) approvedStampGraphic = appObj.GetComponent<TextMeshProUGUI>();
            }

            if (quarantineStampGraphic == null)
            {
                var quarObj = transform.Find("DocumentCanvas/Stamp_Quarantine_Vis");
                if (quarObj != null) quarantineStampGraphic = quarObj.GetComponent<TextMeshProUGUI>();
            }

            if (uvWatermarkGraphic == null)
            {
                var uvObj = transform.Find("DocumentCanvas/Stamp_UV_Watermark");
                if (uvObj != null) uvWatermarkGraphic = uvObj.GetComponent<TextMeshProUGUI>();
            }
        }

        public void BindData(DocumentData data)
        {
            currentData = data;
            AutoResolveReferences();

            if (mainDocumentText != null)
            {
                string statusTag = data.isFalsified ? "<color=#ff3333>[NO VÁLIDO]</color>" : "<color=#33cc33>[VIGENTE]</color>";
                // Sólo los datos que sirven para decidir: el ID y el grupo sanguíneo eran decorativos.
                mainDocumentText.text = $"<b>PASE SANITARIO</b> {statusTag}\n" +
                                       $"<b>NOMBRE:</b> {data.holderName}\n" +
                                       $"<b>EDAD:</b> {data.holderAge} años\n" +
                                       $"<b>VENCE:</b> {data.expirationDate}";
            }

            if (uvWatermarkGraphic != null)
            {
                uvWatermarkGraphic.gameObject.SetActive(false);
            }

            ClearStamps();
        }

        public void SetUVExposure(bool isExposed)
        {
            float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.033f;
            float targetAlpha = isExposed ? 1.0f : 0.0f;
            uvFadeAlpha = Mathf.MoveTowards(uvFadeAlpha, targetAlpha, dt * 6.0f);

            if (uvWatermarkGraphic != null)
            {
                if (uvFadeAlpha > 0.02f)
                {
                    uvWatermarkGraphic.gameObject.SetActive(true);
                    if (currentData != null && !currentData.isFalsified)
                    {
                        // Sello forense auténtico fluorescente (Cian / Esmeralda reactivo)
                        uvWatermarkGraphic.text = "✦ SELLO OFICIAL ✦\nAUTÉNTICO";
                        uvWatermarkGraphic.color = new Color(0.15f, 1.0f, 0.75f, uvFadeAlpha);
                    }
                    else
                    {
                        // Falsificación / irregularidad revelada
                        uvWatermarkGraphic.text = "✖ SIN SELLO ✖\nFALSO";
                        uvWatermarkGraphic.color = new Color(1.0f, 0.22f, 0.15f, uvFadeAlpha);
                    }
                }
                else
                {
                    uvWatermarkGraphic.gameObject.SetActive(false);
                }
            }
        }

        public void ShowStamp(VerdictType verdict)
        {
            AutoResolveReferences();

            if (verdict == VerdictType.ApprovedSafeZone && approvedStampGraphic != null)
            {
                approvedStampGraphic.gameObject.SetActive(true);
                // Rotación orgánica de estampado manual
                float randomAngle = Random.Range(-6f, 6f);
                approvedStampGraphic.transform.localRotation = Quaternion.Euler(0f, 0f, randomAngle);
                StartCoroutine(AnimateStampPop(approvedStampGraphic.transform));
            }
            else if (verdict == VerdictType.SendToQuarantine && quarantineStampGraphic != null)
            {
                quarantineStampGraphic.gameObject.SetActive(true);
                float randomAngle = Random.Range(-6f, 6f);
                quarantineStampGraphic.transform.localRotation = Quaternion.Euler(0f, 0f, randomAngle);
                StartCoroutine(AnimateStampPop(quarantineStampGraphic.transform));
            }
        }

        private IEnumerator AnimateStampPop(Transform stampTransform)
        {
            if (stampTransform == null) yield break;

            Vector3 baseScale = Vector3.one;
            float duration = 0.14f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (stampTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentScale = Mathf.Lerp(1.35f, 1.0f, t);
                stampTransform.localScale = baseScale * currentScale;
                yield return null;
            }

            if (stampTransform != null)
            {
                stampTransform.localScale = baseScale;
            }
        }

        public void ClearStamps()
        {
            if (approvedStampGraphic != null)
            {
                approvedStampGraphic.gameObject.SetActive(false);
                approvedStampGraphic.transform.localScale = Vector3.one;
            }

            if (quarantineStampGraphic != null)
            {
                quarantineStampGraphic.gameObject.SetActive(false);
                quarantineStampGraphic.transform.localScale = Vector3.one;
            }
        }
    }
}
