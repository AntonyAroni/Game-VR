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

        public string SymptomName => symptomName;
        public bool IsPositiveForInfection => isPositiveForInfection;
        public bool IsDiscovered { get; private set; }

        private Camera mainCamera;

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
            }
        }

        private void Update()
        {
            if (!isPositiveForInfection || IsDiscovered || woundTransform == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Verificar si la mordedura está orientada hacia el visor del jugador
            Vector3 dirToCam = (mainCamera.transform.position - woundTransform.position).normalized;
            float alignment = Vector3.Dot(woundTransform.forward, dirToCam);

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
