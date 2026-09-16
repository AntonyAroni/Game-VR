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
        [SerializeField] private BiteMarkSymptom biteSymptom;
        [SerializeField] private HeartbeatSymptom heartbeatSymptom;

        private Quaternion rightArmRestRot;
        private Quaternion leftArmRestRot;
        private Quaternion rightForeArmRestRot;
        private Quaternion leftForeArmRestRot;
        private Quaternion spineRestRot;

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
        private Transform grabbingInteractor;

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
            // Postura anatómica de reposo natural (brazos descansando abajo a los lados)
            rightArmRestRot = Quaternion.Euler(0f, 10f, 68f);
            leftArmRestRot = Quaternion.Euler(0f, -10f, -68f);
            rightForeArmRestRot = Quaternion.Euler(15f, 10f, 0f);
            leftForeArmRestRot = Quaternion.Euler(15f, -10f, 0f);

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

        private void SetupInteractionComponents()
        {
            if (rightHand != null)
            {
                // Colisionador para la mano/muñeca
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
                rightHandGrab.selectEntered.AddListener(OnHandGrabbed);
                rightHandGrab.selectExited.AddListener(OnHandReleased);
            }
        }

        private void OnDisable()
        {
            if (rightHandGrab != null)
            {
                rightHandGrab.selectEntered.RemoveListener(OnHandGrabbed);
                rightHandGrab.selectExited.RemoveListener(OnHandReleased);
            }
        }

        private void OnHandGrabbed(SelectEnterEventArgs args)
        {
            isRightHandGrabbed = true;
            grabbingInteractor = args.interactorObject.transform;
            EventBus.RequestHapticImpulse(HandSide.Both, 0.3f, 0.08f);
            Debug.Log("[Inspección] Tomaste el brazo del superviviente para examinar la piel.");
        }

        private void OnHandReleased(SelectExitEventArgs args)
        {
            isRightHandGrabbed = false;
            grabbingInteractor = null;
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
                if (leftArm != null)
                {
                    leftArm.rotation = transform.rotation * Quaternion.AngleAxis(sin * 16f, Vector3.right) * leftArmRelRot;
                }
                if (rightArm != null && !isRightHandGrabbed)
                {
                    rightArm.rotation = transform.rotation * Quaternion.AngleAxis(-sin * 16f, Vector3.right) * rightArmRelRot;
                }
                if (leftForeArm != null) leftForeArm.localRotation = leftForeArmRestRot;
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

                // Brazo izquierdo en reposo al costado
                if (leftArm != null) leftArm.localRotation = leftArmRestRot;
                if (leftForeArm != null) leftForeArm.localRotation = leftForeArmRestRot;

                // Brazo derecho: reposo vs manipulación directa por el jugador
                if (rightArm != null && rightForeArm != null)
                {
                    if (isRightHandGrabbed && grabbingInteractor != null)
                    {
                        Vector3 dirToHand = (grabbingInteractor.position - rightArm.position).normalized;
                        rightArm.rotation = Quaternion.LookRotation(dirToHand, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
                        rightForeArm.localRotation = Quaternion.Euler(45f, 90f, 0f);
                    }
                    else
                    {
                        rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, rightArmRestRot, Time.deltaTime * 6f);
                        rightForeArm.localRotation = Quaternion.Slerp(rightForeArm.localRotation, rightForeArmRestRot, Time.deltaTime * 6f);
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
