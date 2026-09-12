using TMPro;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Documents
{
    /// <summary>
    /// Renderizador visual del documento sobre un Canvas diegético en el espacio 3D.
    /// Principio SRP: Se encarga exclusivamente de la presentación visual del documento.
    /// Resuelve automáticamente los componentes de texto y sellos gráficos en tiempo de ejecución.
    /// </summary>
    public class DocumentView : MonoBehaviour
    {
        [Header("Campos de Texto (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI mainDocumentText;
        [SerializeField] private TextMeshProUGUI approvedStampGraphic;
        [SerializeField] private TextMeshProUGUI quarantineStampGraphic;

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
        }

        public void BindData(DocumentData data)
        {
            AutoResolveReferences();

            if (mainDocumentText != null)
            {
                string statusTag = data.isFalsified ? "<color=#ff3333>[IRREGULAR / VENCIDO]</color>" : "<color=#33cc33>[VIGENTE]</color>";
                mainDocumentText.text = $"<b>PASE SANITARIO</b> {statusTag}\n" +
                                       $"------------------------\n" +
                                       $"<b>NOMBRE:</b> {data.holderName}\n" +
                                       $"<b>EDAD:</b> {data.holderAge} años\n" +
                                       $"<b>ID:</b> {data.documentId}\n" +
                                       $"<b>VENCE:</b> {data.expirationDate}\n" +
                                       $"<b>GRUPO:</b> {data.bloodType}";
            }

            ClearStamps();
        }

        public void ShowStamp(VerdictType verdict)
        {
            AutoResolveReferences();

            if (verdict == VerdictType.ApprovedSafeZone && approvedStampGraphic != null)
            {
                approvedStampGraphic.gameObject.SetActive(true);
            }
            else if (verdict == VerdictType.SendToQuarantine && quarantineStampGraphic != null)
            {
                quarantineStampGraphic.gameObject.SetActive(true);
            }
        }

        public void ClearStamps()
        {
            if (approvedStampGraphic != null) approvedStampGraphic.gameObject.SetActive(false);
            if (quarantineStampGraphic != null) quarantineStampGraphic.gameObject.SetActive(false);
        }
    }
}
