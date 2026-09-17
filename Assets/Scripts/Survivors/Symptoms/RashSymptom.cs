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
        [SerializeField] private string symptomName = "Erupción Cutánea en Torso";
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
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (clinicalRashMat == null)
            {
                clinicalRashMat = new Material(urpLit);
                clinicalRashMat.name = "M_Rash_Clinical";
                clinicalRashMat.color = new Color(0.72f, 0.08f, 0.08f, 0.95f);
                // Brillo leve de inflamación
                clinicalRashMat.SetFloat("_Smoothness", 0.65f);
            }

            if (uvFluorescentMat == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                uvFluorescentMat = new Material(unlitShader);
                uvFluorescentMat.name = "M_Rash_UVFluorescent";
                uvFluorescentMat.color = new Color(0.1f, 1.0f, 0.35f, 1.0f);
            }
        }

        private void BuildProceduralRashPatches()
        {
            if (rashContainer != null) return;

            rashContainer = new GameObject("Rash_Patches_Container");
            rashContainer.transform.SetParent(transform, false);
            rashContainer.transform.localPosition = Vector3.zero;

            // Generar un conjunto orgánico de manchas en el pecho y costillas anteriores
            Vector3[] patchOffsets = new Vector3[]
            {
                new Vector3(0.04f, 0.05f, 0.14f),   // Costilla derecha
                new Vector3(-0.06f, 0.08f, 0.13f),  // Pectoral izquierdo
                new Vector3(0.01f, -0.04f, 0.15f),  // Región epigástrica
                new Vector3(0.08f, -0.02f, 0.12f),  // Flanco derecho
                new Vector3(-0.07f, -0.05f, 0.12f), // Flanco izquierdo
            };

            Vector3[] patchScales = new Vector3[]
            {
                new Vector3(0.065f, 0.05f, 0.01f),
                new Vector3(0.085f, 0.065f, 0.01f),
                new Vector3(0.055f, 0.045f, 0.01f),
                new Vector3(0.045f, 0.04f, 0.01f),
                new Vector3(0.05f, 0.05f, 0.01f)
            };

            for (int i = 0; i < patchOffsets.Length; i++)
            {
                GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Quad);
                patch.name = $"Rash_Spot_{i + 1}";
                patch.transform.SetParent(rashContainer.transform, false);
                patch.transform.localPosition = patchOffsets[i];
                patch.transform.localRotation = Quaternion.Euler(5f, 0f, Random.Range(-25f, 25f));
                patch.transform.localScale = patchScales[i];

                Destroy(patch.GetComponent<Collider>());

                var rend = patch.GetComponent<Renderer>();
                rend.sharedMaterial = clinicalRashMat;
                patchRenderers.Add(rend);
            }

            rashContainer.SetActive(false);
        }

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
                    DiscoverSymptom("¡Erupción eritematosa localizada en la caja torácica!");
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
                rashContainer.SetActive(shouldShow);
            }
        }

        public bool TryExamine(string toolName, out string feedbackMessage)
        {
            bool isExposed = clothingController != null && clothingController.IsTorsoExposed;
            if (!isExposed)
            {
                feedbackMessage = "El torso está cubierto por la ropa. Retira o levanta el polo primero.";
                return false;
            }

            if (!isPositiveForInfection)
            {
                feedbackMessage = "Piel del torso despejada, sin signos de petequias o eritema infeccioso.";
                return true;
            }

            DiscoverSymptom("Erupción infecciosa en torso confirmada.");
            feedbackMessage = isUvActive 
                ? "¡Fluorescencia vírica verde intensa en tejido vascular bajo haz UV!"
                : "¡Erupción eritematosa activa y petequias purpúreas en la piel del pecho!";
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
