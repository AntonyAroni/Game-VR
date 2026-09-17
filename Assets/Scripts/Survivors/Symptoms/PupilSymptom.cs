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
        [SerializeField] private string symptomName = "Reflejo Pupilar";
        [SerializeField] private bool isPositiveForInfection = false;

        public string SymptomName => symptomName;
        public bool IsPositiveForInfection => isPositiveForInfection;
        public bool IsDiscovered { get; private set; }

        private float lastExamineTime = -10f;
        [SerializeField] private float examineCooldown = 2.0f;

        private GameObject leftPupil;
        private GameObject rightPupil;
        private Material pupilMaterial;
        private Vector3 basePupilScale = new Vector3(0.015f, 0.015f, 0.007f);
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
            UpdatePupilAppearance();
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
                feedbackMessage = "Se requiere una fuente de luz (Linterna Clínica o UV) para evaluar los ojos.";
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
                    DiscoverSymptom("¡Bioluminiscencia corneal y venosa anómala bajo luz UV!");
                    feedbackMessage = "¡ANOMALÍA FORENSE UV: Queratitis viral fluorescente detectada en la córnea!";
                    return true;
                }
                else
                {
                    EventBus.RequestHapticImpulse(HandSide.Both, 0.2f, 0.08f);
                    feedbackMessage = "Examen UV negativo: Estructura ocular limpia sin fluorescencia biológica.";
                    return true;
                }
            }
            else
            {
                if (isPositiveForInfection)
                {
                    DiscoverSymptom("¡Midriasis bilateral arreactiva confirmada con linterna!");
                    feedbackMessage = "¡ANOMALÍA OCULAR: Pupilas totalmente dilatadas e inmóviles ante la luz blanca!";
                    return true;
                }
                else
                {
                    EventBus.RequestHapticImpulse(HandSide.Both, 0.2f, 0.08f);
                    feedbackMessage = "Reflejo fotomotor normal: Las pupilas se contraen simétricamente ante la luz.";
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
