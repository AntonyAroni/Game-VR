using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ZombieCheckpoint.Documents;
using ZombieCheckpoint.HCI;
using ZombieCheckpoint.Survivors;
using ZombieCheckpoint.Survivors.Symptoms;

namespace ZombieCheckpoint.Core
{
    /// <summary>
    /// Administrador central del flujo de la cabina de inspección.
    /// Principio SRP: Orquesta el ciclo de rondas, alternando dinámicamente entre diferentes
    /// personajes civiles (hombres y mujeres) con identidades y síntomas únicos en cada interacción.
    /// </summary>
    public class CheckpointFlowManager : MonoBehaviour
    {
        [Header("Roster de Civiles (Prefabs)")]
        [SerializeField] private List<GameObject> civilianPrefabs = new List<GameObject>();
        [SerializeField] private Material woundMaterial;

        [Header("Referencias de Escena")]
        [SerializeField] private SurvivorModel currentSurvivor;
        [SerializeField] private DocumentInteractable currentDocument;
        [SerializeField] private UsabilityMetricsTracker metricsTracker;
        [SerializeField] private TextMeshProUGUI monitorStatusText;
        [SerializeField] private Light quarantineAlarmLight;

        [Header("Puntos de Posición")]
        [SerializeField] private Transform survivorStandPoint;
        [SerializeField] private Transform documentSpawnPoint;

        [Header("Configuración de Ronda")]
        [SerializeField] private float delayBetweenSurvivors = 3.5f;

        public InspectionState CurrentState { get; private set; } = InspectionState.WaitingNextSurvivor;

        private int roundNumber = 0;
        private int correctCount = 0;

        // Listas de nombres creíbles para el pase sanitario
        private static readonly string[] MaleNames = new string[] {
            "Carlos Méndez", "Marcus Vance", "Mateo Silva", "John Miller", 
            "David Ruiz", "Lucas Thorne", "Andrés Navarro", "Gabriel Soto"
        };

        private static readonly string[] FemaleNames = new string[] {
            "Elena Torres", "Sarah Connor", "Claudia Reyes", "Lisa Hayes", 
            "Ana Vega", "Valeria Gómez", "Sofía Vargas", "Beatriz Morales"
        };

        private static readonly string[] PrefabAssetPaths = new string[] {
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01m_01.prefab",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_01.prefab",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01m_02.prefab",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_02f_01.prefab",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_02m_01.prefab",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_02.prefab"
        };

        private void Awake()
        {
            if (monitorStatusText == null)
            {
                var monObj = GameObject.Find("MonitorText");
                if (monObj != null) monitorStatusText = monObj.GetComponent<TextMeshProUGUI>();
            }

            if (survivorStandPoint == null)
            {
                var sp = GameObject.Find("Survivor_StandPoint_Anchor") ?? GameObject.Find("SurvivorStandPoint");
                if (sp != null) survivorStandPoint = sp.transform;
            }

            if (documentSpawnPoint == null)
            {
                var dsp = GameObject.Find("Document_SpawnPoint_Anchor");
                if (dsp != null) documentSpawnPoint = dsp.transform;
            }

            if (quarantineAlarmLight == null)
            {
                var ql = GameObject.Find("Quarantine_AlarmLight");
                if (ql != null) quarantineAlarmLight = ql.GetComponent<Light>();
            }

            AutoPopulatePrefabsIfEmpty();
        }

        private void AutoPopulatePrefabsIfEmpty()
        {
#if UNITY_EDITOR
            if (civilianPrefabs == null) civilianPrefabs = new List<GameObject>();

            if (civilianPrefabs.Count == 0)
            {
                foreach (var p in PrefabAssetPaths)
                {
                    var pfab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (pfab != null) civilianPrefabs.Add(pfab);
                }
            }

            if (woundMaterial == null)
            {
                woundMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Abandoned_Asylum/Materials/M_InfectedWound.mat");
            }
#endif
        }

        private void OnEnable()
        {
            EventBus.OnVerdictSubmitted += HandleVerdictSubmitted;
            EventBus.OnSymptomDiscovered += HandleSymptomDiscovered;
        }

        private void OnDisable()
        {
            EventBus.OnVerdictSubmitted -= HandleVerdictSubmitted;
            EventBus.OnSymptomDiscovered -= HandleSymptomDiscovered;
        }

        private void Start()
        {
            StartCoroutine(StartNextInspectionRound());
        }

        private void HandleSymptomDiscovered(string symptomName, string feedbackMsg)
        {
            UpdateMonitorText($"<color=#ffcc00>⚠️ SÍNTOMA LOCALIZADO:</color>\n<b>{symptomName}</b>\n\"{feedbackMsg}\"");
        }

        private IEnumerator StartNextInspectionRound()
        {
            CurrentState = InspectionState.WaitingNextSurvivor;
            roundNumber++;

            UpdateMonitorText($"<color=#33ccff>PUESTO DE CONTROL VR - CASO #{roundNumber}</color>\n" +
                             "------------------------------------\n" +
                             "Aproximando nuevo ciudadano a la cabina...");

            yield return new WaitForSeconds(1.0f);

            // 1. Eliminar de forma segura el superviviente anterior si aún existe
            if (currentSurvivor != null)
            {
                Destroy(currentSurvivor.gameObject);
                currentSurvivor = null;
            }

            // 2. Determinar estado biológico del nuevo sujeto
            bool isInfected = (roundNumber % 2 == 0); // Alternar para balancear evaluación
            bool hasBite = isInfected && (Random.value > 0.35f);
            bool hasHeartbeatAnomaly = isInfected && (!hasBite || Random.value > 0.3f);
            bool hasPupilAnomaly = isInfected && (Random.value > 0.3f);
            bool documentExpired = !isInfected && (roundNumber % 3 == 0);

            // 3. Spawnear el nuevo modelo de civil
            GameObject spawnedNpc = SpawnNextCivilian(out bool isFemale);
            string survivorName = isFemale 
                ? FemaleNames[Random.Range(0, FemaleNames.Length)] 
                : MaleNames[Random.Range(0, MaleNames.Length)];
            int survivorAge = Random.Range(20, 58);

            // 4. Configurar el perfil biológico del nuevo PNJ
            if (spawnedNpc != null)
            {
                currentSurvivor = SetupSurvivorComponents(spawnedNpc, hasBite, hasHeartbeatAnomaly, hasPupilAnomaly);
                currentSurvivor.SetupProfile(survivorName, survivorAge, isInfected, hasBite, hasHeartbeatAnomaly, hasPupilAnomaly);
            }

            // 5. Configurar el pase sanitario / documento
            if (currentDocument == null) currentDocument = FindAnyObjectByType<DocumentInteractable>();
            if (currentDocument != null)
            {
                DocumentData docData = documentExpired 
                    ? DocumentData.CreateFalsifiedExpired(survivorName, survivorAge)
                    : DocumentData.CreateRandomValid(survivorName, survivorAge);

                currentDocument.Initialize(docData);
                if (documentSpawnPoint != null)
                {
                    currentDocument.transform.position = documentSpawnPoint.position;
                    currentDocument.transform.rotation = documentSpawnPoint.rotation;
                    var rb = currentDocument.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            if (metricsTracker == null) metricsTracker = FindAnyObjectByType<UsabilityMetricsTracker>();

            CurrentState = InspectionState.Inspecting;
            EventBus.TriggerSurvivorArrived(currentSurvivor);

            UpdateMonitorText($"<color=#33ccff>CIUDADANO #{roundNumber}: {survivorName.ToUpper()}</color>\n" +
                             "------------------------------------\n" +
                             "1. Ausculta el tórax con el estetoscopio.\n" +
                             "2. Enciende la linterna para pupilas o piel.\n" +
                             "3. Compara el pase sanitario.\n" +
                             "4. Emite veredicto (Sello o Botón físico).");

            Debug.Log($"[Checkpoint] Ronda #{roundNumber} iniciada con {survivorName} ({survivorAge} años). Infectado: {isInfected}. Mordedura: {hasBite}. Pulso: {hasHeartbeatAnomaly}. Pupilas: {hasPupilAnomaly}. Doc Vencido: {documentExpired}");
        }

        private GameObject SpawnNextCivilian(out bool isFemale)
        {
            Vector3 spawnPos = survivorStandPoint != null ? survivorStandPoint.position : new Vector3(0f, 0f, 1.45f);
            Quaternion spawnRot = survivorStandPoint != null ? survivorStandPoint.rotation : Quaternion.Euler(0f, 180f, 0f);

            isFemale = false;

            if (civilianPrefabs == null || civilianPrefabs.Count == 0)
            {
                AutoPopulatePrefabsIfEmpty();
            }

            if (civilianPrefabs != null && civilianPrefabs.Count > 0)
            {
                int index = (roundNumber - 1) % civilianPrefabs.Count;
                GameObject prefab = civilianPrefabs[index];
                if (prefab != null)
                {
                    isFemale = prefab.name.Contains("01f") || prefab.name.Contains("02f");
                    GameObject npc = Instantiate(prefab, spawnPos, spawnRot);
                    npc.name = $"Civilian_{roundNumber}_{prefab.name}";
                    return npc;
                }
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = $"Civilian_{roundNumber}_Fallback";
            fallback.transform.position = spawnPos + Vector3.up * 0.9f;
            fallback.transform.rotation = spawnRot;
            return fallback;
        }

        private SurvivorModel SetupSurvivorComponents(GameObject npc, bool hasBite, bool hasHeartbeatAnomaly, bool hasPupilAnomaly)
        {
            var model = npc.GetOrAddComponent<SurvivorModel>();
            var humController = npc.GetOrAddComponent<SurvivorHumanoidController>();

            Transform rightForeArm = null;
            Transform spine1 = null;
            Transform head = null;

            foreach (var t in npc.GetComponentsInChildren<Transform>())
            {
                if (t.name == "RightForeArm") rightForeArm = t;
                else if (t.name == "Spine1") spine1 = t;
                else if (t.name == "Head") head = t;
            }

            BiteMarkSymptom biteSymptom = null;
            if (rightForeArm != null)
            {
                GameObject biteMark = GameObject.CreatePrimitive(PrimitiveType.Quad);
                biteMark.name = "Bite_Wound_Visual";
                biteMark.transform.SetParent(rightForeArm);
                biteMark.transform.localPosition = new Vector3(0.04f, 0.12f, 0f);
                biteMark.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                biteMark.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

                if (woundMaterial != null)
                {
                    biteMark.GetComponent<Renderer>().sharedMaterial = woundMaterial;
                }
                else
                {
                    var redWound = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    redWound.color = new Color(0.7f, 0.05f, 0.05f);
                    biteMark.GetComponent<Renderer>().sharedMaterial = redWound;
                }

                Destroy(biteMark.GetComponent<Collider>());

                biteSymptom = rightForeArm.gameObject.GetOrAddComponent<BiteMarkSymptom>();
                typeof(BiteMarkSymptom).GetField("woundVisualObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(biteSymptom, biteMark);
                typeof(BiteMarkSymptom).GetField("woundTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(biteSymptom, biteMark.transform);
                biteSymptom.Initialize(hasBite);
            }

            HeartbeatSymptom heartbeat = null;
            if (spine1 != null)
            {
                heartbeat = spine1.gameObject.GetOrAddComponent<HeartbeatSymptom>();
                heartbeat.Initialize(hasHeartbeatAnomaly);
            }

            PupilSymptom pupil = null;
            if (head != null)
            {
                var headCol = head.GetComponent<Collider>();
                if (headCol == null)
                {
                    var sc = head.gameObject.AddComponent<SphereCollider>();
                    sc.radius = 0.16f;
                    sc.isTrigger = true;
                }
                head.gameObject.GetOrAddComponent<BodyPartExaminer>();
                pupil = head.gameObject.GetOrAddComponent<PupilSymptom>();
                pupil.Initialize(hasPupilAnomaly);
            }

            typeof(SurvivorModel).GetField("biteSymptom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(model, biteSymptom);
            typeof(SurvivorModel).GetField("heartbeatSymptom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(model, heartbeat);
            typeof(SurvivorModel).GetField("pupilSymptom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(model, pupil);

            return model;
        }

        private void HandleVerdictSubmitted(VerdictType verdict)
        {
            if (CurrentState != InspectionState.Inspecting) return;
            CurrentState = InspectionState.EvaluatingResult;

            bool isTrulyInfected = currentSurvivor != null && currentSurvivor.IsTrulyInfected;
            bool isDocumentInvalid = currentDocument != null && currentDocument.CurrentData != null && currentDocument.CurrentData.isFalsified;
            bool shouldBeQuarantined = isTrulyInfected || isDocumentInvalid;

            bool isCorrect = (verdict == VerdictType.SendToQuarantine && shouldBeQuarantined) ||
                             (verdict == VerdictType.ApprovedSafeZone && !shouldBeQuarantined);

            if (isCorrect) correctCount++;

            string title = isCorrect ? "<color=#00ff66>✔ ¡VEREDICTO CORRECTO!</color>" : "<color=#ff3333>✖ ¡ERROR EN EL VEREDICTO!</color>";
            string explanation = "";
            
            if (isCorrect)
            {
                explanation = verdict == VerdictType.SendToQuarantine 
                    ? "Sujeto con anomalías biológicas/documentales aislado a tiempo."
                    : "Ciudadano sano y en regla admitido a la zona segura.";
                EventBus.RequestSpatialAudio("verdict_correct", transform.position, 1.0f);
            }
            else
            {
                explanation = verdict == VerdictType.ApprovedSafeZone
                    ? "¡ALERTA ROJA! Has dejado pasar a un infectado a la zona segura."
                    : "Falso Positivo: Has enviado a un civil sano a cuarentena.";
                EventBus.RequestSpatialAudio("verdict_error", transform.position, 1.0f);
            }

            int accuracyPercent = Mathf.RoundToInt(((float)correctCount / roundNumber) * 100f);
            UpdateMonitorText($"{title}\n{explanation}\n\n<b>Precisión: {accuracyPercent}% ({correctCount}/{roundNumber})</b>\nSiguiente ciudadano en camino...");

            if (metricsTracker != null)
            {
                metricsTracker.RegisterVerdictDetails(shouldBeQuarantined, verdict);
            }

            EventBus.TriggerEvaluationResult(isCorrect, explanation);

            if (verdict == VerdictType.SendToQuarantine)
            {
                StartCoroutine(FlashQuarantineAlarmRoutine());
            }

            if (currentSurvivor != null)
            {
                currentSurvivor.Depart(verdict);
                StartCoroutine(AnimateDepartureRoutine(currentSurvivor.transform, verdict));
            }

            StartCoroutine(TransitionToNextSurvivorRoutine());
        }

        private IEnumerator FlashQuarantineAlarmRoutine()
        {
            if (quarantineAlarmLight == null) yield break;

            Color origColor = quarantineAlarmLight.color;
            float origIntensity = quarantineAlarmLight.intensity;

            quarantineAlarmLight.color = Color.red;

            for (int i = 0; i < 6; i++)
            {
                quarantineAlarmLight.intensity = 4.0f;
                yield return new WaitForSeconds(0.2f);
                quarantineAlarmLight.intensity = 0.2f;
                yield return new WaitForSeconds(0.2f);
            }

            quarantineAlarmLight.color = origColor;
            quarantineAlarmLight.intensity = origIntensity;
        }

        private IEnumerator AnimateDepartureRoutine(Transform survivorTransform, VerdictType verdict)
        {
            if (survivorTransform == null) yield break;

            Vector3 startPos = survivorTransform.position;
            Vector3 targetPos = (verdict == VerdictType.SendToQuarantine)
                ? startPos + new Vector3(0f, 0f, 2.8f) // Marcha hacia la puerta de cuarentena al fondo
                : startPos + new Vector3(2.4f, 0f, 0.4f); // Marcha hacia el corredor de la zona segura

            float elapsed = 0f;
            float duration = delayBetweenSurvivors * 0.8f;

            while (elapsed < duration)
            {
                if (survivorTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                survivorTransform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }

        [SerializeField] private int casesPerShift = 5;

        private void UpdateMonitorText(string text)
        {
            if (monitorStatusText != null)
            {
                monitorStatusText.text = text;
            }
        }

        private IEnumerator TransitionToNextSurvivorRoutine()
        {
            yield return new WaitForSeconds(delayBetweenSurvivors);
            EventBus.TriggerSurvivorDeparted();

            // Al completar un bloque de turno (cada 5 casos), desplegar el informe formal de IHC
            if (roundNumber > 0 && roundNumber % casesPerShift == 0 && metricsTracker != null)
            {
                CurrentState = InspectionState.SessionCompleted;
                UpdateMonitorText(metricsTracker.GenerateShiftReport());
                EventBus.RequestSpatialAudio("verdict_correct", transform.position, 1.0f);
                yield return new WaitForSeconds(7.0f);
            }

            StartCoroutine(StartNextInspectionRound());
        }
    }
}
