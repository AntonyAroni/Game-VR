using System.Collections.Generic;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Survivors.Symptoms
{
    /// <summary>
    /// Síntoma dermatológico de infección: Erupción eritematosa y petequias purpúreas en el tórax/abdomen.
    /// Principio de IHC: Visibilidad y Diagnóstico Secuencial.
    /// - Solo es visible si el jugador levanta/retira el polo del civil (TorsoClothingController.IsTorsoExposed).
    /// - Reacciona a la radiación UV (lámpara de Wood) con fluorescencia patológica.
    /// </summary>
    public class RashSymptom : MonoBehaviour, ISymptom
    {
        [Header("Configuración del Síntoma")]
        [SerializeField] private string symptomName = "PECHO (ERUPCIÓN)";
        [SerializeField] private bool isPositiveForInfection = false;

        [Header("Control de Visibilidad")]
        [SerializeField] private TorsoClothingController clothingController;
        [SerializeField] private GameObject rashContainer;
        [SerializeField] private List<Renderer> patchRenderers = new List<Renderer>();

        [Header("Materiales")]
        [SerializeField] private Material clinicalRashMat;
        [SerializeField] private Material uvFluorescentMat;

        public string SymptomName => symptomName;
        public bool IsPositiveForInfection => isPositiveForInfection;
        public bool IsDiscovered { get; private set; }

        private Camera mainCamera;
        private bool isUvActive = false;
        private bool patchesSnappedToSkin = false;

        // Puntos de anclaje de cada mancha en el espacio local de Spine1 (+Z = pecho) y su tamaño en metros.
        // Evitan la franja pectoral: el torso femenino del pack incluye top y las manchas quedaban pintadas
        // encima de la prenda. Parte alta del pecho, costillas y abdomen son piel en ambos modelos.
        private static readonly Vector3[] PatchAnchors =
        {
            new Vector3(0.04f, 0.16f, 0f),    // Parte alta del pecho, bajo la clavícula
            new Vector3(-0.08f, -0.03f, 0f),  // Costillas izquierdas
            new Vector3(0.09f, -0.05f, 0f),   // Costillas derechas
            new Vector3(0.0f, -0.09f, 0f),    // Región epigástrica
            new Vector3(-0.05f, -0.13f, 0f),  // Abdomen
        };
        private static readonly float[] PatchSizes = { 0.095f, 0.11f, 0.09f, 0.08f, 0.085f };

        /// <summary>Distancia a la que nace el rayo de apoyo delante del pecho (siempre fuera del cuerpo).</summary>
        private const float SnapRayStartDepth = 0.4f;
        /// <summary>Profundidad de la mancha cuando no hay malla de torso a la que apoyarse.</summary>
        private const float FallbackSurfaceDepth = 0.14f;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (clothingController == null)
            {
                clothingController = GetComponentInParent<TorsoClothingController>() ?? GetComponent<TorsoClothingController>();
            }

            CreateMaterialsIfNull();
            BuildProceduralRashPatches();
        }

        private void CreateMaterialsIfNull()
        {
            if (clinicalRashMat == null)
            {
                // Manchas rojizas de borde difuso con petequias: se leen como lesión de piel, no como un parche.
                clinicalRashMat = SkinMarkFactory.CreateMarkMaterial(SkinMarkFactory.RashTexture, false, Color.white);
                clinicalRashMat.name = "M_Rash_Clinical";
                if (clinicalRashMat.HasProperty("_Smoothness")) clinicalRashMat.SetFloat("_Smoothness", 0.45f);
            }

            if (uvFluorescentMat == null)
            {
                // Misma forma, brillo propio verde bajo la lámpara de Wood.
                uvFluorescentMat = SkinMarkFactory.CreateMarkMaterial(SkinMarkFactory.RashTexture, true, new Color(0.25f, 1.0f, 0.45f, 1.0f));
                uvFluorescentMat.name = "M_Rash_UVFluorescent";
            }
        }

        private void BuildProceduralRashPatches()
        {
            if (rashContainer != null) return;

            rashContainer = new GameObject("Rash_Patches_Container");
            rashContainer.transform.SetParent(transform, false);
            rashContainer.transform.localPosition = Vector3.zero;

            for (int i = 0; i < PatchAnchors.Length; i++)
            {
                GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Quad);
                patch.name = $"Rash_Spot_{i + 1}";
                patch.transform.SetParent(rashContainer.transform, false);
                patch.transform.localScale = Vector3.one * PatchSizes[i];

                Destroy(patch.GetComponent<Collider>());

                var rend = patch.GetComponent<Renderer>();
                SkinMarkFactory.ConfigureMarkRenderer(rend, isUvActive ? uvFluorescentMat : clinicalRashMat);
                patchRenderers.Add(rend);
            }

            PlacePatchesInFrontOfChest();
            rashContainer.SetActive(false);
        }

        /// <summary>
        /// Posición provisional sobre el pecho, con la cara visible del Quad mirando hacia fuera del cuerpo.
        /// Se usa hasta que exista una malla de torso a la que apoyar las manchas.
        /// </summary>
        private void PlacePatchesInFrontOfChest()
        {
            for (int i = 0; i < patchRenderers.Count && i < PatchAnchors.Length; i++)
            {
                if (patchRenderers[i] == null) continue;
                Transform patch = patchRenderers[i].transform;
                patch.localPosition = PatchAnchors[i] + Vector3.forward * FallbackSurfaceDepth;
                // El Quad se ve desde su -Z: su +Z debe apuntar hacia dentro del pecho (-Z local de Spine1).
                patch.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up) * Quaternion.Euler(0f, 0f, PatchRoll(i));
            }
        }

        /// <summary>
        /// Apoya cada mancha exactamente sobre la piel del torso descubierto, siguiendo su curvatura.
        /// Se ejecuta una vez, la primera vez que el torso queda a la vista.
        /// </summary>
        private void SnapPatchesToSkin()
        {
            if (patchesSnappedToSkin || clothingController == null) return;
            SkinnedMeshRenderer torso = clothingController.BareTorsoRenderer;
            if (torso == null) return;

            patchesSnappedToSkin = true;
            Vector3 inward = -transform.forward;
            for (int i = 0; i < patchRenderers.Count && i < PatchAnchors.Length; i++)
            {
                if (patchRenderers[i] == null) continue;
                Vector3 rayOrigin = transform.TransformPoint(PatchAnchors[i] + Vector3.forward * SnapRayStartDepth);
                if (!SkinMarkFactory.TrySnapToSkin(patchRenderers[i].transform, torso, rayOrigin, inward, transform.up, PatchRoll(i)))
                {
                    Debug.LogWarning($"[RashSymptom] No se encontró piel bajo {patchRenderers[i].name}; se mantiene su posición provisional.");
                }
            }
        }

        /// <summary>Giro fijo por mancha: variedad visual sin depender de Random entre rondas.</summary>
        private static float PatchRoll(int index) => index * 67f % 360f;

        public void Initialize(bool hasRash)
        {
            isPositiveForInfection = hasRash;
            IsDiscovered = false;

            if (clothingController == null)
            {
                clothingController = GetComponentInParent<TorsoClothingController>() ?? GetComponent<TorsoClothingController>();
            }

            UpdatePatchVisibility();
        }

        private void OnEnable()
        {
            EventBus.OnFlashlightModeChanged += HandleFlashlightModeChanged;
        }

        private void OnDisable()
        {
            EventBus.OnFlashlightModeChanged -= HandleFlashlightModeChanged;
        }

        private void HandleFlashlightModeChanged(bool uvActive)
        {
            isUvActive = uvActive;
            Material activeMat = isUvActive ? uvFluorescentMat : clinicalRashMat;
            foreach (var r in patchRenderers)
            {
                if (r != null) r.sharedMaterial = activeMat;
            }
        }

        private void Update()
        {
            UpdatePatchVisibility();

            if (!isPositiveForInfection || IsDiscovered) return;

            // Solo se puede descubrir si el torso está descubierto
            bool isExposed = clothingController != null && clothingController.IsTorsoExposed;
            if (!isExposed) return;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Verificar si el jugador está mirando de frente hacia el pecho
            Vector3 chestPos = transform.position + Vector3.up * 0.1f;
            float dist = Vector3.Distance(mainCamera.transform.position, chestPos);
            if (dist < 1.35f)
            {
                Vector3 toPlayer = (mainCamera.transform.position - chestPos).normalized;
                float dot = Vector3.Dot(transform.forward, toPlayer);

                if (dot > 0.35f)
                {
                    DiscoverSymptom("¡Manchas rojas en el pecho!");
                }
            }
        }

        private void UpdatePatchVisibility()
        {
            if (rashContainer == null) return;

            bool isExposed = clothingController == null || clothingController.IsTorsoExposed;
            bool shouldShow = isPositiveForInfection && isExposed;

            if (rashContainer.activeSelf != shouldShow)
            {
                if (shouldShow) SnapPatchesToSkin();
                rashContainer.SetActive(shouldShow);
            }
        }

        public bool TryExamine(string toolName, out string feedbackMessage)
        {
            bool isExposed = clothingController != null && clothingController.IsTorsoExposed;
            if (!isExposed)
            {
                feedbackMessage = "Primero presiona VER PECHO.";
                return false;
            }

            if (!isPositiveForInfection)
            {
                feedbackMessage = "Pecho sano, sin marcas.";
                return true;
            }

            DiscoverSymptom("¡Manchas rojas en el pecho!");
            feedbackMessage = isUvActive 
                ? "¡Las manchas brillan en verde fosforescente!"
                : "¡Manchas rojas en el pecho!";
            return true;
        }

        private void DiscoverSymptom(string message)
        {
            if (IsDiscovered) return;
            IsDiscovered = true;
            EventBus.TriggerSymptomDiscovered(symptomName, message);
            EventBus.RequestHapticImpulse(HandSide.Both, 0.45f, 0.22f);
        }
    }
}
