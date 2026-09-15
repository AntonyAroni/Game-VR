using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Monitor de signos vitales diegético con osciloscopio de electrocardiograma (ECG) en tiempo real.
    /// Principio de IHC: Visibilidad del Estado del Sistema y Retroalimentación Multimodal (Norman).
    /// - Al auscultar con el estetoscopio, renderiza la onda P-Q-R-S-T fisiológica normal o una arritmia errática.
    /// - Calcula y despliega los latidos por minuto (BPM) y el estado clínico del superviviente.
    /// - Genera un barrido de fósforo verde/rojo tipo CRT analógico mediante textura procedural continua.
    /// </summary>
    public class VitalSignsMonitor : MonoBehaviour
    {
        [Header("Componentes de Pantalla")]
        [SerializeField] private RawImage ecgScreenImage;
        [SerializeField] private TextMeshProUGUI bpmDisplayText;
        [SerializeField] private TextMeshProUGUI statusDisplayText;
        [SerializeField] private Image heartPulseIcon;

        [Header("Dimensiones del Osciloscopio")]
        [SerializeField] private int textureWidth = 256;
        [SerializeField] private int textureHeight = 64;
        [SerializeField] private float sweepSpeed = 75f; // Píxeles por segundo

        private Texture2D ecgTexture;
        private Color32[] texturePixels;
        private Color32 backgroundColor = new Color32(8, 16, 12, 255);
        private Color32 gridColor = new Color32(14, 30, 20, 255);
        private Color32 phosphorNormal = new Color32(0, 255, 120, 255);
        private Color32 phosphorInfected = new Color32(255, 40, 40, 255);

        private bool isAuscultating = false;
        private bool isAbnormal = false;
        private float currentBpm = 0f;
        private float sweepX = 0f;
        private int lastSweepX = 0;

        private float wavePhase = 0f;
        private float heartIconScale = 1f;

        private void Awake()
        {
            InitializeTexture();
            ResolveUIReferences();
        }

        private void OnEnable()
        {
            EventBus.OnHeartbeatAuscultationStateChanged += HandleAuscultationStateChanged;
            EventBus.OnSurvivorDeparted += HandleSurvivorDeparted;
            EventBus.OnSurvivorArrived += HandleSurvivorArrived;
        }

        private void OnDisable()
        {
            EventBus.OnHeartbeatAuscultationStateChanged -= HandleAuscultationStateChanged;
            EventBus.OnSurvivorDeparted -= HandleSurvivorDeparted;
            EventBus.OnSurvivorArrived -= HandleSurvivorArrived;
        }

        private void OnDestroy()
        {
            if (ecgTexture != null)
            {
                Destroy(ecgTexture);
            }
        }

        private void InitializeTexture()
        {
            ecgTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            texturePixels = new Color32[textureWidth * textureHeight];
            ClearScreen();
            ecgTexture.SetPixels32(texturePixels);
            ecgTexture.Apply();

            if (ecgScreenImage != null)
            {
                ecgScreenImage.texture = ecgTexture;
            }
        }

        private void ResolveUIReferences()
        {
            if (ecgScreenImage == null)
            {
                ecgScreenImage = GetComponentInChildren<RawImage>();
                if (ecgScreenImage != null && ecgTexture != null)
                {
                    ecgScreenImage.texture = ecgTexture;
                }
            }

            if (bpmDisplayText == null)
            {
                var bpmObj = transform.Find("MonitorCanvas/BpmText") ?? transform.Find("BpmText");
                if (bpmObj != null) bpmDisplayText = bpmObj.GetComponent<TextMeshProUGUI>();
            }

            if (statusDisplayText == null)
            {
                var stObj = transform.Find("MonitorCanvas/StatusText") ?? transform.Find("StatusText");
                if (stObj != null) statusDisplayText = stObj.GetComponent<TextMeshProUGUI>();
            }

            SetStandbyState();
        }

        private void HandleAuscultationStateChanged(bool auscultating, float targetBpm, bool abnormal)
        {
            isAuscultating = auscultating;
            currentBpm = targetBpm;
            isAbnormal = abnormal;

            if (isAuscultating)
            {
                if (isAbnormal)
                {
                    if (bpmDisplayText != null)
                    {
                        bpmDisplayText.text = $"<b>{Mathf.RoundToInt(currentBpm)}</b> <size=60%>BPM</size>";
                        bpmDisplayText.color = new Color(1f, 0.2f, 0.2f);
                    }
                    if (statusDisplayText != null)
                    {
                        statusDisplayText.text = "<color=#ff3333>● ALERTA: TAQUICARDIA / ARRITMIA</color>";
                    }
                }
                else
                {
                    if (bpmDisplayText != null)
                    {
                        bpmDisplayText.text = $"<b>{Mathf.RoundToInt(currentBpm)}</b> <size=60%>BPM</size>";
                        bpmDisplayText.color = new Color(0f, 1f, 0.5f);
                    }
                    if (statusDisplayText != null)
                    {
                        statusDisplayText.text = "<color=#00ff88>● RITMO SINUSAL ESTABLE</color>";
                    }
                }
            }
            else
            {
                SetStandbyState();
            }
        }

        private void HandleSurvivorDeparted()
        {
            SetStandbyState();
            ClearScreen();
        }

        private void HandleSurvivorArrived(object _)
        {
            SetStandbyState();
        }

        private void SetStandbyState()
        {
            isAuscultating = false;
            currentBpm = 0f;
            isAbnormal = false;

            if (bpmDisplayText != null)
            {
                bpmDisplayText.text = "<b>--</b> <size=60%>BPM</size>";
                bpmDisplayText.color = new Color(0.4f, 0.7f, 0.6f);
            }

            if (statusDisplayText != null)
            {
                statusDisplayText.text = "<color=#559988>○ TELEMETRÍA EN ESPERA (AUSCULTAR TÓRAX)</color>";
            }
        }

        private void ClearScreen()
        {
            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    bool isGrid = (x % 32 == 0) || (y % 16 == 0);
                    texturePixels[y * textureWidth + x] = isGrid ? gridColor : backgroundColor;
                }
            }
        }

        private void Update()
        {
            if (ecgTexture == null) return;

            float dt = Time.deltaTime;
            sweepX += sweepSpeed * dt;
            int currentSweepCol = Mathf.FloorToInt(sweepX) % textureWidth;

            // Desvanecer fósforo de columnas anteriores y limpiar la barra de avance
            if (currentSweepCol != lastSweepX)
            {
                int startCol = (lastSweepX + 1) % textureWidth;
                int endCol = currentSweepCol;

                int c = startCol;
                while (c != (endCol + 1) % textureWidth)
                {
                    ClearColumnAndEraseBar(c);
                    float yVal = SampleEcgWave(c);
                    DrawPhosphorPoint(c, yVal);

                    if (c == endCol) break;
                    c = (c + 1) % textureWidth;
                }

                lastSweepX = currentSweepCol;
                ecgTexture.SetPixels32(texturePixels);
                ecgTexture.Apply(false);
            }

            // Animación del icono de corazón pulsante
            if (heartPulseIcon != null)
            {
                if (isAuscultating)
                {
                    heartIconScale = Mathf.Lerp(heartIconScale, 1f, dt * 10f);
                    heartPulseIcon.transform.localScale = Vector3.one * heartIconScale;
                }
                else
                {
                    heartPulseIcon.transform.localScale = Vector3.one;
                }
            }
        }

        private void ClearColumnAndEraseBar(int col)
        {
            // Borrar franja guía (cursor de barrido) unos píxeles adelante
            for (int lead = 1; lead <= 4; lead++)
            {
                int clearCol = (col + lead) % textureWidth;
                for (int y = 0; y < textureHeight; y++)
                {
                    bool isGrid = (clearCol % 32 == 0) || (y % 16 == 0);
                    texturePixels[y * textureWidth + clearCol] = isGrid ? gridColor : backgroundColor;
                }
            }

            // Desvanecimiento suave en la columna actual
            for (int y = 0; y < textureHeight; y++)
            {
                int idx = y * textureWidth + col;
                bool isGrid = (col % 32 == 0) || (y % 16 == 0);
                texturePixels[idx] = isGrid ? gridColor : backgroundColor;
            }
        }

        private float SampleEcgWave(int col)
        {
            float centerY = textureHeight * 0.5f;

            if (!isAuscultating)
            {
                // Ruido basal suave de reposo
                float noise = (Mathf.PerlinNoise(col * 0.15f, Time.time * 2f) - 0.5f) * 2.5f;
                return centerY + noise;
            }

            // Frecuencia de la onda dependiente del BPM
            float beatsPerSecond = currentBpm / 60f;
            wavePhase += (beatsPerSecond / sweepSpeed);
            if (wavePhase > 1f) wavePhase -= 1f;

            float p = wavePhase;
            float signal = 0f;

            if (isAbnormal)
            {
                // Onda de taquicardia ventricular / arritmia caótica
                if (p > 0.1f && p < 0.22f)
                {
                    signal = Mathf.Sin((p - 0.1f) / 0.12f * Mathf.PI) * 24f; // Espiga descontrolada
                    TriggerHeartPulse();
                }
                else if (p > 0.35f && p < 0.55f)
                {
                    signal = -Mathf.Sin((p - 0.35f) / 0.2f * Mathf.PI) * 18f; // Despolarización profunda
                }
                else
                {
                    signal = (Mathf.PerlinNoise(col * 0.4f, Time.time * 8f) - 0.5f) * 8f; // Fibrilación
                }
            }
            else
            {
                // Ciclo clásico P-Q-R-S-T
                if (p >= 0.05f && p < 0.15f)
                {
                    // Onda P
                    signal = Mathf.Sin((p - 0.05f) / 0.10f * Mathf.PI) * 4f;
                }
                else if (p >= 0.18f && p < 0.21f)
                {
                    // Onda Q (depresión previa al pico)
                    signal = -3.5f;
                }
                else if (p >= 0.21f && p < 0.26f)
                {
                    // Onda R (pico ventricular sístole fuerte)
                    float subT = (p - 0.21f) / 0.05f;
                    signal = Mathf.Sin(subT * Mathf.PI) * 26f;
                    TriggerHeartPulse();
                }
                else if (p >= 0.26f && p < 0.29f)
                {
                    // Onda S (depresión posterior)
                    signal = -6f;
                }
                else if (p >= 0.35f && p < 0.50f)
                {
                    // Onda T (repolarización)
                    signal = Mathf.Sin((p - 0.35f) / 0.15f * Mathf.PI) * 6f;
                }
                else
                {
                    // Micro variación basal
                    signal = (Mathf.PerlinNoise(col * 0.1f, Time.time) - 0.5f) * 1.2f;
                }
            }

            return Mathf.Clamp(centerY + signal, 2f, textureHeight - 3f);
        }

        private void TriggerHeartPulse()
        {
            heartIconScale = 1.35f;
        }

        private void DrawPhosphorPoint(int col, float yVal)
        {
            int yCenter = Mathf.RoundToInt(yVal);
            Color32 drawCol = isAbnormal ? phosphorInfected : phosphorNormal;

            // Trazo con grosor de 2 píxeles
            for (int dy = -1; dy <= 1; dy++)
            {
                int py = Mathf.Clamp(yCenter + dy, 0, textureHeight - 1);
                int idx = py * textureWidth + col;
                texturePixels[idx] = drawCol;
            }
        }
    }
}
