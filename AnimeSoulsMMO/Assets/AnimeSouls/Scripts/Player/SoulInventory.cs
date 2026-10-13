using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Progression without classes: power comes only from absorbed souls. Fragments drop in camps;
    /// once enough fragments are gathered the soul is absorbed, grants permanent stats and can be equipped.
    /// </summary>
    public class SoulInventory : MonoBehaviour
    {
        public const int Slots = 3;

        [Serializable]
        class SaveData
        {
            public List<string> fragmentIds = new List<string>();
            public List<int> fragmentCounts = new List<int>();
            public List<string> unlocked = new List<string>();
            public string[] equipped = new string[Slots];
            public int activeSlot;
            public int kills;
            public int bossKills;
        }

        readonly Dictionary<string, int> _fragments = new Dictionary<string, int>();
        readonly HashSet<string> _unlocked = new HashSet<string>();
        public string[] equipped = new string[Slots];
        public int activeSlot;
        public int kills;
        public int bossKills;

        public event Action Changed;
        public event Action<SoulDef> SoulAbsorbed;
        public event Action<SoulDef, int> FragmentsGained;

        static string SavePath => Path.Combine(Application.persistentDataPath, "animesouls_save.json");

        public int UnlockedCount => _unlocked.Count;
        public bool IsUnlocked(SoulDef s) => s != null && _unlocked.Contains(s.id);
        public int Fragments(SoulDef s) => s != null && _fragments.TryGetValue(s.id, out var n) ? n : 0;

        public SoulDef ActiveSoul => AnimeDatabase.Get(equipped[Mathf.Clamp(activeSlot, 0, Slots - 1)]);
        public SoulDef InSlot(int i) => i >= 0 && i < Slots ? AnimeDatabase.Get(equipped[i]) : null;

        /// <summary>Damage multiplier from every absorbed soul (not only equipped ones).</summary>
        public float PowerMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var id in _unlocked)
                {
                    var s = AnimeDatabase.Get(id);
                    if (s != null) m += s.PowerValue;
                }
                return m;
            }
        }

        public float HpBonus
        {
            get
            {
                float hp = 0f;
                foreach (var id in _unlocked)
                {
                    var s = AnimeDatabase.Get(id);
                    if (s != null) hp += s.HpBonus;
                }
                return hp;
            }
        }

        /// <summary>A single headline number for the HUD.</summary>
        public int SoulPower => Mathf.RoundToInt(PowerMultiplier * 100f + HpBonus * 0.5f);

        public void AddFragments(SoulDef s, int amount)
        {
            if (s == null || amount <= 0) return;
            if (IsUnlocked(s))
            {
                FragmentsGained?.Invoke(s, 0);
                return;
            }
            int n = Fragments(s) + amount;
            _fragments[s.id] = n;
            FragmentsGained?.Invoke(s, amount);
            if (n >= s.FragmentsRequired) Absorb(s);
            else Changed?.Invoke();
        }

        public void Absorb(SoulDef s)
        {
            if (s == null || IsUnlocked(s)) return;
            _unlocked.Add(s.id);
            _fragments[s.id] = s.FragmentsRequired;
            for (int i = 0; i < Slots; i++)
            {
                if (string.IsNullOrEmpty(equipped[i]))
                {
                    equipped[i] = s.id;
                    break;
                }
            }
            SoulAbsorbed?.Invoke(s);
            Changed?.Invoke();
            Save();
        }

        public void Equip(SoulDef s, int slot)
        {
            if (!IsUnlocked(s) || slot < 0 || slot >= Slots) return;
            for (int i = 0; i < Slots; i++)
                if (equipped[i] == s.id) equipped[i] = equipped[slot];
            equipped[slot] = s.id;
            Changed?.Invoke();
            Save();
        }

        public void SetActiveSlot(int slot)
        {
            activeSlot = Mathf.Clamp(slot, 0, Slots - 1);
            Changed?.Invoke();
        }

        public void RegisterKill(bool boss)
        {
            kills++;
            if (boss) bossKills++;
        }

        public void Save()
        {
            try
            {
                var d = new SaveData { equipped = (string[])equipped.Clone(), activeSlot = activeSlot, kills = kills, bossKills = bossKills };
                foreach (var kv in _fragments) { d.fragmentIds.Add(kv.Key); d.fragmentCounts.Add(kv.Value); }
                d.unlocked.AddRange(_unlocked);
                File.WriteAllText(SavePath, JsonUtility.ToJson(d, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AnimeSouls] Save failed: " + e.Message);
            }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (d == null) return;
                _fragments.Clear();
                _unlocked.Clear();
                for (int i = 0; i < d.fragmentIds.Count && i < d.fragmentCounts.Count; i++)
                    if (AnimeDatabase.Get(d.fragmentIds[i]) != null) _fragments[d.fragmentIds[i]] = d.fragmentCounts[i];
                foreach (var id in d.unlocked)
                    if (AnimeDatabase.Get(id) != null) _unlocked.Add(id);
                equipped = new string[Slots];
                if (d.equipped != null)
                    for (int i = 0; i < Slots && i < d.equipped.Length; i++)
                        equipped[i] = _unlocked.Contains(d.equipped[i] ?? "") ? d.equipped[i] : null;
                activeSlot = Mathf.Clamp(d.activeSlot, 0, Slots - 1);
                kills = d.kills;
                bossKills = d.bossKills;
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AnimeSouls] Load failed: " + e.Message);
            }
        }

        public void ResetProgress()
        {
            _fragments.Clear();
            _unlocked.Clear();
            equipped = new string[Slots];
            activeSlot = 0;
            kills = 0;
            bossKills = 0;
            Changed?.Invoke();
            Save();
        }

        void OnApplicationQuit() { Save(); }
    }
}
