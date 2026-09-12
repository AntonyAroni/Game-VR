using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Decision
{
    /// <summary>
    /// Botón o pulsador físico tangible en el puesto de control.
    /// Principio de IHC: Affordance de presión y retroalimentación táctil de confirmación.
    /// Soporta XR Simple Interactable (rayo/poke), colisión física directa y clic de ratón en simulador.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DecisionButton : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private VerdictType buttonVerdict = VerdictType.ApprovedSafeZone;
        [SerializeField] private float pressDepressionDistance = 0.02f; // 2 cm de hundimiento
        [SerializeField] private Transform movingCapTransform;
        [SerializeField] private float cooldownSeconds = 1.0f;

        private XRSimpleInteractable simpleInteractable;
        private Vector3 originalCapLocalPos;
        private float lastPressTime = -10f;
        private bool isPressed = false;

        public VerdictType ButtonVerdict => buttonVerdict;
        public bool IsPressed => isPressed;

        private void Awake()
        {
            if (movingCapTransform == null)
            {
                movingCapTransform = transform;
            }
            originalCapLocalPos = movingCapTransform.localPosition;

            // Asegurar soporte de XR Interaction Toolkit para rayos y poke
            simpleInteractable = GetComponent<XRSimpleInteractable>();
            if (simpleInteractable == null)
            {
                simpleInteractable = gameObject.AddComponent<XRSimpleInteractable>();
            }
        }

        private void OnEnable()
        {
            if (simpleInteractable != null)
            {
                simpleInteractable.selectEntered.AddListener(OnXRSelect);
                simpleInteractable.activated.AddListener(OnXRActivated);
            }
        }

        private void OnDisable()
        {
            if (simpleInteractable != null)
            {
                simpleInteractable.selectEntered.RemoveListener(OnXRSelect);
                simpleInteractable.activated.RemoveListener(OnXRActivated);
            }
        }

        private void OnXRSelect(SelectEnterEventArgs args) => PressButton();
        private void OnXRActivated(ActivateEventArgs args) => PressButton();

        public void PressButton()
        {
            if (Time.time - lastPressTime < cooldownSeconds) return;
            lastPressTime = Time.time;

            isPressed = true;
            if (movingCapTransform != null)
            {
                movingCapTransform.localPosition = originalCapLocalPos - new Vector3(0, pressDepressionDistance, 0);
            }

            // Feedback háptico y auditivo
            EventBus.RequestHapticImpulse(HandSide.Both, 0.7f, 0.08f);
            EventBus.RequestSpatialAudio("button_click", transform.position, 1.0f);

            Debug.Log($"[Boton Decision] ¡Pulsado! Veredicto: {buttonVerdict}");

            // Enviar veredicto
            EventBus.TriggerVerdictSubmitted(buttonVerdict);

            Invoke(nameof(ResetButtonPosition), 0.25f);
        }

        private void ResetButtonPosition()
        {
            isPressed = false;
            if (movingCapTransform != null)
            {
                movingCapTransform.localPosition = originalCapLocalPos;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            PressButton();
        }

        private void OnTriggerEnter(Collider other)
        {
            PressButton();
        }

        private void Update()
        {
            // Soporte de clic de ratón directo con el nuevo Input System para pruebas en simulador
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    Ray ray = cam.ScreenPointToRay(mousePos);
                    if (Physics.Raycast(ray, out RaycastHit hit, 5f) && hit.collider.gameObject == gameObject)
                    {
                        PressButton();
                    }
                }
            }
        }
    }
}
