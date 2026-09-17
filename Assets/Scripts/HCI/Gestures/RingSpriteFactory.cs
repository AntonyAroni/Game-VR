using UnityEngine;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Generador procedural del sprite anular usado por el indicador de confirmación.
    /// Principio de Responsabilidad Única (SRP): sólo fabrica y cachea la textura del anillo.
    /// Se genera en código para no depender de ningún asset de imagen importado,
    /// igual que las texturas procedurales del monitor CRT de signos vitales.
    /// </summary>
    public static class RingSpriteFactory
    {
        private static Sprite cachedRing;

        /// <summary>
        /// Devuelve un sprite circular hueco (corona) con bordes suavizados,
        /// apto para rellenarse radialmente con <c>Image.FillMethod.Radial360</c>.
        /// </summary>
        public static Sprite GetRingSprite()
        {
            if (cachedRing != null) return cachedRing;

            const int size = 256;
            const float innerRatio = 0.74f;
            const float outerRatio = 0.96f;
            const float edgeSoftness = 2.0f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GestureRing_Procedural",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            float center = (size - 1) * 0.5f;
            float outerRadius = outerRatio * center;
            float innerRadius = innerRatio * center;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; ++y)
            {
                float dy = y - center;
                for (int x = 0; x < size; ++x)
                {
                    float dx = x - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    // Suavizado analítico en ambos bordes de la corona (evita el aliasing del círculo).
                    float outerFade = Mathf.Clamp01((outerRadius - distance) / edgeSoftness);
                    float innerFade = Mathf.Clamp01((distance - innerRadius) / edgeSoftness);
                    byte alpha = (byte)(Mathf.Clamp01(outerFade * innerFade) * 255f);

                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            cachedRing = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            cachedRing.name = "GestureRing_Sprite";
            cachedRing.hideFlags = HideFlags.HideAndDontSave;
            return cachedRing;
        }
    }
}
