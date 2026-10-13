using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>Material cache. Shaders live in Resources/Shaders so they are included in builds.</summary>
    public static class Mats
    {
        static Shader _toon, _terrain, _glow, _particle, _sky;
        static readonly Dictionary<long, Material> ToonCache = new Dictionary<long, Material>();
        static readonly Dictionary<long, Material> GlowCache = new Dictionary<long, Material>();
        static Material _particleMat;
        static Material _terrainMat;

        static Shader Find(ref Shader cache, string name, string fallback)
        {
            if (cache != null) return cache;
            cache = Shader.Find(name);
            if (cache == null)
            {
                Debug.LogWarning($"[AnimeSouls] Shader '{name}' not found, falling back to '{fallback}'.");
                cache = Shader.Find(fallback);
            }
            return cache;
        }

        public static Shader ToonShader => Find(ref _toon, "AnimeSouls/Toon", "Standard");
        public static Shader TerrainShader => Find(ref _terrain, "AnimeSouls/ToonTerrain", "Standard");
        public static Shader GlowShader => Find(ref _glow, "AnimeSouls/Glow", "Unlit/Color");
        public static Shader ParticleShader => Find(ref _particle, "AnimeSouls/GlowParticle", "Sprites/Default");
        public static Shader SkyShader => Find(ref _sky, "AnimeSouls/Sky", "Skybox/Procedural");

        static long Key(Color c, float extra)
        {
            Color32 k = c;
            return ((long)k.r << 32) | ((long)k.g << 24) | ((long)k.b << 16) | ((long)k.a << 8) | (long)(Mathf.Clamp01(extra / 0.02f) * 255f);
        }

        /// <summary>Shared cel-shaded material. Do not modify the returned instance.</summary>
        public static Material Toon(Color c, float outline = 0.0035f, float emission = 0f)
        {
            long key = Key(c, outline) ^ ((long)(emission * 100) << 40);
            if (ToonCache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(ToonShader) { name = "Toon" };
            m.SetColor("_Color", c);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
            if (m.HasProperty("_Emission")) m.SetFloat("_Emission", emission);
            ToonCache[key] = m;
            return m;
        }

        /// <summary>A fresh cel-shaded material instance (for parts whose colour changes at runtime).</summary>
        public static Material ToonInstance(Color c, float outline = 0.0035f)
        {
            var m = new Material(ToonShader) { name = "ToonInstance" };
            m.SetColor("_Color", c);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
            return m;
        }

        public static Material Terrain()
        {
            if (_terrainMat != null) return _terrainMat;
            _terrainMat = new Material(TerrainShader) { name = "Terrain" };
            _terrainMat.SetColor("_Color", Color.white);
            return _terrainMat;
        }

        /// <summary>Shared glow material, cached by colour.</summary>
        public static Material Glow(Color c)
        {
            long key = Key(c, 0);
            if (GlowCache.TryGetValue(key, out var m) && m != null) return m;
            m = GlowInstance(c);
            GlowCache[key] = m;
            return m;
        }

        /// <summary>A glow material instance that can be faded. Caller owns it.</summary>
        public static Material GlowInstance(Color c)
        {
            var m = new Material(GlowShader) { name = "Glow" };
            m.SetColor("_Color", c);
            m.renderQueue = 3000;
            return m;
        }

        public static Material Particle()
        {
            if (_particleMat != null) return _particleMat;
            _particleMat = new Material(ParticleShader) { name = "GlowParticle" };
            _particleMat.SetColor("_Color", Color.white);
            return _particleMat;
        }

        public static Material Sky()
        {
            var m = new Material(SkyShader) { name = "AnimeSky" };
            return m;
        }

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }

        public static Color Brighten(this Color c, float amount)
        {
            return Color.Lerp(c, Color.white, amount).WithAlpha(c.a);
        }

        public static Color Darken(this Color c, float amount)
        {
            return Color.Lerp(c, Color.black, amount).WithAlpha(c.a);
        }
    }
}
