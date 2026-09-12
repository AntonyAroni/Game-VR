using System.Collections.Generic;
using UnityEngine;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Survivors.Symptoms;

namespace ZombieCheckpoint.Survivors
{
    /// <summary>
    /// Componente acoplable a cualquier extremidad (antebrazo, tórax, cuello) para permitir su inspección.
    /// Principio de IHC: Affordances y Feedback Visual. Resalta la extremidad cuando las manos o herramientas se aproximan.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BodyPartExaminer : MonoBehaviour, IInspectableBodyPart
    {
        [Header("Identificación de la Extremidad")]
        [SerializeField] private string bodyPartName = "Antebrazo Derecho";
        [SerializeField] private List<Component> localSymptoms = new List<Component>();

        [Header("Retroalimentación Visual (Affordance)")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color highlightColor = new Color(0.3f, 0.8f, 1f, 0.5f);
        
        private Material originalMaterial;
        private Color originalColor;
        private bool isHighlighted;

        public string BodyPartName => bodyPartName;
        public Transform PartTransform => transform;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null && targetRenderer.material.HasProperty("_BaseColor"))
            {
                originalColor = targetRenderer.material.GetColor("_BaseColor");
            }
            else if (targetRenderer != null && targetRenderer.material.HasProperty("_Color"))
            {
                originalColor = targetRenderer.material.color;
            }

            // Auto-recopilar síntomas hijos si la lista está vacía
            if (localSymptoms.Count == 0)
            {
                GetComponentsInChildren(true, localSymptoms);
            }
        }

        public bool ReceiveInspection(string toolName)
        {
            bool anySuccess = false;
            var directSymptoms = GetComponents<ISymptom>();
            foreach (var symptom in directSymptoms)
            {
                if (symptom.TryExamine(toolName, out string msg))
                {
                    anySuccess = true;
                    EventBus.TriggerToolApplied(toolName, gameObject);
                }
            }

            if (!anySuccess)
            {
                foreach (var comp in localSymptoms)
                {
                    if (comp is ISymptom symptom)
                    {
                        if (symptom.TryExamine(toolName, out string msg))
                        {
                            anySuccess = true;
                            EventBus.TriggerToolApplied(toolName, gameObject);
                        }
                    }
                }
            }

            return anySuccess;
        }

        public void SetHighlight(bool highlighted)
        {
            if (targetRenderer == null || isHighlighted == highlighted) return;
            isHighlighted = highlighted;

            if (targetRenderer.material.HasProperty("_BaseColor"))
            {
                targetRenderer.material.SetColor("_BaseColor", highlighted ? highlightColor : originalColor);
            }
            else if (targetRenderer.material.HasProperty("_Color"))
            {
                targetRenderer.material.color = highlighted ? highlightColor : originalColor;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Detección de proximidad para feedback de affordance
            if (other.CompareTag("PlayerHand") || other.CompareTag("Tool"))
            {
                SetHighlight(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("PlayerHand") || other.CompareTag("Tool"))
            {
                SetHighlight(false);
            }
        }
    }
}
