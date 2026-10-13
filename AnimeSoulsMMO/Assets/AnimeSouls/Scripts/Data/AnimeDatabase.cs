using System.Collections.Generic;
using UnityEngine;
using static AnimeSouls.Accessory;
using E = AnimeSouls.Element;
using H = AnimeSouls.HairStyle;
using R = AnimeSouls.Rarity;

namespace AnimeSouls
{
    /// <summary>
    /// All content of the game: 10 anime, 5 souls each, 3 skills per soul, themed mobs per camp.
    /// Damage numbers: Beam/Pull = per tick (4 ticks/s), Rain/Multi/Clones = per hit, Shield = absorb, Heal = HP.
    /// </summary>
    public static class AnimeDatabase
    {
        public static readonly List<AnimeDef> Anime = new List<AnimeDef>();
        public static readonly Dictionary<string, SoulDef> SoulsById = new Dictionary<string, SoulDef>();
        public static readonly List<SoulDef> AllSouls = new List<SoulDef>();

        static AnimeDatabase()
        {
            BuildNaruto();
            BuildOnePiece();
            BuildDragonBall();
            BuildBleach();
            BuildAttackOnTitan();
            BuildDemonSlayer();
            BuildJujutsuKaisen();
            BuildMyHeroAcademia();
            BuildHunterXHunter();
            BuildFullmetal();

            foreach (var a in Anime)
            {
                foreach (var s in a.souls)
                {
                    s.animeIndex = a.index;
                    s.id = MakeId(a.name, s.name);
                    s.swordBasic = s.look.Has(Swords) || s.look.Has(BigSword);
                    foreach (var k in s.skills)
                    {
                        if (string.IsNullOrEmpty(k.desc)) k.desc = Describe(k);
                    }
                    SoulsById[s.id] = s;
                    AllSouls.Add(s);
                }
            }
        }

        public static SoulDef Get(string id)
        {
            return id != null && SoulsById.TryGetValue(id, out var s) ? s : null;
        }

        static string MakeId(string anime, string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in (anime + "_" + name).ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }

        // ------------------------------------------------------------------ helpers

        static Appearance L(string hair, H style, string outfit, string pants, params Accessory[] acc)
        {
            return new Appearance
            {
                hair = Hex.C(hair),
                hairStyle = style,
                outfit = Hex.C(outfit),
                pants = Hex.C(pants),
                accessories = acc
            };
        }

        static SkillDef K(SkillType t, string name, float dmg, float cd, string color, E el)
        {
            var s = new SkillDef { type = t, name = name, damage = dmg, cooldown = cd, color = Hex.C(color), element = el };
            switch (t)
            {
                case SkillType.Projectile: s.range = 30; break;
                case SkillType.MultiProjectile: s.range = 26; s.count = 5; s.size = 0.35f; break;
                case SkillType.Nova: s.radius = 5; break;
                case SkillType.GroundSlam: s.range = 12; s.radius = 4; break;
                case SkillType.Dash: s.range = 9; break;
                case SkillType.Clones: s.count = 2; s.duration = 10; break;
                case SkillType.Summon: s.count = 2; s.duration = 12; break;
                case SkillType.Beam: s.range = 22; s.radius = 1.2f; s.duration = 1.2f; break;
                case SkillType.StretchPunch: s.range = 10; break;
                case SkillType.Slash: s.range = 4.5f; break;
                case SkillType.Buff: s.duration = 12; s.radius = 6; break;
                case SkillType.Shield: s.duration = 6; break;
                case SkillType.Teleport: s.range = 14; s.radius = 3; break;
                case SkillType.Rain: s.range = 18; s.radius = 6; s.count = 6; break;
                case SkillType.Pull: s.range = 14; s.radius = 5; s.duration = 2; break;
            }
            return s;
        }

        static SkillDef Proj(string n, float d, float cd, string c, E e) => K(SkillType.Projectile, n, d, cd, c, e);
        static SkillDef Multi(string n, float d, float cd, string c, E e) => K(SkillType.MultiProjectile, n, d, cd, c, e);
        static SkillDef Nova(string n, float d, float cd, string c, E e) => K(SkillType.Nova, n, d, cd, c, e);
        static SkillDef Slam(string n, float d, float cd, string c, E e) => K(SkillType.GroundSlam, n, d, cd, c, e);
        static SkillDef Dash(string n, float d, float cd, string c, E e) => K(SkillType.Dash, n, d, cd, c, e);
        static SkillDef Clones(string n, float d, float cd, string c, E e) => K(SkillType.Clones, n, d, cd, c, e);
        static SkillDef Summon(string n, float d, float cd, string c, E e) => K(SkillType.Summon, n, d, cd, c, e);
        static SkillDef Beam(string n, float d, float cd, string c, E e) => K(SkillType.Beam, n, d, cd, c, e);
        static SkillDef Punch(string n, float d, float cd, string c, E e) => K(SkillType.StretchPunch, n, d, cd, c, e);
        static SkillDef Slash(string n, float d, float cd, string c, E e) => K(SkillType.Slash, n, d, cd, c, e);
        static SkillDef Buff(string n, float d, float cd, string c, E e) => K(SkillType.Buff, n, d, cd, c, e);
        static SkillDef Heal(string n, float d, float cd, string c, E e) => K(SkillType.Heal, n, d, cd, c, e);
        static SkillDef Shield(string n, float d, float cd, string c, E e) => K(SkillType.Shield, n, d, cd, c, e);
        static SkillDef Tele(string n, float d, float cd, string c, E e) => K(SkillType.Teleport, n, d, cd, c, e);
        static SkillDef Rain(string n, float d, float cd, string c, E e) => K(SkillType.Rain, n, d, cd, c, e);
        static SkillDef Pull(string n, float d, float cd, string c, E e) => K(SkillType.Pull, n, d, cd, c, e);

        static SoulDef Soul(string name, string title, R rarity, E el, string sig, Appearance look, SkillDef q, SkillDef e, SkillDef r)
        {
            r.Ult();
            return new SoulDef
            {
                name = name, title = title, rarity = rarity, element = el, signature = Hex.C(sig), look = look,
                skills = new[] { q, e, r }
            };
        }

        static MobDef Mob(string name, Appearance look, float hp = 1f, float dmg = 1f, float speed = 3.6f, float reach = 1.9f)
        {
            return new MobDef { name = name, look = look, hpMul = hp, dmgMul = dmg, speed = speed, reach = reach };
        }

        static void Add(string name, string camp, CampTheme theme, int tier, string ground, string accent, MobDef[] mobs, params SoulDef[] souls)
        {
            Anime.Add(new AnimeDef
            {
                index = Anime.Count, name = name, campName = camp, theme = theme, tier = tier,
                ground = Hex.C(ground), accent = Hex.C(accent), mobs = mobs, souls = souls
            });
        }

        // ------------------------------------------------------------------ content

        static void BuildNaruto()
        {
            Add("Naruto", "Околиці Прихованого Листя", CampTheme.Leaf, 1, "#7FBF5A", "#FF7A1A",
                new[]
                {
                    Mob("Rogue Ninja", L("#2A2A33", H.Short, "#2B2B35", "#2B2B35", Mask, Headband).Eyes("#B02020")),
                    Mob("Sound Ninja", L("#6B6B7A", H.Messy, "#6B5A7A", "#3A3346", Headband), 1.1f, 0.9f)
                },
                Soul("Naruto Uzumaki", "Сьомий Хокаге", R.Epic, E.Wind, "#FF8A1E",
                    L("#FFD21F", H.Spiky, "#FF7A1A", "#FF7A1A", Headband).Eyes("#2E7BFF"),
                    Dash("Rasengan", 60, 4, "#6EC8FF", E.Wind).Rng(7).Rad(3),
                    Clones("Shadow Clone Jutsu", 12, 14, "#FFD21F", E.Wind).Cnt(3).Dur(10),
                    Proj("Rasenshuriken", 180, 22, "#BDEBFF", E.Wind).Size(1.4f).Spd(22).Rad(7).Stun(1f)),
                Soul("Sasuke Uchiha", "Останній Учіха", R.Epic, E.Lightning, "#5A6BFF",
                    L("#1B1D33", H.Spiky, "#3A3F6B", "#DCD6C8", Headband, Swords).Eyes("#D81E1E"),
                    Dash("Chidori", 70, 5, "#9FD8FF", E.Lightning).Rng(9).Rad(1.5f),
                    Proj("Great Fireball", 55, 6, "#FF6A1A", E.Fire).Size(1.2f).Spd(20).Rad(4),
                    Slam("Kirin", 220, 25, "#BFE6FF", E.Lightning).Rng(18).Rad(9).Stun(1.5f)),
                Soul("Kakashi Hatake", "Ніндзя-Копіювальник", R.Epic, E.Lightning, "#9FD8FF",
                    L("#D9DDE3", H.Wild, "#3E5B3A", "#2C3A55", Mask, Headband).Eyes("#222222"),
                    Dash("Lightning Blade", 65, 5, "#9FD8FF", E.Lightning).Rng(8).Rad(1.5f),
                    Summon("Summon Ninken", 10, 15, "#8A7A5A", E.Physical).Cnt(3).Dur(10),
                    Pull("Kamui", 30, 22, "#6B4FA0", E.Dark).Rad(6).Dur(2.5f)),
                Soul("Itachi Uchiha", "Геній Клану", R.Legendary, E.Fire, "#B5222A",
                    L("#121219", H.Ponytail, "#1E1E28", "#1E1E28", Headband, Cape).Eyes("#D81E1E").Accent("#B5222A"),
                    Multi("Phoenix Flower", 22, 5, "#FF7A2A", E.Fire).Rad(2),
                    Tele("Crow Clone", 30, 7, "#301830", E.Dark).Rad(3),
                    Rain("Amaterasu", 45, 24, "#2A0F2F", E.Fire).Rad(7).Cnt(6)),
                Soul("Gaara", "Казекаге", R.Epic, E.Earth, "#D9B77A",
                    L("#B22222", H.Short, "#8A3A3A", "#6B4A33", Gourd).Eyes("#7FD0C8"),
                    Slam("Sand Coffin", 60, 6, "#D9B77A", E.Earth).Rng(14).Rad(3.5f).Stun(1.2f),
                    Shield("Sand Shield", 150, 14, "#D9B77A", E.Earth).Dur(6),
                    Beam("Sand Tsunami", 40, 20, "#C9A26A", E.Earth).Rad(4).Dur(1.5f))
            );
        }

        static void BuildOnePiece()
        {
            Add("One Piece", "Піратська Бухта", CampTheme.Pirate, 2, "#E8D49A", "#D7262E",
                new[]
                {
                    Mob("Marine Soldier", L("#3A2A1E", H.Short, "#F2F2F2", "#2E4FA0", Cap)),
                    Mob("Pirate Brute", L("#6B3A1E", H.Wild, "#6B3A2A", "#3A2A1E").Scale(1.3f), 1.4f, 1.2f, 3.0f, 2.2f)
                },
                Soul("Monkey D. Luffy", "Солом'яний Капелюх", R.Epic, E.Physical, "#D7262E",
                    L("#111111", H.Messy, "#D7262E", "#3A6EA5", StrawHat),
                    Punch("Gum-Gum Pistol", 55, 3, "#F2C9A0", E.Physical).Rng(12),
                    Punch("Gum-Gum Gatling", 9, 8, "#F2C9A0", E.Physical).Rng(9).Cnt(12),
                    Buff("Gear Fifth", 80, 30, "#FFFFFF", E.Light).Boost(1.8f, 1.4f, 1.15f).Dur(12).Tint("#FFFFFF").Rad(6)),
                Soul("Roronoa Zoro", "Мисливець на Піратів", R.Epic, E.Physical, "#3DBA4A",
                    L("#3DBA4A", H.Short, "#F2F2F2", "#1B1B1B", Swords),
                    Dash("Oni Giri", 60, 5, "#9EF5A0", E.Physical).Rng(9).Rad(1.5f),
                    Nova("Tatsumaki", 55, 7, "#9EF5A0", E.Wind).Rad(5).Kb(8),
                    Slash("Asura: Ichibugin", 90, 20, "#6BE07A", E.Physical).Rng(6).Arc(160).Cnt(3)),
                Soul("Sanji", "Чорна Нога", R.Rare, E.Fire, "#FFD95A",
                    L("#FFD95A", H.Messy, "#1C1C2A", "#1C1C2A").Eyes("#2E7BFF"),
                    Buff("Diable Jambe", 0, 16, "#FF7A1A", E.Fire).Boost(1.4f).Dur(8),
                    Slam("Concasse", 65, 6, "#FF7A1A", E.Fire).Rng(8).Rad(3.5f),
                    Nova("Hell Memories", 160, 22, "#FF5A1A", E.Fire).Rad(8)),
                Soul("Portgas D. Ace", "Вогняний Кулак", R.Epic, E.Fire, "#FF6A1A",
                    L("#1A1A1A", H.Messy, "#F2C9A0", "#2E2A24", CowboyHat).Accent("#E87A2A"),
                    Proj("Fire Fist", 70, 5, "#FF6A1A", E.Fire).Size(1f).Spd(28).Rad(3),
                    Multi("Fire Fly Bullets", 14, 7, "#FFB43C", E.Fire).Cnt(8),
                    Proj("Flame Emperor", 230, 25, "#FF4A1A", E.Fire).Size(3f).Spd(14).Rad(9)),
                Soul("Trafalgar Law", "Хірург Смерті", R.Legendary, E.Spirit, "#6FC3FF",
                    L("#15151F", H.Short, "#F0E35A", "#6B5A44", FurHat, Swords),
                    Tele("Room: Shambles", 40, 7, "#6FC3FF", E.Spirit).Rng(16).Rad(3),
                    Dash("Counter Shock", 75, 8, "#BFE6FF", E.Lightning).Rng(6).Stun(1f),
                    Beam("Gamma Knife", 60, 22, "#7CF9FF", E.Spirit).Rng(14).Rad(1.2f).Dur(1f))
            );
        }

        static void BuildDragonBall()
        {
            Add("Dragon Ball", "Плато Намек", CampTheme.Namek, 5, "#7FD6B0", "#FFB43C",
                new[]
                {
                    Mob("Saibaman", L("#5FB33A", H.Bald, "#4E9A2E", "#3E7A24").Skin("#6CC24A").Eyes("#C02020").Head(1.3f).Scale(0.8f), 0.8f, 1.1f, 4.6f),
                    Mob("Frieza Soldier", L("#5A3A2A", H.Short, "#3A3F6B", "#2A2E4E", Armor).Skin("#C9A27A"), 1.2f, 1f)
                },
                Soul("Son Goku", "Саян, вихований на Землі", R.Legendary, E.Light, "#FF7F1E",
                    L("#111111", H.Spiky, "#FF7F1E", "#FF7F1E", Tail).Accent("#2E4FA0"),
                    Beam("Kamehameha", 35, 6, "#6FD6FF", E.Light).Rng(24).Rad(1.4f).Dur(1.2f),
                    Tele("Instant Transmission", 50, 8, "#FFE45C", E.Light).Rng(20).Rad(3),
                    Buff("Super Saiyan", 60, 30, "#FFE04A", E.Light).Boost(2f, 1.3f).Dur(15).Tint("#FFE04A").Rad(5)),
                Soul("Vegeta", "Принц Саянів", R.Epic, E.Light, "#2A3F9A",
                    L("#111111", H.Spiky, "#2A3F9A", "#2A3F9A", Armor),
                    Proj("Big Bang Attack", 75, 5, "#9CDBFF", E.Light).Size(1f).Rad(4),
                    Beam("Galick Gun", 30, 8, "#B66BFF", E.Light).Rng(20).Dur(1f),
                    Beam("Final Flash", 55, 26, "#FFF27A", E.Light).Rng(32).Rad(2.5f).Dur(1.5f)),
                Soul("Son Gohan", "Ультимативний Воїн", R.Epic, E.Light, "#6B3FA0",
                    L("#111111", H.Spiky, "#6B3FA0", "#6B3FA0").Accent("#C0282A"),
                    Proj("Masenko", 60, 5, "#FFE45C", E.Light).Rad(3),
                    Dash("Spirit Rush", 55, 6, "#FFFFFF", E.Physical).Rng(10),
                    Beam("Father-Son Kamehameha", 60, 24, "#6FD6FF", E.Light).Rng(30).Rad(2.4f).Dur(1.6f)),
                Soul("Piccolo", "Воїн Намеку", R.Rare, E.Spirit, "#6CC24A",
                    L("#6CC24A", H.Bald, "#5A2E86", "#5A2E86", Antennae, Cape).Skin("#6CC24A").Accent("#F2F2F2"),
                    Beam("Special Beam Cannon", 80, 8, "#FFE87A", E.Light).Rng(26).Rad(0.6f).Dur(0.6f),
                    Rain("Hellzone Grenade", 30, 9, "#FFE45C", E.Light).Cnt(8).Rad(6),
                    Clones("Multi-Form", 18, 22, "#6CC24A", E.Spirit).Cnt(3).Dur(12)),
                Soul("Frieza", "Імператор Всесвіту", R.Legendary, E.Dark, "#C64BFF",
                    L("#8A3FB8", H.Bald, "#F2EFF5", "#F2EFF5", Horns, Tail).Skin("#F2EFF5").Eyes("#D81E1E").Accent("#8A3FB8"),
                    Proj("Death Beam", 70, 4, "#C64BFF", E.Dark).Spd(60).Size(0.25f).Pierce(),
                    Multi("Death Saucer", 45, 7, "#FF5CCB", E.Dark).Cnt(2).Size(0.9f).Pierce(),
                    Proj("Supernova", 260, 28, "#FF8A3C", E.Fire).Size(4f).Spd(10).Rad(11))
            );
        }

        static void BuildBleach()
        {
            Add("Bleach", "Руїни Товариства Душ", CampTheme.SoulSociety, 5, "#D8D2C2", "#1B1B1B",
                new[]
                {
                    Mob("Hollow", L("#1B1B1B", H.Bald, "#1B1B1B", "#1B1B1B", HollowMask).Skin("#2A2A2A").Eyes("#FFD21F").Scale(1.25f), 1.2f, 1.1f),
                    Mob("Arrancar", L("#3A3A4A", H.Messy, "#F2F2F2", "#F2F2F2", HollowMask, Swords).Eyes("#4AD1FF"), 1f, 1.2f, 4.2f)
                },
                Soul("Ichigo Kurosaki", "Тимчасовий Жнець Душ", R.Legendary, E.Spirit, "#FF8A1F",
                    L("#FF8A1F", H.Spiky, "#111111", "#111111", BigSword).Eyes("#6B3A1E"),
                    Proj("Getsuga Tensho", 75, 5, "#7EE6FF", E.Spirit).Size(1.4f).Spd(30).Pierce(),
                    Tele("Flash Step", 35, 6, "#FFFFFF", E.Spirit).Rad(3),
                    Buff("Bankai: Tensa Zangetsu", 70, 30, "#B0002A", E.Dark).Boost(1.8f, 1.5f).Dur(14).Rad(6)),
                Soul("Rukia Kuchiki", "Сіраюкі", R.Rare, E.Ice, "#CFF3FF",
                    L("#111111", H.Short, "#111111", "#111111", Swords).Eyes("#4A3A8A"),
                    Slam("Tsukishiro", 60, 6, "#CFF3FF", E.Ice).Rng(12).Rad(4.5f).Stun(1.5f),
                    Proj("Hado 33: Sokatsui", 55, 5, "#6BB8FF", E.Spirit).Rad(2.5f),
                    Nova("Hakka no Togame", 140, 22, "#E8FBFF", E.Ice).Rad(9).Stun(2.5f)),
                Soul("Byakuya Kuchiki", "Капітан 6-го Загону", R.Epic, E.Wind, "#FFB7D5",
                    L("#111111", H.Long, "#111111", "#111111", Cape, Scarf, Swords).Accent("#F2F2F2"),
                    Multi("Senbonzakura", 10, 5, "#FFB7D5", E.Wind).Cnt(10).Size(0.25f),
                    Tele("Shunpo", 30, 6, "#FFFFFF", E.Spirit),
                    Rain("Senbonzakura Kageyoshi", 25, 22, "#FF9BC8", E.Wind).Cnt(14).Rad(8)),
                Soul("Sosuke Aizen", "Володар Лас Ночес", R.Legendary, E.Dark, "#B88BFF",
                    L("#4A2E1E", H.Short, "#F2F2F2", "#F2F2F2", Swords).Eyes("#8A5A2A"),
                    Clones("Kyoka Suigetsu", 20, 14, "#B88BFF", E.Dark).Cnt(2).Dur(8),
                    Slam("Hado 90: Kurohitsugi", 110, 9, "#2B0B3F", E.Dark).Rad(5).Stun(1.5f),
                    Buff("Hogyoku Ascension", 90, 30, "#B88BFF", E.Dark).Boost(2f, 1.2f).Dur(15).Rad(7)),
                Soul("Kenpachi Zaraki", "Кенпачі", R.Epic, E.Physical, "#FFE04A",
                    L("#111111", H.Spiky, "#111111", "#111111", Cape, Swords).Accent("#F2F2F2"),
                    Slash("Brutal Slash", 70, 4, "#FFE04A", E.Physical).Rng(5).Arc(120),
                    Buff("Battle Frenzy", 0, 16, "#FFE04A", E.Physical).Boost(1.5f, 1.2f).Dur(10),
                    Slash("Kendo", 260, 22, "#FFF27A", E.Physical).Rng(9).Arc(60))
            );
        }

        static void BuildAttackOnTitan()
        {
            Add("Attack on Titan", "Руїни Стіни Роза", CampTheme.Walls, 3, "#9FB57A", "#2E6B3A",
                new[]
                {
                    Mob("Pure Titan", L("#3B2A1E", H.Short, "#E2B497", "#E2B497").Skin("#E2B497").Scale(2.6f), 2.2f, 1.3f, 2.6f, 3.2f),
                    Mob("Abnormal Titan", L("#2A1E14", H.Long, "#D9A58A", "#D9A58A").Skin("#D9A58A").Scale(3.1f), 2.8f, 1.5f, 4.0f, 3.6f)
                },
                Soul("Eren Yeager", "Атакуючий Титан", R.Epic, E.Physical, "#3EC7A8",
                    L("#3B2A1E", H.Short, "#8A6A4A", "#F0EBDD", Cape).Eyes("#3EC7A8").Accent("#2E6B3A"),
                    Dash("ODM Slash", 55, 4, "#CFE8FF", E.Physical).Rng(10).Rad(1.5f),
                    Slam("Titan Fist", 75, 7, "#FFE45C", E.Earth).Rng(6).Rad(4),
                    Buff("Attack Titan Shift", 100, 32, "#FFE45C", E.Lightning).Boost(1.9f, 1.1f, 2.2f).Dur(15).Rad(7)),
                Soul("Mikasa Ackerman", "Аккерман", R.Epic, E.Physical, "#C0282A",
                    L("#111111", H.Short, "#8A6A4A", "#F0EBDD", Cape, Scarf, Swords).Accent("#2E6B3A"),
                    Nova("Spinning Slash", 55, 5, "#CFE8FF", E.Physical).Rad(4),
                    Dash("ODM Dash", 45, 5, "#CFE8FF", E.Physical).Rng(12).Rad(1.5f),
                    Multi("Thunder Spears", 50, 20, "#FFB43C", E.Fire).Cnt(4).Rad(3)),
                Soul("Levi Ackerman", "Найсильніший Солдат Людства", R.Legendary, E.Physical, "#CFE8FF",
                    L("#111111", H.Short, "#8A6A4A", "#F0EBDD", Cape, Swords).Accent("#2E6B3A"),
                    Dash("Whirlwind Slash", 70, 4, "#CFE8FF", E.Physical).Rng(12).Rad(3),
                    Slash("Blade Flurry", 30, 6, "#E8F4FF", E.Physical).Rng(4).Arc(140).Cnt(4),
                    Slash("Beast Titan Takedown", 35, 22, "#FFFFFF", E.Physical).Rng(6).Arc(360).Cnt(8)),
                Soul("Armin Arlert", "Колосальний Титан", R.Epic, E.Fire, "#FF9A3C",
                    L("#F5D77A", H.Short, "#8A6A4A", "#F0EBDD", Cape).Eyes("#2E7BFF").Accent("#2E6B3A"),
                    Proj("Flare Signal", 40, 4, "#FF5A3C", E.Fire).Rad(3),
                    Heal("Strategic Mind", 110, 14, "#8CFFB0", E.Light),
                    Nova("Colossal Blast", 220, 30, "#FF9A3C", E.Fire).Rad(14).Kb(14)),
                Soul("Reiner Braun", "Бронетитан", R.Rare, E.Earth, "#E8D27A",
                    L("#E8D27A", H.Short, "#8A6A4A", "#F0EBDD", Cape).Accent("#2E6B3A"),
                    Dash("Shoulder Charge", 60, 5, "#E8D27A", E.Earth).Rng(10).Rad(2).Kb(10),
                    Shield("Armor Harden", 180, 14, "#E8D27A", E.Earth).Dur(7),
                    Buff("Armored Titan", 80, 30, "#E8D27A", E.Earth).Boost(1.6f, 1f, 2f).Dur(15).Rad(6))
            );
        }

        static void BuildDemonSlayer()
        {
            Add("Demon Slayer", "Ліс Гліцинії", CampTheme.Wisteria, 2, "#4E8A4A", "#B58BE0",
                new[]
                {
                    Mob("Lesser Demon", L("#3A1E2A", H.Wild, "#6B2A3A", "#3A1E2A", Horns).Skin("#D9C8D8").Eyes("#D81E1E")),
                    Mob("Spider Demon", L("#F2F2F2", H.Long, "#F2F2F2", "#D9D9E2").Skin("#F2EBF2").Eyes("#D81E1E"), 1.1f, 1.1f, 4f)
                },
                Soul("Tanjiro Kamado", "Дихання Сонця", R.Epic, E.Water, "#2E8B57",
                    L("#6B1E1E", H.Short, "#1B1B2A", "#1B1B2A", Cape, Swords).Eyes("#A23A2A").Accent("#2E8B57"),
                    Slash("Water Surface Slash", 60, 4, "#4FB4FF", E.Water).Rng(5),
                    Dash("Water Wheel", 55, 6, "#4FB4FF", E.Water).Rng(9).Rad(2),
                    Slash("Hinokami Kagura", 70, 22, "#FF6A1A", E.Fire).Rng(6).Arc(360).Cnt(3)),
                Soul("Nezuko Kamado", "Сестра-Демон", R.Rare, E.Blood, "#FF7AB8",
                    L("#1B1B1B", H.Long, "#F5A3C7", "#6B3A2A", Mask).Eyes("#FF7AB8").Accent("#8A5A2A"),
                    Dash("Demon Kick", 55, 5, "#FF7AB8", E.Physical).Rng(7),
                    Nova("Exploding Blood", 60, 7, "#FF4F7A", E.Fire).Rad(5),
                    Buff("Demon Form", 50, 28, "#FF4F7A", E.Blood).Boost(1.7f, 1.3f).Dur(12).Rad(5)),
                Soul("Zenitsu Agatsuma", "Дихання Грому", R.Rare, E.Lightning, "#FFE45C",
                    L("#FFD21F", H.Short, "#1B1B2A", "#1B1B2A", Cape, Swords).Eyes("#C0A030").Accent("#F5B52A"),
                    Dash("Thunderclap and Flash", 75, 5, "#FFE45C", E.Lightning).Rng(14).Rad(1.5f),
                    Rain("Sixfold", 30, 8, "#FFE45C", E.Lightning).Cnt(6).Rad(6),
                    Dash("Flaming Thunder God", 220, 24, "#FF9A2A", E.Fire).Rng(22).Rad(4)),
                Soul("Inosuke Hashibira", "Дихання Звіра", R.Rare, E.Physical, "#5A7AA0",
                    L("#2E4A6B", H.Short, "#F2C9A0", "#6B5A44", BoarHead, Swords),
                    Slash("Fang Slash", 55, 4, "#CFE8FF", E.Physical).Rng(4).Arc(90),
                    Slash("Crazy Cutting", 18, 7, "#CFE8FF", E.Physical).Cnt(6),
                    Nova("Beast Rampage", 150, 22, "#A0C8FF", E.Physical).Rad(7)),
                Soul("Kyojuro Rengoku", "Хашира Полум'я", R.Legendary, E.Fire, "#FFB22E",
                    L("#FFB22E", H.Wild, "#1B1B2A", "#1B1B2A", Cape, Swords).Eyes("#D8641E").Accent("#F2F2F2"),
                    Dash("Unknowing Fire", 75, 5, "#FF7A1A", E.Fire).Rng(12).Rad(1.5f),
                    Slash("Rising Scorching Sun", 70, 6, "#FF9A2A", E.Fire).Rng(5).Arc(150),
                    Dash("Ninth Form: Rengoku", 260, 25, "#FF5A1A", E.Fire).Rng(20).Rad(5))
            );
        }

        static void BuildJujutsuKaisen()
        {
            Add("Jujutsu Kaisen", "Руїни Сібуї", CampTheme.Shibuya, 4, "#8A8A92", "#6B4A8A",
                new[]
                {
                    Mob("Cursed Spirit", L("#3A2A4A", H.Bald, "#6B4A8A", "#4A3266", Horns).Skin("#7A5A9A").Eyes("#FFD21F").Head(1.4f), 1f, 1.1f),
                    Mob("Grasshopper Curse", L("#3A5A2A", H.Bald, "#4E7A3A", "#3A5A2A", Antennae).Skin("#6B9A4A").Eyes("#D81E1E"), 0.9f, 1.2f, 5f)
                },
                Soul("Yuji Itadori", "Посудина", R.Epic, E.Spirit, "#F5A0B0",
                    L("#F5A0B0", H.Short, "#1B1F3A", "#1B1F3A").Accent("#C0282A"),
                    Slam("Divergent Fist", 65, 4, "#6FD6FF", E.Spirit).Rng(3).Rad(2.5f),
                    Dash("Manji Kick", 50, 5, "#FFFFFF", E.Physical).Rng(8),
                    Punch("Black Flash", 240, 18, "#FF2A3C", E.Dark).Rng(5).Rad(3)),
                Soul("Megumi Fushiguro", "Десять Тіней", R.Epic, E.Dark, "#2E2E4A",
                    L("#111111", H.Spiky, "#1B1F3A", "#1B1F3A"),
                    Summon("Divine Dogs", 16, 14, "#E8E8E8", E.Dark).Cnt(2).Dur(12),
                    Rain("Nue", 40, 9, "#BFE6FF", E.Lightning).Cnt(4).Rad(5),
                    Pull("Chimera Shadow Garden", 30, 24, "#2B0B3F", E.Dark).Rad(8).Dur(3).Stun(1.5f)),
                Soul("Nobara Kugisaki", "Солом'яна Лялька", R.Rare, E.Spirit, "#D87A3A",
                    L("#D87A3A", H.Short, "#1B1F3A", "#1B1F3A"),
                    Multi("Hairpin", 22, 5, "#FFC34A", E.Spirit).Cnt(4).Rad(2),
                    Slam("Resonance", 80, 8, "#4AD1FF", E.Dark).Rng(15).Rad(2).Stun(0.8f),
                    Rain("Hammer Barrage", 40, 20, "#FFC34A", E.Spirit).Cnt(8)),
                Soul("Satoru Gojo", "Найсильніший", R.Legendary, E.Spirit, "#6FD6FF",
                    L("#FFFFFF", H.Spiky, "#111A33", "#111A33", Blindfold).Eyes("#6FD6FF"),
                    Pull("Lapse: Blue", 25, 7, "#3C7BFF", E.Spirit).Rng(16).Rad(6),
                    Shield("Infinity", 300, 16, "#9FE8FF", E.Spirit).Dur(5),
                    Proj("Hollow Purple", 300, 28, "#A04CFF", E.Spirit).Size(3f).Spd(26).Rad(4).Pierce()),
                Soul("Ryomen Sukuna", "Король Прокльонів", R.Legendary, E.Dark, "#B0002A",
                    L("#F5A0B0", H.Spiky, "#F2F2F2", "#2A2A2A").Eyes("#D81E1E"),
                    Slash("Dismantle", 75, 4, "#FF5A6A", E.Dark).Rng(9).Arc(50),
                    Proj("Fuga", 90, 9, "#FF6A1A", E.Fire).Size(1f).Spd(24).Rad(5),
                    Rain("Malevolent Shrine", 35, 30, "#B0002A", E.Dark).Cnt(20).Rad(12).Rng(10))
            );
        }

        static void BuildMyHeroAcademia()
        {
            Add("My Hero Academia", "Полігон Академії Ю.Ей.", CampTheme.Academy, 1, "#9AB878", "#2E5BC0",
                new[]
                {
                    Mob("League Thug", L("#3A3A3A", H.Messy, "#3A3A3A", "#2A2A2A", Mask).Eyes("#D81E1E")),
                    Mob("Nomu", L("#1B1B2A", H.Bald, "#1B1B2A", "#1B1B2A", Brain).Skin("#2A2A3A").Eyes("#FFD21F").Scale(1.5f), 1.6f, 1.2f, 3f, 2.3f)
                },
                Soul("Izuku Midoriya", "Деку", R.Epic, E.Lightning, "#2E7D5B",
                    L("#1F4A2E", H.Messy, "#2E7D5B", "#2E7D5B").Eyes("#2E7D5B"),
                    Proj("Delaware Smash", 55, 4, "#8CFFB0", E.Wind).Spd(40).Size(0.8f).Pierce(),
                    Dash("Shoot Style", 60, 5, "#6BFF8A", E.Lightning).Rng(10),
                    Slam("100% Detroit Smash", 230, 24, "#8CFFB0", E.Lightning).Rng(5).Rad(6).Kb(16)),
                Soul("Katsuki Bakugo", "Герой-Вибух", R.Epic, E.Fire, "#FF8A1E",
                    L("#F5E6A0", H.Spiky, "#1B1B1B", "#1B1B1B").Eyes("#D81E1E").Accent("#FF8A1E"),
                    Proj("AP Shot", 60, 4, "#FFB43C", E.Fire).Spd(45).Size(0.4f).Rad(2),
                    Dash("Explosion Dash", 40, 5, "#FF8A1E", E.Fire).Rng(10).Rad(3),
                    Nova("Howitzer Impact", 200, 24, "#FF6A1A", E.Fire).Rad(8).Kb(12)),
                Soul("Shoto Todoroki", "Пів-Холоду Пів-Жару", R.Epic, E.Ice, "#BFEFFF",
                    L("#E8E8E8", H.Short, "#2E3F6B", "#2E3F6B").Eyes("#4A6BB0").Accent("#C0282A"),
                    Slam("Ice Wall", 55, 6, "#BFEFFF", E.Ice).Rng(10).Rad(4).Stun(1.5f),
                    Beam("Flashfire", 25, 8, "#FF7A2A", E.Fire).Rng(14).Rad(1.2f).Dur(1f),
                    Beam("Heaven-Piercing Ice Wall", 45, 22, "#BFEFFF", E.Ice).Rng(30).Rad(3).Dur(1f).Stun(2f)),
                Soul("All Might", "Символ Миру", R.Legendary, E.Physical, "#2E5BC0",
                    L("#FFE04A", H.Wild, "#2E5BC0", "#2E5BC0").Eyes("#2E7BFF").Scale(1.25f).Accent("#C0282A"),
                    Proj("Texas Smash", 80, 5, "#E8F4FF", E.Wind).Size(1.5f).Spd(35).Pierce(),
                    Slam("Detroit Smash", 100, 7, "#FFE04A", E.Physical).Rng(4).Rad(5),
                    Punch("United States of Smash", 360, 26, "#FFE04A", E.Physical).Rng(8).Rad(5).Kb(20)),
                Soul("Ochaco Uraraka", "Юравіті", R.Rare, E.Spirit, "#FFB7D5",
                    L("#6B3A2A", H.Short, "#F5A3C7", "#1B1B1B"),
                    Nova("Zero Gravity", 20, 7, "#FFB7D5", E.Spirit).Rad(5).Stun(2f),
                    Rain("Meteor Shower", 35, 9, "#C9A27A", E.Earth).Cnt(7),
                    Rain("Comet Home Run", 45, 22, "#C9A27A", E.Earth).Cnt(12).Rad(8))
            );
        }

        static void BuildHunterXHunter()
        {
            Add("Hunter x Hunter", "Підніжжя Небесної Арени", CampTheme.Arena, 3, "#6FAF5A", "#C8A23A",
                new[]
                {
                    Mob("Chimera Ant Soldier", L("#2A3A2A", H.Bald, "#3A5A3A", "#2A3A2A", Antennae).Skin("#4E6B4A").Eyes("#D81E1E"), 1f, 1.1f),
                    Mob("Ant Royal Guard", L("#4A3A6B", H.Bald, "#6B5AB0", "#3A2E5A", Antennae, Tail).Skin("#8A7AC0").Eyes("#FFD21F").Scale(1.35f), 1.7f, 1.3f, 4.4f, 2.2f)
                },
                Soul("Gon Freecss", "Джаджанкен", R.Epic, E.Physical, "#2E8B3A",
                    L("#111111", H.Spiky, "#2E8B3A", "#2E8B3A").Eyes("#6B3A1E"),
                    Punch("Jajanken: Rock", 90, 5, "#FFE07A", E.Physical).Rng(3.5f).Rad(3),
                    Proj("Jajanken: Paper", 55, 5, "#FFE07A", E.Spirit).Rad(3),
                    Buff("Adult Gon", 120, 34, "#FFD34A", E.Spirit).Boost(2.4f, 1.2f, 1.4f).Dur(12).Rad(6)),
                Soul("Killua Zoldyck", "Божественна Швидкість", R.Epic, E.Lightning, "#BFE6FF",
                    L("#F2F2F5", H.Wild, "#5A3F8A", "#3A3A4A").Eyes("#4A8ACF"),
                    Slam("Thunderbolt", 70, 5, "#BFE6FF", E.Lightning).Rng(14).Rad(3).Stun(0.6f),
                    Clones("Rhythm Echo", 14, 14, "#BFE6FF", E.Lightning).Cnt(3).Dur(6),
                    Buff("Godspeed", 60, 30, "#BFE6FF", E.Lightning).Boost(1.5f, 2f).Dur(12).Tint("#E8F6FF").Rad(5)),
                Soul("Kurapika", "Останній з Курта", R.Epic, E.Spirit, "#D81E3A",
                    L("#FFD95A", H.Short, "#2E4FA0", "#2E4FA0").Eyes("#D81E3A"),
                    Proj("Chain Jail", 50, 8, "#C0C8D8", E.Spirit).Spd(30).Size(0.4f).Stun(2.5f),
                    Heal("Holy Chain", 120, 14, "#8CFFB0", E.Light),
                    Buff("Emperor Time", 80, 30, "#D81E3A", E.Spirit).Boost(1.8f, 1.3f).Dur(12).Rad(5)),
                Soul("Hisoka Morow", "Фокусник", R.Legendary, E.Spirit, "#FF5CCB",
                    L("#D2204A", H.Spiky, "#F2F2F2", "#E2A2C8").Eyes("#FFD21F"),
                    Pull("Bungee Gum", 40, 7, "#FF5CCB", E.Spirit).Rng(14).Rad(3).Dur(1f),
                    Multi("Card Throw", 24, 5, "#F2F2F2", E.Physical).Size(0.25f).Spd(45),
                    Rain("Joker's Gambit", 45, 22, "#FF5CCB", E.Spirit).Cnt(10).Rad(7)),
                Soul("Isaac Netero", "Голова Асоціації", R.Legendary, E.Light, "#FFD34A",
                    L("#F2F2F2", H.Bun, "#F2F2F2", "#3A3A4A").Scale(0.95f),
                    Slam("Guanyin: First Hand", 90, 6, "#FFD34A", E.Light).Rng(10).Rad(4),
                    Rain("Kannon Barrage", 40, 10, "#FFD34A", E.Light).Cnt(8).Rad(6),
                    Beam("Zero Hand", 90, 30, "#FFF0A0", E.Light).Rng(30).Rad(3).Dur(1.5f))
            );
        }

        static void BuildFullmetal()
        {
            Add("Fullmetal Alchemist", "Руїни Аместріса", CampTheme.Alchemy, 4, "#B8A27A", "#B5222A",
                new[]
                {
                    Mob("Chimera", L("#6B4A2A", H.Short, "#8A6A4A", "#6B4A2A").Skin("#8A6A4A").Eyes("#FFD21F").Beast().Scale(1.3f), 1.2f, 1.2f, 4.8f, 2.2f),
                    Mob("Mannequin Soldier", L("#E2D8C8", H.Bald, "#E8DDCB", "#E8DDCB").Skin("#EDE3D3").Eyes("#1B1B1B"), 1f, 1.1f)
                },
                Soul("Edward Elric", "Сталевий Алхімік", R.Epic, E.Earth, "#B5222A",
                    L("#F5C84A", H.Ponytail, "#1B1B1B", "#1B1B1B", Cape).Eyes("#E8B83A").Accent("#B5222A"),
                    Slash("Arm Blade", 55, 4, "#DDE6F0", E.Physical).Rng(4),
                    Slam("Earth Spikes", 70, 6, "#C9A27A", E.Earth).Rng(12).Rad(4),
                    Rain("Grand Transmutation", 45, 24, "#4AD1FF", E.Earth).Cnt(10).Rad(9)),
                Soul("Alphonse Elric", "Жива Броня", R.Rare, E.Earth, "#9AA5B1",
                    L("#9AA5B1", H.Bald, "#9AA5B1", "#8A95A1", Helmet, Armor).Skin("#9AA5B1").Eyes("#FF3A2A").Scale(1.35f),
                    Punch("Steel Fist", 60, 4, "#DDE6F0", E.Physical).Rng(3),
                    Shield("Stone Wall", 200, 14, "#C9A27A", E.Earth).Dur(6),
                    Proj("Cannon Transmutation", 150, 22, "#C9A27A", E.Earth).Size(1f).Spd(30).Rad(6)),
                Soul("Roy Mustang", "Полум'яний Алхімік", R.Legendary, E.Fire, "#FF6A1A",
                    L("#111111", H.Short, "#2E3F7A", "#2E3F7A"),
                    Slam("Finger Snap", 75, 4, "#FF6A1A", E.Fire).Rng(18).Rad(3.5f),
                    Beam("Flame Wave", 30, 8, "#FF7A2A", E.Fire).Rng(16).Rad(2).Dur(0.8f),
                    Rain("Inferno", 50, 26, "#FF4A1A", E.Fire).Cnt(14).Rad(10)),
                Soul("Scar", "Ішварський Месник", R.Epic, E.Earth, "#8A5A3A",
                    L("#E8E8E8", H.Short, "#3A3A3A", "#1B1B1B", Glasses).Skin("#8A5A3A").Eyes("#D81E1E"),
                    Punch("Destruction Hand", 85, 5, "#4AD1FF", E.Earth).Rng(3).Rad(3),
                    Slam("Ground Rupture", 70, 7, "#C9A27A", E.Earth).Rng(8).Rad(4.5f),
                    Nova("Reconstruction Array", 180, 24, "#4AD1FF", E.Earth).Rad(10).Stun(1f)),
                Soul("Riza Hawkeye", "Соколине Око", R.Rare, E.Physical, "#F5D77A",
                    L("#F5D77A", H.Bun, "#2E3F7A", "#2E3F7A").Eyes("#6B3A1E"),
                    Proj("Precision Shot", 70, 4, "#FFF0A0", E.Physical).Spd(70).Size(0.2f),
                    Multi("Rapid Fire", 16, 6, "#FFF0A0", E.Physical).Cnt(6).Spd(60).Size(0.15f),
                    Proj("Sniper Volley", 240, 20, "#FFFFFF", E.Physical).Spd(90).Size(0.3f).Pierce().Rng(50))
            );
        }

        // ------------------------------------------------------------------ text

        public static string RarityName(Rarity r)
        {
            switch (r)
            {
                case Rarity.Legendary: return "Легендарна";
                case Rarity.Epic: return "Епічна";
                default: return "Рідкісна";
            }
        }

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Legendary: return Hex.C("#FFB43C");
                case Rarity.Epic: return Hex.C("#C08BFF");
                default: return Hex.C("#6FC3FF");
            }
        }

        public static string ElementName(Element e)
        {
            switch (e)
            {
                case Element.Wind: return "Вітер";
                case Element.Fire: return "Вогонь";
                case Element.Lightning: return "Блискавка";
                case Element.Water: return "Вода";
                case Element.Ice: return "Лід";
                case Element.Dark: return "Темрява";
                case Element.Light: return "Світло";
                case Element.Earth: return "Земля";
                case Element.Spirit: return "Дух";
                case Element.Blood: return "Кров";
                default: return "Фізична";
            }
        }

        static string Describe(SkillDef s)
        {
            string d = Mathf.RoundToInt(s.damage).ToString();
            string extra = "";
            if (s.stun > 0) extra += $" Оглушує на {s.stun:0.#}с.";
            if (s.element == Element.Fire) extra += " Підпалює.";
            if (s.element == Element.Ice) extra += " Сповільнює.";
            switch (s.type)
            {
                case SkillType.Projectile:
                    return $"Снаряд: {d} шкоди" + (s.radius > 0 ? $", вибух радіусом {s.radius:0.#}м" : "") + (s.pierce ? ", пробиває ворогів" : "") + "." + extra;
                case SkillType.MultiProjectile:
                    return $"Віяло з {s.count} снарядів по {d} шкоди." + extra;
                case SkillType.Nova:
                    return $"Вибух навколо себе: {d} шкоди в радіусі {s.radius:0.#}м." + extra;
                case SkillType.GroundSlam:
                    return $"Удар у точку під курсором: {d} шкоди в радіусі {s.radius:0.#}м." + extra;
                case SkillType.Dash:
                    return $"Ривок на {s.range:0.#}м, {d} шкоди всім на шляху" + (s.radius > 0 ? $" і вибух у кінці" : "") + "." + extra;
                case SkillType.Clones:
                    return $"Створює {s.count} клонів на {s.duration:0}с, кожен удар — {d} шкоди.";
                case SkillType.Summon:
                    return $"Викликає {s.count} істот на {s.duration:0}с, кожен укус — {d} шкоди.";
                case SkillType.Beam:
                    return $"Промінь на {s.range:0}м: {d} шкоди 4 рази на секунду протягом {s.duration:0.#}с." + extra;
                case SkillType.StretchPunch:
                    return s.count > 1 ? $"Шквал із {s.count} ударів по {d} шкоди." : $"Далекий удар на {s.range:0}м: {d} шкоди." + extra;
                case SkillType.Slash:
                    return (s.count > 1 ? $"{s.count} помахів" : "Помах") + $" по {d} шкоди в дузі {s.angle:0}°." + extra;
                case SkillType.Buff:
                    return $"Трансформація на {s.duration:0}с: шкода x{s.buffDamage:0.0#}" + (s.buffSpeed > 1 ? $", швидкість x{s.buffSpeed:0.0#}" : "") + (s.damage > 0 ? $". Вибух {d} при активації" : "") + ".";
                case SkillType.Heal:
                    return $"Відновлює {d} здоров'я.";
                case SkillType.Shield:
                    return $"Щит, що поглинає {d} шкоди протягом {s.duration:0}с.";
                case SkillType.Teleport:
                    return $"Миттєве переміщення до курсора (до {s.range:0}м)" + (s.damage > 0 ? $", {d} шкоди при появі" : "") + ".";
                case SkillType.Rain:
                    return $"{s.count} ударів з неба по {d} шкоди навколо курсора." + extra;
                case SkillType.Pull:
                    return $"Вир, що затягує ворогів і завдає {d} шкоди 4 рази на секунду." + extra;
            }
            return "";
        }
    }
}
