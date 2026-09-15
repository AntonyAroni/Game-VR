using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Documents
{
    /// <summary>
    /// Componente interactivo para el documento físico en VR.
    /// Principio de IHC: Manipulación directa en espacio 3D. El usuario puede tomar el papel,
    /// acercarlo a los ojos para leer la letra pequeña y apoyarlo en la mesa para sellarlo.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class DocumentInteractable : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private DocumentView documentView;
        
        public DocumentData CurrentData { get; private set; }
        public VerdictType AppliedVerdict { get; private set; } = VerdictType.None;

        private XRGrabInteractable grabInteractable;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (documentView == null)
            {
                documentView = GetComponentInChildren<DocumentView>();
            }
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnDocumentGrabbed);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnDocumentGrabbed);
            }
        }

        private void OnDocumentGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
        {
            EventBus.RequestHapticImpulse(HandSide.Both, 0.25f, 0.05f);
            EventBus.TriggerToolApplied("Document", gameObject);
            Debug.Log("[Documento] Pase sanitario tomado para inspección de identidad.");
        }

        public void Initialize(DocumentData data)
        {
            CurrentData = data;
            AppliedVerdict = VerdictType.None;
            
            if (documentView == null) documentView = GetComponentInChildren<DocumentView>();
            if (documentView != null)
            {
                documentView.BindData(data);
            }
        }

        private float lastUVExposeTime = -10f;

        public void ReceiveUVLight(bool isUVActive)
        {
            if (isUVActive)
            {
                lastUVExposeTime = Time.time;
            }
        }

        private void Update()
        {
            bool isCurrentlyUnderUV = (Time.time - lastUVExposeTime) < 0.2f;
            if (documentView != null)
            {
                documentView.SetUVExposure(isCurrentlyUnderUV);
            }
        }

        public void ApplyStamp(VerdictType verdict)
        {
            AppliedVerdict = verdict;
            if (documentView != null)
            {
                documentView.ShowStamp(verdict);
            }
        }
    }
}
