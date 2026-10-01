using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Survivors.Symptoms
{
    /// <summary>
    /// Síntoma ocular de respuesta pupilar a la luz (Pupil Light Reflex) y fluorescencia UV.
    /// Principio de IHC: Feedback Multimodal e Interacción Diegética Física.
    /// - Ciudadano Sano: Miosis fotomotora activa (las pupilas se contraen en tiempo real de 1.0x a 0.35x ante la luz blanca).
    /// - Sujeto Infectado: Midriasis bilateral fija (pupilas dilatadas a 1.35x e inmóviles).
    /// - Modo UV (Luz de Wood): Revela bioluminiscencia viral corneal fluorescente en sujetos infectados.
    /// </summary>
    public class PupilSymptom : MonoBehaviour, ISymptom
    {
        [Header("Configuración del Síntoma")]
        [SerializeField] private string symptomName = "OJOS (PUPILAS)";
        [SerializeField] private bool isPositiveForInfection = false;

        public string SymptomName => symptomName;
        public bool IsPositiveForInfection => isPositiveForInfection;
        public bool IsDiscovered { get; private set; }

        private float lastExamineTime = -10f;
        [SerializeField] private float examineCooldown = 2.0f;

        private GameObject leftPupil;
        private GameObject rightPupil;
        private Material pupilMaterial;
        // Disco de ~7.5 mm: algo mayor que una pupila real (4-6 mm) para que su reacción a la luz se lea en
        // la resolución del visor, pero ya no más grande que el iris (antes medía 15 mm y tapaba el ojo).
        private Vector3 basePupilScale = new Vector3(0.0075f, 0.0075f, 0.003f);

        // Centro del iris en el espacio local del hueso Head (X lateral, Y altura), medido sobre las dos
        // cabezas del pack de personajes. La profundidad se resuelve apoyando la pupila sobre la malla.
        private static readonly Vector2 MaleEyeAnchor = new Vector2(0.033f, 0.076f);
        private static readonly Vector2 FemaleEyeAnchor = new Vector2(0.032f, 0.073f);
        private const float FallbackEyeDepth = 0.108f;
        private const float EyeRayStartDepth = 0.3f;
        private float currentPupilScaleFactor = 1.0f;
        private float lastLightExposeTime = -10f;
        private bool lastExposeWasUV = false;

        private void Awake()
        {
            EnsureEyePupils();
        }

        public void Initialize(bool isAbnormal)
        {
            isPositiveForInfection = isAbnormal;
            IsDiscovered = false;
            lastExamineTime = -10f;
            lastLightExposeTime = -10f;
            currentPupilScaleFactor = isAbnormal ? 1.35f : 1.0f;
            EnsureEyePupils();
            if (Application.isPlaying)
            {
                SeatPupilsOnEyes();
            }
            UpdatePupilAppearance();
        }

        /// <summary>
        /// Coloca cada pupila en el centro del iris de la cabeza real del civil y la apoya sobre la superficie
        /// del ojo. Antes usaban una posición genérica que en ambas cabezas caía en las mejillas.
        /// </summary>
        private void SeatPupilsOnEyes()
        {
            SkinnedMeshRenderer headSkin = FindHeadSkin();
            bool isFemaleHead = headSkin != null && headSkin.name.ToLowerInvariant().Contains("_01f_");
            Vector2 anchor = isFemaleHead ? FemaleEyeAnchor : MaleEyeAnchor;

            SeatPupil(leftPupil, new Vector2(-anchor.x, anchor.y), headSkin);
            SeatPupil(rightPupil, new Vector2(anchor.x, anchor.y), headSkin);
        }

        private void SeatPupil(GameObject pupil, Vector2 anchor, SkinnedMeshRenderer headSkin)
        {
            if (pupil == null) return;
            Transform pupilTransform = pupil.transform;
            pupilTransform.localPosition = new Vector3(anchor.x, anchor.y, FallbackEyeDepth);
            pupilTransform.localRotation = Quaternion.identity;
            if (headSkin == null) return;

            // +Z local del hueso Head apunta hacia la cara.
            Vector3 rayOrigin = transform.TransformPoint(new Vector3(anchor.x, anchor.y, EyeRayStartDepth));
            Vector3 rayDirection = transform.TransformDirection(Vector3.back);
            if (SkinMarkFactory.TryRaycastSkin(headSkin, rayOrigin, rayDirection, out Vector3 hit, out Vector3 normal))
            {
                // Media profundidad del disco por delante de la córnea: visible, sin flotar.
                pupilTransform.position = hit + normal * (basePupilScale.z * 0.5f);
                pupilTransform.rotation = Quaternion.LookRotation(-normal, transform.up);
            }
            else
            {
                Debug.LogWarning($"[PupilSymptom] No se encontró la superficie del ojo en {headSkin.name}; se usa la profundidad por defecto.");
            }
        }

        /// <summary>Malla de cabeza de mayor detalle (LOD0) del civil al que pertenece este hueso.</summary>
        private SkinnedMeshRenderer FindHeadSkin()
        {
            SkinnedMeshRenderer fallback = null;
            foreach (var smr in transform.root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string lowerName = smr.name.ToLowerInvariant();
                if (!lowerName.Contains("head")) continue;
                if (lowerName.EndsWith("_lod0")) return smr;
                if (fallback == null) fallback = smr;
            }
            return fallback;
        }

        private void EnsureEyePupils()
        {
            if (leftPupil == null)
            {
                var existingLeft = transform.Find("Pupil_Left");
                if (existingLeft != null) leftPupil = existingLeft.gameObject;
                else leftPupil = CreatePupilObject("Pupil_Left", new Vector3(-0.033f, 0.052f, 0.108f));
            }

            if (rightPupil == null)
            {
                var existingRight = transform.Find("Pupil_Right");
                if (existingRight != null) rightPupil = existingRight.gameObject;
                else rightPupil = CreatePupilObject("Pupil_Right", new Vector3(0.033f, 0.052f, 0.108f));
            }
        }

        private GameObject CreatePupilObject(string pupilName, Vector3 localPos)
        {
            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = pupilName;
            pupil.transform.SetParent(transform, false);
            pupil.transform.localPosition = localPos;
            pupil.transform.localRotation = Quaternion.identity;
            pupil.transform.localScale = basePupilScale;

            var col = pupil.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            if (pupilMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                pupilMaterial = new Material(shader) { name = "M_Pupil_Dynamic_Instance" };
            }

            pupil.GetComponent<Renderer>().sharedMaterial = pupilMaterial;
            return pupil;
        }

        private void UpdatePupilAppearance()
        {
            if (pupilMaterial == null) return;

            if (isPositiveForInfection)
            {
                // Aspecto vítreo / catarata dilatada anómala
                pupilMaterial.color = new Color(0.12f, 0.04f, 0.04f, 1.0f);
            }
            else
            {
                // Iris / pupila reactiva sana profunda
                pupilMaterial.color = new Color(0.02f, 0.02f, 0.02f, 1.0f);
            }
        }

        public bool TryExamine(string toolName, out string feedbackMessage)
        {
            bool isNormalFlashlight = (toolName == "Flashlight");
            bool isUVFlashlight = (toolName == "Flashlight_UV");

            if (!isNormalFlashlight && !isUVFlashlight)
            {
                feedbackMessage = "Usa la linterna.";
                return false;
            }

            lastLightExposeTime = Time.time;
            lastExposeWasUV = isUVFlashlight;

            if (Time.time - lastExamineTime < examineCooldown)
            {
                feedbackMessage = "";
                return false;
            }
            lastExamineTime = Time.time;

            EventBus.RequestSpatialAudio("pupil_scan_beep", transform.position, 1.0f);

            if (isUVFlashlight)
            {
                if (isPositiveForInfection)
                {
                    DiscoverSymptom("¡Los ojos brillan en verde fosforescente!");
                    feedbackMessage = "¡Los ojos brillan en verde fosforescente!";
                    return true;
                }
                else
                {
                    EventBus.RequestHapticImpulse(HandSide.Both, 0.2f, 0.08f);
                    feedbackMessage = "Ojos sanos, no brillan.";
                    return true;
                }
            }
            else
            {
                if (isPositiveForInfection)
                {
                    DiscoverSymptom("¡Las pupilas no se mueven con la luz!");
                    feedbackMessage = "¡Las pupilas no se mueven con la luz!";
                    return true;
                }
                else
                {
                    EventBus.RequestHapticImpulse(HandSide.Both, 0.2f, 0.08f);
                    feedbackMessage = "Pupilas sanas, reaccionan a la luz.";
                    return true;
                }
            }
        }

        private void DiscoverSymptom(string message)
        {
            if (IsDiscovered) return;
            IsDiscovered = true;
            EventBus.TriggerSymptomDiscovered(symptomName, message);
            EventBus.RequestHapticImpulse(HandSide.Both, 0.55f, 0.2f);
        }

        private void Update()
        {
            bool isIlluminated = (Time.time - lastLightExposeTime) < 0.25f;

            float targetScaleFactor = 1.0f;
            Color targetEmission = Color.black;

            if (isPositiveForInfection)
            {
                // Pupila patológica: midriasis fija dilatada
                targetScaleFactor = 1.35f;

                if (isIlluminated && lastExposeWasUV)
                {
                    // Fluorescencia viral verde-esmeralda bajo radiación UV
                    targetEmission = new Color(0.2f, 1.8f, 0.35f);
                }
            }
            else
            {
                // Pupila sana: miosis rápida ante haz de luz blanca clínica
                if (isIlluminated && !lastExposeWasUV)
                {
                    targetScaleFactor = 0.35f; // Miosis intensa reactiva
                }
                else
                {
                    targetScaleFactor = 1.0f; // Tono basal de reposo
                }
            }

            // Lerp dinámico de dilatación / contracción
            currentPupilScaleFactor = Mathf.MoveTowards(currentPupilScaleFactor, targetScaleFactor, Time.deltaTime * 4.0f);

            if (leftPupil != null)
            {
                leftPupil.transform.localScale = basePupilScale * currentPupilScaleFactor;
            }
            if (rightPupil != null)
            {
                rightPupil.transform.localScale = basePupilScale * currentPupilScaleFactor;
            }

            if (pupilMaterial != null)
            {
                if (targetEmission != Color.black)
                {
                    pupilMaterial.EnableKeyword("_EMISSION");
                    pupilMaterial.SetColor("_EmissionColor", targetEmission);
                }
                else
                {
                    pupilMaterial.DisableKeyword("_EMISSION");
                    pupilMaterial.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}
