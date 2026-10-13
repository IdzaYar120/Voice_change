using System;
using UnityEngine;

namespace AnimeSouls
{
    public enum Team { Player, Enemy }

    public enum Rarity { Rare, Epic, Legendary }

    public enum Element { Physical, Wind, Fire, Lightning, Water, Ice, Dark, Light, Earth, Spirit, Blood }

    public enum SkillType
    {
        Projectile,      // single bolt, optional explosion (radius) and pierce
        MultiProjectile, // fan of bolts
        Nova,            // burst around the caster
        GroundSlam,      // delayed impact at the aimed point
        Dash,            // rush forward hitting everything on the way, optional explosion at the end
        Clones,          // copies of the caster that fight for a while
        Summon,          // beasts that fight for a while
        Beam,            // channelled line that ticks damage
        StretchPunch,    // long-reach punch (count > 1 = gatling)
        Slash,           // melee arc (count > 1 = flurry)
        Buff,            // transformation: damage/speed/scale multipliers, optional burst on activation
        Heal,
        Shield,
        Teleport,        // blink to the aimed point, optional burst on arrival
        Rain,            // many strikes falling around the aimed point
        Pull             // vortex dragging enemies in and ticking damage
    }

    public enum HairStyle { Spiky, Short, Long, Ponytail, Messy, Bald, Wild, Bun }

    public enum Accessory
    {
        None, StrawHat, Headband, Mask, Antennae, Horns, Cape, Swords, BigSword, BoarHead, Blindfold,
        Scarf, Armor, Cap, HollowMask, FurHat, CowboyHat, Tail, Gourd, Glasses, Helmet, Brain
    }

    [Serializable]
    public class Appearance
    {
        public Color skin = Hex.C("#F6D2B4");
        public Color hair = Color.black;
        public Color outfit = Color.gray;
        public Color pants = Color.gray;
        public Color eyes = Hex.C("#2A2A35");
        public Color accent = Hex.C("#C0282A");
        public HairStyle hairStyle = HairStyle.Short;
        public Accessory[] accessories = new Accessory[0];
        public float scale = 1f;
        public float headScale = 1f;
        public bool beast;

        public bool Has(Accessory a) => Array.IndexOf(accessories, a) >= 0;

        public Appearance Clone()
        {
            var a = (Appearance)MemberwiseClone();
            a.accessories = (Accessory[])accessories.Clone();
            return a;
        }

        // Fluent helpers so the database stays compact.
        public Appearance Skin(string hex) { skin = Hex.C(hex); return this; }
        public Appearance Eyes(string hex) { eyes = Hex.C(hex); return this; }
        public Appearance Accent(string hex) { accent = Hex.C(hex); return this; }
        public Appearance Scale(float s) { scale = s; return this; }
        public Appearance Head(float s) { headScale = s; return this; }
        public Appearance Beast() { beast = true; return this; }
    }

    public class SkillDef
    {
        public string name;
        public string desc;
        public SkillType type;
        public Element element;
        public Color color = Color.white;

        public float damage;
        public float cooldown = 5f;
        public float cost = -1f;     // < 0 means "derive from cooldown"
        public float range = 12f;
        public float radius = 0f;
        public float size = 0.5f;
        public float speed = 30f;
        public float duration = 0f;
        public float stun = 0f;
        public float knockback = 4f;
        public float angle = 110f;
        public int count = 1;
        public bool pierce;

        public float buffDamage = 1f;
        public float buffSpeed = 1f;
        public float buffScale = 1f;
        public bool hasTint;
        public Color tint;

        public bool ultimate;

        public float EnergyCost => cost >= 0 ? cost : Mathf.Round(8f + cooldown * 2.2f);

        public SkillDef Rng(float v) { range = v; return this; }
        public SkillDef Rad(float v) { radius = v; return this; }
        public SkillDef Size(float v) { size = v; return this; }
        public SkillDef Spd(float v) { speed = v; return this; }
        public SkillDef Dur(float v) { duration = v; return this; }
        public SkillDef Stun(float v) { stun = v; return this; }
        public SkillDef Kb(float v) { knockback = v; return this; }
        public SkillDef Arc(float v) { angle = v; return this; }
        public SkillDef Cnt(int v) { count = v; return this; }
        public SkillDef Pierce() { pierce = true; return this; }
        public SkillDef Cost(float v) { cost = v; return this; }
        public SkillDef Desc(string v) { desc = v; return this; }
        public SkillDef Ult() { ultimate = true; return this; }
        public SkillDef Boost(float dmg, float spd = 1f, float scale = 1f)
        {
            buffDamage = dmg; buffSpeed = spd; buffScale = scale; return this;
        }
        public SkillDef Tint(string hex) { hasTint = true; tint = Hex.C(hex); return this; }
    }

    public class SoulDef
    {
        public string id;
        public string name;
        public string title;
        public int animeIndex;
        public Rarity rarity;
        public Element element;
        public Color signature;
        public Appearance look;
        public SkillDef[] skills;   // Q, E, R (R is the ultimate)
        public bool swordBasic;

        public AnimeDef Anime => AnimeDatabase.Anime[animeIndex];

        public int FragmentsRequired
        {
            get
            {
                switch (rarity)
                {
                    case Rarity.Legendary: return 16;
                    case Rarity.Epic: return 10;
                    default: return 6;
                }
            }
        }

        public float PowerValue
        {
            get
            {
                switch (rarity)
                {
                    case Rarity.Legendary: return 0.20f;
                    case Rarity.Epic: return 0.12f;
                    default: return 0.08f;
                }
            }
        }

        public float HpBonus
        {
            get
            {
                switch (rarity)
                {
                    case Rarity.Legendary: return 110f;
                    case Rarity.Epic: return 65f;
                    default: return 40f;
                }
            }
        }
    }

    public class MobDef
    {
        public string name;
        public Appearance look;
        public float hpMul = 1f;
        public float dmgMul = 1f;
        public float speed = 3.6f;
        public float reach = 1.9f;
    }

    public enum CampTheme { Leaf, Pirate, Namek, SoulSociety, Walls, Wisteria, Shibuya, Academy, Arena, Alchemy }

    public class AnimeDef
    {
        public int index;
        public string name;
        public string campName;
        public CampTheme theme;
        public int tier;
        public Color ground;
        public Color accent;
        public MobDef[] mobs;
        public SoulDef[] souls;
        public Vector3 campPosition; // filled by the world generator
        public float campRadius = 48f;
    }

    public static class Hex
    {
        public static Color C(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.magenta;
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length < 6) return Color.magenta;
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
