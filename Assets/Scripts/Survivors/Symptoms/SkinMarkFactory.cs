using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZombieCheckpoint.Survivors.Symptoms
{
    /// <summary>
    /// Fábrica de marcas cutáneas (erupción y mordedura) pegadas a la piel del civil.
    /// Principio de IHC: Visibilidad. Un síntoma que el jugador debe encontrar con la vista tiene que
    /// parecer una lesión real sobre la piel: bordes difusos, puntos sueltos y apoyo exacto en la superficie.
    /// Los rectángulos de color plano flotando delante del cuerpo no se leían como lesiones y, de canto,
    /// desaparecían.
    /// - Las texturas se generan una sola vez por proceso y se comparten entre civiles.
    /// - Los materiales parten de plantillas en <c>Resources/SkinMarks</c> para que la variante
    ///   transparente del shader se incluya en la compilación de Quest.
    /// - <see cref="TrySnapToSkin"/> proyecta la marca sobre la malla deformada (BakeMesh) en el momento de mostrarla.
    /// </summary>
    public static class SkinMarkFactory
    {
        private const string LitTemplatePath = "SkinMarks/M_SkinMark_Lit";
        private const string FluorescentTemplatePath = "SkinMarks/M_SkinMark_UV";
        private const int TextureSize = 128;

        /// <summary>Separación sobre la piel para evitar z-fighting con la malla del cuerpo.</summary>
        public const float SurfaceOffset = 0.0025f;

        private static Texture2D rashTexture;
        private static Texture2D biteTexture;
        private static Material litTemplate;
        private static Material fluorescentTemplate;
        private static bool templatesLoaded;

        private static Mesh bakedMesh;
        private static readonly List<Vector3> bakedVertices = new List<Vector3>(4096);
        private static readonly List<int> bakedTriangles = new List<int>(8192);
        private static readonly List<int> subMeshTriangles = new List<int>(8192);

        /// <summary>Erupción: manchas rojizas de borde difuso salpicadas de petequias oscuras.</summary>
        public static Texture2D RashTexture => rashTexture != null ? rashTexture : (rashTexture = BuildRashTexture());

        /// <summary>Mordedura: dos arcos de punciones dentales sobre un hematoma violáceo.</summary>
        public static Texture2D BiteTexture => biteTexture != null ? biteTexture : (biteTexture = BuildBiteTexture());

        /// <summary>
        /// Crea un material de marca cutánea. <paramref name="fluorescent"/> usa la plantilla sin iluminación
        /// (brillo propio bajo la lámpara UV) y tiñe la textura con <paramref name="tint"/>.
        /// </summary>
        public static Material CreateMarkMaterial(Texture2D texture, bool fluorescent, Color tint)
        {
            LoadTemplates();

            Material template = fluorescent ? fluorescentTemplate : litTemplate;
            Material mat;
            if (template != null)
            {
                mat = new Material(template);
            }
            else
            {
                Debug.LogWarning($"[SkinMarkFactory] Falta la plantilla Resources/{(fluorescent ? FluorescentTemplatePath : LitTemplatePath)}. " +
                                 "Se usa un material transparente creado en runtime (la variante podría faltar en Quest).");
                mat = CreateFallbackTransparent(fluorescent);
            }

            mat.name = fluorescent ? "M_SkinMark_UV (Instance)" : "M_SkinMark_Lit (Instance)";
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            return mat;
        }

        /// <summary>
        /// Configura un renderer de marca: sin sombras propias y sin sondas, para que no oscurezca la piel.
        /// </summary>
        public static void ConfigureMarkRenderer(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        /// <summary>
        /// Coloca un Quad (cara visible hacia -Z local) sobre la piel, en el primer punto donde el rayo
        /// corta la malla deformada actual de <paramref name="skin"/>. Devuelve false si no hay impacto,
        /// en cuyo caso la marca no se mueve.
        /// </summary>
        /// <param name="mark">Transform del Quad de la marca.</param>
        /// <param name="skin">Malla de piel sobre la que se apoya (torso o brazos).</param>
        /// <param name="rayOrigin">Origen del rayo, fuera del cuerpo (espacio mundo).</param>
        /// <param name="rayDirection">Dirección del rayo, hacia el interior del cuerpo (espacio mundo).</param>
        /// <param name="up">Referencia de "arriba" para orientar la marca.</param>
        /// <param name="rollDegrees">Giro sobre la normal para que cada marca sea distinta.</param>
        public static bool TrySnapToSkin(Transform mark, SkinnedMeshRenderer skin, Vector3 rayOrigin, Vector3 rayDirection,
                                         Vector3 up, float rollDegrees)
        {
            if (mark == null || skin == null || skin.sharedMesh == null) return false;
            if (!TryRaycastSkin(skin, rayOrigin, rayDirection.normalized, out Vector3 point, out Vector3 normal)) return false;

            Vector3 projectedUp = Vector3.ProjectOnPlane(up, normal);
            if (projectedUp.sqrMagnitude < 1e-6f) projectedUp = Vector3.ProjectOnPlane(Vector3.up, normal);

            // El Quad integrado se ve desde su -Z: su forward debe apuntar hacia dentro de la piel.
            Quaternion facing = Quaternion.LookRotation(-normal, projectedUp);
            mark.SetPositionAndRotation(point + normal * SurfaceOffset, Quaternion.AngleAxis(rollDegrees, normal) * facing);
            return true;
        }

        /// <summary>
        /// Intersección rayo-malla (Möller–Trumbore) contra la pose actual de la piel. Se ejecuta una vez
        /// por marca al mostrarla, nunca por fotograma.
        /// </summary>
        public static bool TryRaycastSkin(SkinnedMeshRenderer skin, Vector3 origin, Vector3 direction,
                                          out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitPoint = Vector3.zero;
            hitNormal = -direction;

            if (bakedMesh == null) bakedMesh = new Mesh { name = "SkinMarkFactory_Baked" };
            skin.BakeMesh(bakedMesh, true);
            bakedMesh.GetVertices(bakedVertices);
            bakedTriangles.Clear();
            for (int sub = 0; sub < bakedMesh.subMeshCount; sub++)
            {
                bakedMesh.GetTriangles(subMeshTriangles, sub);
                bakedTriangles.AddRange(subMeshTriangles);
            }

            // BakeMesh(useScale: true) ya aplica la escala: sólo falta posición y rotación del renderer.
            Matrix4x4 toWorld = Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one);
            float bestDistance = float.MaxValue;
            bool found = false;

            for (int i = 0; i + 2 < bakedTriangles.Count; i += 3)
            {
                Vector3 a = toWorld.MultiplyPoint3x4(bakedVertices[bakedTriangles[i]]);
                Vector3 b = toWorld.MultiplyPoint3x4(bakedVertices[bakedTriangles[i + 1]]);
                Vector3 c = toWorld.MultiplyPoint3x4(bakedVertices[bakedTriangles[i + 2]]);

                if (!IntersectTriangle(origin, direction, a, b, c, out float distance) || distance >= bestDistance) continue;

                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, direction) > 0f) normal = -normal;

                bestDistance = distance;
                hitPoint = origin + direction * distance;
                hitNormal = normal;
                found = true;
            }

            return found;
        }

        private static bool IntersectTriangle(Vector3 origin, Vector3 dir, Vector3 a, Vector3 b, Vector3 c, out float distance)
        {
            distance = 0f;
            Vector3 edge1 = b - a;
            Vector3 edge2 = c - a;
            Vector3 p = Vector3.Cross(dir, edge2);
            float det = Vector3.Dot(edge1, p);
            if (Mathf.Abs(det) < 1e-8f) return false;

            float invDet = 1f / det;
            Vector3 t = origin - a;
            float u = Vector3.Dot(t, p) * invDet;
            if (u < 0f || u > 1f) return false;

            Vector3 q = Vector3.Cross(t, edge1);
            float v = Vector3.Dot(dir, q) * invDet;
            if (v < 0f || u + v > 1f) return false;

            distance = Vector3.Dot(edge2, q) * invDet;
            return distance > 0f;
        }

        private static void LoadTemplates()
        {
            if (templatesLoaded) return;
            templatesLoaded = true;
            litTemplate = Resources.Load<Material>(LitTemplatePath);
            fluorescentTemplate = Resources.Load<Material>(FluorescentTemplatePath);
        }

        private static Material CreateFallbackTransparent(bool unlit)
        {
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        // ------------------------------------------------------------------
        // Texturas procedurales (se generan una vez y se comparten)
        // ------------------------------------------------------------------

        private static Texture2D NewMarkTexture(string name)
        {
            return new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
        }

        private static Texture2D BuildRashTexture()
        {
            var tex = NewMarkTexture("T_SkinMark_Rash");
            var pixels = new Color[TextureSize * TextureSize];
            var rng = new System.Random(7341);

            // Manchas eritematosas superpuestas: forma irregular, nunca un círculo perfecto.
            const int blotchCount = 5;
            var centers = new Vector2[blotchCount];
            var radii = new float[blotchCount];
            for (int i = 0; i < blotchCount; i++)
            {
                centers[i] = new Vector2(0.5f + RandomRange(rng, -0.18f, 0.18f), 0.5f + RandomRange(rng, -0.18f, 0.18f));
                radii[i] = RandomRange(rng, 0.14f, 0.24f);
            }

            // Petequias: puntos pequeños y oscuros dispersos sobre y alrededor de las manchas.
            const int dotCount = 34;
            var dots = new Vector3[dotCount];
            for (int i = 0; i < dotCount; i++)
            {
                float angle = RandomRange(rng, 0f, Mathf.PI * 2f);
                float dist = Mathf.Sqrt(RandomRange(rng, 0f, 1f)) * 0.42f;
                dots[i] = new Vector3(0.5f + Mathf.Cos(angle) * dist, 0.5f + Mathf.Sin(angle) * dist, RandomRange(rng, 0.008f, 0.02f));
            }

            Color inflamed = new Color(0.72f, 0.08f, 0.08f);
            Color petechia = new Color(0.36f, 0.02f, 0.06f);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    var uv = new Vector2((x + 0.5f) / TextureSize, (y + 0.5f) / TextureSize);

                    float blotch = 0f;
                    for (int i = 0; i < blotchCount; i++)
                    {
                        float d = Vector2.Distance(uv, centers[i]) / radii[i];
                        blotch = Mathf.Max(blotch, 1f - Mathf.SmoothStep(0f, 1f, d));
                    }
                    float grain = Mathf.PerlinNoise(uv.x * 9f + 3.1f, uv.y * 9f + 1.7f);
                    blotch *= Mathf.Lerp(0.7f, 1f, grain);

                    float dot = 0f;
                    for (int i = 0; i < dotCount; i++)
                    {
                        float d = Vector2.Distance(uv, new Vector2(dots[i].x, dots[i].y)) / dots[i].z;
                        dot = Mathf.Max(dot, 1f - Mathf.SmoothStep(0.55f, 1f, d));
                    }

                    // Desvanecer hacia el borde del Quad para que nunca se vea su contorno cuadrado.
                    float edge = 1f - Mathf.SmoothStep(0.36f, 0.5f, Vector2.Distance(uv, new Vector2(0.5f, 0.5f)));

                    Color c = Color.Lerp(inflamed, petechia, dot);
                    // Opacidad alta en el núcleo: la mancha debe distinguirse de la piel a un metro en el visor.
                    c.a = Mathf.Clamp01(Mathf.Max(Mathf.Clamp01(blotch * 1.35f) * 0.9f, dot)) * edge;
                    pixels[y * TextureSize + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        private static Texture2D BuildBiteTexture()
        {
            var tex = NewMarkTexture("T_SkinMark_Bite");
            var pixels = new Color[TextureSize * TextureSize];
            var rng = new System.Random(1913);

            // Punciones dentales: arco superior más ancho que el inferior, como una dentadura humana.
            var punctures = new List<Vector3>(16);
            AddDentalArc(punctures, rng, 0.62f, 0.27f, -1f, 7);
            AddDentalArc(punctures, rng, 0.38f, 0.22f, 1f, 6);

            Color bruise = new Color(0.38f, 0.12f, 0.2f);
            Color raw = new Color(0.62f, 0.07f, 0.06f);
            Color clot = new Color(0.22f, 0.02f, 0.02f);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    var uv = new Vector2((x + 0.5f) / TextureSize, (y + 0.5f) / TextureSize);
                    var centered = new Vector2((uv.x - 0.5f) / 0.42f, (uv.y - 0.5f) / 0.34f);

                    // Hematoma ovalado alrededor de la mordida.
                    float halo = 1f - Mathf.SmoothStep(0.35f, 1f, centered.magnitude);
                    halo *= Mathf.Lerp(0.6f, 1f, Mathf.PerlinNoise(uv.x * 7f + 11f, uv.y * 7f + 5f));

                    float wound = 0f;
                    for (int i = 0; i < punctures.Count; i++)
                    {
                        float d = Vector2.Distance(uv, new Vector2(punctures[i].x, punctures[i].y)) / punctures[i].z;
                        wound = Mathf.Max(wound, 1f - Mathf.SmoothStep(0.4f, 1f, d));
                    }

                    Color c = Color.Lerp(bruise, raw, Mathf.Clamp01(wound * 1.6f));
                    c = Color.Lerp(c, clot, Mathf.Clamp01(wound * wound * 1.2f));
                    c.a = Mathf.Clamp01(Mathf.Max(halo * 0.8f, wound));
                    pixels[y * TextureSize + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        private static void AddDentalArc(List<Vector3> punctures, System.Random rng, float baseY, float halfWidth, float curvature, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float x = Mathf.Lerp(0.5f - halfWidth, 0.5f + halfWidth, t);
                float bend = (t - 0.5f) * 2f;
                float y = baseY + curvature * 0.09f * bend * bend + RandomRange(rng, -0.012f, 0.012f);
                float size = RandomRange(rng, 0.75f, 1f) * (i == 0 || i == count - 1 ? 0.036f : 0.046f);
                punctures.Add(new Vector3(x, y, size));
            }
        }

        private static float RandomRange(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
