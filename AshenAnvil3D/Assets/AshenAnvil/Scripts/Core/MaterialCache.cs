using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Shares one material per colour, created from the generated URP base material.
    /// Sharing keeps draw calls low (SRP batcher + GPU instancing) on mobile.
    /// </summary>
    public static class MaterialCache
    {
        static readonly Dictionary<int, Material> lit = new Dictionary<int, Material>();
        static readonly Dictionary<int, Material> vfx = new Dictionary<int, Material>();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>Editor hook: lets the prefab baker save generated materials as assets.</summary>
        public static Func<Material, Material> PersistHook;

        static int Key(Color c, float extra)
        {
            Color32 c32 = c;
            return (c32.r << 24) ^ (c32.g << 16) ^ (c32.b << 8) ^ c32.a ^ Mathf.RoundToInt(extra * 1000f) * 7919;
        }

        public static Material Lit(Color color, float emission = 0f)
        {
            int key = Key(color, emission);
            if (lit.TryGetValue(key, out var m) && m != null) return m;
            var baseMat = GameDatabase.Instance != null ? GameDatabase.Instance.litMaterial : null;
            m = baseMat != null ? new Material(baseMat) : new Material(FallbackShader());
            m.name = "Lit_" + ColorUtility.ToHtmlStringRGB(color) + (emission > 0 ? "_E" : "");
            m.color = color;
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, color);
            if (emission > 0f && m.HasProperty(EmissionColor))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor(EmissionColor, color * emission);
            }
            m.enableInstancing = true;
            if (PersistHook != null) m = PersistHook(m);
            lit[key] = m;
            return m;
        }

        public static Material Vfx(Color color)
        {
            int key = Key(color, 0);
            if (vfx.TryGetValue(key, out var m) && m != null) return m;
            var baseMat = GameDatabase.Instance != null ? GameDatabase.Instance.vfxMaterial : null;
            m = baseMat != null ? new Material(baseMat) : new Material(FallbackShader());
            m.name = "Vfx_" + ColorUtility.ToHtmlStringRGBA(color);
            m.color = color;
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, color);
            if (PersistHook != null) m = PersistHook(m);
            vfx[key] = m;
            return m;
        }

        static Shader FallbackShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        public static void Clear()
        {
            lit.Clear();
            vfx.Clear();
        }
    }
}
