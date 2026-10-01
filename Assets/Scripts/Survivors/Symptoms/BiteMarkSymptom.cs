using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Survivors.Symptoms
{
    /// <summary>
    /// Síntoma de mordedura oculta en la piel.
    /// Principio de IHC: Mapeo natural. El usuario debe rotar el brazo para exponer la cara interna y observar la herida.
    /// </summary>
    public class BiteMarkSymptom : MonoBehaviour, ISymptom
    {
        [Header("Configuración del Síntoma")]
        [SerializeField] private string symptomName = "MORDEDURA";
        [SerializeField] private bool isPositiveForInfection = true;
        [SerializeField] private GameObject woundVisualObject;
        [SerializeField] private Transform woundTransform;
        
        [Header("Detección por Ángulo de Visión")]
        [Tooltip("Ángulo mínimo hacia la cámara para considerarse visible")]
        [SerializeField] private float visibilityThreshold = 0.4f;

        [Header("Apariencia de la Herida")]
        [Tooltip("Diámetro de la mordedura sobre la piel (m).")]
        [SerializeField] private float woundSize = 0.08f;

        public string SymptomName => symptomName;
        public bool IsPositiveForInfection => isPositiveForInfection;
        public bool IsDiscovered { get; private set; }

        private Camera mainCamera;
        private Material biteMaterial;

        /// <summary>
        /// true cuando la herida es un Quad apoyado en la piel por <see cref="SkinMarkFactory"/>:
        /// su cara visible mira hacia -Z, así que la normal exterior es -forward.
        /// </summary>
        private bool woundFacesNegativeZ;

        // En el rig del pack de personajes, +X local de RightForeArm recorre el hueso hasta la muñeca (0.28 m),
        // +Y apunta hacia el cuerpo y -Z hacia delante. La herida va a media distancia, en la cara
        // anterior-interna: se intuye de reojo y se ve entera al girar la muñeca del civil.
        private static readonly Vector3 WoundAnchorOnBone = new Vector3(0.15f, 0f, 0f);
        private static readonly Vector3 WoundSideOnBone = new Vector3(0f, 0.7071f, -0.7071f);
        private const float SnapRayStartDistance = 0.15f;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (woundTransform == null && woundVisualObject != null)
            {
                woundTransform = woundVisualObject.transform;
            }
        }

        public void Initialize(bool hasBite)
        {
            isPositiveForInfection = hasBite;
            IsDiscovered = false;
            if (woundVisualObject != null)
            {
                woundVisualObject.SetActive(hasBite);
                if (hasBite && Application.isPlaying)
                {
                    DressWoundOnSkin();
                }
            }
        }

        /// <summary>
        /// Convierte la herida en una mordedura realista (arcos dentales sobre hematoma) apoyada sobre la piel
        /// del antebrazo. Antes era un cuadrado rojo plano que, visto de canto, parecía una línea suelta.
        /// Respeta un material con textura asignado desde el Inspector.
        /// </summary>
        private void DressWoundOnSkin()
        {
            if (woundTransform == null) woundTransform = woundVisualObject.transform;
            if (!woundVisualObject.TryGetComponent(out Renderer woundRenderer)) return;

            Material current = woundRenderer.sharedMaterial;
            bool hasAuthoredTexture = current != null && current.mainTexture != null;
            if (!hasAuthoredTexture)
            {
                if (biteMaterial == null)
                {
                    biteMaterial = SkinMarkFactory.CreateMarkMaterial(SkinMarkFactory.BiteTexture, false, Color.white);
                    biteMaterial.name = "M_BiteWound";
                    if (biteMaterial.HasProperty("_Smoothness")) biteMaterial.SetFloat("_Smoothness", 0.55f);
                }
                SkinMarkFactory.ConfigureMarkRenderer(woundRenderer, biteMaterial);
            }

            SkinnedMeshRenderer armSkin = FindArmSkin();
            if (armSkin == null) return;

            woundTransform.localScale = Vector3.one * woundSize;
            Vector3 outward = transform.TransformDirection(WoundSideOnBone);
            Vector3 anchor = transform.TransformPoint(WoundAnchorOnBone);
            Vector3 rayOrigin = anchor + outward * SnapRayStartDistance;
            // "Arriba" de la marca a lo largo del antebrazo, para que los arcos dentales crucen el brazo.
            woundFacesNegativeZ = SkinMarkFactory.TrySnapToSkin(woundTransform, armSkin, rayOrigin, -outward, transform.right, 0f);
            if (!woundFacesNegativeZ)
            {
                Debug.LogWarning($"[BiteMarkSymptom] No se encontró la piel del antebrazo bajo la herida de {name}; se mantiene su posición original.");
            }
        }

        /// <summary>Malla de brazos de mayor detalle (LOD0) del civil al que pertenece este hueso.</summary>
        private SkinnedMeshRenderer FindArmSkin()
        {
            Transform root = transform.root;
            SkinnedMeshRenderer fallback = null;
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string lowerName = smr.name.ToLowerInvariant();
                if (!lowerName.Contains("arm")) continue;
                if (lowerName.EndsWith("_lod0")) return smr;
                if (fallback == null) fallback = smr;
            }
            return fallback;
        }

        private void Update()
        {
            if (!isPositiveForInfection || IsDiscovered || woundTransform == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Verificar si la mordedura está orientada hacia el visor del jugador
            Vector3 dirToCam = (mainCamera.transform.position - woundTransform.position).normalized;
            Vector3 woundNormal = woundFacesNegativeZ ? -woundTransform.forward : woundTransform.forward;
            float alignment = Vector3.Dot(woundNormal, dirToCam);

            // Si está orientada de frente y a menos de 1.2 metros
            float distance = Vector3.Distance(mainCamera.transform.position, woundTransform.position);
            if (alignment > visibilityThreshold && distance < 1.2f)
            {
                DiscoverSymptom("¡Tiene una mordedura de infectado!");
            }
        }

        public bool TryExamine(string toolName, out string feedbackMessage)
        {
            if (!isPositiveForInfection)
            {
                feedbackMessage = "Brazo limpio, sin marcas.";
                return true;
            }

            DiscoverSymptom("¡Tiene una mordedura de infectado!");
            feedbackMessage = "¡Tiene una mordedura de infectado!";
            return true;
        }

        private void DiscoverSymptom(string message)
        {
            if (IsDiscovered) return;
            IsDiscovered = true;
            EventBus.TriggerSymptomDiscovered(symptomName, message);
            EventBus.RequestHapticImpulse(HandSide.Both, 0.4f, 0.2f);
        }
    }
}
