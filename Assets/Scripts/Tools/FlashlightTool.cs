using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Survivors;

namespace ZombieCheckpoint.Tools
{
    public enum FlashlightMode
    {
        ClinicalWhite,
        Ultraviolet
    }

    /// <summary>
    /// Linterna de diagnóstico médico / luz UV.
    /// Principio de IHC: Feedback Multimodal Inmediato. Un clic mecánico táctil y auditivo confirma el encendido/apagado.
    /// Soporta interacción directa VR (gatillo) y atajos del simulador (clic izquierdo o tecla F).
    /// Alterna entre modo Luz Clínica (blanca) y Luz Forense Ultravioleta 395nm (tecla U o clic central).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class FlashlightTool : MonoBehaviour, IInspectionTool
    {
        [Header("Configuración")]
        [SerializeField] private string toolName = "Flashlight";
        [SerializeField] private FlashlightMode currentMode = FlashlightMode.ClinicalWhite;
        [SerializeField] private Light spotLight;
        [SerializeField] private float beamRange = 4.0f;
        [SerializeField] private LayerMask detectionLayer = ~0;
        [SerializeField] private bool startsOn = true;
        [SerializeField] private GameObject volumetricBeamObject;

        private XRGrabInteractable grabInteractable;
        private HandSide currentHoldingHand = HandSide.Right;
        private bool isOn = true;
        private int lastToggleFrame = -1;
        private MeshRenderer meshRenderer;
        private Material bulbMaterial;
        private Material lensMaterial;

        public string ToolName => toolName;
        public FlashlightMode CurrentMode => currentMode;
        public bool IsGrabbed => grabInteractable != null && grabInteractable.isSelected;
        public bool IsOn => isOn;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            EnsureLightReference();
            CacheMaterials();
            EnsureVolumetricBeam();
            isOn = startsOn;
            UpdateLightState();
        }

        private void Start()
        {
            EnsureLightReference();
            CacheMaterials();
            EnsureVolumetricBeam();
            UpdateLightState();
        }

        private void EnsureLightReference()
        {
            if (spotLight == null)
            {
                spotLight = GetComponentInChildren<Light>(true);
            }

            if (spotLight != null)
            {
                spotLight.intensity = Mathf.Max(spotLight.intensity, 25.0f);
                spotLight.range = Mathf.Max(spotLight.range, 10.0f);
            }
        }

        private void CacheMaterials()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>() ?? GetComponentInChildren<MeshRenderer>();
            }

            if (meshRenderer != null)
            {
                foreach (var mat in meshRenderer.materials)
                {
                    if (mat.name.Contains("Bulb")) bulbMaterial = mat;
                    else if (mat.name.Contains("Lens")) lensMaterial = mat;
                }
            }
        }

        private void EnsureVolumetricBeam()
        {
            if (spotLight == null) return;

            if (volumetricBeamObject == null)
            {
                var beamTransform = spotLight.transform.Find("VolumetricBeam");
                if (beamTransform != null)
                {
                    volumetricBeamObject = beamTransform.gameObject;
                }
                else
                {
                    volumetricBeamObject = CreateProceduralBeam(spotLight.transform);
                }
            }
        }

        private GameObject CreateProceduralBeam(Transform parent)
        {
            int segments = 24;
            float length = 3.5f;
            float startRadius = 0.045f;
            float endRadius = 0.85f;

            Mesh mesh = new Mesh { name = "Flashlight_Beam_Mesh" };
            Vector3[] vertices = new Vector3[segments * 2];
            Color[] colors = new Color[segments * 2];
            int[] triangles = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * startRadius, sin * startRadius, 0.05f);
                colors[i * 2] = new Color(0.85f, 0.95f, 1f, 0.20f);

                vertices[i * 2 + 1] = new Vector3(cos * endRadius, sin * endRadius, length);
                colors[i * 2 + 1] = new Color(0.85f, 0.95f, 1f, 0.0f);

                int next = (i + 1) % segments;
                int triIdx = i * 6;

                triangles[triIdx + 0] = i * 2;
                triangles[triIdx + 1] = next * 2;
                triangles[triIdx + 2] = i * 2 + 1;

                triangles[triIdx + 3] = next * 2;
                triangles[triIdx + 4] = next * 2 + 1;
                triangles[triIdx + 5] = i * 2 + 1;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject beam = new GameObject("VolumetricBeam");
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = Vector3.zero;
            beam.transform.localRotation = Quaternion.identity;

            var mf = beam.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = beam.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "M_Flashlight_Beam_Instance" };
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.SetColor("_BaseColor", new Color(0.9f, 0.95f, 1.0f, 0.25f));
            mat.renderQueue = 3000;
            mr.sharedMaterial = mat;

            return beam;
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
        }

        private void OnActivated(ActivateEventArgs args)
        {
            OnPrimaryActionTriggered();
        }

        public void OnPrimaryActionTriggered()
        {
            if (Time.frameCount == lastToggleFrame) return; // Evitar doble conmutación en el mismo fotograma
            lastToggleFrame = Time.frameCount;

            isOn = !isOn;
            UpdateLightState();

            // Feedback háptico y auditivo de clic
            EventBus.RequestHapticImpulse(currentHoldingHand, 0.45f, 0.06f);
            EventBus.RequestSpatialAudio("flashlight_click", transform.position, 1.0f);
            Debug.Log($"[Linterna] {(isOn ? "💡 Luz ENCENDIDA (Haz visible activo)" : "🌑 Luz APAGADA")}");
        }

        public void ToggleLightMode()
        {
            currentMode = (currentMode == FlashlightMode.ClinicalWhite) 
                ? FlashlightMode.Ultraviolet 
                : FlashlightMode.ClinicalWhite;

            UpdateLightState();
            EventBus.RequestHapticImpulse(currentHoldingHand, 0.4f, 0.08f);
            EventBus.RequestSpatialAudio("uv_switch", transform.position, 1.0f);
            if (currentMode == FlashlightMode.Ultraviolet)
            {
                EventBus.RequestSpatialAudio("uv_hum", transform.position, 0.65f);
            }
            Debug.Log($"[Linterna] 🔄 Modo cambiado a: {(currentMode == FlashlightMode.Ultraviolet ? "🟣 ULTRAVIOLETA (Luz Forense Wood 395nm)" : "⚪ LUZ BLANCA CLÍNICA")}");
        }

        private void UpdateLightState()
        {
            EnsureLightReference();

            Color lightColor = (currentMode == FlashlightMode.Ultraviolet) 
                ? new Color(0.48f, 0.12f, 1.0f) 
                : new Color(1.0f, 0.96f, 0.88f);

            Color beamColor = (currentMode == FlashlightMode.Ultraviolet) 
                ? new Color(0.55f, 0.15f, 1.0f, 0.35f) 
                : new Color(0.9f, 0.95f, 1.0f, 0.22f);

            Color emission = !isOn 
                ? Color.black 
                : (currentMode == FlashlightMode.Ultraviolet ? new Color(1.8f, 0.35f, 3.8f) : new Color(3.0f, 2.8f, 2.0f));

            if (spotLight != null)
            {
                spotLight.enabled = isOn;
                spotLight.color = lightColor;
            }

            if (volumetricBeamObject != null)
            {
                volumetricBeamObject.SetActive(isOn);
                var beamRend = volumetricBeamObject.GetComponent<MeshRenderer>();
                if (beamRend != null && beamRend.sharedMaterial != null)
                {
                    beamRend.sharedMaterial.SetColor("_BaseColor", beamColor);
                }
            }

            // Emisión visual de la bombilla y la lente
            if (bulbMaterial != null)
            {
                if (isOn) bulbMaterial.EnableKeyword("_EMISSION");
                else bulbMaterial.DisableKeyword("_EMISSION");
                bulbMaterial.SetColor("_EmissionColor", emission);
            }

            if (lensMaterial != null)
            {
                if (isOn) lensMaterial.EnableKeyword("_EMISSION");
                else lensMaterial.DisableKeyword("_EMISSION");
                lensMaterial.SetColor("_EmissionColor", isOn ? emission * 0.7f : Color.black);
            }
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;

            bool fPressed = keyboard != null && (keyboard.fKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            bool uPressed = keyboard != null && (keyboard.uKey.wasPressedThisFrame || keyboard.xKey.wasPressedThisFrame);
            bool middleClick = mouse != null && mouse.middleButton.wasPressedThisFrame;
            bool clickPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

            if (uPressed || middleClick)
            {
                ToggleLightMode();
            }

            if (fPressed)
            {
                OnPrimaryActionTriggered();
            }
            else if (clickPressed)
            {
                if (IsGrabbed)
                {
                    OnPrimaryActionTriggered();
                }
                else if (Camera.main != null)
                {
                    Ray rayClick = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
                    if (Physics.Raycast(rayClick, out RaycastHit hitInfo, 5f) && 
                        (hitInfo.collider.gameObject == gameObject || hitInfo.transform.IsChildOf(transform)))
                    {
                        OnPrimaryActionTriggered();
                    }
                }
            }

            if (!isOn || spotLight == null) return;

            // Haz de luz proyectado hacia adelante
            Ray ray = new Ray(spotLight.transform.position, spotLight.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, beamRange, detectionLayer))
            {
                ApplyToTarget(hit.collider.gameObject);
            }
        }

        public void ApplyToTarget(GameObject target)
        {
            string inspectionIdentifier = (currentMode == FlashlightMode.Ultraviolet) ? "Flashlight_UV" : "Flashlight";

            if (target.TryGetComponent(out IInspectableBodyPart bodyPart))
            {
                bodyPart.ReceiveInspection(inspectionIdentifier);
            }
            else
            {
                var parentBody = target.GetComponentInParent<IInspectableBodyPart>();
                if (parentBody != null)
                {
                    parentBody.ReceiveInspection(inspectionIdentifier);
                }
            }

            // Exposición a UV en documentos
            var doc = target.GetComponent<ZombieCheckpoint.Documents.DocumentInteractable>() 
                   ?? target.GetComponentInParent<ZombieCheckpoint.Documents.DocumentInteractable>();
            if (doc != null)
            {
                doc.ReceiveUVLight(currentMode == FlashlightMode.Ultraviolet);
            }
        }
    }
}
