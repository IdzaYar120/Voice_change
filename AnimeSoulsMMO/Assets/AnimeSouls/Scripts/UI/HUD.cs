using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Immediate-mode HUD, laid out on a virtual 1080p canvas: player frame, skill bar, soul slots,
    /// nameplates, floating damage, boss bar, Soul Codex (Tab), world map with fast travel (M), help (F1).
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD I;
        public PlayerAvatar player;

        public static bool BlocksGameplayInput => I != null && (I._codexOpen || I._mapOpen);

        struct Note { public string text; public Color color; public float until; }
        struct Popup { public Vector3 pos; public string text; public Color color; public float born; public bool big; }

        readonly List<Note> _notes = new List<Note>();
        readonly List<Popup> _popups = new List<Popup>();
        bool _codexOpen, _mapOpen, _helpOpen = true;
        int _codexAnime;
        SoulDef _codexSoul;
        string _bannerTitle, _bannerSub;
        Color _bannerColor;
        float _bannerUntil;
        AnimeDef _currentArea;
        bool _areaInit;
        float _helpAutoHide;

        Texture2D _white;
        GUIStyle _label, _labelC, _labelR, _small, _smallC, _title, _big, _button, _window;
        float _s = 1f;
        float W => Screen.width / _s;
        const float H = 1080f;

        public static void Notify(string text, Color color, float seconds = 2.5f)
        {
            if (I == null) return;
            I._notes.Add(new Note { text = text, color = color, until = Time.time + seconds });
            if (I._notes.Count > 6) I._notes.RemoveAt(0);
        }

        public static void Banner(string title, string sub, Color color, float seconds = 3.5f)
        {
            if (I == null) return;
            I._bannerTitle = title;
            I._bannerSub = sub;
            I._bannerColor = color;
            I._bannerUntil = Time.time + seconds;
        }

        void Awake()
        {
            I = this;
            _white = Texture2D.whiteTexture;
            Health.AnyDamaged += OnAnyDamaged;
            _helpAutoHide = Time.time + 25f;
        }

        void OnDestroy()
        {
            Health.AnyDamaged -= OnAnyDamaged;
        }

        public void Bind(PlayerAvatar p)
        {
            player = p;
            p.inventory.FragmentsGained += (s, n) =>
            {
                if (n > 0) Notify($"+{n} фрагмент душі: {s.name}  ({p.inventory.Fragments(s)}/{s.FragmentsRequired})", s.signature);
                else Notify($"Душа {s.name} вже поглинута", new Color(0.8f, 0.8f, 0.85f), 1.5f);
            };
            p.inventory.SoulAbsorbed += s =>
            {
                Banner("ДУШУ ПОГЛИНУТО: " + s.name, $"{s.title} · {s.Anime.name} · {AnimeDatabase.RarityName(s.rarity)}  —  Tab: Кодекс душ, 1/2/3: змінити душу", AnimeDatabase.RarityColor(s.rarity), 5f);
            };
        }

        void OnAnyDamaged(Health victim, float amount, Color color, bool big)
        {
            if (player == null || victim == null) return;
            if (Vector3.Distance(victim.transform.position, player.transform.position) > 45f) return;
            var c = victim.isPlayer ? Hex.C("#FF5A5A") : color;
            string text = amount < 0 ? "+" + Mathf.RoundToInt(-amount) : Mathf.RoundToInt(amount).ToString();
            _popups.Add(new Popup
            {
                pos = victim.transform.position + Vector3.up * (victim.Height + 0.3f) + Random.insideUnitSphere * 0.4f,
                text = text, color = c, born = Time.time, big = big
            });
            if (_popups.Count > 80) _popups.RemoveAt(0);
        }

        void Update()
        {
            if (Inp.Down(KeyCode.Tab)) { _codexOpen = !_codexOpen; _mapOpen = false; }
            if (Inp.Down(KeyCode.M)) { _mapOpen = !_mapOpen; _codexOpen = false; }
            if (Inp.Down(KeyCode.F1)) _helpOpen = !_helpOpen;
            if (Inp.Down(KeyCode.Escape)) { _codexOpen = false; _mapOpen = false; }
            if (_helpOpen && Time.time > _helpAutoHide && _helpAutoHide > 0f) { _helpOpen = false; _helpAutoHide = -1f; }

            if (player != null)
            {
                var area = World.CampAt(player.transform.position, 6f);
                if (!_areaInit || area != _currentArea)
                {
                    if (_areaInit)
                    {
                        if (area != null)
                            Banner(area.campName, $"{area.name} · Рівень {(area.tier - 1) * 10 + 1}-{area.tier * 10 + 2} · Душі: {SoulNames(area)}", area.accent, 4f);
                        else if (World.InHub(player.transform.position))
                            Banner("Нексус Душ", "Безпечна зона", Hex.C("#8FE8FF"), 2.5f);
                    }
                    _currentArea = area;
                    _areaInit = true;
                }
            }
        }

        static string SoulNames(AnimeDef a)
        {
            var names = new List<string>();
            foreach (var s in a.souls) names.Add(s.name.Split(' ')[0]);
            return string.Join(", ", names);
        }

        // ------------------------------------------------------------------ styles

        void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, richText = true };
            _label.normal.textColor = Color.white;
            _labelC = new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter };
            _labelR = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
            _small = new GUIStyle(_label) { fontSize = 15, fontStyle = FontStyle.Normal, wordWrap = true };
            _smallC = new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            _title = new GUIStyle(_label) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
            _big = new GUIStyle(_label) { fontSize = 46, alignment = TextAnchor.MiddleCenter };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            _window = new GUIStyle(GUI.skin.box);
        }

        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = old;
        }

        void Frame(Rect r, Color fill, Color border, float t = 2f)
        {
            Fill(r, border);
            Fill(new Rect(r.x + t, r.y + t, r.width - 2 * t, r.height - 2 * t), fill);
        }

        void Bar(Rect r, float frac, Color c, string text = null)
        {
            Frame(r, new Color(0.08f, 0.07f, 0.12f, 0.85f), new Color(0, 0, 0, 0.9f));
            var inner = new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(frac), r.height - 4);
            Fill(inner, c);
            Fill(new Rect(inner.x, inner.y, inner.width, inner.height * 0.4f), new Color(1, 1, 1, 0.18f));
            if (text != null) Shadow(r, text, _smallC, Color.white);
        }

        void Shadow(Rect r, string text, GUIStyle style, Color color)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0, 0, 0, 0.85f * color.a);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        Vector2? ToGui(Camera cam, Vector3 world)
        {
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0.1f) return null;
            return new Vector2(sp.x / _s, (Screen.height - sp.y) / _s);
        }

        // ------------------------------------------------------------------ draw

        void OnGUI()
        {
            EnsureStyles();
            _s = Screen.height / H;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_s, _s, 1f));
            if (player == null) return;
            var cam = player.cam != null ? player.cam.Camera : Camera.main;

            if (cam != null)
            {
                DrawNameplates(cam);
                DrawPopups(cam);
            }
            DrawPlayerFrame();
            DrawSkillBar();
            DrawBossBar();
            DrawNotes();
            DrawBanner();
            DrawAreaInfo();
            if (_helpOpen) DrawHelp();
            if (player.health.IsDead) DrawDeath();
            if (_codexOpen) DrawCodex();
            if (_mapOpen) DrawMap();
            if (Inp.Missing) DrawInputWarning();
        }

        void DrawNameplates(Camera cam)
        {
            var ppos = player.transform.position;
            foreach (var h in Health.All)
            {
                if (h == null || h.isPlayer || h.IsDead) continue;
                float d = Vector3.Distance(h.transform.position, ppos);
                if (d > (h.isBoss ? 70f : 42f)) continue;
                var g = ToGui(cam, h.transform.position + Vector3.up * (h.Height + 0.45f));
                if (!g.HasValue) continue;
                var p = g.Value;
                float w = h.isBoss ? 150f : 90f;
                bool enemy = h.team == Team.Enemy;
                var col = enemy ? (h.isBoss ? Hex.C("#FF7A3C") : Hex.C("#E84A4A")) : Hex.C("#5ADC7A");
                if (h == player.AimTarget) Frame(new Rect(p.x - w / 2 - 4, p.y - 26, w + 8, 40), new Color(1, 1, 1, 0.15f), new Color(1, 1, 1, 0.7f), 1f);
                Shadow(new Rect(p.x - 150, p.y - 24, 300, 18), enemy ? $"{h.displayName}  <size=12>Рів. {h.level}</size>" : h.displayName, _smallC, Color.white);
                Bar(new Rect(p.x - w / 2, p.y - 4, w, 10), h.hp / h.maxHp, col);
                if (h.shield > 0f) Fill(new Rect(p.x - w / 2, p.y + 6, w * Mathf.Clamp01(h.shield / h.maxHp), 3), Hex.C("#BFEFFF"));
                if (h.IsStunned) Shadow(new Rect(p.x - 50, p.y - 44, 100, 18), "★ оглушено ★", _smallC, Hex.C("#FFE45C"));
            }
        }

        void DrawPopups(Camera cam)
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                var pu = _popups[i];
                float age = Time.time - pu.born;
                if (age > 1.1f) { _popups.RemoveAt(i); continue; }
                var g = ToGui(cam, pu.pos + Vector3.up * age * 1.5f);
                if (!g.HasValue) continue;
                var style = pu.big ? _title : _labelC;
                float pop = age < 0.12f ? 1f + (0.12f - age) * 4f : 1f;
                int oldSize = style.fontSize;
                style.fontSize = Mathf.RoundToInt(oldSize * pop);
                Shadow(new Rect(g.Value.x - 100, g.Value.y - 20, 200, 40), pu.text, style, pu.color.WithAlpha(Mathf.Clamp01(1.4f - age)));
                style.fontSize = oldSize;
            }
        }

        void DrawPlayerFrame()
        {
            var h = player.health;
            var inv = player.inventory;
            var c = player.caster;
            var r = new Rect(20, 20, 420, 132);
            Frame(r, new Color(0.06f, 0.05f, 0.1f, 0.72f), new Color(0.55f, 0.85f, 1f, 0.5f));
            var soul = inv.ActiveSoul;
            var portrait = new Rect(32, 32, 72, 72);
            Frame(portrait, soul != null ? soul.signature.Darken(0.2f) : Hex.C("#3A3F5A"), soul != null ? AnimeDatabase.RarityColor(soul.rarity) : Color.gray, 3f);
            Shadow(portrait, soul != null ? Initials(soul.name) : "?", _title, Color.white);
            Shadow(new Rect(116, 28, 320, 24), h.displayName, _label, Color.white);
            Shadow(new Rect(116, 50, 320, 22), soul != null ? $"<color=#{ColorUtility.ToHtmlStringRGB(soul.signature.Brighten(0.3f))}>{soul.name}</color> · {soul.Anime.name}" : "<color=#9AA>Без душі — збери фрагменти в кемпах</color>", _small, Color.white);
            Bar(new Rect(116, 76, 312, 20), h.hp / h.maxHp, Hex.C("#4ADC6A"), $"{Mathf.CeilToInt(h.hp)} / {Mathf.CeilToInt(h.maxHp)}" + (h.shield > 0 ? $"  (+{Mathf.CeilToInt(h.shield)})" : ""));
            Bar(new Rect(116, 100, 312, 14), c.energy / c.maxEnergy, Hex.C("#4AA8FF"), $"{Mathf.FloorToInt(c.energy)}");
            Shadow(new Rect(32, 118, 400, 24), $"Сила душ: <color=#FFD34A>{inv.SoulPower}</color>   Душ: {inv.UnlockedCount}/{AnimeDatabase.AllSouls.Count}   Шкода x{c.DamageMultiplier:0.00}", _small, Color.white);
            if (c.BuffActive)
            {
                var b = c.ActiveBuff;
                Shadow(new Rect(20, 156, 420, 22), $"<color=#{ColorUtility.ToHtmlStringRGB(b.color)}>◆ {b.name}</color>  {c.BuffRemaining:0}с", _small, Color.white);
            }
        }

        static string Initials(string name)
        {
            var parts = name.Split(' ');
            if (parts.Length == 1) return name.Substring(0, Mathf.Min(2, name.Length)).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }

        void DrawSkillBar()
        {
            var inv = player.inventory;
            var c = player.caster;
            var soul = inv.ActiveSoul;
            float slot = 86f, gap = 10f;
            int n = 4;
            float total = n * slot + (n - 1) * gap;
            float x0 = W / 2 - total / 2;
            float y = H - slot - 28;

            // Soul slots above the skill bar.
            float sw = 150f;
            float sx0 = W / 2 - (SoulInventory.Slots * sw + (SoulInventory.Slots - 1) * 8) / 2;
            for (int i = 0; i < SoulInventory.Slots; i++)
            {
                var s = inv.InSlot(i);
                var r = new Rect(sx0 + i * (sw + 8), y - 46, sw, 36);
                bool active = i == inv.activeSlot && s != null;
                Frame(r, s != null ? s.signature.Darken(active ? 0.1f : 0.55f).WithAlpha(0.9f) : new Color(0.1f, 0.1f, 0.15f, 0.7f),
                    active ? Color.white : new Color(0, 0, 0, 0.8f), active ? 3f : 2f);
                Shadow(new Rect(r.x + 6, r.y, 24, r.height), (i + 1).ToString(), _label, Hex.C("#FFD34A"));
                Shadow(new Rect(r.x + 26, r.y, r.width - 28, r.height), s != null ? s.name : "— порожньо —", _small, s != null ? Color.white : Color.gray);
            }

            for (int i = 0; i < n; i++)
            {
                var r = new Rect(x0 + i * (slot + gap), y, slot, slot);
                SkillDef s = null;
                string key = i == 0 ? "ЛКМ" : PlayerAvatar.SkillKeys[i - 1].ToString();
                string name;
                Color col;
                if (i == 0)
                {
                    name = soul != null && soul.swordBasic ? "Удар мечем" : "Удар";
                    col = soul != null ? soul.signature : Hex.C("#8FE8FF");
                }
                else
                {
                    s = soul != null && i - 1 < soul.skills.Length ? soul.skills[i - 1] : null;
                    name = s != null ? s.name : "—";
                    col = s != null ? s.color : Color.gray;
                }
                bool ult = s != null && s.ultimate;
                Frame(r, col.Darken(0.6f).WithAlpha(0.92f), ult ? Hex.C("#FFB43C") : new Color(0, 0, 0, 0.9f), ult ? 3f : 2f);
                Fill(new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 34), col.WithAlpha(0.85f));
                if (s != null)
                {
                    float cd = c.CooldownFraction(s);
                    if (cd > 0f)
                    {
                        Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, (r.height - 4) * cd), new Color(0, 0, 0, 0.65f));
                        Shadow(new Rect(r.x, r.y + 8, r.width, 40), c.CooldownRemaining(s).ToString("0.0"), _title, Color.white);
                    }
                    else if (c.energy < s.EnergyCost) Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), new Color(0.1f, 0.2f, 0.6f, 0.5f));
                    Shadow(new Rect(r.x + 4, r.y + r.height - 22, r.width - 8, 18), Mathf.RoundToInt(s.EnergyCost).ToString(), _smallC, Hex.C("#8FC8FF"));
                }
                Shadow(new Rect(r.x + 5, r.y + 2, 60, 22), key, _label, Hex.C("#FFD34A"));
                var nameRect = new Rect(r.x - 20, r.y + r.height + 2, r.width + 40, 20);
                Shadow(nameRect, name, _smallC, Color.white);

                // Tooltip on hover.
                if (s != null && r.Contains(Event.current.mousePosition))
                {
                    var tip = new Rect(Mathf.Min(r.x, W - 380), y - 190, 360, 130);
                    Frame(tip, new Color(0.05f, 0.04f, 0.09f, 0.95f), col);
                    Shadow(new Rect(tip.x + 10, tip.y + 6, 340, 24), s.name + (ult ? "  <color=#FFB43C>[УЛЬТА]</color>" : ""), _label, Color.white);
                    Shadow(new Rect(tip.x + 10, tip.y + 32, 340, 20), $"{AnimeDatabase.ElementName(s.element)} · КД {s.cooldown * c.cooldownMultiplier:0.#}с · Енергія {s.EnergyCost:0}", _small, Hex.C("#AFC8FF"));
                    Shadow(new Rect(tip.x + 10, tip.y + 54, 340, 70), s.desc, _small, Color.white);
                }
            }
        }

        void DrawBossBar()
        {
            var area = World.CampAt(player.transform.position, 20f);
            if (area == null) return;
            MobCamp camp = null;
            foreach (var mc in MobCamp.All) if (mc.anime == area) { camp = mc; break; }
            if (camp == null) return;
            var r = new Rect(W / 2 - 300, 22, 600, 26);
            if (camp.boss != null && !camp.boss.health.IsDead)
            {
                var h = camp.boss.health;
                Shadow(new Rect(r.x, r.y - 2, r.width, 22), $"{h.displayName}  ·  {camp.boss.bossSoul.title}", _labelC, Hex.C("#FFD3A0"));
                Bar(new Rect(r.x, r.y + 22, r.width, 18), h.hp / h.maxHp, Hex.C("#FF6A3C"), $"{Mathf.CeilToInt(h.hp)} / {Mathf.CeilToInt(h.maxHp)}");
            }
            else
            {
                Shadow(new Rect(r.x, r.y, r.width, 24), $"Наступне відлуння: {camp.NextBossSoul.name} через {camp.BossRespawnIn:0}с", _smallC, new Color(1, 1, 1, 0.8f));
            }
        }

        void DrawNotes()
        {
            _notes.RemoveAll(n => Time.time > n.until);
            float y = H * 0.62f;
            for (int i = 0; i < _notes.Count; i++)
            {
                var n = _notes[i];
                float a = Mathf.Clamp01(n.until - Time.time);
                Shadow(new Rect(W / 2 - 400, y + i * 26, 800, 26), n.text, _labelC, n.color.WithAlpha(a));
            }
        }

        void DrawBanner()
        {
            if (Time.time > _bannerUntil || string.IsNullOrEmpty(_bannerTitle)) return;
            float left = _bannerUntil - Time.time;
            float a = Mathf.Clamp01(left) * Mathf.Clamp01((Time.time - (_bannerUntil - 5f)) * 3f + 1f);
            var r = new Rect(0, H * 0.2f, W, 110);
            Fill(new Rect(0, r.y + 6, W, 98), new Color(0, 0, 0, 0.45f * a));
            Fill(new Rect(W * 0.2f, r.y + 6, W * 0.6f, 3), _bannerColor.WithAlpha(a));
            Fill(new Rect(W * 0.2f, r.y + 101, W * 0.6f, 3), _bannerColor.WithAlpha(a));
            Shadow(new Rect(0, r.y + 12, W, 56), _bannerTitle, _big, _bannerColor.Brighten(0.3f).WithAlpha(a));
            Shadow(new Rect(0, r.y + 66, W, 30), _bannerSub, _labelC, new Color(1, 1, 1, a));
        }

        void DrawAreaInfo()
        {
            string area = _currentArea != null ? _currentArea.campName : (World.InHub(player.transform.position) ? "Нексус Душ" : "Дикі землі");
            var r = new Rect(W - 380, 20, 360, 30);
            Frame(r, new Color(0.06f, 0.05f, 0.1f, 0.7f), _currentArea != null ? _currentArea.accent : new Color(0.55f, 0.85f, 1f, 0.6f));
            Shadow(r, area, _labelC, Color.white);
            Shadow(new Rect(W - 380, 52, 360, 22), "Tab — Кодекс душ · M — Карта · F1 — Допомога", _smallC, new Color(1, 1, 1, 0.75f));

            // Compass towards the nearest camp the player has not mastered yet.
            AnimeDef goal = null;
            float best = float.MaxValue;
            foreach (var a in AnimeDatabase.Anime)
            {
                int done = 0;
                foreach (var s in a.souls) if (player.inventory.IsUnlocked(s)) done++;
                if (done == a.souls.Length) continue;
                float d = Vector3.Distance(a.campPosition, player.transform.position) + a.tier * 120f;
                if (d < best) { best = d; goal = a; }
            }
            if (goal != null && goal != _currentArea)
            {
                var to = goal.campPosition - player.transform.position;
                to.y = 0f;
                float camYaw = player.cam != null ? player.cam.yaw : 0f;
                float ang = Mathf.DeltaAngle(camYaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                string arrow = ang > 22f ? "→" : ang < -22f ? "←" : "↑";
                if (Mathf.Abs(ang) > 135f) arrow = "↓";
                Shadow(new Rect(W - 380, 76, 360, 22), $"{arrow} Рекомендовано: {goal.campName} ({to.magnitude:0}м)", _smallC, goal.accent.Brighten(0.4f));
            }
        }

        void DrawHelp()
        {
            var r = new Rect(20, 190, 420, 330);
            Frame(r, new Color(0.05f, 0.04f, 0.09f, 0.8f), new Color(0.55f, 0.85f, 1f, 0.5f));
            string text =
                "<b>КЕРУВАННЯ</b>\n" +
                "WASD — рух · Shift — біг · Пробіл — стрибок\n" +
                "ПКМ (утримувати) — обертати камеру · Колесо — зум\n" +
                "ЛКМ — базова атака (у бік курсора)\n" +
                "Q / E / R — навички активної душі (R — ульта)\n" +
                "1 / 2 / 3 — змінити душу в слоті\n" +
                "Tab — Кодекс душ · M — карта і телепорт\n\n" +
                "<b>ЯК СТАТИ СИЛЬНІШИМ</b>\n" +
                "Класів немає. Вбивай мобів у тематичних кемпах — вони дропають фрагменти душ персонажів цього аніме. " +
                "Збери достатньо фрагментів — поглинеш душу і отримаєш її навички. Кожна поглинута душа назавжди підсилює шкоду і здоров'я. " +
                "Боси-відлуння дають 8 фрагментів своєї душі.\n\n<color=#AAB>F1 — сховати</color>";
            GUI.Label(new Rect(r.x + 12, r.y + 8, r.width - 24, r.height - 16), text, _small);
        }

        void DrawDeath()
        {
            Fill(new Rect(0, 0, W, H), new Color(0.3f, 0f, 0f, 0.35f));
            Shadow(new Rect(0, H * 0.4f, W, 60), "ВИ ЗАГИНУЛИ", _big, Hex.C("#FF6A6A"));
            Shadow(new Rect(0, H * 0.4f + 60, W, 30), $"Відродження через {player.RespawnIn:0.0}с", _labelC, Color.white);
        }

        void DrawInputWarning()
        {
            var r = new Rect(W / 2 - 420, H / 2 - 60, 840, 120);
            Frame(r, new Color(0.3f, 0.05f, 0.05f, 0.95f), Color.red);
            GUI.Label(new Rect(r.x + 16, r.y + 10, r.width - 32, r.height - 20),
                "Увімкни старий Input Manager: Edit → Project Settings → Player → Other Settings → Active Input Handling = \"Both\" " +
                "(або \"Input Manager (Old)\"), потім перезапусти редактор. Меню Anime Souls → Setup Project робить це автоматично.", _small);
        }

        // ------------------------------------------------------------------ codex

        void DrawCodex()
        {
            var inv = player.inventory;
            var r = new Rect(W / 2 - 720, 110, 1440, 820);
            Frame(r, new Color(0.04f, 0.03f, 0.08f, 0.96f), Hex.C("#8FE8FF"), 3f);
            Shadow(new Rect(r.x, r.y + 10, r.width, 40), $"КОДЕКС ДУШ   ·   поглинуто {inv.UnlockedCount} / {AnimeDatabase.AllSouls.Count}   ·   вбивств: {inv.kills}, босів: {inv.bossKills}", _title, Color.white);

            // Anime list.
            for (int i = 0; i < AnimeDatabase.Anime.Count; i++)
            {
                var a = AnimeDatabase.Anime[i];
                int done = 0;
                foreach (var s in a.souls) if (inv.IsUnlocked(s)) done++;
                var b = new Rect(r.x + 20, r.y + 70 + i * 72, 300, 64);
                bool sel = i == _codexAnime;
                Frame(b, sel ? a.accent.Darken(0.3f) : new Color(0.1f, 0.09f, 0.15f, 1f), sel ? Color.white : a.accent.Darken(0.2f), sel ? 3f : 2f);
                Shadow(new Rect(b.x + 12, b.y + 6, 280, 26), a.name, _label, Color.white);
                Shadow(new Rect(b.x + 12, b.y + 32, 280, 24), $"Тір {a.tier} · {done}/5 душ", _small, new Color(1, 1, 1, 0.8f));
                if (GUI.Button(b, GUIContent.none, GUIStyle.none)) { _codexAnime = i; _codexSoul = null; }
            }

            var anime = AnimeDatabase.Anime[_codexAnime];
            Shadow(new Rect(r.x + 340, r.y + 64, 1000, 30), $"{anime.campName}  —  мобів: {MobNames(anime)}", _label, anime.accent.Brighten(0.4f));

            for (int i = 0; i < anime.souls.Length; i++)
            {
                var s = anime.souls[i];
                bool unlocked = inv.IsUnlocked(s);
                var card = new Rect(r.x + 340 + i * 216, r.y + 104, 204, 300);
                bool sel = _codexSoul == s;
                Frame(card, unlocked ? s.signature.Darken(0.55f) : new Color(0.12f, 0.11f, 0.16f, 1f), sel ? Color.white : AnimeDatabase.RarityColor(s.rarity), sel ? 4f : 2f);
                var swatch = new Rect(card.x + 52, card.y + 14, 100, 100);
                Frame(swatch, unlocked ? s.look.hair : new Color(0.2f, 0.2f, 0.25f), unlocked ? s.look.outfit : Color.black, 6f);
                Shadow(swatch, unlocked ? Initials(s.name) : "?", _big, Color.white);
                Shadow(new Rect(card.x + 4, card.y + 120, card.width - 8, 26), s.name, _labelC, Color.white);
                Shadow(new Rect(card.x + 4, card.y + 146, card.width - 8, 20), s.title, _smallC, new Color(1, 1, 1, 0.75f));
                Shadow(new Rect(card.x + 4, card.y + 168, card.width - 8, 20), AnimeDatabase.RarityName(s.rarity) + " · " + AnimeDatabase.ElementName(s.element), _smallC, AnimeDatabase.RarityColor(s.rarity));
                Bar(new Rect(card.x + 14, card.y + 196, card.width - 28, 18), inv.Fragments(s) / (float)s.FragmentsRequired,
                    unlocked ? Hex.C("#FFD34A") : s.signature, unlocked ? "ПОГЛИНУТО" : $"{inv.Fragments(s)} / {s.FragmentsRequired}");
                if (unlocked)
                {
                    for (int k = 0; k < SoulInventory.Slots; k++)
                    {
                        bool inSlot = inv.equipped[k] == s.id;
                        var br = new Rect(card.x + 14 + k * 60, card.y + 226, 54, 30);
                        if (GUI.Button(br, inSlot ? $"[{k + 1}]" : $"{k + 1}", _button))
                        {
                            inv.Equip(s, k);
                            Notify($"{s.name} у слоті {k + 1}", s.signature, 1.5f);
                        }
                    }
                    Shadow(new Rect(card.x + 4, card.y + 262, card.width - 8, 20), "Призначити в слот", _smallC, new Color(1, 1, 1, 0.6f));
                }
                else Shadow(new Rect(card.x + 4, card.y + 236, card.width - 8, 40), "Фрагменти падають з мобів\nі боса цього кемпу", _smallC, new Color(1, 1, 1, 0.55f));
                if (GUI.Button(new Rect(card.x, card.y, card.width, 190), GUIContent.none, GUIStyle.none)) _codexSoul = s;
            }

            var shown = _codexSoul ?? anime.souls[0];
            var det = new Rect(r.x + 340, r.y + 420, 1076, 380);
            Frame(det, new Color(0.07f, 0.06f, 0.12f, 1f), shown.signature.Darken(0.2f));
            Shadow(new Rect(det.x + 16, det.y + 10, 1000, 30), $"{shown.name} — {shown.title}", _title, shown.signature.Brighten(0.4f));
            Shadow(new Rect(det.x + 16, det.y + 46, 1040, 22),
                $"Пасивно після поглинання: +{shown.PowerValue * 100:0}% шкоди, +{shown.HpBonus:0} здоров'я · Потрібно фрагментів: {shown.FragmentsRequired}", _small, new Color(1, 1, 1, 0.8f));
            string[] keys = { "Q", "E", "R" };
            for (int i = 0; i < shown.skills.Length; i++)
            {
                var s = shown.skills[i];
                var sr = new Rect(det.x + 16, det.y + 80 + i * 96, det.width - 32, 88);
                Frame(sr, s.color.Darken(0.75f), s.ultimate ? Hex.C("#FFB43C") : s.color.Darken(0.3f));
                Fill(new Rect(sr.x + 8, sr.y + 8, 72, 72), s.color);
                Shadow(new Rect(sr.x + 8, sr.y + 8, 72, 72), keys[i], _big, Color.white);
                Shadow(new Rect(sr.x + 96, sr.y + 6, 800, 26), s.name + (s.ultimate ? "   <color=#FFB43C>УЛЬТА</color>" : ""), _label, Color.white);
                Shadow(new Rect(sr.x + 96, sr.y + 32, 880, 22), $"{AnimeDatabase.ElementName(s.element)} · перезарядка {s.cooldown:0.#}с · енергія {s.EnergyCost:0}", _small, Hex.C("#AFC8FF"));
                Shadow(new Rect(sr.x + 96, sr.y + 54, 880, 30), s.desc, _small, Color.white);
            }
            if (GUI.Button(new Rect(r.xMax - 140, r.y + 12, 120, 34), "Закрити", _button)) _codexOpen = false;
        }

        static string MobNames(AnimeDef a)
        {
            var n = new List<string>();
            foreach (var m in a.mobs) n.Add(m.name);
            return string.Join(", ", n);
        }

        // ------------------------------------------------------------------ map

        void DrawMap()
        {
            float size = 860f;
            var r = new Rect(W / 2 - size / 2, H / 2 - size / 2 + 20, size, size);
            Frame(r, new Color(0.25f, 0.55f, 0.85f, 0.97f), Hex.C("#8FE8FF"), 3f);
            Shadow(new Rect(r.x, r.y - 44, r.width, 40), "КАРТА ОСТРОВА ДУШ — клікни на кемп, щоб телепортуватись", _label, Color.white);

            Vector2 ToMap(Vector3 w) => new Vector2(r.center.x + w.x / World.Size * size, r.center.y - w.z / World.Size * size);

            GUI.DrawTexture(r, MapTexture());

            var hub = ToMap(Vector3.zero);
            foreach (var a in AnimeDatabase.Anime)
            {
                var p = ToMap(a.campPosition);
                DrawLine(hub, p, Hex.C("#D2B47E"), 3f);
            }
            var hubRect = new Rect(hub.x - 16, hub.y - 16, 32, 32);
            Frame(hubRect, Hex.C("#8FE8FF"), Color.white);
            Shadow(new Rect(hub.x - 80, hub.y + 16, 160, 20), "Нексус Душ", _smallC, Color.white);
            if (GUI.Button(hubRect, GUIContent.none, GUIStyle.none)) Travel(World.HubPosition + new Vector3(0, 0.2f, -8f), "Нексус Душ");

            foreach (var a in AnimeDatabase.Anime)
            {
                var p = ToMap(a.campPosition);
                var cr = new Rect(p.x - 18, p.y - 18, 36, 36);
                int done = 0;
                foreach (var s in a.souls) if (player.inventory.IsUnlocked(s)) done++;
                bool hover = cr.Contains(Event.current.mousePosition);
                Frame(cr, a.accent, hover ? Color.white : Color.black, hover ? 3f : 2f);
                Shadow(cr, a.tier.ToString(), _labelC, Color.white);
                Shadow(new Rect(p.x - 140, p.y + 18, 280, 20), $"{a.name} ({done}/5)", _smallC, Color.white);
                if (GUI.Button(cr, GUIContent.none, GUIStyle.none))
                {
                    var dir = (Vector3.zero - a.campPosition);
                    dir.y = 0f;
                    Travel(a.campPosition + dir.normalized * (a.campRadius + 8f), a.campName);
                }
            }

            var pp = ToMap(player.transform.position);
            Fill(new Rect(pp.x - 7, pp.y - 7, 14, 14), Color.black);
            Fill(new Rect(pp.x - 5, pp.y - 5, 10, 10), Hex.C("#FFE45C"));
            Shadow(new Rect(r.x, r.yMax + 6, r.width, 24), "Цифра на кемпі — тір складності (1 — найлегший). Почни з тіру 1: Naruto та My Hero Academia.", _smallC, Color.white);
        }

        Texture2D _mapTex;

        /// <summary>Island silhouette rendered once from the height field.</summary>
        Texture2D MapTexture()
        {
            if (_mapTex != null) return _mapTex;
            const int res = 256;
            _mapTex = new Texture2D(res, res, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[res * res];
            var sea = new Color(0.25f, 0.55f, 0.85f, 1f);
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                // Texture row 0 is the bottom, which maps to the south (-Z) edge of the world.
                float wx = (x + 0.5f) / res * World.Size - World.Half;
                float wz = (y + 0.5f) / res * World.Size - World.Half;
                float h = World.GroundHeight(wx, wz);
                Color col;
                if (h < 0.3f) col = sea;
                else if (h < 2.5f) col = Hex.C("#EED9A2");
                else if (h > 54f) col = Hex.C("#F4F6FF");
                else if (h > 38f) col = Hex.C("#9C9AA8");
                else col = Color.Lerp(Hex.C("#7CC85A"), Hex.C("#3E8A3A"), Mathf.Clamp01((h - 4f) / 34f));
                foreach (var a in AnimeDatabase.Anime)
                    if (World.CampInfluence(a, wx, wz, 1f) > 0.5f) col = Color.Lerp(col, a.ground, 0.7f);
                px[y * res + x] = col;
            }
            _mapTex.SetPixels(px);
            _mapTex.Apply();
            return _mapTex;
        }

        void Travel(Vector3 pos, string name)
        {
            if (player.health.IsDead) return;
            player.TravelTo(pos);
            _mapOpen = false;
            Notify("Телепорт: " + name, Hex.C("#8FE8FF"));
        }

        void DrawLine(Vector2 a, Vector2 b, Color c, float width)
        {
            // Built in screen space so the rotation pivot is unaffected by the virtual-canvas scale.
            var old = GUI.matrix;
            var oldColor = GUI.color;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float len = Vector2.Distance(a, b) * _s;
            GUI.matrix = Matrix4x4.TRS(new Vector3(a.x * _s, a.y * _s, 0f), Quaternion.Euler(0, 0, angle), Vector3.one);
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, -width * _s * 0.5f, len, width * _s), _white);
            GUI.matrix = old;
            GUI.color = oldColor;
        }
    }
}
