using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Documents;

namespace ZombieCheckpoint.Tools
{
    /// <summary>
    /// Sello tangible (metáfora de Papers, Please).
    /// Principio de IHC: Manipulación Directa y Tangible. El usuario toma el sello físicamente y lo estampa contra el papel.
    /// Funciona por colisión física directa y proximidad cuando se sostiene en la mano.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class StampTool : MonoBehaviour, IInspectionTool
    {
        [Header("Configuración del Sello")]
        [SerializeField] private string toolName = "Stamp";
        [SerializeField] private VerdictType stampVerdict = VerdictType.ApprovedSafeZone;
        [SerializeField] private float cooldownSeconds = 0.5f;

        private XRGrabInteractable grabInteractable;
        private HandSide currentHoldingHand = HandSide.Right;
        private float lastStampTime = -10f;
        private DocumentInteractable[] sceneDocuments;

        public string ToolName => toolName;
        public bool IsGrabbed => grabInteractable != null && grabInteractable.isSelected;
        public VerdictType StampVerdict => stampVerdict;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
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
            EventBus.RequestHapticImpulse(currentHoldingHand, 0.2f, 0.05f);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            OnPrimaryActionTriggered();
        }

        public void OnPrimaryActionTriggered()
        {
            // Atajo: si se aprieta gatillo mientras se sostiene cerca del papel, estampar
            StampNearestDocument();
        }

        private void Update()
        {
            if (!IsGrabbed) return;

            // Detección de proximidad al papel: si está a menos de 15 cm de la mesa/documento
            if (sceneDocuments == null || sceneDocuments.Length == 0)
            {
                sceneDocuments = FindObjectsByType<DocumentInteractable>();
            }

            foreach (var doc in sceneDocuments)
            {
                if (doc == null) continue;
                float dist = Vector3.Distance(transform.position, doc.transform.position);
                if (dist < 0.15f)
                {
                    ApplyToTarget(doc.gameObject);
                    break;
                }
            }
        }

        private void StampNearestDocument()
        {
            if (sceneDocuments != null && sceneDocuments.Length > 0 && sceneDocuments[0] != null)
            {
                ApplyToTarget(sceneDocuments[0].gameObject);
            }
        }

        public void ApplyToTarget(GameObject target)
        {
            if (Time.time - lastStampTime < cooldownSeconds) return;

            if (target.TryGetComponent(out DocumentInteractable doc))
            {
                lastStampTime = Time.time;
                doc.ApplyStamp(stampVerdict);

                // Feedback físico de impacto y sonido
                EventBus.RequestHapticImpulse(currentHoldingHand, 0.85f, 0.15f);
                EventBus.RequestSpatialAudio("stamp_impact", transform.position, 1.0f);

                Debug.Log($"[Sello] ¡Documento estampado con {stampVerdict}!");

                // Notificar veredicto
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
    }
}
