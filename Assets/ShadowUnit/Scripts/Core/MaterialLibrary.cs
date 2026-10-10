using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowUnit
{
    /// <summary>
    /// Creates materials at runtime from base materials in Resources/ShadowUnit (made by the editor setup menu),
    /// so the right shader is always included in builds for both the Built-in pipeline and URP.
    /// Also generates small procedural textures so surfaces are not flat colours.
    /// </summary>
    public static class MaterialLibrary
    {
        static Material litBase, unlitBase;
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        static Material LitBase
        {
            get
            {
                if (litBase != null) return litBase;
                litBase = Resources.Load<Material>("ShadowUnit/SU_Lit");
                if (litBase == null)
                {
                    bool urp = GraphicsSettings.currentRenderPipeline != null;
                    Shader s = urp ? Shader.Find("Universal Render Pipeline/Lit") : null;
                    if (s == null) s = Shader.Find("Standard");
                    if (s == null) s = Shader.Find("Legacy Shaders/Diffuse");
                    litBase = new Material(s);
                }
                return litBase;
            }
        }

        static Material UnlitBase
        {
            get
            {
                if (unlitBase != null) return unlitBase;
                unlitBase = Resources.Load<Material>("ShadowUnit/SU_Unlit");
                if (unlitBase == null) unlitBase = new Material(Shader.Find("Sprites/Default"));
                return unlitBase;
            }
        }

        public static Material Lit(string key, Color color, Texture2D tex = null, Vector2? tiling = null, float smoothness = 0.12f, float metallic = 0f)
        {
            Vector2 t = tiling ?? Vector2.one;
            string k = key + "|" + t.x.ToString("0.0") + "x" + t.y.ToString("0.0");
            Material m;
            if (Cache.TryGetValue(k, out m)) return m;
            m = new Material(LitBase) { name = "SU_" + key };
            m.color = color;
            if (tex != null)
            {
                m.mainTexture = tex;
                m.mainTextureScale = t;
                if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", t);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            Cache[k] = m;
            return m;
        }

        public static Material Emissive(string key, Color color, Color emission)
        {
            Material m = Lit(key, color, null, null, 0.6f, 0f);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
            return m;
        }

        public static Material Unlit(string key, Color color, Texture2D tex = null)
        {
            Material m;
            if (Cache.TryGetValue("U_" + key, out m)) return m;
            m = new Material(UnlitBase) { name = "SU_U_" + key };
            m.color = color;
            if (tex != null) m.mainTexture = tex;
            Cache["U_" + key] = m;
            return m;
        }

        // ---------- procedural textures ----------

        static Texture2D NewTex(string key, int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = key, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            return t;
        }

        static float Noise(float x, float y, float scale) => Mathf.PerlinNoise(x * scale + 13.1f, y * scale + 7.7f);

        /// <summary>Grey-scale detail texture; tint comes from the material colour.</summary>
        public static Texture2D Tex(string kind)
        {
            Texture2D cached;
            if (Textures.TryGetValue(kind, out cached)) return cached;
            const int S = 128;
            Texture2D t = NewTex(kind, S);
            var px = new Color[S * S];
            var rnd = new System.Random(kind.GetHashCode());
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = x / (float)S, v = y / (float)S;
                float grain = (float)rnd.NextDouble();
                float n1 = Noise(u, v, 4f), n2 = Noise(u, v, 16f);
                float val;
                switch (kind)
                {
                    case "dirt":
                        val = 0.72f + n1 * 0.2f + n2 * 0.12f + (grain - 0.5f) * 0.16f;
                        if (grain > 0.985f) val += 0.25f;
                        break;
                    case "concrete":
                        val = 0.8f + n2 * 0.1f + (grain - 0.5f) * 0.08f - Mathf.Max(0, Noise(u * 0.3f, v * 2f, 6f) - 0.55f) * 0.5f;
                        if (y % 64 < 2 || x % 64 < 2) val -= 0.18f;
                        break;
                    case "asphalt":
                        val = 0.55f + n2 * 0.1f + (grain - 0.5f) * 0.22f;
                        break;
                    case "sandbag":
                    {
                        int row = y / 32; float off = (row % 2) * 0.5f;
                        float bx = Mathf.Repeat(u * 2f + off, 1f), by = (y % 32) / 32f;
                        float edge = Mathf.Min(Mathf.Min(bx, 1 - bx) * 4f, Mathf.Min(by, 1 - by) * 4f);
                        val = 0.55f + Mathf.Clamp01(edge) * 0.35f + (grain - 0.5f) * 0.1f;
                        break;
                    }
                    case "wood":
                        val = 0.7f + Mathf.Sin(v * 90f + n1 * 6f) * 0.06f + (grain - 0.5f) * 0.08f;
                        if (y % 32 < 2) val -= 0.35f;
                        break;
                    case "metal":
                        val = 0.72f + Mathf.Sin(u * Mathf.PI * 16f) * 0.12f + (grain - 0.5f) * 0.05f - Mathf.Max(0, n1 - 0.6f) * 0.6f;
                        break;
                    case "camo":
                        val = n1 > 0.62f ? 0.55f : n1 > 0.45f ? 0.8f : 1f;
                        val += (grain - 0.5f) * 0.06f;
                        break;
                    default:
                        val = 0.85f + (grain - 0.5f) * 0.1f;
                        break;
                }
                val = Mathf.Clamp01(val);
                px[y * S + x] = new Color(val, val, val, 1f);
            }
            t.SetPixels(px);
            t.Apply(true);
            Textures[kind] = t;
            return t;
        }

        /// <summary>Soft round sprite used for particles and flashes.</summary>
        public static Texture2D Soft
        {
            get
            {
                Texture2D cached;
                if (Textures.TryGetValue("soft", out cached)) return cached;
                const int S = 32;
                var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S / 2f, S / 2f)) / (S / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    t.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
                t.Apply();
                Textures["soft"] = t;
                return t;
            }
        }
    }
}
