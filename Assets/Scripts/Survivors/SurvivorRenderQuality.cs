using System.Collections.Generic;
using UnityEngine;

namespace ZombieCheckpoint.Survivors
{
    /// <summary>
    /// Garantiza la máxima fidelidad visual del civil mientras se le inspecciona de cerca.
    /// Principio de IHC: Visibilidad. La inspección corporal es una tarea de búsqueda visual a menos de
    /// un metro (mordeduras, manchas, pupilas). Con el lodBias de Quest (0.4) el pack de personajes caía a
    /// LOD1-LOD2 a esa distancia: cabeza de ~180 triángulos, hombros facetados y deformación con 2 huesos.
    /// Este componente:
    /// - Fuerza el LOD0 sólo dentro del radio de inspección y devuelve el control al LODGroup fuera de él,
    ///   de modo que el coste extra existe únicamente para el civil que se está examinando.
    /// - Sube la deformación a 4 huesos por vértice (hombros y codos sin quiebres al levantar los brazos).
    /// - Activa filtrado anisotrópico en las texturas de piel y ropa, que se miran en ángulo rasante.
    /// </summary>
    [DisallowMultipleComponent]
    public class SurvivorRenderQuality : MonoBehaviour
    {
        [Header("Detalle de Inspección")]
        [Tooltip("Distancia cámara-civil (m) por debajo de la cual se fuerza el LOD de mayor detalle.")]
        [SerializeField] private float inspectionDistance = 3.0f;

        [Tooltip("Nivel de filtrado anisotrópico aplicado a las texturas del civil.")]
        [Range(1, 16)]
        [SerializeField] private int textureAnisoLevel = 8;

        [Tooltip("Segundos entre comprobaciones de distancia (no hace falta evaluarlo cada fotograma).")]
        [SerializeField] private float checkInterval = 0.2f;

        private static readonly HashSet<Texture> tunedTextures = new HashSet<Texture>();
        private static readonly int[] textureProperties =
        {
            Shader.PropertyToID("_BaseMap"),
            Shader.PropertyToID("_BumpMap"),
            Shader.PropertyToID("_MetallicGlossMap"),
            Shader.PropertyToID("_OcclusionMap")
        };

        private LODGroup lodGroup;
        private Transform cameraTransform;
        private bool isForcingDetail;
        private float nextCheckTime;

        private void Awake()
        {
            if (!TryGetComponent(out lodGroup))
            {
                Debug.LogWarning($"[SurvivorRenderQuality] {name} no tiene LODGroup: sólo se aplicará la calidad de piel y texturas.");
            }

            ApplySkinAndTextureQuality();
        }

        private void OnDisable()
        {
            SetDetailForced(false);
        }

        /// <summary>
        /// Vuelve a aplicar calidad de deformación y texturas. Llamar si se añaden mallas después de Awake
        /// (por ejemplo, el torso descubierto instanciado por <see cref="TorsoClothingController"/>).
        /// </summary>
        public void ApplySkinAndTextureQuality()
        {
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.quality = SkinQuality.Bone4;
                TuneTextures(smr.sharedMaterials);
            }
        }

        private void Update()
        {
            if (lodGroup == null || Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;

            if (cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null) return;
                cameraTransform = mainCamera.transform;
            }

            Vector3 center = lodGroup.transform.TransformPoint(lodGroup.localReferencePoint);
            float sqrDistance = (cameraTransform.position - center).sqrMagnitude;
            SetDetailForced(sqrDistance <= inspectionDistance * inspectionDistance);
        }

        private void SetDetailForced(bool forced)
        {
            if (lodGroup == null || forced == isForcingDetail) return;
            isForcingDetail = forced;
            // ForceLOD(-1) devuelve la selección automática por distancia al LODGroup.
            lodGroup.ForceLOD(forced ? 0 : -1);
        }

        private void TuneTextures(Material[] materials)
        {
            foreach (var material in materials)
            {
                if (material == null) continue;
                foreach (int property in textureProperties)
                {
                    if (!material.HasProperty(property)) continue;
                    Texture texture = material.GetTexture(property);
                    if (texture == null || !tunedTextures.Add(texture)) continue;
                    texture.anisoLevel = Mathf.Max(texture.anisoLevel, textureAnisoLevel);
                }
            }
        }
    }
}
