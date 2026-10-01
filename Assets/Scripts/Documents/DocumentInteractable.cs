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

        private bool autoRespawnIfFallen = true;
        private float respawnFloorY = 0.50f;
        private float maxDistanceAllowed = 1.25f;
        
        public DocumentData CurrentData { get; private set; }
        public VerdictType AppliedVerdict { get; private set; } = VerdictType.None;

        private XRGrabInteractable grabInteractable;
        private Rigidbody docRigidbody;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Transform spawnPointAnchor;

        public bool IsGrabbed => grabInteractable != null && grabInteractable.isSelected;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            docRigidbody = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            gameObject.GetOrAddComponent<ZombieCheckpoint.HCI.DirectInteractionOnlyFilter>();

            if (documentView == null)
            {
                documentView = GetComponentInChildren<DocumentView>();
            }
        }

        private void Start()
        {
            var dsp = GameObject.Find("Document_SpawnPoint_Anchor");
            if (dsp != null)
            {
                spawnPointAnchor = dsp.transform;
                initialPosition = spawnPointAnchor.position;
                initialRotation = spawnPointAnchor.rotation;
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
            // Auto-reposición si el documento cae al suelo, es lanzado lejos o arrojado hacia el paciente
            if (!IsGrabbed && autoRespawnIfFallen)
            {
                Vector3 origin = spawnPointAnchor != null ? spawnPointAnchor.position : initialPosition;
                bool fallenToFloor = transform.position.y < respawnFloorY;
                bool thrownFarAway = Vector3.Distance(transform.position, origin) > maxDistanceAllowed;
                bool thrownPastBooth = transform.position.z > 1.15f;

                if (fallenToFloor || thrownFarAway || thrownPastBooth)
                {
                    ResetToDesk();
                }
            }

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

        /// <summary>
        /// Reposiciona el pasaporte/documento sobre su ancla del mostrador.
        /// </summary>
        public void ResetToDesk()
        {
            Vector3 resetPos = spawnPointAnchor != null ? spawnPointAnchor.position : initialPosition;
            Quaternion resetRot = spawnPointAnchor != null ? spawnPointAnchor.rotation : initialRotation;

            transform.SetPositionAndRotation(resetPos, resetRot);
            if (docRigidbody != null)
            {
                docRigidbody.linearVelocity = Vector3.zero;
                docRigidbody.angularVelocity = Vector3.zero;
                docRigidbody.Sleep();
            }
            Debug.Log("[Documento] Reposicionado automáticamente sobre el mostrador de inspección.");
        }
    }
}
