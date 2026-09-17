using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    [Serializable]
    public struct SoundEntry
    {
        public string soundId;
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>
    /// Gestiona el audio 3D espacializado con síntesis procedural de sonido integrada.
    /// Garantiza que todos los efectos (latidos, clics, sellos, alarmas) funcionen inmediatamente
    /// sin requerir descargas externas de audio.
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        [Header("Biblioteca de Efectos de Sonido")]
        [SerializeField] private List<SoundEntry> audioLibrary = new List<SoundEntry>();

        private readonly Dictionary<string, SoundEntry> soundLookup = new Dictionary<string, SoundEntry>();

        private void Awake()
        {
            // Registrar clips serializados
            foreach (var entry in audioLibrary)
            {
                if (!string.IsNullOrEmpty(entry.soundId) && entry.audioClip != null)
                {
                    soundLookup[entry.soundId] = entry;
                }
            }

            // Generar clips procedurales para los sonidos esenciales que falten
            EnsureProceduralSound("heartbeat_normal", GenerateHeartbeatClip(60f, false));
            EnsureProceduralSound("heartbeat_infected", GenerateHeartbeatClip(150f, true));
            EnsureProceduralSound("flashlight_click", GenerateClickClip(1200f, 0.04f));
            EnsureProceduralSound("stamp_impact", GenerateImpactClip(90f, 0.2f));
            EnsureProceduralSound("button_click", GenerateClickClip(800f, 0.06f));
            EnsureProceduralSound("verdict_correct", GenerateChimeClip(523.25f, 659.25f, 783.99f)); // Acorde mayor (Do-Mi-Sol)
            EnsureProceduralSound("verdict_error", GenerateBuzzerClip(130f, 0.35f));
            EnsureProceduralSound("pneumatic_hiss", GeneratePneumaticHissClip(0.85f));
            EnsureProceduralSound("door_heavy_latch", GenerateHeavyLatchClip(0.45f));
            EnsureProceduralSound("pupil_scan_beep", GenerateChimeClip(880f, 1318.5f, 1760f)); // Tono agudo de escaneo médico
            EnsureProceduralSound("footstep_soft", GenerateFootstepClip(130f, 0.12f));
            EnsureProceduralSound("footstep_heavy", GenerateFootstepClip(85f, 0.16f));
            EnsureProceduralSound("uv_hum", GenerateUVHumClip(0.35f));
            EnsureProceduralSound("uv_switch", GenerateClickClip(2400f, 0.035f));
            EnsureProceduralSound("cloth_rustle", GenerateClothRustleClip(0.35f));
        }

        private void OnEnable()
        {
            EventBus.OnSpatialAudioRequested += PlaySpatialSound;
        }

        private void OnDisable()
        {
            EventBus.OnSpatialAudioRequested -= PlaySpatialSound;
        }

        public void PlaySpatialSound(string soundId, Vector3 position, float volumeMultiplier)
        {
            if (soundLookup.TryGetValue(soundId, out SoundEntry entry))
            {
                GameObject host = new GameObject($"Audio_{soundId}");
                host.transform.position = position;

                AudioSource src = host.AddComponent<AudioSource>();
                src.clip = entry.audioClip;
                src.volume = entry.volume * volumeMultiplier;
                src.spatialBlend = 0.85f; // Sonido espacial 3D con suficiente presencia en auriculares
                src.minDistance = 0.5f;
                src.maxDistance = 20.0f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.Play();

                Destroy(host, entry.audioClip.length + 0.15f);
            }
            else
            {
                Debug.LogWarning($"[SpatialAudio] Clip '{soundId}' no encontrado.");
            }
        }

        private void EnsureProceduralSound(string id, AudioClip proceduralClip)
        {
            if (!soundLookup.ContainsKey(id) && proceduralClip != null)
            {
                SoundEntry entry = new SoundEntry
                {
                    soundId = id,
                    audioClip = proceduralClip,
                    volume = 1.0f
                };
                soundLookup[id] = entry;
            }
        }

        #region Generadores Procedurales de Sonido

        private AudioClip GenerateHeartbeatClip(float frequency, bool distorted)
        {
            int sampleRate = 44100;
            float duration = 0.3f;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                // Pulso sinusoidal con envolvente exponencial
                float envelope = Mathf.Exp(-t * 22f);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);

                if (distorted)
                {
                    // Añadir armónicos rugosos para zombi
                    wave += 0.5f * Mathf.Sin(2f * Mathf.PI * (frequency * 1.5f) * t);
                    wave = Mathf.Clamp(wave * 1.8f, -1f, 1f);
                }

                samples[i] = wave * envelope * 0.9f;
            }

            AudioClip clip = AudioClip.Create($"proc_{frequency}", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateClickClip(float freq, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 80f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = wave * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create("proc_click", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateImpactClip(float baseFreq, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = baseFreq * (1f - (t / duration) * 0.6f); // Pitch drop
                float envelope = Mathf.Exp(-t * 18f);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.2f * Mathf.Exp(-t * 30f);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + noise) * envelope;
            }

            AudioClip clip = AudioClip.Create("proc_impact", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateChimeClip(float f1, float f2, float f3)
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 5f);
                float wave = (Mathf.Sin(2f * Mathf.PI * f1 * t) +
                              Mathf.Sin(2f * Mathf.PI * f2 * t) +
                              Mathf.Sin(2f * Mathf.PI * f3 * t)) / 3f;
                samples[i] = wave * envelope * 0.8f;
            }

            AudioClip clip = AudioClip.Create("proc_chime", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateBuzzerClip(float freq, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Min(1f, t * 50f) * Mathf.Exp(-t * 3f);
                // Onda cuadrada
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                samples[i] = wave * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create("proc_buzzer", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GeneratePneumaticHissClip(float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                // Envolvente de entrada rápida y desvanecimiento progresivo
                float envelope = Mathf.Min(1f, t * 25f) * Mathf.Exp(-t * 2.8f);
                // Ruido blanco filtrado con oscilación de aire comprimido
                float whiteNoise = (UnityEngine.Random.value * 2f - 1f);
                float airTone = Mathf.Sin(2f * Mathf.PI * 450f * t) * 0.25f;
                samples[i] = (whiteNoise * 0.75f + airTone) * envelope * 0.55f;
            }

            AudioClip clip = AudioClip.Create("proc_pneumatic", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateHeavyLatchClip(float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 14f);
                // Impacto metálico con resonancia grave
                float sub = Mathf.Sin(2f * Mathf.PI * 65f * t);
                float metalRing = Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.4f * Mathf.Exp(-t * 20f);
                float click = (UnityEngine.Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 80f);
                samples[i] = (sub + metalRing + click) * envelope * 0.85f;
            }

            AudioClip clip = AudioClip.Create("proc_latch", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateFootstepClip(float baseFreq, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 32f);
                // Golpe amortiguado en piso hospitalario: sub-golpe + fricción de zapato
                float lowTap = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                float soleFriction = (UnityEngine.Random.value * 2f - 1f) * 0.45f * Mathf.Exp(-t * 45f);
                samples[i] = (lowTap * 0.65f + soleFriction) * envelope * 0.75f;
            }

            AudioClip clip = AudioClip.Create("proc_footstep", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateUVHumClip(float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                // Zumbido tenue de reactor de mercurio / balastro UV (~120Hz + 2400Hz armónico)
                float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.35f;
                float buzz = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.12f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.04f;
                samples[i] = (hum + buzz + noise) * 0.4f;
            }

            AudioClip clip = AudioClip.Create("proc_uv_hum", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateClothRustleClip(float duration)
        {
            int sampleRate = 44100;
            int samplesCount = (int)(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * (t / duration));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                float softSwoosh = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.15f;
                samples[i] = (noise + softSwoosh) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("proc_cloth_rustle", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
