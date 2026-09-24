using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Survivors.Symptoms;

namespace ZombieCheckpoint.Survivors
{
    /// <summary>
    /// Controlador físico y postural para avatares humanoides civiles en el puesto de inspección.
    /// Principio de IHC: Manipulación Directa y Ergonomía.
    /// - Aplica postura natural de reposo (elimina la pose en T rígida).
    /// - Simula respiración sutil continua.
    /// - Permite al jugador tomar la mano/muñeca del PNJ para girar el antebrazo e inspeccionar la piel.
    /// </summary>
    [RequireComponent(typeof(SurvivorModel))]
    public class SurvivorHumanoidController : MonoBehaviour
    {
        [Header("Referencias de Huesos Superiores")]
        [SerializeField] private Transform spine1Bone;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForeArm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftShoulder;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform leftForeArm;
        [SerializeField] private Transform leftHand;

        [Header("Referencias de Huesos Inferiores (Locomoción)")]
        [SerializeField] private Transform hipsBone;
        [SerializeField] private Transform leftUpLeg;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightUpLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private Transform headBone;

        [Header("Inspección de Muñeca en VR")]
        [SerializeField] private XRGrabInteractable rightHandGrab;
        [SerializeField] private XRGrabInteractable leftHandGrab;
        [SerializeField] private BiteMarkSymptom biteSymptom;
        [SerializeField] private HeartbeatSymptom heartbeatSymptom;

        [Header("Pose de Inspección (Brazos Elevados)")]
        [SerializeField] private bool isInspectionPose = false;

        private Quaternion rightArmRestRot;
        private Quaternion leftArmRestRot;
        private Quaternion rightForeArmRestRot;
        private Quaternion leftForeArmRestRot;
        private Quaternion spineRestRot;

        private Quaternion rightArmInspectRot;
        private Quaternion leftArmInspectRot;
        private Quaternion rightForeArmInspectRot;
        private Quaternion leftForeArmInspectRot;

        // Caché de orientación relativa a la raíz para cinemática procedural
        private Vector3 hipsRestLocalPos;
        private Quaternion hipsRestLocalRot;
        private Quaternion leftUpLegRelRot;
        private Quaternion rightUpLegRelRot;
        private Quaternion leftLegRelRot;
        private Quaternion rightLegRelRot;
        private Quaternion leftArmRelRot;
        private Quaternion rightArmRelRot;

        // Estado de locomoción
        public bool IsWalking { get; private set; } = false;
        public float WalkSpeed { get; set; } = 1.0f;
        private float walkCycle = 0f;
        private float lastStepAudioTime = -10f;
        private float lastSinVal = 0f;

        private bool isRightHandGrabbed = false;
        private Transform rightGrabbingInteractor;
        private bool isLeftHandGrabbed = false;
        private Transform leftGrabbingInteractor;

        public bool IsInspectionPose => isInspectionPose;

        private void Awake()
        {
            AutoDetectBones();
            CacheRestRotations();
            SetupInteractionComponents();
        }

        public void AutoDetectBones()
        {
            Transform[] all = GetComponentsInChildren<Transform>();
            foreach (var t in all)
            {
                string n = t.name;
                if (n == "Spine1") spine1Bone = t;
                else if (n == "RightShoulder") rightShoulder = t;
                else if (n == "RightArm") rightArm = t;
                else if (n == "RightForeArm") rightForeArm = t;
                else if (n == "RightHand") rightHand = t;
                else if (n == "LeftShoulder") leftShoulder = t;
                else if (n == "LeftArm") leftArm = t;
                else if (n == "LeftForeArm") leftForeArm = t;
                else if (n == "LeftHand") leftHand = t;
                else if (n == "Hips") hipsBone = t;
                else if (n == "LeftUpLeg") leftUpLeg = t;
                else if (n == "LeftLeg") leftLeg = t;
                else if (n == "LeftFoot") leftFoot = t;
                else if (n == "RightUpLeg") rightUpLeg = t;
                else if (n == "RightLeg") rightLeg = t;
                else if (n == "RightFoot") rightFoot = t;
                else if (n == "Head") headBone = t;
            }
        }

        private void CacheRestRotations()
        {
            // Postura anatómica de reposo natural (brazos descansando abajo a los lados de forma simétrica)
            // Nota de rig: RightShoulder ya tiene rotación de espejo (0, 180, 186.73), por lo que ambos brazos
            // comparten la misma rotación local para lograr simetría exacta en espacio de mundo.
            rightArmRestRot = Quaternion.Euler(5.1f, 352.8f, 70.7f);
            leftArmRestRot = Quaternion.Euler(5.1f, 352.8f, 70.7f);
            rightForeArmRestRot = Quaternion.Euler(0f, 15f, 0f);
            leftForeArmRestRot = Quaternion.Euler(0f, 15f, 0f);

            // Postura de inspección clínica natural: hombros elevados a 47°, 25° hacia adelante (scaption),
            // y antebrazos flexionados a 70° en los codos (manos cómodamente frente al tórax, exponiendo axilas y costados).
            rightArmInspectRot = Quaternion.Euler(350f, 340f, 325f);
            leftArmInspectRot = Quaternion.Euler(350f, 340f, 325f);
            rightForeArmInspectRot = Quaternion.Euler(0f, 70f, 0f);
            leftForeArmInspectRot = Quaternion.Euler(0f, 70f, 0f);

            if (leftArm != null) leftArm.localRotation = leftArmRestRot;
            if (rightArm != null) rightArm.localRotation = rightArmRestRot;
            if (leftForeArm != null) leftForeArm.localRotation = leftForeArmRestRot;
            if (rightForeArm != null) rightForeArm.localRotation = rightForeArmRestRot;

            if (spine1Bone != null) spineRestRot = spine1Bone.localRotation;

            if (hipsBone != null)
            {
                hipsRestLocalPos = hipsBone.localPosition;
                hipsRestLocalRot = hipsBone.localRotation;
            }

            if (leftUpLeg != null) leftUpLegRelRot = Quaternion.Inverse(transform.rotation) * leftUpLeg.rotation;
            if (rightUpLeg != null) rightUpLegRelRot = Quaternion.Inverse(transform.rotation) * rightUpLeg.rotation;
            if (leftLeg != null) leftLegRelRot = Quaternion.Inverse(transform.rotation) * leftLeg.rotation;
            if (rightLeg != null) rightLegRelRot = Quaternion.Inverse(transform.rotation) * rightLeg.rotation;
            if (leftArm != null) leftArmRelRot = Quaternion.Inverse(transform.rotation) * leftArm.rotation;
            if (rightArm != null) rightArmRelRot = Quaternion.Inverse(transform.rotation) * rightArm.rotation;
        }

        public void SetWalking(bool walking, float speed = 1.0f)
        {
            IsWalking = walking;
            WalkSpeed = speed;
            if (!walking)
            {
                walkCycle = 0f;
            }
        }

        /// <summary>
        /// Activa o desactiva la postura médica donde el civil levanta ambos brazos.
        /// Facilita la auscultación torácica y la inspección visual de marcas en axilas y costados.
        /// </summary>
        public void SetInspectionPose(bool raised)
        {
            isInspectionPose = raised;
            EventBus.RequestHapticImpulse(HandSide.Both, 0.25f, 0.1f);
            Debug.Log($"[Inspección] Pose de inspección (brazos elevados): {isInspectionPose}");
        }

        public void ToggleInspectionPose()
        {
            SetInspectionPose(!isInspectionPose);
        }

        private void SetupInteractionComponents()
        {
            // Mano derecha
            if (rightHand != null)
            {
                var col = rightHand.GetComponent<Collider>();
                if (col == null)
                {
                    var sphere = rightHand.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = 0.08f;
                }

                var rb = rightHand.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = rightHand.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                }

                if (rightHandGrab == null)
                {
                    rightHandGrab = rightHand.gameObject.GetOrAddComponent<XRGrabInteractable>();
                }
                var rFilter = rightHand.gameObject.GetOrAddComponent<ZombieCheckpoint.HCI.DirectInteractionOnlyFilter>();
                rFilter.MaxDistance = 1.20f;
            }

            // Mano izquierda
            if (leftHand != null)
            {
                var col = leftHand.GetComponent<Collider>();
                if (col == null)
                {
                    var sphere = leftHand.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = 0.08f;
                }

                var rb = leftHand.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = leftHand.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                }

                if (leftHandGrab == null)
                {
                    leftHandGrab = leftHand.gameObject.GetOrAddComponent<XRGrabInteractable>();
                }
                var lFilter = leftHand.gameObject.GetOrAddComponent<ZombieCheckpoint.HCI.DirectInteractionOnlyFilter>();
                lFilter.MaxDistance = 1.20f;
            }

            // Auscultación en el pecho
            if (spine1Bone != null)
            {
                var chestCol = spine1Bone.GetComponent<Collider>();
                if (chestCol == null)
                {
                    var sc = spine1Bone.gameObject.AddComponent<SphereCollider>();
                    sc.radius = 0.22f;
                    sc.isTrigger = true;
                }

                if (heartbeatSymptom == null)
                {
                    heartbeatSymptom = spine1Bone.gameObject.GetOrAddComponent<HeartbeatSymptom>();
                }

                var examiner = spine1Bone.GetComponent<BodyPartExaminer>();
                if (examiner == null) spine1Bone.gameObject.AddComponent<BodyPartExaminer>();
            }

            // Mordedura en el antebrazo derecho
            if (rightForeArm != null)
            {
                var armCol = rightForeArm.GetComponent<Collider>();
                if (armCol == null)
                {
                    var cap = rightForeArm.gameObject.AddComponent<CapsuleCollider>();
                    cap.radius = 0.055f;
                    cap.height = 0.22f;
                    cap.direction = 0;
                    cap.isTrigger = true;
                }

                if (biteSymptom == null)
                {
                    biteSymptom = rightForeArm.gameObject.GetOrAddComponent<BiteMarkSymptom>();
                }

                var armExaminer = rightForeArm.GetComponent<BodyPartExaminer>();
                if (armExaminer == null) rightForeArm.gameObject.AddComponent<BodyPartExaminer>();
            }
        }

        private void OnEnable()
        {
            if (rightHandGrab != null)
            {
                rightHandGrab.selectEntered.AddListener(OnRightHandGrabbed);
                rightHandGrab.selectExited.AddListener(OnRightHandReleased);
            }
            if (leftHandGrab != null)
            {
                leftHandGrab.selectEntered.AddListener(OnLeftHandGrabbed);
                leftHandGrab.selectExited.AddListener(OnLeftHandReleased);
            }
        }

        private void OnDisable()
        {
            if (rightHandGrab != null)
            {
                rightHandGrab.selectEntered.RemoveListener(OnRightHandGrabbed);
                rightHandGrab.selectExited.RemoveListener(OnRightHandReleased);
            }
            if (leftHandGrab != null)
            {
                leftHandGrab.selectEntered.RemoveListener(OnLeftHandGrabbed);
                leftHandGrab.selectExited.RemoveListener(OnLeftHandReleased);
            }
        }

        private void OnRightHandGrabbed(SelectEnterEventArgs args)
        {
            isRightHandGrabbed = true;
            rightGrabbingInteractor = args.interactorObject.transform;
            EventBus.RequestHapticImpulse(HandSide.Right, 0.3f, 0.08f);
            Debug.Log("[Inspección] Tomaste el antebrazo derecho del superviviente para examinar la piel.");
        }

        private void OnRightHandReleased(SelectExitEventArgs args)
        {
            isRightHandGrabbed = false;
            rightGrabbingInteractor = null;
        }

        private void OnLeftHandGrabbed(SelectEnterEventArgs args)
        {
            isLeftHandGrabbed = true;
            leftGrabbingInteractor = args.interactorObject.transform;
            EventBus.RequestHapticImpulse(HandSide.Left, 0.3f, 0.08f);
            Debug.Log("[Inspección] Tomaste el antebrazo izquierdo del superviviente.");
        }

        private void OnLeftHandReleased(SelectExitEventArgs args)
        {
            isLeftHandGrabbed = false;
            leftGrabbingInteractor = null;
        }

        private void Update()
        {
            // Atajo de teclado ergonómico para PC/Desktop (Tecla V para Valoración/Brazos o B)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.vKey.wasPressedThisFrame || keyboard.bKey.wasPressedThisFrame))
            {
                ToggleInspectionPose();
            }
        }

        private void LateUpdate()
        {
            // 1. Simulación de locomoción orgánica o postura de reposo
            if (IsWalking)
            {
                walkCycle += Time.deltaTime * WalkSpeed * 6.5f;
                float sin = Mathf.Sin(walkCycle);

                // Detección de pisadas para sonido espacial
                if ((sin >= 0f && lastSinVal < 0f) || (sin < 0f && lastSinVal >= 0f))
                {
                    if (Time.time - lastStepAudioTime > 0.22f)
                    {
                        EventBus.RequestSpatialAudio("footstep_soft", transform.position, 0.75f);
                        lastStepAudioTime = Time.time;
                    }
                }
                lastSinVal = sin;

                // A. Marcha de piernas: zancada alternada
                float leftThighPitch = -sin * 22f; // Inclinación hacia adelante/atrás
                float rightThighPitch = sin * 22f;

                // Flexión de rodilla hacia atrás en la fase de elevación/avance
                float leftKneeBend = Mathf.Max(0f, sin) * 36f;
                float rightKneeBend = Mathf.Max(0f, -sin) * 36f;

                if (leftUpLeg != null)
                {
                    leftUpLeg.rotation = transform.rotation * Quaternion.AngleAxis(leftThighPitch, Vector3.right) * leftUpLegRelRot;
                }
                if (rightUpLeg != null)
                {
                    rightUpLeg.rotation = transform.rotation * Quaternion.AngleAxis(rightThighPitch, Vector3.right) * rightUpLegRelRot;
                }
                if (leftLeg != null)
                {
                    leftLeg.rotation = transform.rotation * Quaternion.AngleAxis(leftThighPitch + leftKneeBend, Vector3.right) * leftLegRelRot;
                }
                if (rightLeg != null)
                {
                    rightLeg.rotation = transform.rotation * Quaternion.AngleAxis(rightThighPitch + rightKneeBend, Vector3.right) * rightLegRelRot;
                }

                // B. Oscilación vertical de caderas y balanceo pélvico
                if (hipsBone != null)
                {
                    float bobY = Mathf.Abs(sin) * 0.024f;
                    hipsBone.localPosition = hipsRestLocalPos + new Vector3(0f, bobY, 0f);
                    float hipRoll = sin * 2.0f;
                    hipsBone.localRotation = hipsRestLocalRot * Quaternion.Euler(0f, hipRoll, 0f);
                }

                // C. Balanceo pendular de brazos en contrafase
                if (leftArm != null && !isLeftHandGrabbed)
                {
                    leftArm.rotation = transform.rotation * Quaternion.AngleAxis(sin * 16f, Vector3.right) * leftArmRelRot;
                }
                if (rightArm != null && !isRightHandGrabbed)
                {
                    rightArm.rotation = transform.rotation * Quaternion.AngleAxis(-sin * 16f, Vector3.right) * rightArmRelRot;
                }
                if (leftForeArm != null && !isLeftHandGrabbed) leftForeArm.localRotation = leftForeArmRestRot;
                if (rightForeArm != null && !isRightHandGrabbed) rightForeArm.localRotation = rightForeArmRestRot;
            }
            else
            {
                // Retorno suave de piernas y caderas a postura de reposo
                if (hipsBone != null)
                {
                    hipsBone.localPosition = Vector3.Lerp(hipsBone.localPosition, hipsRestLocalPos, Time.deltaTime * 6f);
                    hipsBone.localRotation = Quaternion.Slerp(hipsBone.localRotation, hipsRestLocalRot, Time.deltaTime * 6f);
                }
                if (leftUpLeg != null) leftUpLeg.rotation = Quaternion.Slerp(leftUpLeg.rotation, transform.rotation * leftUpLegRelRot, Time.deltaTime * 6f);
                if (rightUpLeg != null) rightUpLeg.rotation = Quaternion.Slerp(rightUpLeg.rotation, transform.rotation * rightUpLegRelRot, Time.deltaTime * 6f);
                if (leftLeg != null) leftLeg.rotation = Quaternion.Slerp(leftLeg.rotation, transform.rotation * leftLegRelRot, Time.deltaTime * 6f);
                if (rightLeg != null) rightLeg.rotation = Quaternion.Slerp(rightLeg.rotation, transform.rotation * rightLegRelRot, Time.deltaTime * 6f);

                // --- MANIPULACIÓN DEL BRAZO IZQUIERDO ---
                if (leftArm != null && leftForeArm != null)
                {
                    if (isLeftHandGrabbed && leftGrabbingInteractor != null)
                    {
                        Vector3 dirToHand = (leftGrabbingInteractor.position - leftArm.position).normalized;
                        Quaternion lookRot = Quaternion.LookRotation(dirToHand, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
                        leftArm.rotation = Quaternion.Slerp(leftArm.rotation, lookRot, Time.deltaTime * 10f);
                        float dist = Vector3.Distance(leftArm.position, leftGrabbingInteractor.position);
                        float bend = Mathf.Clamp((0.55f - dist) * 110f, 15f, 95f);
                        Quaternion targetBend = Quaternion.Euler(bend, -90f, 0f);
                        leftForeArm.localRotation = Quaternion.Slerp(leftForeArm.localRotation, targetBend, Time.deltaTime * 10f);
                    }
                    else
                    {
                        Quaternion targetArm = isInspectionPose ? leftArmInspectRot : leftArmRestRot;
                        Quaternion targetFore = isInspectionPose ? leftForeArmInspectRot : leftForeArmRestRot;
                        leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, targetArm, Time.deltaTime * 3.5f);
                        leftForeArm.localRotation = Quaternion.Slerp(leftForeArm.localRotation, targetFore, Time.deltaTime * 3.5f);
                    }
                }

                // --- MANIPULACIÓN DEL BRAZO DERECHO ---
                if (rightArm != null && rightForeArm != null)
                {
                    if (isRightHandGrabbed && rightGrabbingInteractor != null)
                    {
                        Vector3 dirToHand = (rightGrabbingInteractor.position - rightArm.position).normalized;
                        Quaternion lookRot = Quaternion.LookRotation(dirToHand, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
                        rightArm.rotation = Quaternion.Slerp(rightArm.rotation, lookRot, Time.deltaTime * 10f);
                        float dist = Vector3.Distance(rightArm.position, rightGrabbingInteractor.position);
                        float bend = Mathf.Clamp((0.55f - dist) * 110f, 15f, 95f);
                        Quaternion targetBend = Quaternion.Euler(bend, 90f, 0f);
                        rightForeArm.localRotation = Quaternion.Slerp(rightForeArm.localRotation, targetBend, Time.deltaTime * 10f);
                    }
                    else
                    {
                        Quaternion targetArm = isInspectionPose ? rightArmInspectRot : rightArmRestRot;
                        Quaternion targetFore = isInspectionPose ? rightForeArmInspectRot : rightForeArmRestRot;
                        rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetArm, Time.deltaTime * 3.5f);
                        rightForeArm.localRotation = Quaternion.Slerp(rightForeArm.localRotation, targetFore, Time.deltaTime * 3.5f);
                    }
                }
            }

            // 2. Simulación sutil de respiración en el pecho (siempre activa)
            if (spine1Bone != null)
            {
                float breathCycle = Mathf.Sin(Time.time * 2.2f) * 1.5f;
                spine1Bone.localRotation = spineRestRot * Quaternion.Euler(breathCycle, 0f, 0f);
            }
        }
    }
}
