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
        [Header("Referencias de Huesos")]
        [SerializeField] private Transform spine1Bone;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForeArm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftShoulder;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform leftForeArm;
        [SerializeField] private Transform leftHand;

        [Header("Inspección de Muñeca en VR")]
        [SerializeField] private XRGrabInteractable rightHandGrab;
        [SerializeField] private BiteMarkSymptom biteSymptom;
        [SerializeField] private HeartbeatSymptom heartbeatSymptom;

        private Quaternion rightArmRestRot;
        private Quaternion leftArmRestRot;
        private Quaternion rightForeArmRestRot;
        private Quaternion leftForeArmRestRot;
        private Quaternion spineRestRot;

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
                    rightHandGrab = rightHand.GetComponent<XRGrabInteractable>() 
                                 ?? rightHand.gameObject.AddComponent<XRGrabInteractable>();
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
                    heartbeatSymptom = spine1Bone.GetComponent<HeartbeatSymptom>() 
                                    ?? spine1Bone.gameObject.AddComponent<HeartbeatSymptom>();
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
                    biteSymptom = rightForeArm.GetComponent<BiteMarkSymptom>() 
                               ?? rightForeArm.gameObject.AddComponent<BiteMarkSymptom>();
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
            // 1. Simulación sutil de respiración en el pecho
            if (spine1Bone != null)
            {
                float breathCycle = Mathf.Sin(Time.time * 2.2f) * 1.5f;
                spine1Bone.localRotation = spineRestRot * Quaternion.Euler(breathCycle, 0f, 0f);
            }

            // 2. Brazo izquierdo siempre relajado al costado
            if (leftArm != null) leftArm.localRotation = leftArmRestRot;
            if (leftForeArm != null) leftForeArm.localRotation = leftForeArmRestRot;

            // 3. Brazo derecho: reposo vs manipulación directa por el jugador
            if (rightArm != null && rightForeArm != null)
            {
                if (isRightHandGrabbed && grabbingInteractor != null)
                {
                    // Levantar el brazo hacia la mano del jugador
                    Vector3 dirToHand = (grabbingInteractor.position - rightArm.position).normalized;
                    rightArm.rotation = Quaternion.LookRotation(dirToHand, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
                    
                    // Rotar el antebrazo para exponer la cara interna
                    rightForeArm.localRotation = Quaternion.Euler(45f, 90f, 0f);
                }
                else
                {
                    // Transición suave de vuelta al reposo
                    rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, rightArmRestRot, Time.deltaTime * 6f);
                    rightForeArm.localRotation = Quaternion.Slerp(rightForeArm.localRotation, rightForeArmRestRot, Time.deltaTime * 6f);
                }
            }
        }
    }
}
