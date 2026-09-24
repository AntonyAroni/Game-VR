using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Survivors.Symptoms;

namespace ZombieCheckpoint.Tools
{
    /// <summary>
    /// Herramienta de estetoscopio para auscultar el tórax o cuello del PNJ.
    /// Principio de IHC: Mapeo natural directo. Detecta contacto por física y proximidad (distancia < 0.4m)
    /// para máxima fiabilidad tanto en visor VR como con el simulador de teclado/ratón.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class StethoscopeTool : MonoBehaviour, IInspectionTool
    {
        [Header("Configuración")]
        [SerializeField] private string toolName = "Stethoscope";
        [SerializeField] private Collider bellCollider;
        [SerializeField] private float detectionDistance = 0.45f;

        [Header("Físicas y Reposición")]
        [SerializeField] private bool autoRespawnIfFallen = true;
        [SerializeField] private float respawnFloorY = 0.40f;

        private XRGrabInteractable grabInteractable;
        private Rigidbody toolRigidbody;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private HeartbeatSymptom currentTargetSymptom;
        private HandSide currentHoldingHand = HandSide.Right;
        private HeartbeatSymptom[] sceneHeartbeatSymptoms;

        public string ToolName => toolName;
        public bool IsGrabbed => grabInteractable != null && grabInteractable.isSelected;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            toolRigidbody = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            gameObject.GetOrAddComponent<ZombieCheckpoint.HCI.DirectInteractionOnlyFilter>();

            if (bellCollider == null)
            {
                bellCollider = GetComponentInChildren<Collider>();
            }
        }

        private void Start()
        {
            sceneHeartbeatSymptoms = FindObjectsByType<HeartbeatSymptom>();
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrabbed);
                grabInteractable.selectExited.AddListener(OnReleased);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
            StopListening();
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            string interactorName = args.interactorObject.transform.name.ToLower();
            currentHoldingHand = interactorName.Contains("left") ? HandSide.Left : HandSide.Right;
            EventBus.RequestHapticImpulse(currentHoldingHand, 0.2f, 0.05f);
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            StopListening();
        }

        public void OnPrimaryActionTriggered() { }

        private void Update()
        {
            if (!IsGrabbed)
            {
                if (autoRespawnIfFallen && transform.position.y < respawnFloorY)
                {
                    ResetToDesk();
                }
                return;
            }

            // Comprobación de proximidad robusta contra cualquier objetivo cardíaco en escena
            bool needsRefresh = (sceneHeartbeatSymptoms == null || sceneHeartbeatSymptoms.Length == 0);
            if (!needsRefresh)
            {
                bool hasLiveElement = false;
                for (int i = 0; i < sceneHeartbeatSymptoms.Length; i++)
                {
                    if (sceneHeartbeatSymptoms[i] != null) { hasLiveElement = true; break; }
                }
                needsRefresh = !hasLiveElement;
            }

            if (needsRefresh)
            {
                sceneHeartbeatSymptoms = FindObjectsByType<HeartbeatSymptom>(FindObjectsSortMode.None);
            }

            HeartbeatSymptom closest = null;
            float minDist = detectionDistance;
            Vector3 origin = bellCollider != null ? bellCollider.bounds.center : transform.position;

            foreach (var s in sceneHeartbeatSymptoms)
            {
                if (s == null) continue;
                float d = Vector3.Distance(origin, s.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = s;
                }
            }

            if (closest != null)
            {
                if (currentTargetSymptom != closest)
                {
                    StartListeningTo(closest);
                }
            }
            else if (currentTargetSymptom != null)
            {
                StopListening();
            }
        }

        public void ApplyToTarget(GameObject target)
        {
            if (target.TryGetComponent(out HeartbeatSymptom symptom))
            {
                StartListeningTo(symptom);
            }
        }

        private void StartListeningTo(HeartbeatSymptom symptom)
        {
            currentTargetSymptom = symptom;
            currentTargetSymptom.StartAuscultation(currentHoldingHand);
            currentTargetSymptom.TryExamine(toolName, out string msg);
            Debug.Log($"[Estetoscopio] Auscultando... {msg}");
        }

        private void StopListening()
        {
            if (currentTargetSymptom != null)
            {
                currentTargetSymptom.StopAuscultation();
                currentTargetSymptom = null;
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

        private void OnTriggerExit(Collider other)
        {
            if (currentTargetSymptom != null && other.gameObject == currentTargetSymptom.gameObject)
            {
                StopListening();
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
