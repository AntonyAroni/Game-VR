using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Documents;

namespace ZombieCheckpoint.Tools
{
    /// <summary>
    /// Sello tangible diegético (metáfora de Papers, Please).
    /// Principio de IHC: Manipulación Directa y Retroalimentación Háptica/Visual Inmediata.
    /// Funciona mediante contacto físico real de la almohadilla inferior contra el documento,
    /// o mediante el gatillo (trigger) cuando la base del sello se encuentra sobre el papel.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class StampTool : MonoBehaviour, IInspectionTool
    {
        [Header("Configuración del Sello")]
        [SerializeField] private string toolName = "Stamp";
        [SerializeField] private VerdictType stampVerdict = VerdictType.ApprovedSafeZone;
        [SerializeField] private float cooldownSeconds = 0.6f;
        [SerializeField] private Collider baseTriggerCollider;
        [Header("Físicas y Reposición")]
        [SerializeField] private bool autoRespawnIfFallen = true;
        [SerializeField] private float respawnFloorY = 0.40f;

        private XRGrabInteractable grabInteractable;
        private Rigidbody toolRigidbody;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private HandSide currentHoldingHand = HandSide.Right;
        private float lastStampTime = -10f;
        private DocumentInteractable[] sceneDocuments;

        public string ToolName => toolName;
        public bool IsGrabbed => grabInteractable != null && grabInteractable.isSelected;
        public VerdictType StampVerdict => stampVerdict;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            toolRigidbody = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            gameObject.GetOrAddComponent<ZombieCheckpoint.HCI.DirectInteractionOnlyFilter>();

            ResolveBaseCollider();
        }

        private void ResolveBaseCollider()
        {
            if (baseTriggerCollider == null)
            {
                var padTransform = transform.Find("Stamp_InkPad") ?? transform.Find("Stamp_Base");
                if (padTransform != null)
                {
                    baseTriggerCollider = padTransform.GetComponent<Collider>();
                }
                if (baseTriggerCollider == null)
                {
                    baseTriggerCollider = GetComponentInChildren<Collider>();
                }
            }
        }

        private void Start()
        {
            sceneDocuments = FindObjectsByType<DocumentInteractable>();
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrabbed);
                grabInteractable.activated.AddListener(OnActivated);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.activated.RemoveListener(OnActivated);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            string interactorName = args.interactorObject.transform.name.ToLower();
            currentHoldingHand = interactorName.Contains("left") ? HandSide.Left : HandSide.Right;
            EventBus.RequestHapticImpulse(currentHoldingHand, 0.25f, 0.05f);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            OnPrimaryActionTriggered();
        }

        public void OnPrimaryActionTriggered()
        {
            // Si el jugador aprieta el gatillo mientras sostiene el sello a menos de 8 cm del papel, estampar
            if (!IsGrabbed) return;

            if (sceneDocuments == null || sceneDocuments.Length == 0)
            {
                sceneDocuments = FindObjectsByType<DocumentInteractable>();
            }

            Vector3 stampBasePos = baseTriggerCollider != null ? baseTriggerCollider.bounds.center : transform.position;

            foreach (var doc in sceneDocuments)
            {
                if (doc == null) continue;
                float dist = Vector3.Distance(stampBasePos, doc.transform.position);
                if (dist < 0.08f)
                {
                    ApplyToTarget(doc.gameObject);
                    break;
                }
            }
        }

        public void ApplyToTarget(GameObject target)
        {
            if (Time.time - lastStampTime < cooldownSeconds) return;

            if (target.TryGetComponent(out DocumentInteractable doc))
            {
                // Evitar doble estampado si ya se aplicó un veredicto en este documento
                if (doc.AppliedVerdict != VerdictType.None) return;

                lastStampTime = Time.time;
                doc.ApplyStamp(stampVerdict);

                // Feedback físico potente: golpe seco de madera/goma + impulso háptico en la mano
                EventBus.RequestHapticImpulse(currentHoldingHand, 0.90f, 0.14f);
                EventBus.RequestSpatialAudio("stamp_impact", transform.position, 1.0f);

                Debug.Log($"[Sello] ¡Documento estampado oficialmente con {stampVerdict}!");

                // Notificar veredicto formal al sistema de inspección
                EventBus.TriggerVerdictSubmitted(stampVerdict);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            ApplyToTarget(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            ApplyToTarget(other.gameObject);
        }

        private void Update()
        {
            if (!IsGrabbed && autoRespawnIfFallen && transform.position.y < respawnFloorY)
            {
                ResetToDesk();
            }
        }

        public void ResetToDesk()
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (toolRigidbody != null)
            {
                toolRigidbody.linearVelocity = Vector3.zero;
                toolRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}
