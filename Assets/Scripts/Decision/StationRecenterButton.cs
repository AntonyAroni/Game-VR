using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.HCI;

namespace ZombieCheckpoint.Decision
{
    /// <summary>
    /// Botón diegético de recentrado en el mostrador de inspección.
    /// Principio de IHC: Affordance, Retroalimentación Inmediata y Prevención de Errores.
    /// Permite al evaluador pulsar físicamente con su mano virtual o mando
    /// para re-alinear al instante la cámara y el visor frente a la mesa.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class StationRecenterButton : MonoBehaviour
    {
        [Header("Configuración Mecánica")]
        [SerializeField] private float pressDepressionDistance = 0.015f;
        [SerializeField] private Transform movingCapTransform;
        [SerializeField] private float cooldownSeconds = 0.6f;

        private XRSimpleInteractable simpleInteractable;
        private Vector3 originalCapLocalPos;
        private float lastPressTime = -10f;
        private bool isPressed = false;

        public void Configure(Transform cap)
        {
            movingCapTransform = cap;
            if (cap != null) originalCapLocalPos = cap.localPosition;
        }

        private void Awake()
        {
            if (movingCapTransform == null) movingCapTransform = transform;
            originalCapLocalPos = movingCapTransform.localPosition;

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
            if (isPressed || Time.time - lastPressTime < cooldownSeconds) return;
            lastPressTime = Time.time;

            isPressed = true;
            if (movingCapTransform != null)
            {
                movingCapTransform.localPosition = originalCapLocalPos - new Vector3(0, pressDepressionDistance, 0);
            }

            EventBus.RequestHapticImpulse(HandSide.Both, 0.45f, 0.08f);
            EventBus.RequestSpatialAudio("button_click", transform.position, 0.85f);

            // Ejecutar el recentrado frente al mostrador
            var psm = PlayerStationManager.Instance != null 
                ? PlayerStationManager.Instance 
                : FindAnyObjectByType<PlayerStationManager>();

            if (psm != null)
            {
                psm.RecenterToStation(forceForwardYaw: true);
            }

            Invoke(nameof(ResetButtonPosition), 0.2f);
        }

        private void ResetButtonPosition()
        {
            isPressed = false;
            if (movingCapTransform != null)
            {
                movingCapTransform.localPosition = originalCapLocalPos;
            }
        }

        private void OnCollisionEnter(Collision collision) => PressButton();
        private void OnTriggerEnter(Collider other) => PressButton();

        private void Update()
        {
            // Soporte de clic con ratón en Editor
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
