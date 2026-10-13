using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Themed set dressing for each camp, built from primitives. Props stay outside a radius of ~24m so the
    /// centre remains an open arena for mobs and the boss.
    /// </summary>
    public static class CampBuilder
    {
        class B
        {
            public Transform root;
            public Vector3 c;
            public System.Random rng;

            public float R() => (float)rng.NextDouble();
            public float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();

            Vector3 W(Vector3 local)
            {
                float x = c.x + local.x, z = c.z + local.z;
                return new Vector3(x, World.GroundHeight(x, z) + local.y, z);
            }

            public GameObject Box(Vector3 local, Vector3 size, Color col, float rotY = 0f, bool collider = true, float emission = 0f)
            {
                var go = MeshFactory.Part("Box", MeshFactory.Cube, Mats.Toon(col, 0.0035f, emission), root, Vector3.zero, size, null);
                go.transform.position = W(local + Vector3.up * size.y * 0.5f);
                go.transform.rotation = Quaternion.Euler(0, rotY, 0);
                if (collider) go.AddComponent<BoxCollider>();
                return go;
            }

            public GameObject Cyl(Vector3 local, float radius, float height, Color col, bool collider = true, float emission = 0f)
            {
                var go = MeshFactory.Part("Cyl", MeshFactory.Cylinder, Mats.Toon(col, 0.0035f, emission), root, Vector3.zero,
                    new Vector3(radius * 2f, height * 0.5f, radius * 2f), null);
                go.transform.position = W(local + Vector3.up * height * 0.5f);
                if (collider) go.AddComponent<CapsuleCollider>();
                return go;
            }

            public GameObject Cone(Vector3 local, float radius, float height, Color col, int seg = 8, float rotY = 0f)
            {
                var go = MeshFactory.Part("Cone", MeshFactory.Cone(seg), Mats.Toon(col), root, Vector3.zero,
                    new Vector3(radius * 2f, height, radius * 2f), null);
                go.transform.position = W(local);
                go.transform.rotation = Quaternion.Euler(0, rotY, 0);
                return go;
            }

            public GameObject Ball(Vector3 local, Vector3 size, Color col, bool low = true, float emission = 0f)
            {
                var go = MeshFactory.Part("Ball", low ? MeshFactory.LowSphere : MeshFactory.Sphere, Mats.Toon(col, 0.0035f, emission), root, Vector3.zero, size, null);
                go.transform.position = W(local);
                return go;
            }

            public GameObject Glow(Vector3 local, Mesh mesh, Vector3 size, Color col, Quaternion rot)
            {
                var go = MeshFactory.Part("Glow", mesh, Mats.Glow(col), root, Vector3.zero, size, rot, false);
                go.transform.position = W(local);
                return go;
            }

            public void House(Vector3 local, float w, float d, float h, Color wall, Color roof, float rotY)
            {
                var q = Quaternion.Euler(0, rotY, 0);
                Box(local, new Vector3(w, h, d), wall, rotY);
                var roofGo = Cone(local + Vector3.up * h, 0.5f, h * 0.6f, roof, 4, rotY + 45f);
                float s = Mathf.Max(w, d) * 1.55f;
                roofGo.transform.localScale = new Vector3(s, h * 0.65f, s);
                Box(local + q * new Vector3(0, 0, d * 0.5f + 0.01f), new Vector3(w * 0.25f, h * 0.55f, 0.15f), wall.Darken(0.55f), rotY, false);
                Box(local + q * new Vector3(w * 0.3f, h * 0.45f, d * 0.5f + 0.01f), new Vector3(w * 0.18f, h * 0.2f, 0.12f), Hex.C("#BFE6FF"), rotY, false, 0.3f);
            }

            public void Tree(Vector3 local, Color leaf, float s = 1f)
            {
                Cyl(local, 0.35f * s, 4.5f * s, Hex.C("#7A5236"));
                Ball(local + Vector3.up * 5.2f * s, new Vector3(5f, 4.2f, 5f) * s, leaf);
                Ball(local + new Vector3(1.3f, 4.4f, 0.5f) * s, Vector3.one * 3f * s, leaf);
            }

            public void Palm(Vector3 local, float s = 1f)
            {
                var trunkCol = Hex.C("#A0763E");
                Vector3 p = local;
                float lean = R(-1f, 1f);
                for (int i = 0; i < 5; i++)
                {
                    var seg = Cyl(p, 0.3f * s, 1.6f * s, trunkCol, i == 0);
                    seg.transform.rotation = Quaternion.Euler(lean * 6f * i, 0, lean * 4f * i);
                    p += new Vector3(lean * 0.25f * i, 1.5f * s, 0);
                }
                for (int i = 0; i < 6; i++)
                {
                    var leaf = Box(p, new Vector3(0.9f * s, 0.12f, 4.5f * s), Hex.C("#3FA84A"), i * 60f, false);
                    leaf.transform.rotation = Quaternion.Euler(18f, i * 60f, 0);
                    leaf.transform.position += leaf.transform.forward * 1.8f * s;
                }
            }

            public Vector3 Ring(float radius, float angleDeg)
            {
                float a = angleDeg * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            }

            public float FaceCentre(Vector3 local) => Mathf.Atan2(-local.x, -local.z) * Mathf.Rad2Deg;
        }

        public static void Build(AnimeDef a, Transform root)
        {
            var b = new B { root = root, c = a.campPosition, rng = new System.Random(1000 + a.index * 31) };
            Gate(b, a);
            switch (a.theme)
            {
                case CampTheme.Leaf: Leaf(b); break;
                case CampTheme.Pirate: Pirate(b); break;
                case CampTheme.Namek: Namek(b); break;
                case CampTheme.SoulSociety: SoulSociety(b); break;
                case CampTheme.Walls: Walls(b); break;
                case CampTheme.Wisteria: Wisteria(b); break;
                case CampTheme.Shibuya: Shibuya(b); break;
                case CampTheme.Academy: Academy(b); break;
                case CampTheme.Arena: Arena(b); break;
                case CampTheme.Alchemy: Alchemy(b); break;
            }
            // Boss altar marking the centre.
            b.Cyl(Vector3.zero, 3.2f, 0.25f, Hex.C("#5A5866"), false);
            b.Glow(Vector3.up * 0.3f, MeshFactory.Ring, Vector3.one * 3f, a.accent.WithAlpha(0.8f), Quaternion.identity)
                .AddComponent<Spin>().degreesPerSecond = new Vector3(0, 30, 0);
        }

        /// <summary>Two banners on the side facing the hub, so every camp has a readable entrance.</summary>
        static void Gate(B b, AnimeDef a)
        {
            var toHub = new Vector3(-b.c.x, 0, -b.c.z).normalized;
            var side = Vector3.Cross(Vector3.up, toHub);
            var gate = toHub * (a.campRadius + 2f);
            float rot = Mathf.Atan2(toHub.x, toHub.z) * Mathf.Rad2Deg;
            foreach (float s in new[] { -1f, 1f })
            {
                var p = gate + side * 6f * s;
                b.Box(p, new Vector3(1.2f, 7f, 1.2f), Hex.C("#4A3A2A"), rot);
                b.Box(p + Vector3.up * 2f + toHub * 0.65f, new Vector3(1.6f, 4f, 0.1f), a.accent, rot, false);
                b.Glow(p + Vector3.up * 7.8f, MeshFactory.Sphere, Vector3.one * 1.1f, a.accent, Quaternion.identity);
            }
        }

        // ------------------------------------------------------------------ themes

        static void Leaf(B b)
        {
            var wall = Hex.C("#F2E3C2");
            Color[] roofs = { Hex.C("#C8502A"), Hex.C("#3A7AB0"), Hex.C("#5AA04A") };
            for (int i = 0; i < 7; i++)
            {
                var p = b.Ring(b.R(30f, 40f), i * 51f + b.R(-10f, 10f));
                b.House(p, b.R(6f, 9f), b.R(6f, 8f), b.R(4.5f, 7f), wall, roofs[i % roofs.Length], b.FaceCentre(p));
            }
            // Red tower like the Hokage residence.
            var t = b.Ring(42f, 200f);
            b.Cyl(t, 6f, 12f, Hex.C("#D2533A"));
            b.Cone(t + Vector3.up * 12f, 7.5f, 4f, Hex.C("#9A2F22"), 12);
            b.Box(t + Vector3.up * 8f + Vector3.forward * 6f, new Vector3(3f, 2.4f, 0.3f), Hex.C("#F2E3C2"), 0, false);
            // Training logs.
            for (int i = 0; i < 3; i++) b.Cyl(b.Ring(20f, 120f + i * 8f), 0.45f, 2.4f, Hex.C("#8A5A36"));
            for (int i = 0; i < 8; i++) b.Tree(b.Ring(b.R(44f, 50f), b.R(0, 360)), Hex.C("#3E9A4A"), b.R(1.2f, 1.8f));
        }

        static void Pirate(B b)
        {
            // Ship on the far side.
            var s = b.Ring(36f, 90f);
            float rot = b.FaceCentre(s) + 90f;
            var q = Quaternion.Euler(0, rot, 0);
            var hull = Hex.C("#7A4A2A");
            b.Box(s, new Vector3(6.5f, 3.5f, 22f), hull, rot);
            b.Box(s + Vector3.up * 3.5f, new Vector3(6.9f, 0.4f, 22.4f), Hex.C("#A0703E"), rot);
            b.Box(s + q * new Vector3(0, 3.9f, -9f), new Vector3(6.5f, 2.5f, 4f), hull, rot);
            b.Cone(s + q * new Vector3(0, 1.2f, 12.5f), 2.4f, 4f, hull, 4, rot).transform.rotation = q * Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 2; i++)
            {
                var m = s + q * new Vector3(0, 3.9f, -3f + i * 8f);
                b.Cyl(m, 0.3f, 14f, Hex.C("#5A3A22"), false);
                b.Box(m + Vector3.up * 6.5f + q * Vector3.right * 0.4f, new Vector3(0.15f, 6f, 7f), Hex.C("#F8F4E8"), rot, false);
            }
            b.Box(s + q * new Vector3(0, 18.4f, 5f), new Vector3(0.1f, 1.6f, 2.6f), Hex.C("#151515"), rot, false);
            b.Ball(s + q * new Vector3(0.12f, 18.6f, 5f), Vector3.one * 0.7f, Hex.C("#FFFFFF"));
            // Huts, barrels, crates, palms.
            for (int i = 0; i < 4; i++)
            {
                var p = b.Ring(b.R(30f, 40f), 180f + i * 45f);
                b.House(p, 5f, 5f, 3.5f, Hex.C("#C9A06A"), Hex.C("#E8C86A"), b.FaceCentre(p));
            }
            for (int i = 0; i < 12; i++)
            {
                var p = b.Ring(b.R(25f, 30f), b.R(0, 360));
                if (b.R() < 0.5f) b.Cyl(p, 0.6f, 1.4f, Hex.C("#8A5A2E"));
                else b.Box(p, Vector3.one * 1.2f, Hex.C("#B5874A"), b.R(0, 90));
            }
            for (int i = 0; i < 9; i++) b.Palm(b.Ring(b.R(42f, 50f), b.R(0, 360)), b.R(1f, 1.5f));
        }

        static void Namek(B b)
        {
            for (int i = 0; i < 6; i++)
            {
                var p = b.Ring(b.R(30f, 40f), i * 60f + b.R(-12, 12));
                float r = b.R(4f, 6f);
                var dome = b.Ball(p, new Vector3(r * 2f, r * 1.6f, r * 2f), Hex.C("#F4F2EA"), false);
                dome.AddComponent<SphereCollider>();
                for (int k = 0; k < 3; k++)
                    b.Ball(p + b.Ring(r * 0.95f, k * 40f + b.FaceCentre(p)) + Vector3.up * r * 0.4f, Vector3.one * 0.9f, Hex.C("#2A3A4A"), false);
            }
            for (int i = 0; i < 12; i++)
            {
                var p = b.Ring(b.R(26f, 48f), b.R(0, 360));
                float h = b.R(6f, 10f);
                b.Cyl(p, 0.25f, h, Hex.C("#6B5A3A"));
                b.Ball(p + Vector3.up * h, Vector3.one * b.R(3f, 4.5f), Hex.C("#3FA87A"), false);
            }
            for (int i = 0; i < 7; i++)
            {
                var p = b.Ring(b.R(44f, 52f), b.R(0, 360));
                b.Cone(p, b.R(3f, 5f), b.R(14f, 26f), Hex.C("#8A9A7A"), 6, b.R(0, 90));
            }
        }

        static void SoulSociety(B b)
        {
            var white = Hex.C("#F4F1EA");
            var roof = Hex.C("#3A3A48");
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f + 22.5f;
                var p = b.Ring(44f, ang);
                b.Box(p, new Vector3(2f, 6f, 30f), white, -ang);
                b.Box(p + Vector3.up * 6f, new Vector3(3f, 0.6f, 31f), roof, -ang, false);
            }
            for (int i = 0; i < 5; i++)
            {
                var p = b.Ring(b.R(30f, 36f), i * 72f + 10f);
                b.House(p, b.R(8f, 11f), b.R(6f, 8f), b.R(5f, 6f), white, roof, b.FaceCentre(p));
            }
            // White tower.
            var t = b.Ring(38f, 250f);
            b.Box(t, new Vector3(6f, 26f, 6f), white, 20f);
            b.Cone(t + Vector3.up * 26f, 5.5f, 5f, roof, 4, 65f);
            for (int i = 0; i < 10; i++)
            {
                var p = b.Ring(26f, i * 36f);
                b.Cyl(p, 0.2f, 2.2f, Hex.C("#3A3A48"), false);
                b.Box(p + Vector3.up * 2.2f, new Vector3(0.6f, 0.8f, 0.6f), Hex.C("#FFF2C8"), 0, false, 0.8f);
            }
        }

        static void Walls(B b)
        {
            var stone = Hex.C("#B8B0A0");
            for (int i = -5; i <= 5; i++)
            {
                if (i == 1) continue; // breach
                float ang = 270f + i * 9f;
                var p = b.Ring(50f, ang);
                float h = i == 0 || i == 2 ? 22f : 36f;
                b.Box(p, new Vector3(9f, h, 6f), stone.Darken(b.R(0f, 0.12f)), -ang + 90f);
            }
            for (int i = 0; i < 8; i++)
            {
                var p = b.Ring(b.R(28f, 40f), 60f + i * 25f);
                b.Box(p, new Vector3(b.R(4f, 7f), b.R(2f, 5f), b.R(4f, 6f)), Hex.C("#C8A27A"), b.R(0, 90));
                if (b.R() < 0.6f) b.Box(p + Vector3.up * 0.1f, new Vector3(1.2f, 0.4f, 3f), Hex.C("#6B5A44"), b.R(0, 180), false);
            }
            for (int i = 0; i < 10; i++)
            {
                var p = b.Ring(b.R(26f, 44f), b.R(30f, 230f));
                b.Ball(p, new Vector3(b.R(1.5f, 3.5f), b.R(1f, 2f), b.R(1.5f, 3f)), stone);
            }
        }

        static void Wisteria(B b)
        {
            var bamboo = Hex.C("#7BC25A");
            for (int c = 0; c < 9; c++)
            {
                var centre = b.Ring(b.R(34f, 48f), c * 40f + b.R(-10f, 10f));
                for (int i = 0; i < 9; i++)
                {
                    var p = centre + new Vector3(b.R(-3f, 3f), 0, b.R(-3f, 3f));
                    b.Cyl(p, 0.18f, b.R(9f, 15f), bamboo, i == 0);
                }
            }
            var purple = Hex.C("#B58BE0");
            for (int i = 0; i < 6; i++)
            {
                var p = b.Ring(b.R(27f, 32f), i * 60f + 30f);
                b.Cyl(p, 0.5f, 5f, Hex.C("#6B4A3A"));
                b.Ball(p + Vector3.up * 6f, new Vector3(7f, 3.5f, 7f), purple);
                for (int k = 0; k < 6; k++)
                {
                    var hang = b.Cone(p + Vector3.up * 5.5f + b.Ring(b.R(1.5f, 3f), k * 60f), 0.6f, 2.6f, purple.Brighten(0.2f), 6);
                    hang.transform.rotation = Quaternion.Euler(180, 0, 0);
                }
            }
            // Torii gate.
            var g = b.Ring(26f, 180f);
            float rot = b.FaceCentre(g) + 90f;
            var q = Quaternion.Euler(0, rot, 0);
            var red = Hex.C("#D0302A");
            b.Cyl(g + q * new Vector3(-3.5f, 0, 0), 0.4f, 7f, red);
            b.Cyl(g + q * new Vector3(3.5f, 0, 0), 0.4f, 7f, red);
            b.Box(g + Vector3.up * 7f, new Vector3(10f, 0.6f, 0.8f), Hex.C("#2A1E1E"), rot, false);
            b.Box(g + Vector3.up * 5.8f, new Vector3(8.4f, 0.4f, 0.5f), red, rot, false);
        }

        static void Shibuya(B b)
        {
            for (int i = 0; i < 9; i++)
            {
                var p = b.Ring(b.R(34f, 46f), i * 40f + b.R(-8, 8));
                float h = b.R(14f, 34f), w = b.R(8f, 12f);
                float rot = b.FaceCentre(p);
                var tone = Color.Lerp(Hex.C("#6E6E7A"), Hex.C("#B0AEB8"), b.R());
                b.Box(p, new Vector3(w, h, w), tone, rot);
                var q = Quaternion.Euler(0, rot, 0);
                for (float y = 3f; y < h - 2f; y += 4f)
                    b.Box(p + q * new Vector3(0, y, w * 0.5f + 0.02f), new Vector3(w * 0.85f, 1.4f, 0.1f), Hex.C("#9FD8FF"), rot, false, 0.35f);
            }
            for (int i = 0; i < 6; i++)
            {
                var p = b.Ring(24f, i * 60f);
                b.Cyl(p, 0.15f, 5f, Hex.C("#3A3A44"), false);
                b.Box(p + Vector3.up * 5f, new Vector3(0.5f, 0.3f, 1.2f), Hex.C("#FFF2C8"), b.FaceCentre(p), false, 0.8f);
            }
            for (int i = -3; i <= 3; i++)
                b.Box(new Vector3(i * 2.2f, 0.02f, 14f), new Vector3(1.2f, 0.05f, 6f), Hex.C("#F2F2F2"), 0, false);
            // The curtain ("veil") over the district.
            var veilMat = Mats.GlowInstance(Hex.C("#3A1E5A").WithAlpha(0.18f));
            veilMat.SetFloat("_Core", 0f);
            var veil = MeshFactory.Part("Veil", MeshFactory.Sphere, veilMat, b.root, Vector3.zero, new Vector3(120f, 70f, 120f), null, false);
            veil.transform.position = b.c;
        }

        static void Academy(B b)
        {
            var glass = Hex.C("#8FC8F0");
            var frame = Hex.C("#5A6070");
            var p = b.Ring(40f, 300f);
            float rot = b.FaceCentre(p);
            var q = Quaternion.Euler(0, rot, 0);
            for (int i = -1; i <= 1; i += 2)
            {
                b.Box(p + q * new Vector3(i * 9f, 0, 0), new Vector3(6f, 28f, 10f), glass, rot, true, 0.2f);
                b.Box(p + q * new Vector3(i * 9f, 28f, 0), new Vector3(6.4f, 0.8f, 10.4f), frame, rot, false);
            }
            b.Box(p + Vector3.up * 16f, new Vector3(12f, 5f, 6f), glass, rot, false, 0.2f);
            // Track around the arena.
            b.Glow(Vector3.up * 0.06f, MeshFactory.Ring, Vector3.one * 22f, Hex.C("#D85A3A").WithAlpha(0.9f), Quaternion.identity);
            // Training robots and bleachers.
            for (int i = 0; i < 6; i++)
            {
                var r = b.Ring(30f, 60f + i * 30f);
                b.Box(r, new Vector3(2f, 3f, 1.6f), Hex.C("#8A8F9A"), b.FaceCentre(r));
                b.Ball(r + Vector3.up * 3.6f, Vector3.one * 1.4f, Hex.C("#5A606A"));
                b.Ball(r + Vector3.up * 3.7f + Quaternion.Euler(0, b.FaceCentre(r), 0) * Vector3.forward * 0.6f, Vector3.one * 0.45f, Hex.C("#FF3A2A"), true, 0.8f);
            }
            for (int i = 0; i < 4; i++)
            {
                var r = b.Ring(46f, 120f + i * 30f);
                for (int k = 0; k < 3; k++) b.Box(r + Vector3.up * k * 0.8f + (r.normalized * k * 1.2f), new Vector3(8f, 0.8f, 1.4f), Hex.C("#C8CCD6"), b.FaceCentre(r));
            }
        }

        static void Arena(B b)
        {
            // Heavens Arena: tapered tower made of stacked drums.
            var t = b.Ring(42f, 60f);
            float y = 0f;
            for (int i = 0; i < 8; i++)
            {
                float r = 9f - i * 0.8f, h = 10f;
                b.Cyl(t + Vector3.up * y, r, h, i % 2 == 0 ? Hex.C("#D8D4C8") : Hex.C("#B8B4A8"), i == 0);
                y += h;
            }
            b.Cone(t + Vector3.up * y, 3f, 12f, Hex.C("#C8A23A"), 8);
            // Square fighting stage.
            b.Box(Vector3.zero + Vector3.forward * 0f, new Vector3(18f, 0.2f, 18f), Hex.C("#A8A296"), 0, false);
            for (int i = 0; i < 4; i++) b.Cyl(b.Ring(12.7f, 45f + i * 90f), 0.4f, 3f, Hex.C("#C8A23A"));
            for (int i = 0; i < 10; i++) b.Tree(b.Ring(b.R(30f, 50f), b.R(120f, 360f)), Hex.C("#3E9A4A"), b.R(1.2f, 2f));
        }

        static void Alchemy(B b)
        {
            // Transmutation circle.
            var blue = Hex.C("#4AD1FF").WithAlpha(0.85f);
            b.Glow(Vector3.up * 0.06f, MeshFactory.Ring, Vector3.one * 18f, blue, Quaternion.identity);
            b.Glow(Vector3.up * 0.07f, MeshFactory.Ring, Vector3.one * 12f, blue, Quaternion.identity);
            for (int i = 0; i < 3; i++)
            {
                float ang = i * 120f;
                var mid = b.Ring(6f, ang + 60f);
                b.Glow(mid + Vector3.up * 0.08f, MeshFactory.Cube, new Vector3(0.3f, 0.02f, 20.8f), blue, Quaternion.Euler(0, -(ang + 60f), 0));
            }
            var brick = Hex.C("#B8653A");
            for (int i = 0; i < 7; i++)
            {
                var p = b.Ring(b.R(30f, 42f), i * 51f + b.R(-8, 8));
                b.House(p, b.R(6f, 9f), b.R(6f, 8f), b.R(6f, 9f), brick, Hex.C("#4A3A3A"), b.FaceCentre(p));
            }
            for (int i = 0; i < 10; i++)
            {
                var p = b.Ring(25f, i * 36f + 18f);
                b.Cyl(p, 0.7f, i % 3 == 0 ? 2.5f : 7f, Hex.C("#D8D0BE"));
            }
        }
    }
}
