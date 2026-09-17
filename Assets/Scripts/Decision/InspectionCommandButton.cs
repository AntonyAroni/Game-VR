using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Survivors;

namespace ZombieCheckpoint.Decision
{
    public enum InspectionCommandType
    {
        ToggleRaiseArms,
        ToggleExposeTorso
    }

    /// <summary>
    /// Botón diegético de comando de inspección en el mostrador de control.
    /// Principio de IHC: Visibilidad, Ergonomía y Affordance.
    /// Permite ordenar al sospechoso "¡Levante los brazos!" o "¡Descubra el torso!"
    /// sin necesidad de sostener físicamente cada extremidad de forma prolongada.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class InspectionCommandButton : MonoBehaviour
    {
        [Header("Configuración del Comando")]
        [SerializeField] private InspectionCommandType commandType = InspectionCommandType.ToggleRaiseArms;
        [SerializeField] private float pressDepressionDistance = 0.018f;
        [SerializeField] private Transform movingCapTransform;
        [SerializeField] private float cooldownSeconds = 0.5f;

        private XRSimpleInteractable simpleInteractable;
        private Vector3 originalCapLocalPos;
        private float lastPressTime = -10f;
        private bool isPressed = false;

        public InspectionCommandType CommandType => commandType;
        public bool IsPressed => isPressed;

        public void Configure(InspectionCommandType type, Transform cap)
        {
            commandType = type;
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

            EventBus.RequestHapticImpulse(HandSide.Both, 0.5f, 0.08f);
            EventBus.RequestSpatialAudio("button_click", transform.position, 0.9f);

            ExecuteCommand();

            Invoke(nameof(ResetButtonPosition), 0.2f);
        }

        private void ExecuteCommand()
        {
            if (commandType == InspectionCommandType.ToggleRaiseArms)
            {
                var humanoid = FindAnyObjectByType<SurvivorHumanoidController>();
                if (humanoid != null)
                {
                    humanoid.ToggleInspectionPose();
                }
            }
            else if (commandType == InspectionCommandType.ToggleExposeTorso)
            {
                var torso = FindAnyObjectByType<TorsoClothingController>();
                if (torso != null)
                {
                    torso.ToggleTorso();
                }
            }
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
