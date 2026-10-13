using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Builds chibi-proportioned anime characters out of primitives: big head, big eyes, spiky hair and
    /// signature accessories (straw hat, headband, mask, blindfold, swords...).
    /// </summary>
    public static class CharacterBuilder
    {
        public static CharacterRig Build(Appearance look, Transform parent)
        {
            return look.beast ? BuildBeast(look, parent) : BuildHumanoid(look, parent);
        }

        static GameObject P(string name, Mesh mesh, Material m, Transform parent, Vector3 pos, Vector3 scale, Quaternion? rot, CharacterRig rig)
        {
            var go = MeshFactory.Part(name, mesh, m, parent, pos, scale, rot);
            rig.renderers.Add(go.GetComponent<Renderer>());
            return go;
        }

        static Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        // ------------------------------------------------------------------ humanoid

        static CharacterRig BuildHumanoid(Appearance look, Transform parent)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<CharacterRig>();
            rig.baseScale = look.scale;
            rig.height = 2.15f * look.scale;

            var skin = Mats.Toon(look.skin);
            var outfit = Mats.Toon(look.outfit);
            var pants = Mats.Toon(look.pants);
            var shoes = Mats.Toon(look.pants.Darken(0.55f));
            var hairMat = Mats.ToonInstance(look.hair);
            rig.RegisterHair(hairMat);

            rig.hips = Pivot("Hips", root.transform, Vector3.zero);

            // Legs
            rig.legL = Pivot("LegL", rig.hips, new Vector3(-0.13f, 0.9f, 0));
            rig.legR = Pivot("LegR", rig.hips, new Vector3(0.13f, 0.9f, 0));
            foreach (var leg in new[] { rig.legL, rig.legR })
            {
                P("Leg", MeshFactory.Capsule, pants, leg, new Vector3(0, -0.44f, 0), new Vector3(0.21f, 0.45f, 0.21f), null, rig);
                P("Shoe", MeshFactory.Cube, shoes, leg, new Vector3(0, -0.86f, 0.05f), new Vector3(0.2f, 0.12f, 0.32f), null, rig);
            }

            // Torso
            rig.torso = Pivot("Torso", rig.hips, Vector3.zero);
            P("Body", MeshFactory.Capsule, outfit, rig.torso, new Vector3(0, 1.2f, 0), new Vector3(0.5f, 0.38f, 0.32f), null, rig);
            P("Belt", MeshFactory.Cylinder, Mats.Toon(look.outfit.Darken(0.35f)), rig.torso, new Vector3(0, 0.92f, 0), new Vector3(0.44f, 0.04f, 0.3f), null, rig);
            P("Neck", MeshFactory.Cylinder, skin, rig.torso, new Vector3(0, 1.58f, 0), new Vector3(0.12f, 0.06f, 0.12f), null, rig);

            // Arms
            rig.armL = Pivot("ArmL", rig.torso, new Vector3(-0.31f, 1.47f, 0));
            rig.armR = Pivot("ArmR", rig.torso, new Vector3(0.31f, 1.47f, 0));
            foreach (var arm in new[] { rig.armL, rig.armR })
            {
                P("Arm", MeshFactory.Capsule, outfit, arm, new Vector3(0, -0.32f, 0), new Vector3(0.15f, 0.34f, 0.15f), null, rig);
                var hand = P("Hand", MeshFactory.Sphere, skin, arm, new Vector3(0, -0.68f, 0), Vector3.one * 0.15f, null, rig);
                if (arm == rig.armR) rig.handR = hand.transform;
            }

            // Head
            rig.head = Pivot("Head", rig.torso, new Vector3(0, 1.6f, 0));
            var headScale = Pivot("HeadScale", rig.head, Vector3.zero);
            headScale.localScale = Vector3.one * look.headScale;
            var h = headScale;
            bool helmet = look.Has(Accessory.Helmet);
            bool boar = look.Has(Accessory.BoarHead);
            P("Skull", MeshFactory.Sphere, skin, h, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.52f, 0.5f), null, rig);
            if (!helmet && !boar) BuildFace(look, h, rig);
            if (!helmet && !boar) BuildHair(look, h, hairMat, rig);

            foreach (var acc in look.accessories) BuildAccessory(acc, look, rig, h, hairMat);
            return rig;
        }

        static void BuildFace(Appearance look, Transform h, CharacterRig rig)
        {
            var white = Mats.Toon(Color.white, 0f);
            var iris = Mats.Toon(look.eyes, 0f);
            var dark = Mats.Toon(Hex.C("#1B1520"), 0f);
            for (int s = -1; s <= 1; s += 2)
            {
                P("EyeWhite", MeshFactory.Sphere, white, h, new Vector3(s * 0.1f, 0.24f, 0.215f), new Vector3(0.12f, 0.15f, 0.05f), null, rig);
                P("Iris", MeshFactory.Sphere, iris, h, new Vector3(s * 0.098f, 0.232f, 0.236f), new Vector3(0.085f, 0.115f, 0.03f), null, rig);
                P("Shine", MeshFactory.Sphere, white, h, new Vector3(s * 0.098f + 0.02f, 0.258f, 0.25f), new Vector3(0.03f, 0.035f, 0.01f), null, rig);
                P("Brow", MeshFactory.Cube, dark, h, new Vector3(s * 0.1f, 0.335f, 0.225f), new Vector3(0.11f, 0.018f, 0.02f), Quaternion.Euler(0, 0, s * -8f), rig);
            }
            P("Mouth", MeshFactory.Cube, dark, h, new Vector3(0, 0.1f, 0.235f), new Vector3(0.07f, 0.012f, 0.01f), null, rig);
        }

        static void Spike(Transform h, Material m, Vector3 dir, float length, float width, CharacterRig rig)
        {
            dir.Normalize();
            var basePos = new Vector3(0, 0.3f, -0.02f) + dir * 0.18f;
            P("Spike", MeshFactory.Cone(6), m, h, basePos, new Vector3(width, length, width), Quaternion.FromToRotation(Vector3.up, dir), rig);
        }

        static void BuildHair(Appearance look, Transform h, Material m, CharacterRig rig)
        {
            if (look.hairStyle == HairStyle.Bald) return;

            P("HairCap", MeshFactory.Sphere, m, h, new Vector3(0, 0.33f, -0.04f), new Vector3(0.53f, 0.5f, 0.54f), null, rig);
            var rnd = new System.Random(look.hair.GetHashCode() ^ (int)look.hairStyle * 7919);
            float Rf(float a, float b) => a + (b - a) * (float)rnd.NextDouble();

            // Bangs across the forehead for most styles.
            if (look.hairStyle != HairStyle.Bun)
            {
                for (int i = -2; i <= 2; i++)
                {
                    var dir = new Vector3(i * 0.35f, -0.6f, 1f);
                    var pos = new Vector3(i * 0.07f, 0.44f, 0.17f);
                    P("Bang", MeshFactory.Cone(5), m, h, pos, new Vector3(0.1f, 0.17f, 0.07f), Quaternion.FromToRotation(Vector3.up, dir), rig);
                }
            }

            switch (look.hairStyle)
            {
                case HairStyle.Spiky:
                    for (int i = 0; i < 11; i++)
                    {
                        float az = i * (360f / 11f) + Rf(-10f, 10f);
                        float el = Rf(25f, 70f);
                        var dir = Quaternion.Euler(-el, az, 0) * Vector3.forward;
                        if (dir.z > 0.5f) dir.y += 0.6f; // keep the face clear
                        Spike(h, m, dir, Rf(0.32f, 0.48f), Rf(0.13f, 0.17f), rig);
                    }
                    Spike(h, m, Vector3.up, 0.42f, 0.16f, rig);
                    break;
                case HairStyle.Wild:
                    for (int i = 0; i < 8; i++)
                    {
                        var dir = new Vector3(0.7f + Rf(-0.3f, 0.3f), 0.9f + Rf(-0.2f, 0.3f), -0.3f + Rf(-0.6f, 0.4f));
                        Spike(h, m, dir, Rf(0.34f, 0.5f), Rf(0.14f, 0.18f), rig);
                    }
                    for (int i = 0; i < 4; i++) Spike(h, m, new Vector3(Rf(-0.4f, 0.2f), 1f, Rf(-0.8f, -0.2f)), Rf(0.3f, 0.4f), 0.15f, rig);
                    break;
                case HairStyle.Messy:
                    for (int i = 0; i < 9; i++)
                    {
                        var dir = Quaternion.Euler(-Rf(10f, 60f), Rf(0, 360), 0) * Vector3.forward;
                        if (dir.z > 0.4f) dir.y += 0.8f;
                        Spike(h, m, dir, Rf(0.16f, 0.24f), Rf(0.13f, 0.17f), rig);
                    }
                    break;
                case HairStyle.Short:
                    for (int i = 0; i < 6; i++)
                    {
                        var dir = Quaternion.Euler(-Rf(10f, 40f), 120f + i * 24f, 0) * Vector3.forward;
                        Spike(h, m, dir, 0.14f, 0.14f, rig);
                    }
                    break;
                case HairStyle.Long:
                    P("LongBack", MeshFactory.Cube, m, h, new Vector3(0, 0.02f, -0.2f), new Vector3(0.5f, 0.75f, 0.14f), Quaternion.Euler(-6f, 0, 0), rig);
                    P("SideL", MeshFactory.Cube, m, h, new Vector3(-0.24f, 0.12f, 0.02f), new Vector3(0.07f, 0.45f, 0.24f), null, rig);
                    P("SideR", MeshFactory.Cube, m, h, new Vector3(0.24f, 0.12f, 0.02f), new Vector3(0.07f, 0.45f, 0.24f), null, rig);
                    break;
                case HairStyle.Ponytail:
                    P("Tie", MeshFactory.Sphere, m, h, new Vector3(0, 0.3f, -0.28f), Vector3.one * 0.16f, null, rig);
                    P("Tail", MeshFactory.Capsule, m, h, new Vector3(0, 0.0f, -0.32f), new Vector3(0.13f, 0.28f, 0.13f), Quaternion.Euler(-12f, 0, 0), rig);
                    break;
                case HairStyle.Bun:
                    P("Bun", MeshFactory.Sphere, m, h, new Vector3(0, 0.6f, -0.08f), Vector3.one * 0.2f, null, rig);
                    break;
            }
        }

        static void BuildAccessory(Accessory acc, Appearance look, CharacterRig rig, Transform h, Material hairMat)
        {
            var t = rig.torso;
            switch (acc)
            {
                case Accessory.StrawHat:
                {
                    var straw = Mats.Toon(Hex.C("#EBCB6E"));
                    P("Brim", MeshFactory.Cylinder, straw, h, new Vector3(0, 0.47f, 0), new Vector3(0.86f, 0.012f, 0.86f), null, rig);
                    P("Crown", MeshFactory.Cylinder, straw, h, new Vector3(0, 0.55f, 0), new Vector3(0.46f, 0.08f, 0.46f), null, rig);
                    P("Band", MeshFactory.Cylinder, Mats.Toon(Hex.C("#D7262E")), h, new Vector3(0, 0.5f, 0), new Vector3(0.47f, 0.03f, 0.47f), null, rig);
                    break;
                }
                case Accessory.Headband:
                    P("Band", MeshFactory.Cylinder, Mats.Toon(Hex.C("#22305A")), h, new Vector3(0, 0.37f, 0), new Vector3(0.54f, 0.035f, 0.54f), null, rig);
                    P("Plate", MeshFactory.Cube, Mats.Toon(Hex.C("#C8CCD6"), 0.002f), h, new Vector3(0, 0.37f, 0.265f), new Vector3(0.17f, 0.075f, 0.02f), null, rig);
                    break;
                case Accessory.Mask:
                    P("Mask", MeshFactory.Sphere, Mats.Toon(look.outfit.Darken(0.3f)), h, new Vector3(0, 0.11f, 0.005f), new Vector3(0.53f, 0.27f, 0.53f), null, rig);
                    break;
                case Accessory.Antennae:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var dir = new Vector3(s * 0.4f, 1f, 0.5f).normalized;
                        P("Antenna", MeshFactory.Cylinder, Mats.Toon(look.skin), h, new Vector3(s * 0.08f, 0.5f, 0.1f) + dir * 0.12f, new Vector3(0.03f, 0.13f, 0.03f), Quaternion.FromToRotation(Vector3.up, dir), rig);
                        P("Tip", MeshFactory.Sphere, Mats.Toon(look.skin), h, new Vector3(s * 0.08f, 0.5f, 0.1f) + dir * 0.26f, Vector3.one * 0.06f, null, rig);
                    }
                    break;
                case Accessory.Horns:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var dir = new Vector3(s * 0.6f, 1f, 0.1f);
                        P("Horn", MeshFactory.Cone(6), Mats.Toon(Hex.C("#EDE3D0")), h, new Vector3(s * 0.15f, 0.43f, 0.02f), new Vector3(0.09f, 0.26f, 0.09f), Quaternion.FromToRotation(Vector3.up, dir), rig);
                    }
                    break;
                case Accessory.Cape:
                    P("Cape", MeshFactory.Cube, Mats.Toon(look.accent), t, new Vector3(0, 1.0f, -0.22f), new Vector3(0.62f, 1.05f, 0.04f), Quaternion.Euler(8f, 0, 0), rig);
                    P("Collar", MeshFactory.Cylinder, Mats.Toon(look.accent), t, new Vector3(0, 1.52f, -0.02f), new Vector3(0.42f, 0.05f, 0.36f), null, rig);
                    break;
                case Accessory.Swords:
                {
                    var blade = Mats.Toon(Hex.C("#DDE3EC"), 0.002f);
                    var hilt = Mats.Toon(Hex.C("#2A1E2E"), 0.002f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var rot = Quaternion.Euler(0, 0, s * 35f);
                        P("Sheath", MeshFactory.Cube, hilt, t, new Vector3(0, 1.15f, -0.22f), new Vector3(0.06f, 0.95f, 0.05f), rot, rig);
                        P("Guard", MeshFactory.Cylinder, blade, t, new Vector3(0, 1.15f, -0.22f) + rot * new Vector3(0, 0.5f, 0), new Vector3(0.12f, 0.01f, 0.12f), rot, rig);
                        P("Hilt", MeshFactory.Cube, hilt, t, new Vector3(0, 1.15f, -0.22f) + rot * new Vector3(0, 0.62f, 0), new Vector3(0.05f, 0.22f, 0.05f), rot, rig);
                    }
                    break;
                }
                case Accessory.BigSword:
                {
                    var rot = Quaternion.Euler(0, 0, 30f);
                    P("BigBlade", MeshFactory.Cube, Mats.Toon(Hex.C("#DDE3EC")), t, new Vector3(0, 1.15f, -0.24f), new Vector3(0.24f, 1.5f, 0.04f), rot, rig);
                    P("BigHilt", MeshFactory.Cube, Mats.Toon(Hex.C("#F2F2F2")), t, new Vector3(0, 1.15f, -0.24f) + rot * new Vector3(0, 0.95f, 0), new Vector3(0.07f, 0.4f, 0.07f), rot, rig);
                    break;
                }
                case Accessory.BoarHead:
                {
                    var fur = Mats.Toon(Hex.C("#8A96A8"));
                    P("Boar", MeshFactory.Sphere, fur, h, new Vector3(0, 0.27f, 0), new Vector3(0.62f, 0.58f, 0.68f), null, rig);
                    P("Snout", MeshFactory.Cylinder, Mats.Toon(Hex.C("#E8B8B8")), h, new Vector3(0, 0.2f, 0.33f), new Vector3(0.2f, 0.06f, 0.16f), Quaternion.Euler(90, 0, 0), rig);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        P("Ear", MeshFactory.Cone(4), fur, h, new Vector3(s * 0.2f, 0.5f, -0.02f), new Vector3(0.12f, 0.18f, 0.08f), Quaternion.Euler(0, 0, -s * 25f), rig);
                        P("Eye", MeshFactory.Sphere, Mats.Toon(Hex.C("#2A9AD8"), 0f), h, new Vector3(s * 0.12f, 0.33f, 0.29f), Vector3.one * 0.07f, null, rig);
                        P("Tusk", MeshFactory.Cone(5), Mats.Toon(Color.white), h, new Vector3(s * 0.1f, 0.1f, 0.31f), new Vector3(0.04f, 0.12f, 0.04f), Quaternion.Euler(-20, 0, s * -10f), rig);
                    }
                    break;
                }
                case Accessory.Blindfold:
                    P("Blindfold", MeshFactory.Cylinder, Mats.Toon(Hex.C("#151520")), h, new Vector3(0, 0.25f, 0.005f), new Vector3(0.535f, 0.06f, 0.535f), null, rig);
                    break;
                case Accessory.Scarf:
                    P("Scarf", MeshFactory.Cylinder, Mats.Toon(Hex.C("#C0282A")), t, new Vector3(0, 1.55f, 0), new Vector3(0.38f, 0.07f, 0.34f), null, rig);
                    P("ScarfEnd", MeshFactory.Cube, Mats.Toon(Hex.C("#C0282A")), t, new Vector3(0.1f, 1.35f, -0.18f), new Vector3(0.12f, 0.4f, 0.03f), Quaternion.Euler(10, 0, 10), rig);
                    break;
                case Accessory.Armor:
                {
                    var plate = Mats.Toon(look.outfit.Brighten(0.65f));
                    P("Chest", MeshFactory.Cube, plate, t, new Vector3(0, 1.3f, 0), new Vector3(0.54f, 0.44f, 0.38f), null, rig);
                    for (int s = -1; s <= 1; s += 2)
                        P("Pad", MeshFactory.Sphere, Mats.Toon(Hex.C("#E8D27A")), t, new Vector3(s * 0.33f, 1.5f, 0), new Vector3(0.26f, 0.12f, 0.26f), null, rig);
                    break;
                }
                case Accessory.Cap:
                    P("Cap", MeshFactory.Cylinder, Mats.Toon(Color.white), h, new Vector3(0, 0.49f, -0.01f), new Vector3(0.5f, 0.07f, 0.5f), null, rig);
                    P("Visor", MeshFactory.Cube, Mats.Toon(Hex.C("#22305A")), h, new Vector3(0, 0.45f, 0.26f), new Vector3(0.3f, 0.02f, 0.16f), null, rig);
                    break;
                case Accessory.HollowMask:
                {
                    var bone = Mats.Toon(Hex.C("#F4F1EA"));
                    var black = Mats.Toon(Color.black, 0f);
                    P("Mask", MeshFactory.Sphere, bone, h, new Vector3(0, 0.25f, 0.13f), new Vector3(0.46f, 0.5f, 0.28f), null, rig);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        P("Hole", MeshFactory.Sphere, black, h, new Vector3(s * 0.09f, 0.28f, 0.26f), new Vector3(0.1f, 0.06f, 0.03f), Quaternion.Euler(0, 0, s * 15f), rig);
                        P("Horn", MeshFactory.Cone(6), bone, h, new Vector3(s * 0.17f, 0.42f, 0.06f), new Vector3(0.08f, 0.22f, 0.08f), Quaternion.FromToRotation(Vector3.up, new Vector3(s, 1.2f, 0.4f)), rig);
                    }
                    P("Teeth", MeshFactory.Cube, black, h, new Vector3(0, 0.1f, 0.255f), new Vector3(0.18f, 0.015f, 0.01f), null, rig);
                    P("EyeGlow", MeshFactory.Sphere, Mats.Toon(look.eyes, 0f, 0.9f), h, new Vector3(0.09f, 0.28f, 0.27f), Vector3.one * 0.03f, null, rig);
                    break;
                }
                case Accessory.FurHat:
                {
                    var fur = Mats.Toon(Color.white);
                    P("FurHat", MeshFactory.Cylinder, fur, h, new Vector3(0, 0.5f, 0), new Vector3(0.56f, 0.12f, 0.56f), null, rig);
                    P("FurBrim", MeshFactory.Sphere, fur, h, new Vector3(0, 0.42f, 0), new Vector3(0.6f, 0.08f, 0.6f), null, rig);
                    var spot = Mats.Toon(Hex.C("#6B4A2A"), 0f);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90f * Mathf.Deg2Rad;
                        P("Spot", MeshFactory.Sphere, spot, h, new Vector3(Mathf.Cos(a) * 0.27f, 0.53f, Mathf.Sin(a) * 0.27f), new Vector3(0.07f, 0.06f, 0.07f), null, rig);
                    }
                    break;
                }
                case Accessory.CowboyHat:
                {
                    var orange = Mats.Toon(look.accent);
                    P("Brim", MeshFactory.Cylinder, orange, h, new Vector3(0, 0.47f, 0), new Vector3(0.9f, 0.012f, 0.9f), null, rig);
                    P("Crown", MeshFactory.Cylinder, orange, h, new Vector3(0, 0.56f, 0), new Vector3(0.44f, 0.1f, 0.44f), null, rig);
                    P("Band", MeshFactory.Cylinder, Mats.Toon(Hex.C("#2E4FA0")), h, new Vector3(0, 0.5f, 0), new Vector3(0.45f, 0.025f, 0.45f), null, rig);
                    break;
                }
                case Accessory.Tail:
                {
                    var tailMat = look.hairStyle == HairStyle.Bald ? Mats.Toon(look.skin) : Mats.Toon(Hex.C("#6B4A2A"));
                    P("Tail1", MeshFactory.Capsule, tailMat, t, new Vector3(0, 0.82f, -0.3f), new Vector3(0.09f, 0.22f, 0.09f), Quaternion.Euler(-60f, 0, 0), rig);
                    P("Tail2", MeshFactory.Capsule, tailMat, t, new Vector3(0.1f, 0.68f, -0.55f), new Vector3(0.08f, 0.2f, 0.08f), Quaternion.Euler(-110f, 30f, 0), rig);
                    break;
                }
                case Accessory.Gourd:
                {
                    var tan = Mats.Toon(Hex.C("#C9A06A"));
                    P("GourdLow", MeshFactory.Sphere, tan, t, new Vector3(0, 1.15f, -0.32f), Vector3.one * 0.42f, null, rig);
                    P("GourdHigh", MeshFactory.Sphere, tan, t, new Vector3(0, 1.5f, -0.32f), Vector3.one * 0.3f, null, rig);
                    P("Strap", MeshFactory.Cube, Mats.Toon(Hex.C("#6B4A33")), t, new Vector3(0, 1.25f, 0.02f), new Vector3(0.06f, 0.7f, 0.36f), Quaternion.Euler(0, 0, 35f), rig);
                    break;
                }
                case Accessory.Glasses:
                    P("Glasses", MeshFactory.Cube, Mats.Toon(Hex.C("#151520"), 0f), h, new Vector3(0, 0.26f, 0.25f), new Vector3(0.34f, 0.07f, 0.02f), null, rig);
                    break;
                case Accessory.Helmet:
                {
                    var steel = Mats.Toon(look.outfit);
                    P("Helmet", MeshFactory.Cube, steel, h, new Vector3(0, 0.27f, 0), new Vector3(0.56f, 0.6f, 0.56f), null, rig);
                    P("Spike", MeshFactory.Cone(6), steel, h, new Vector3(0, 0.57f, 0), new Vector3(0.12f, 0.3f, 0.12f), null, rig);
                    P("Plume", MeshFactory.Capsule, Mats.Toon(Hex.C("#F2F2F2")), h, new Vector3(0, 0.62f, -0.25f), new Vector3(0.08f, 0.25f, 0.08f), Quaternion.Euler(-40, 0, 0), rig);
                    P("Slit", MeshFactory.Cube, Mats.Toon(Hex.C("#151520"), 0f), h, new Vector3(0, 0.3f, 0.285f), new Vector3(0.4f, 0.07f, 0.01f), null, rig);
                    for (int s = -1; s <= 1; s += 2)
                        P("EyeGlow", MeshFactory.Sphere, Mats.Toon(look.eyes, 0f, 1f), h, new Vector3(s * 0.1f, 0.3f, 0.29f), Vector3.one * 0.05f, null, rig);
                    break;
                }
                case Accessory.Brain:
                    P("Brain", MeshFactory.LowSphere, Mats.Toon(Hex.C("#F2A0B0")), h, new Vector3(0, 0.44f, -0.03f), new Vector3(0.48f, 0.34f, 0.52f), null, rig);
                    break;
            }
        }

        // ------------------------------------------------------------------ beast

        static CharacterRig BuildBeast(Appearance look, Transform parent)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<CharacterRig>();
            rig.beast = true;
            rig.baseScale = look.scale;
            rig.height = 1.3f * look.scale;

            var body = Mats.Toon(look.outfit);
            var skin = Mats.Toon(look.skin);
            var dark = Mats.Toon(look.outfit.Darken(0.4f));
            rig.hips = Pivot("Hips", root.transform, Vector3.zero);
            P("Body", MeshFactory.Capsule, body, rig.hips, new Vector3(0, 0.75f, 0), new Vector3(0.6f, 0.65f, 0.6f), Quaternion.Euler(90, 0, 0), rig);
            P("Mane", MeshFactory.LowSphere, Mats.Toon(look.hair), rig.hips, new Vector3(0, 0.95f, 0.35f), new Vector3(0.75f, 0.7f, 0.6f), null, rig);

            rig.legL = Pivot("LegFL", rig.hips, new Vector3(-0.2f, 0.6f, 0.4f));
            rig.legR = Pivot("LegFR", rig.hips, new Vector3(0.2f, 0.6f, 0.4f));
            rig.legBL = Pivot("LegBL", rig.hips, new Vector3(-0.2f, 0.6f, -0.4f));
            rig.legBR = Pivot("LegBR", rig.hips, new Vector3(0.2f, 0.6f, -0.4f));
            foreach (var leg in new[] { rig.legL, rig.legR, rig.legBL, rig.legBR })
            {
                P("Leg", MeshFactory.Capsule, body, leg, new Vector3(0, -0.3f, 0), new Vector3(0.16f, 0.32f, 0.16f), null, rig);
                P("Paw", MeshFactory.Sphere, dark, leg, new Vector3(0, -0.58f, 0.04f), new Vector3(0.18f, 0.1f, 0.22f), null, rig);
            }

            rig.head = Pivot("Head", rig.hips, new Vector3(0, 1.05f, 0.65f));
            P("Skull", MeshFactory.Sphere, body, rig.head, new Vector3(0, 0.05f, 0.1f), new Vector3(0.45f, 0.42f, 0.5f), null, rig);
            P("Snout", MeshFactory.Sphere, skin, rig.head, new Vector3(0, -0.04f, 0.35f), new Vector3(0.24f, 0.2f, 0.28f), null, rig);
            P("Nose", MeshFactory.Sphere, Mats.Toon(Color.black, 0f), rig.head, new Vector3(0, 0.0f, 0.49f), Vector3.one * 0.07f, null, rig);
            for (int s = -1; s <= 1; s += 2)
            {
                P("Ear", MeshFactory.Cone(4), dark, rig.head, new Vector3(s * 0.14f, 0.24f, 0.02f), new Vector3(0.12f, 0.2f, 0.08f), Quaternion.Euler(-10, 0, -s * 20f), rig);
                P("Eye", MeshFactory.Sphere, Mats.Toon(look.eyes, 0f, 0.6f), rig.head, new Vector3(s * 0.11f, 0.1f, 0.3f), Vector3.one * 0.08f, null, rig);
            }
            P("Tail", MeshFactory.Cone(5), body, rig.hips, new Vector3(0, 0.85f, -0.6f), new Vector3(0.14f, 0.6f, 0.14f), Quaternion.Euler(-120, 0, 0), rig);
            return rig;
        }
    }
}
