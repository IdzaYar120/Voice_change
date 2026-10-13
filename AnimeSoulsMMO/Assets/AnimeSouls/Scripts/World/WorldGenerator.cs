using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>Builds the whole island at runtime: terrain, water, forests, the hub, clouds and the 10 camps.</summary>
    public class WorldGenerator : MonoBehaviour
    {
        public int seed = 20261013;
        const int ChunkCells = 64;

        static readonly Color GrassA = Hex.C("#6DBA4E");
        static readonly Color GrassB = Hex.C("#9BD35E");
        static readonly Color Road = Hex.C("#D2B47E");
        static readonly Color Sand = Hex.C("#EED9A2");
        static readonly Color Seabed = Hex.C("#C2A876");
        static readonly Color Rock = Hex.C("#8E8C9A");
        static readonly Color Snow = Hex.C("#F4F6FF");
        static readonly Color HubStone = Hex.C("#D8D0BE");

        public void Build()
        {
            var t0 = Time.realtimeSinceStartup;
            World.Generate(seed);
            BuildTerrain();
            BuildWater();
            BuildForests();
            BuildHub();
            BuildClouds();
            foreach (var a in AnimeDatabase.Anime)
            {
                var campRoot = new GameObject("Camp_" + a.name);
                campRoot.transform.SetParent(transform, false);
                CampBuilder.Build(a, campRoot.transform);
                var camp = campRoot.AddComponent<MobCamp>();
                camp.Init(a);
            }
            Debug.Log($"[AnimeSouls] World built in {(Time.realtimeSinceStartup - t0):0.00}s");
        }

        // ------------------------------------------------------------------ terrain

        void BuildTerrain()
        {
            var root = new GameObject("Terrain");
            root.transform.SetParent(transform, false);
            int chunks = World.Cells / ChunkCells;
            for (int cz = 0; cz < chunks; cz++)
            for (int cx = 0; cx < chunks; cx++)
            {
                var mesh = BuildChunkMesh(cx * ChunkCells, cz * ChunkCells);
                var go = new GameObject($"Chunk_{cx}_{cz}");
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Mats.Terrain();
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.layer = 0;
            }
        }

        Mesh BuildChunkMesh(int x0, int z0)
        {
            int quads = ChunkCells * ChunkCells;
            var verts = new Vector3[quads * 6];
            var cols = new Color[quads * 6];
            var tris = new int[quads * 6];
            int v = 0;
            for (int z = z0; z < z0 + ChunkCells; z++)
            for (int x = x0; x < x0 + ChunkCells; x++)
            {
                var p00 = P(x, z); var p10 = P(x + 1, z); var p01 = P(x, z + 1); var p11 = P(x + 1, z + 1);
                AddTri(verts, cols, tris, ref v, p00, p01, p11);
                AddTri(verts, cols, tris, ref v, p00, p11, p10);
            }
            var mesh = new Mesh { name = "TerrainChunk" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector3 P(int gx, int gz)
        {
            return new Vector3(gx * World.Cell - World.Half, World.Heights[gx, gz], gz * World.Cell - World.Half);
        }

        static void AddTri(Vector3[] verts, Color[] cols, int[] tris, ref int v, Vector3 a, Vector3 b, Vector3 c)
        {
            if (Vector3.Cross(b - a, c - a).y < 0) { var t = b; b = c; c = t; }
            var centre = (a + b + c) / 3f;
            var n = Vector3.Cross(b - a, c - a).normalized;
            var col = GroundColor(centre.x, centre.z, centre.y, 1f - n.y);
            // Vertex colours bypass the sRGB->linear conversion that material colours get.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) col = col.linear;
            verts[v] = a; verts[v + 1] = b; verts[v + 2] = c;
            cols[v] = col; cols[v + 1] = col; cols[v + 2] = col;
            tris[v] = v; tris[v + 1] = v + 1; tris[v + 2] = v + 2;
            v += 3;
        }

        static Color GroundColor(float x, float z, float h, float steep)
        {
            float n = Mathf.PerlinNoise(x * 0.025f + 300f, z * 0.025f + 300f);
            float n2 = Mathf.PerlinNoise(x * 0.11f + 70f, z * 0.11f + 70f);
            var c = Color.Lerp(GrassA, GrassB, n * 0.8f + n2 * 0.2f);

            float road = World.RoadDistance(x, z);
            float campW = 0f;
            foreach (var a in AnimeDatabase.Anime)
            {
                float w = World.CampInfluence(a, x, z, 18f);
                if (w > 0f)
                {
                    c = Color.Lerp(c, a.ground * (0.92f + 0.16f * n2), w);
                    campW = Mathf.Max(campW, w);
                }
            }

            float hubD = new Vector2(x, z).magnitude;
            c = Color.Lerp(c, HubStone * (0.95f + 0.1f * n2), 1f - World.Smooth(World.HubRadius - 6f, World.HubRadius + 4f, hubD));

            if (campW < 0.6f && hubD > World.HubRadius - 4f)
                c = Color.Lerp(c, Road * (0.93f + 0.1f * n2), (1f - World.Smooth(2.5f, 4.5f, road)) * (1f - campW));

            if (steep > 0.35f || h > 38f) c = Color.Lerp(c, Rock * (0.9f + 0.15f * n2), Mathf.Max(World.Smooth(0.35f, 0.5f, steep), World.Smooth(38f, 44f, h)));
            if (h > 54f) c = Color.Lerp(c, Snow, World.Smooth(54f, 58f, h));
            if (h < 2.2f) c = Color.Lerp(c, Sand, World.Smooth(2.2f, 1.2f, h));
            if (h < -0.6f) c = Color.Lerp(c, Seabed, World.Smooth(-0.6f, -3f, h));
            return c;
        }

        void BuildWater()
        {
            var water = MeshFactory.Part("Ocean", MeshFactory.Quad, Mats.Toon(Hex.C("#45B4F2"), 0f), transform,
                new Vector3(0, World.WaterLevel - 0.25f, 0), new Vector3(World.Size * 3f, World.Size * 3f, 1f),
                Quaternion.Euler(90, 0, 0), false);
            water.GetComponent<MeshRenderer>().receiveShadows = false;
        }

        // ------------------------------------------------------------------ forests

        class Batch
        {
            public readonly Dictionary<Material, List<CombineInstance>> parts = new Dictionary<Material, List<CombineInstance>>();
            public void Add(Material m, Mesh mesh, Matrix4x4 trs)
            {
                if (!parts.TryGetValue(m, out var list)) parts[m] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mesh, transform = trs });
            }
        }

        void BuildForests()
        {
            var root = new GameObject("Forests");
            root.transform.SetParent(transform, false);
            var colliders = new GameObject("TreeColliders");
            colliders.transform.SetParent(root.transform, false);

            int chunks = World.Cells / ChunkCells;
            var batches = new Batch[chunks, chunks];
            var rng = new System.Random(seed + 7);
            float R() => (float)rng.NextDouble();

            var trunk = Mats.Toon(Hex.C("#7A5236"));
            var leaves = new[] { Mats.Toon(Hex.C("#3E9A4A")), Mats.Toon(Hex.C("#56B04A")), Mats.Toon(Hex.C("#2F7F4A")) };
            var pine = Mats.Toon(Hex.C("#2E7A52"));
            var blossom = Mats.Toon(Hex.C("#FFB7D5"));
            var rock = Mats.Toon(Hex.C("#9C9AA8"));
            var flower = new[] { Mats.Toon(Hex.C("#FFE45C"), 0f), Mats.Toon(Hex.C("#FF7AB8"), 0f), Mats.Toon(Hex.C("#FFFFFF"), 0f) };

            const float spacing = 11f;
            int n = (int)(World.Size / spacing);
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                float x = ix * spacing - World.Half + R() * spacing;
                float z = iz * spacing - World.Half + R() * spacing;
                float h = World.GroundHeight(x, z);
                if (h < 2.5f || h > 50f) continue;
                if (World.Slope(x, z) > 0.55f) continue;
                if (new Vector2(x, z).magnitude < World.HubRadius + 18f) continue;
                if (World.RoadDistance(x, z) < 7f) continue;
                bool nearCamp = false;
                foreach (var a in AnimeDatabase.Anime)
                    if (World.CampInfluence(a, x, z, 22f) > 0.01f) { nearCamp = true; break; }
                if (nearCamp) continue;

                float density = Mathf.PerlinNoise(x * 0.008f + 11f, z * 0.008f + 11f);
                float roll = R();
                int cx = Mathf.Clamp((int)((x + World.Half) / (ChunkCells * World.Cell)), 0, chunks - 1);
                int cz = Mathf.Clamp((int)((z + World.Half) / (ChunkCells * World.Cell)), 0, chunks - 1);
                var batch = batches[cx, cz] ?? (batches[cx, cz] = new Batch());
                var basePos = new Vector3(x, h - 0.2f, z);
                float rot = R() * 360f;

                if (roll < density * 0.85f)
                {
                    float s = 0.8f + R() * 0.7f;
                    bool isPine = h > 24f || R() < 0.3f;
                    if (isPine)
                    {
                        batch.Add(trunk, MeshFactory.Cone(5), Matrix4x4.TRS(basePos, Quaternion.Euler(0, rot, 0), new Vector3(0.7f, 3f, 0.7f) * s));
                        for (int k = 0; k < 3; k++)
                        {
                            float y = (2f + k * 2.1f) * s;
                            float w = (4.4f - k * 1.1f) * s;
                            batch.Add(pine, MeshFactory.Cone(7), Matrix4x4.TRS(basePos + Vector3.up * y, Quaternion.Euler(0, rot + k * 20f, 0), new Vector3(w, 3.2f * s, w)));
                        }
                    }
                    else
                    {
                        var leaf = R() < 0.08f ? blossom : leaves[rng.Next(leaves.Length)];
                        batch.Add(trunk, MeshFactory.Cone(5), Matrix4x4.TRS(basePos, Quaternion.Euler(0, rot, 0), new Vector3(0.9f, 5f, 0.9f) * s));
                        batch.Add(leaf, MeshFactory.LowSphere, Matrix4x4.TRS(basePos + Vector3.up * 5f * s, Quaternion.Euler(R() * 30f, rot, 0), new Vector3(5.2f, 4.4f, 5.2f) * s));
                        batch.Add(leaf, MeshFactory.LowSphere, Matrix4x4.TRS(basePos + new Vector3(1.4f, 4.2f, 0.6f) * s, Quaternion.Euler(0, rot, 20f), Vector3.one * 3.2f * s));
                        batch.Add(leaf, MeshFactory.LowSphere, Matrix4x4.TRS(basePos + new Vector3(-1.2f, 4.5f, -0.8f) * s, Quaternion.Euler(10f, rot, 0), Vector3.one * 3f * s));
                    }
                    var col = new GameObject("Tree");
                    col.transform.SetParent(colliders.transform, false);
                    col.transform.position = basePos + Vector3.up * 2f;
                    var cap = col.AddComponent<CapsuleCollider>();
                    cap.radius = 0.35f * s;
                    cap.height = 4f;
                }
                else if (roll < density * 0.85f + 0.05f)
                {
                    float s = 0.8f + R() * 2.2f;
                    batch.Add(rock, MeshFactory.LowSphere, Matrix4x4.TRS(basePos, Quaternion.Euler(R() * 40f, rot, R() * 40f), new Vector3(1.4f, 0.9f, 1.2f) * s));
                }
                else if (roll < density * 0.85f + 0.2f)
                {
                    var m = flower[rng.Next(flower.Length)];
                    for (int k = 0; k < 5; k++)
                    {
                        var fp = basePos + new Vector3(R() * 4f - 2f, 0, R() * 4f - 2f);
                        fp.y = World.GroundHeight(fp) + 0.1f;
                        batch.Add(m, MeshFactory.LowSphere, Matrix4x4.TRS(fp, Quaternion.identity, Vector3.one * 0.35f));
                    }
                }
            }

            for (int cz = 0; cz < chunks; cz++)
            for (int cx = 0; cx < chunks; cx++)
            {
                var b = batches[cx, cz];
                if (b == null) continue;
                foreach (var kv in b.parts)
                {
                    var mesh = new Mesh { name = "ForestBatch", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                    var go = new GameObject($"Forest_{cx}_{cz}");
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
                }
            }
        }

        // ------------------------------------------------------------------ hub

        void BuildHub()
        {
            var root = new GameObject("SoulNexus");
            root.transform.SetParent(transform, false);
            float y = World.HubHeight;
            var stone = Mats.Toon(Hex.C("#E8E2D2"));
            var stoneDark = Mats.Toon(Hex.C("#A9A294"));

            MeshFactory.Part("Platform", MeshFactory.Cylinder, stone, root.transform, new Vector3(0, y - 0.45f, 0), new Vector3(52, 0.5f, 52));
            MeshFactory.Part("Inner", MeshFactory.Cylinder, stoneDark, root.transform, new Vector3(0, y - 0.3f, 0), new Vector3(20, 0.4f, 20));
            var rune = MeshFactory.Part("Rune", MeshFactory.Ring, Mats.Glow(Hex.C("#8FE8FF").WithAlpha(0.8f)), root.transform, new Vector3(0, y + 0.13f, 0), Vector3.one * 9.5f, null, false);
            rune.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 12f, 0);
            var rune2 = MeshFactory.Part("Rune2", MeshFactory.Ring, Mats.Glow(Hex.C("#C08BFF").WithAlpha(0.8f)), root.transform, new Vector3(0, y + 0.14f, 0), Vector3.one * 7f, null, false);
            rune2.AddComponent<Spin>().degreesPerSecond = new Vector3(0, -20f, 0);

            // Pedestal and the floating soul crystal.
            var ped = MeshFactory.Part("Pedestal", MeshFactory.Cylinder, stoneDark, root.transform, new Vector3(0, y + 0.8f, 0), new Vector3(4, 1.2f, 4));
            ped.AddComponent<CapsuleCollider>();
            var crystalPivot = new GameObject("CrystalPivot");
            crystalPivot.transform.SetParent(root.transform, false);
            crystalPivot.transform.localPosition = new Vector3(0, y + 7f, 0);
            crystalPivot.AddComponent<Bob>().amplitude = 0.6f;
            var crystal = MeshFactory.Part("Crystal", MeshFactory.LowSphere, Mats.Toon(Hex.C("#8FE8FF"), 0.004f, 0.55f), crystalPivot.transform, Vector3.zero, new Vector3(3f, 7f, 3f));
            crystal.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 40f, 0);
            VFX.Aura(crystalPivot.transform, Hex.C("#8FE8FF"), 2.5f, 0f, 25f);
            var light = new GameObject("CrystalLight").AddComponent<Light>();
            light.transform.SetParent(crystalPivot.transform, false);
            light.type = LightType.Point;
            light.color = Hex.C("#8FE8FF");
            light.range = 30f;
            light.intensity = 1.6f;

            // A pillar per anime, facing its camp, crowned with the camp's colour.
            foreach (var a in AnimeDatabase.Anime)
            {
                var dir = new Vector3(a.campPosition.x, 0, a.campPosition.z).normalized;
                var p = dir * 22f + Vector3.up * y;
                var pillar = MeshFactory.Part("Pillar_" + a.name, MeshFactory.Cube, stone, root.transform, p + Vector3.up * 3.5f, new Vector3(1.6f, 7f, 1.6f), Quaternion.LookRotation(dir));
                pillar.AddComponent<BoxCollider>();
                MeshFactory.Part("Cap", MeshFactory.Cube, stoneDark, root.transform, p + Vector3.up * 7.2f, new Vector3(2.2f, 0.5f, 2.2f), Quaternion.LookRotation(dir));
                var orb = VFX.Orb(p + Vector3.up * 8.6f, 1.3f, a.accent, null);
                orb.transform.SetParent(root.transform, true);
                orb.AddComponent<Bob>().amplitude = 0.2f;
                MeshFactory.Part("Banner", MeshFactory.Cube, Mats.Toon(a.accent), root.transform, p + Vector3.up * 4.5f + dir * 0.85f, new Vector3(1.2f, 3.2f, 0.08f), Quaternion.LookRotation(dir));
            }

            // Lanterns around the platform edge.
            var lanternBody = Mats.Toon(Hex.C("#B5222A"));
            var lanternGlow = Mats.Toon(Hex.C("#FFE6A0"), 0.003f, 0.8f);
            for (int i = 0; i < 16; i++)
            {
                float ang = (i + 0.5f) * Mathf.PI * 2f / 16f;
                var p = new Vector3(Mathf.Cos(ang) * 27f, y, Mathf.Sin(ang) * 27f);
                MeshFactory.Part("Post", MeshFactory.Cylinder, stoneDark, root.transform, p + Vector3.up * 1f, new Vector3(0.3f, 1f, 0.3f));
                MeshFactory.Part("Lantern", MeshFactory.Cube, lanternGlow, root.transform, p + Vector3.up * 2.3f, new Vector3(0.6f, 0.7f, 0.6f));
                MeshFactory.Part("LanternRoof", MeshFactory.Cone(4), lanternBody, root.transform, p + Vector3.up * 2.65f, new Vector3(1f, 0.5f, 1f), Quaternion.Euler(0, 45, 0));
            }
        }

        // ------------------------------------------------------------------ clouds

        void BuildClouds()
        {
            var root = new GameObject("Clouds");
            root.transform.SetParent(transform, false);
            var drift = root.AddComponent<CloudDrift>();
            var white = Mats.Toon(Hex.C("#FFFFFF"), 0.0025f, 0.25f);
            var rng = new System.Random(seed + 99);
            for (int i = 0; i < 45; i++)
            {
                var c = new GameObject("Cloud");
                c.transform.SetParent(root.transform, false);
                c.transform.localPosition = new Vector3((float)rng.NextDouble() * World.Size * 1.4f - World.Size * 0.7f,
                    120f + (float)rng.NextDouble() * 50f, (float)rng.NextDouble() * World.Size * 1.4f - World.Size * 0.7f);
                int puffs = 4 + rng.Next(4);
                for (int k = 0; k < puffs; k++)
                {
                    float s = 10f + (float)rng.NextDouble() * 14f;
                    var part = MeshFactory.Part("Puff", MeshFactory.Sphere, white, c.transform,
                        new Vector3(k * 9f - puffs * 4.5f, (float)rng.NextDouble() * 4f, (float)rng.NextDouble() * 10f - 5f),
                        new Vector3(s * 1.3f, s * 0.75f, s), null, false);
                    part.GetComponent<MeshRenderer>().receiveShadows = false;
                }
            }
            drift.speed = 2.5f;
        }
    }

    public class CloudDrift : MonoBehaviour
    {
        public float speed = 2f;

        void Update()
        {
            foreach (Transform c in transform)
            {
                var p = c.localPosition;
                p.x += speed * Time.deltaTime;
                if (p.x > World.Size * 0.75f) p.x = -World.Size * 0.75f;
                c.localPosition = p;
            }
        }
    }
}
