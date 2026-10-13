using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// The island height field. Heights are generated once on a grid and sampled with the same
    /// triangle split as the terrain mesh, so ground queries match the rendered surface exactly.
    /// </summary>
    public static class World
    {
        public const float Size = 1536f;
        public const float Half = Size * 0.5f;
        public const float Cell = 4f;
        public const int Cells = (int)(Size / Cell);
        public const float WaterLevel = 0f;
        public const float HubRadius = 42f;
        public const float HubHeight = 9f;

        public static float[,] Heights;
        public static bool Ready => Heights != null;
        public static Vector3 HubPosition => new Vector3(0, HubHeight, 0);

        static float _seedX = 137.2f, _seedZ = 911.7f;

        public static void Generate(int seed)
        {
            var rng = new System.Random(seed);
            _seedX = (float)rng.NextDouble() * 1000f;
            _seedZ = (float)rng.NextDouble() * 1000f;

            PlaceCamps();

            Heights = new float[Cells + 1, Cells + 1];
            for (int z = 0; z <= Cells; z++)
            {
                for (int x = 0; x <= Cells; x++)
                {
                    Heights[x, z] = ComputeHeight(x * Cell - Half, z * Cell - Half);
                }
            }
        }

        static void PlaceCamps()
        {
            foreach (var a in AnimeDatabase.Anime)
            {
                float angle = (a.index * 36f + 12f) * Mathf.Deg2Rad;
                float r = 150f + a.tier * 85f;
                var pos = new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
                pos.y = CampHeight(pos.x, pos.z);
                a.campPosition = pos;
            }
        }

        // ------------------------------------------------------------------ noise

        public static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        static float Fbm(float x, float z, int octaves)
        {
            float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq + _seedX, z * freq + _seedZ);
                norm += amp;
                amp *= 0.5f;
                freq *= 2.03f;
            }
            return sum / norm;
        }

        static float Hills(float x, float z)
        {
            float n = Fbm(x * 0.0035f, z * 0.0035f, 5);
            return 8f + Mathf.Max((n - 0.35f) * 55f, -4f) * 0.6f;
        }

        static float Mountains(float x, float z)
        {
            float m = Fbm(x * 0.0013f + 50f, z * 0.0013f + 50f, 4);
            float k = Mathf.Clamp01((m - 0.48f) * 2.6f);
            return k * k * 75f;
        }

        static float CampHeight(float x, float z) => Mathf.Max(5f, Hills(x, z));

        public static float RoadDistance(float x, float z)
        {
            float best = float.MaxValue;
            var p = new Vector2(x, z);
            foreach (var a in AnimeDatabase.Anime)
            {
                var b = new Vector2(a.campPosition.x, a.campPosition.z);
                best = Mathf.Min(best, SegmentDistance(p, Vector2.zero, b));
            }
            return best;
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>0..1 influence of a camp at a point (1 inside the camp).</summary>
        public static float CampInfluence(AnimeDef a, float x, float z, float margin = 30f)
        {
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(a.campPosition.x, a.campPosition.z));
            return 1f - Smooth(a.campRadius, a.campRadius + margin, d);
        }

        static float ComputeHeight(float x, float z)
        {
            float hills = Hills(x, z);
            float mountains = Mountains(x, z);

            // Keep roads walkable: mountains fade out near the roads.
            float road = RoadDistance(x, z);
            mountains *= Smooth(8f, 40f, road);
            float h = hills + mountains;

            // Island falloff into the ocean.
            float d = Mathf.Sqrt(x * x + z * z) / (Half * 0.92f);
            float island = 1f - Smooth(0.8f, 1.0f, d);
            h = Mathf.Lerp(-14f, h, island);

            // Flat plateaus for the hub and each camp.
            float hub = 1f - Smooth(HubRadius, HubRadius + 35f, Mathf.Sqrt(x * x + z * z));
            h = Mathf.Lerp(h, HubHeight, hub);
            foreach (var a in AnimeDatabase.Anime)
            {
                float w = CampInfluence(a, x, z);
                if (w > 0f) h = Mathf.Lerp(h, a.campPosition.y, w);
            }
            return h;
        }

        // ------------------------------------------------------------------ queries

        public static float GroundHeight(Vector3 p) => GroundHeight(p.x, p.z);

        public static float GroundHeight(float wx, float wz)
        {
            if (Heights == null) return 0f;
            float gx = Mathf.Clamp((wx + Half) / Cell, 0, Cells - 0.001f);
            float gz = Mathf.Clamp((wz + Half) / Cell, 0, Cells - 0.001f);
            int x = (int)gx, z = (int)gz;
            float fx = gx - x, fz = gz - z;
            float h00 = Heights[x, z], h10 = Heights[x + 1, z], h01 = Heights[x, z + 1], h11 = Heights[x + 1, z + 1];
            // Matches the terrain triangulation (00,01,11) / (00,11,10).
            if (fz > fx) return h00 + fz * (h01 - h00) + fx * (h11 - h01);
            return h00 + fx * (h10 - h00) + fz * (h11 - h10);
        }

        /// <summary>Ground height, never below the water surface.</summary>
        public static float WalkHeight(Vector3 p) => Mathf.Max(GroundHeight(p), WaterLevel - 0.6f);

        public static Vector3 OnGround(Vector3 p) { p.y = GroundHeight(p); return p; }

        public static float Slope(float x, float z)
        {
            float e = Cell;
            float dx = GroundHeight(x + e, z) - GroundHeight(x - e, z);
            float dz = GroundHeight(x, z + e) - GroundHeight(x, z - e);
            return new Vector2(dx, dz).magnitude / (2f * e);
        }

        public static AnimeDef CampAt(Vector3 p, float extra = 10f)
        {
            foreach (var a in AnimeDatabase.Anime)
            {
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.campPosition.x, a.campPosition.z));
                if (d < a.campRadius + extra) return a;
            }
            return null;
        }

        public static bool InHub(Vector3 p) => new Vector2(p.x, p.z).magnitude < HubRadius + 8f;
    }
}
