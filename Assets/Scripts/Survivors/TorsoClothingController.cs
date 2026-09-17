using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Survivors
{
    /// <summary>
    /// Gestiona la vestimenta del torso (polo/camisa) del superviviente en el puesto de inspección.
    /// Principio de IHC: Affordance y Manipulación Física.
    /// Permite al evaluador tirar del dobladillo de la prenda en VR o presionar un botón de inspección,
    /// desvistiendo/levantando el polo para examinar la caja torácica, auscultar directamente y verificar
    /// posibles manchas rojas o erupciones infecciosas (RashSymptom).
    /// </summary>
    public class TorsoClothingController : MonoBehaviour
    {
        [Header("Estado de la Vestimenta")]
        [SerializeField] private bool isTorsoExposed = false;

        [Header("Renderers de la Prenda Superior")]
        [SerializeField] private List<Renderer> shirtRenderers = new List<Renderer>();

        [Header("Malla de Torso Descubierto")]
        [SerializeField] private GameObject bareTorsoObject;

        [Header("Interacción Tangible")]
        [SerializeField] private XRGrabInteractable shirtGrabHandle;

        public bool IsTorsoExposed => isTorsoExposed;

        private void Awake()
        {
            AutoDetectShirtRenderers();
            SetupShirtGrabHandle();
        }

        /// <summary>
        /// Localiza automáticamente los SkinnedMeshRenderers correspondientes al polo/camiseta del PNJ.
        /// </summary>
        public void AutoDetectShirtRenderers()
        {
            shirtRenderers.Clear();
            var allRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRenderers)
            {
                string rName = r.name.ToLower();
                if (rName.Contains("shirt") || rName.Contains("tshirt") || rName.Contains("torso_cloth"))
                {
                    shirtRenderers.Add(r);
                }
            }
        }

        /// <summary>
        /// Configura el modelo de torso descubierto adecuado según el sexo del PNJ.
        /// </summary>
        public void ConfigureBareTorso(GameObject torsoPrefab, Material bodyMat = null)
        {
            if (torsoPrefab == null) return;

            // Instanciar como hijo de este GameObject
            bareTorsoObject = Instantiate(torsoPrefab, transform);
            bareTorsoObject.name = "NPC_BareTorso_Visual";

            // Vincular los huesos del SkinnedMeshRenderer al esqueleto del personaje
            var targetSmr = bareTorsoObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (targetSmr != null)
            {
                BindBonesToSkeleton(targetSmr);
                if (bodyMat != null)
                {
                    targetSmr.sharedMaterial = bodyMat;
                }
            }

            bareTorsoObject.SetActive(isTorsoExposed);
        }

        private void BindBonesToSkeleton(SkinnedMeshRenderer smr)
        {
            var boneMap = new Dictionary<string, Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (!boneMap.ContainsKey(t.name))
                {
                    boneMap[t.name] = t;
                }
            }

            if (smr.bones != null && smr.bones.Length > 0)
            {
                Transform[] remappedBones = new Transform[smr.bones.Length];
                for (int i = 0; i < smr.bones.Length; i++)
                {
                    if (smr.bones[i] != null && boneMap.TryGetValue(smr.bones[i].name, out var found))
                    {
                        remappedBones[i] = found;
                    }
                    else
                    {
                        remappedBones[i] = smr.bones[i];
                    }
                }
                smr.bones = remappedBones;
            }

            if (smr.rootBone != null && boneMap.TryGetValue(smr.rootBone.name, out var rootFound))
            {
                smr.rootBone = rootFound;
            }
        }

        private void SetupShirtGrabHandle()
        {
            // Ubicar el área de agarre a la altura del esternón / dobladillo
            Transform spine = null;
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                if (t.name == "Spine" || t.name == "Spine1")
                {
                    spine = t;
                    break;
                }
            }

            if (spine != null && shirtGrabHandle == null)
            {
                GameObject handleObj = new GameObject("Shirt_GrabHandle");
                handleObj.transform.SetParent(spine);
                handleObj.transform.localPosition = new Vector3(0f, 0.08f, 0.16f); // Cara anterior del abdomen
                handleObj.transform.localRotation = Quaternion.identity;

                var box = handleObj.AddComponent<BoxCollider>();
                box.size = new Vector3(0.24f, 0.14f, 0.12f);
                box.isTrigger = true;

                var rb = handleObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;

                shirtGrabHandle = handleObj.AddComponent<XRGrabInteractable>();
                shirtGrabHandle.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            }
        }

        private void OnEnable()
        {
            if (shirtGrabHandle != null)
            {
                shirtGrabHandle.selectEntered.AddListener(OnShirtGrabbed);
            }
        }

        private void OnDisable()
        {
            if (shirtGrabHandle != null)
            {
                shirtGrabHandle.selectEntered.RemoveListener(OnShirtGrabbed);
            }
        }

        private void OnShirtGrabbed(SelectEnterEventArgs args)
        {
            ToggleTorso();
            EventBus.RequestHapticImpulse(HandSide.Both, 0.35f, 0.15f);
        }

        private void Update()
        {
            // Atajo de teclado ergonómico en PC (Tecla C para Camisa / Torso)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
            {
                ToggleTorso();
            }
        }

        public void ToggleTorso()
        {
            SetTorsoExposed(!isTorsoExposed);
        }

        /// <summary>
        /// Cambia la visibilidad de la vestimenta y revela el torso del civil.
        /// </summary>
        public void SetTorsoExposed(bool exposed)
        {
            isTorsoExposed = exposed;

            // 1. Alternar renderers de ropa
            foreach (var r in shirtRenderers)
            {
                if (r != null) r.enabled = !isTorsoExposed;
            }

            // 2. Alternar torso descubierto si está configurado
            if (bareTorsoObject != null)
            {
                bareTorsoObject.SetActive(isTorsoExposed);
            }

            // 3. Audio de roce y desprendimiento textil
            EventBus.RequestSpatialAudio("cloth_rustle", transform.position, 0.85f);

            string msg = isTorsoExposed 
                ? "[Inspección] Polo levantado: Torso y caja torácica expuestos para evaluación clínica."
                : "[Inspección] Polo colocado nuevamente.";
            Debug.Log(msg);
        }
    }
}
