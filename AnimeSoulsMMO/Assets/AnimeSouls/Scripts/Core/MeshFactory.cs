using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>Built-in primitive meshes plus a few generated shapes (cones, arcs, rings).</summary>
    public static class MeshFactory
    {
        static readonly Dictionary<PrimitiveType, Mesh> Primitives = new Dictionary<PrimitiveType, Mesh>();
        static readonly Dictionary<int, Mesh> Cones = new Dictionary<int, Mesh>();
        static Mesh _arc;
        static Mesh _ring;
        static Mesh _lowSphere;

        public static Mesh Sphere => Primitive(PrimitiveType.Sphere);
        public static Mesh Cube => Primitive(PrimitiveType.Cube);
        public static Mesh Cylinder => Primitive(PrimitiveType.Cylinder);
        public static Mesh Capsule => Primitive(PrimitiveType.Capsule);
        public static Mesh Quad => Primitive(PrimitiveType.Quad);

        public static Mesh Primitive(PrimitiveType type)
        {
            if (Primitives.TryGetValue(type, out var m) && m != null) return m;
            var go = GameObject.CreatePrimitive(type);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            Primitives[type] = m;
            return m;
        }

        /// <summary>Cone with base radius 0.5 at y=0 and tip at y=1. Flat shaded.</summary>
        public static Mesh Cone(int segments = 8)
        {
            if (Cones.TryGetValue(segments, out var cached) && cached != null) return cached;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var tip = new Vector3(0, 1, 0);
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                int b = verts.Count;
                verts.Add(p0); verts.Add(tip); verts.Add(p1);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(Vector3.zero);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }
            var mesh = new Mesh { name = "Cone" + segments };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Cones[segments] = mesh;
            return mesh;
        }

        /// <summary>Flat-shaded icosahedron with radius 0.5: cheap faceted "anime low-poly" sphere.</summary>
        public static Mesh LowSphere
        {
            get
            {
                if (_lowSphere != null) return _lowSphere;
                float t = (1f + Mathf.Sqrt(5f)) / 2f;
                var p = new[]
                {
                    new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                    new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                    new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
                };
                int[] f =
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                    1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                    4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
                };
                var verts = new Vector3[f.Length];
                var tris = new int[f.Length];
                for (int i = 0; i < f.Length; i += 3)
                {
                    var a = p[f[i]].normalized * 0.5f;
                    var b = p[f[i + 1]].normalized * 0.5f;
                    var c = p[f[i + 2]].normalized * 0.5f;
                    // Unity front faces satisfy cross(b - a, c - a) pointing outwards.
                    if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0) { var tmp = b; b = c; c = tmp; }
                    verts[i] = a; verts[i + 1] = b; verts[i + 2] = c;
                    tris[i] = i; tris[i + 1] = i + 1; tris[i + 2] = i + 2;
                }
                _lowSphere = new Mesh { name = "LowSphere" };
                _lowSphere.vertices = verts;
                _lowSphere.triangles = tris;
                _lowSphere.RecalculateNormals();
                _lowSphere.RecalculateBounds();
                return _lowSphere;
            }
        }

        /// <summary>Flat crescent in the XZ plane, centred on +Z, spanning 180 degrees. Scale X to widen.</summary>
        public static Mesh Arc
        {
            get
            {
                if (_arc != null) return _arc;
                const int seg = 24;
                var verts = new List<Vector3>();
                var tris = new List<int>();
                var cols = new List<Color>();
                for (int i = 0; i <= seg; i++)
                {
                    float t = i / (float)seg;
                    float a = Mathf.Lerp(-90f, 90f, t) * Mathf.Deg2Rad;
                    float thick = Mathf.Sin(t * Mathf.PI); // thick middle, sharp tips
                    var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    verts.Add(dir * 1f);
                    verts.Add(dir * (1f - 0.35f * thick));
                    cols.Add(new Color(1, 1, 1, thick));
                    cols.Add(new Color(1, 1, 1, 0.2f * thick));
                }
                for (int i = 0; i < seg; i++)
                {
                    int b = i * 2;
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                    tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
                }
                _arc = new Mesh { name = "Arc" };
                _arc.SetVertices(verts);
                _arc.SetColors(cols);
                _arc.SetTriangles(tris, 0);
                _arc.RecalculateNormals();
                _arc.RecalculateBounds();
                return _arc;
            }
        }

        /// <summary>Thin flat ring (radius 1) in the XZ plane.</summary>
        public static Mesh Ring
        {
            get
            {
                if (_ring != null) return _ring;
                const int seg = 48;
                var verts = new List<Vector3>();
                var tris = new List<int>();
                for (int i = 0; i <= seg; i++)
                {
                    float a = i * Mathf.PI * 2f / seg;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    verts.Add(dir);
                    verts.Add(dir * 0.86f);
                }
                for (int i = 0; i < seg; i++)
                {
                    int b = i * 2;
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
                }
                _ring = new Mesh { name = "Ring" };
                _ring.SetVertices(verts);
                _ring.SetTriangles(tris, 0);
                _ring.RecalculateNormals();
                _ring.RecalculateBounds();
                return _ring;
            }
        }

        /// <summary>Creates a child object rendering the given mesh. No collider.</summary>
        public static GameObject Part(string name, Mesh mesh, Material mat, Transform parent,
            Vector3 localPos, Vector3 localScale, Quaternion? localRot = null, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
