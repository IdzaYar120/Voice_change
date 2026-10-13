using UnityEngine;

namespace AnimeSouls
{
    /// <summary>Scales and fades an object over its lifetime, then destroys it.</summary>
    public class FadeOut : MonoBehaviour
    {
        public float life = 0.5f;
        public Vector3 endScale;
        public bool scaleEnabled = true;
        Vector3 _startScale;
        Material _mat;
        Color _baseColor;
        float _t;
        LineRenderer _line;
        Color _lineStart, _lineEnd;

        public void Init(float lifetime, Material ownedMaterial, Vector3? targetScale = null)
        {
            life = Mathf.Max(0.01f, lifetime);
            _mat = ownedMaterial;
            if (_mat != null && _mat.HasProperty("_Color")) _baseColor = _mat.GetColor("_Color");
            _startScale = transform.localScale;
            scaleEnabled = targetScale.HasValue;
            endScale = targetScale ?? _startScale;
            _line = GetComponent<LineRenderer>();
            if (_line != null) { _lineStart = _line.startColor; _lineEnd = _line.endColor; }
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / life);
            if (scaleEnabled)
            {
                float e = 1f - (1f - k) * (1f - k); // ease out
                transform.localScale = Vector3.LerpUnclamped(_startScale, endScale, e);
            }
            float fade = 1f - k * k;
            if (_mat != null) _mat.SetColor("_Color", _baseColor.WithAlpha(_baseColor.a * fade));
            if (_line != null)
            {
                _line.startColor = _lineStart.WithAlpha(_lineStart.a * fade);
                _line.endColor = _lineEnd.WithAlpha(_lineEnd.a * fade);
            }
            if (_t >= life) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }

    public class FollowTarget : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;

        void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }
            transform.position = target.position + offset;
        }
    }

    public class Spin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0, 180, 0);
        void Update() { transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self); }
    }

    public class Bob : MonoBehaviour
    {
        public float amplitude = 0.25f;
        public float speed = 2f;
        Vector3 _base;
        float _phase;
        void Start() { _base = transform.localPosition; _phase = Random.value * 10f; }
        void Update() { transform.localPosition = _base + Vector3.up * Mathf.Sin(Time.time * speed + _phase) * amplitude; }
    }

    /// <summary>Spawns throwaway visual effects built from primitives and particles.</summary>
    public static class VFX
    {
        public static GameObject Orb(Vector3 pos, float size, Color c, Transform parent = null)
        {
            var go = MeshFactory.Part("Orb", MeshFactory.Sphere, Mats.Glow(c), parent, Vector3.zero, Vector3.one * size, null, false);
            if (parent == null) go.transform.position = pos;
            else go.transform.localPosition = pos;
            return go;
        }

        public static void Burst(Vector3 pos, float radius, Color c, float life = 0.45f)
        {
            radius = Mathf.Max(0.5f, radius);
            var mat = Mats.GlowInstance(c.WithAlpha(0.7f));
            var sphere = MeshFactory.Part("Burst", MeshFactory.Sphere, mat, null, Vector3.zero, Vector3.one * 0.3f, null, false);
            sphere.transform.position = pos;
            sphere.AddComponent<FadeOut>().Init(life, mat, Vector3.one * radius * 2f);
            Ring(new Vector3(pos.x, World.GroundHeight(pos) + 0.15f, pos.z), radius * 1.15f, c, life * 1.3f);
            Particles(pos, c, Mathf.Clamp((int)(radius * 8), 8, 60), radius * 4f, 0.25f + radius * 0.06f, 0.6f);
        }

        public static void Ring(Vector3 pos, float radius, Color c, float life = 0.5f)
        {
            var mat = Mats.GlowInstance(c.WithAlpha(0.85f));
            var ring = MeshFactory.Part("Ring", MeshFactory.Ring, mat, null, Vector3.zero, Vector3.one * 0.2f, null, false);
            ring.transform.position = pos;
            ring.AddComponent<FadeOut>().Init(life, mat, Vector3.one * radius);
        }

        /// <summary>A static ground marker that fades out (telegraph for slams).</summary>
        public static void Telegraph(Vector3 pos, float radius, Color c, float life)
        {
            var mat = Mats.GlowInstance(c.WithAlpha(0.45f));
            var disc = MeshFactory.Part("Telegraph", MeshFactory.Cylinder, mat, null, Vector3.zero,
                new Vector3(radius * 2f, 0.02f, radius * 2f), null, false);
            disc.transform.position = new Vector3(pos.x, World.GroundHeight(pos) + 0.08f, pos.z);
            disc.AddComponent<FadeOut>().Init(life, mat);
        }

        static ParticleSystem MakeSystem(string name, Vector3 pos, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent, true);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Particle();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
            return ps;
        }

        public static void Particles(Vector3 pos, Color c, int count, float speed, float size, float life, float gravity = 0f)
        {
            var ps = MakeSystem("Particles", pos, null);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(c, c.Brighten(0.5f));
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count + 8;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.2f;
            ps.Play();
            Object.Destroy(ps.gameObject, life + 0.5f);
        }

        /// <summary>Looping particle aura around a transform. Destroy the returned object to stop it.</summary>
        public static GameObject Aura(Transform parent, Color c, float radius = 0.7f, float height = 1f, float rate = 40f)
        {
            var ps = MakeSystem("Aura", parent.position + Vector3.up * height, parent);
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(c, c.Brighten(0.4f));
            main.gravityModifier = -0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
            ps.Play();
            return ps.gameObject;
        }

        public static void Smoke(Vector3 pos, float size = 1f)
        {
            Particles(pos + Vector3.up * 0.8f, new Color(0.92f, 0.92f, 0.95f, 0.9f), 18, 2.5f * size, 0.9f * size, 0.7f);
        }

        public static TrailRenderer Trail(GameObject go, Color c, float width, float time = 0.25f)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = Mats.Glow(Color.white);
            tr.time = time;
            tr.startWidth = width;
            tr.endWidth = 0f;
            tr.startColor = c.WithAlpha(0.9f);
            tr.endColor = c.WithAlpha(0f);
            tr.minVertexDistance = 0.1f;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        public static void Slash(Vector3 pos, Quaternion rot, float radius, float arcDegrees, Color c, float life = 0.25f)
        {
            int pieces = arcDegrees > 200f ? 2 : 1;
            for (int i = 0; i < pieces; i++)
            {
                var mat = Mats.GlowInstance(c.WithAlpha(0.95f));
                float widthScale = pieces == 2 ? 1f : Mathf.Clamp(Mathf.Sin(Mathf.Min(arcDegrees, 180f) * 0.5f * Mathf.Deg2Rad), 0.35f, 1f);
                var go = MeshFactory.Part("Slash", MeshFactory.Arc, mat, null, Vector3.zero,
                    new Vector3(radius * widthScale, 1f, radius), null, false);
                go.transform.position = pos;
                go.transform.rotation = rot * Quaternion.Euler(0, i * 180f, Random.Range(-25f, 25f));
                go.AddComponent<FadeOut>().Init(life, mat, new Vector3(radius * widthScale * 1.15f, 1f, radius * 1.15f));
            }
        }

        public static GameObject Beam(Vector3 from, Vector3 dir, float length, float width, Color c, float life)
        {
            dir = dir.normalized;
            var root = new GameObject("Beam");
            root.transform.position = from + dir * (length * 0.5f);
            root.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            var outerMat = Mats.GlowInstance(c.WithAlpha(0.75f));
            var outer = MeshFactory.Part("Outer", MeshFactory.Cylinder, outerMat, root.transform, Vector3.zero,
                new Vector3(width, length * 0.5f, width), null, false);
            outer.AddComponent<FadeOut>().Init(life, outerMat);
            var coreMat = Mats.GlowInstance(Color.white.WithAlpha(0.95f));
            var core = MeshFactory.Part("Core", MeshFactory.Cylinder, coreMat, root.transform, Vector3.zero,
                new Vector3(width * 0.45f, length * 0.5f, width * 0.45f), null, false);
            core.AddComponent<FadeOut>().Init(life, coreMat);
            Object.Destroy(root, life + 0.05f);
            return root;
        }

        public static void Line(Vector3 from, Vector3 to, float width, Color c, float life, int jaggedSegments = 0)
        {
            var go = new GameObject("Line");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.Glow(Color.white);
            int n = Mathf.Max(2, jaggedSegments + 2);
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                var p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < n - 1 && jaggedSegments > 0) p += Random.insideUnitSphere * 0.6f;
                lr.SetPosition(i, p);
            }
            lr.widthMultiplier = width;
            lr.startColor = c;
            lr.endColor = c.Brighten(0.4f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<FadeOut>().Init(life, null);
        }

        public static void Lightning(Vector3 from, Vector3 to, Color c, float life = 0.2f)
        {
            Line(from, to, 0.18f, c, life, 6);
            Line(from, to, 0.06f, Color.white, life, 6);
        }
    }
}
