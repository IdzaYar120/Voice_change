using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// A themed camp: keeps its mobs populated, rotates a boss "echo" through the anime's five souls and
    /// drops soul fragments on kills.
    /// </summary>
    public class MobCamp : MonoBehaviour
    {
        public AnimeDef anime;
        public readonly List<MobAI> mobs = new List<MobAI>();
        public MobAI boss;

        const float MobRespawn = 18f;
        const float BossRespawn = 75f;

        int _bossRotation;
        readonly List<float> _pendingMobs = new List<float>();
        float _bossRespawnAt = -1f;

        public int MobCount => 6 + anime.tier;
        public float TierScale => Mathf.Pow(1.6f, anime.tier - 1);
        public int BaseLevel => (anime.tier - 1) * 10 + 1;

        public static readonly List<MobCamp> All = new List<MobCamp>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Init(AnimeDef a)
        {
            anime = a;
            _bossRotation = a.index % a.souls.Length;
            for (int i = 0; i < MobCount; i++) SpawnMob();
            SpawnBoss();
        }

        void Update()
        {
            for (int i = _pendingMobs.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _pendingMobs[i])
                {
                    _pendingMobs.RemoveAt(i);
                    SpawnMob();
                }
            }
            if (_bossRespawnAt > 0f && Time.time >= _bossRespawnAt)
            {
                _bossRespawnAt = -1f;
                SpawnBoss();
            }
        }

        public SoulDef NextBossSoul => anime.souls[_bossRotation % anime.souls.Length];
        public float BossRespawnIn => _bossRespawnAt > 0f ? _bossRespawnAt - Time.time : 0f;

        Vector3 RandomSpot(float minR, float maxR)
        {
            var dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            var p = anime.campPosition + new Vector3(dir.x, 0, dir.y) * Random.Range(minR, maxR);
            return World.OnGround(p);
        }

        void SpawnMob()
        {
            var def = anime.mobs[Random.Range(0, anime.mobs.Length)];
            var pos = RandomSpot(6f, anime.campRadius * 0.5f);
            var go = new GameObject(def.name);
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            var health = go.AddComponent<Health>();
            health.team = Team.Enemy;
            health.displayName = def.name;
            health.level = BaseLevel + Random.Range(0, 5);
            health.Init(70f * TierScale * def.hpMul);
            health.rig = CharacterBuilder.Build(def.look, go.transform);

            var caster = go.AddComponent<SkillCaster>();
            caster.unlimitedEnergy = true;
            caster.appearance = def.look;

            var ai = go.AddComponent<MobAI>();
            ai.def = def;
            ai.health = health;
            ai.caster = caster;
            caster.mover = ai;
            ai.Setup(this, pos, 9f * TierScale * def.dmgMul, def.speed, def.reach * Mathf.Max(1f, def.look.scale * 0.8f));
            mobs.Add(ai);
            health.OnDeath += h => OnMobDied(ai);
            VFX.Smoke(pos, def.look.scale);
        }

        void SpawnBoss()
        {
            var soul = NextBossSoul;
            _bossRotation++;
            var look = soul.look.Clone();
            look.scale *= 1.45f;
            var pos = World.OnGround(anime.campPosition);

            var go = new GameObject("Boss_" + soul.name);
            go.transform.SetParent(transform, true);
            go.transform.position = pos;

            var health = go.AddComponent<Health>();
            health.team = Team.Enemy;
            health.isBoss = true;
            health.displayName = "Відлуння: " + soul.name;
            health.level = anime.tier * 10 + 2;
            health.Init(70f * TierScale * 9f);
            health.rig = CharacterBuilder.Build(look, go.transform);

            var caster = go.AddComponent<SkillCaster>();
            caster.unlimitedEnergy = true;
            caster.appearance = soul.look;
            caster.cooldownMultiplier = 1.6f;
            caster.baseDamageMultiplier = 0.32f * TierScale;

            var ai = go.AddComponent<MobAI>();
            ai.bossSoul = soul;
            ai.health = health;
            ai.caster = caster;
            caster.mover = ai;
            ai.Setup(this, pos, 14f * TierScale, 4.2f, 2.6f);
            boss = ai;
            health.OnDeath += h => OnBossDied(ai);

            VFX.Burst(pos + Vector3.up * 2f, 5f, soul.signature, 0.8f);
            VFX.Aura(go.transform, soul.signature, 1.2f, 1.5f, 20f);
        }

        void OnMobDied(MobAI ai)
        {
            mobs.Remove(ai);
            _pendingMobs.Add(Time.time + MobRespawn);
            var soul = RollSoul();
            SoulOrb.Spawn(ai.transform.position + Vector3.up * 1.2f, soul, 1);
            Destroy(ai.gameObject, 2.5f);
        }

        void OnBossDied(MobAI ai)
        {
            boss = null;
            _bossRespawnAt = Time.time + BossRespawn;
            SoulOrb.Spawn(ai.transform.position + Vector3.up * 1.5f, ai.bossSoul, 8);
            // A bonus fragment of another soul from this anime.
            SoulOrb.Spawn(ai.transform.position + Vector3.up * 1.5f + Random.insideUnitSphere, RollSoul(), 2);
            VFX.Burst(ai.transform.position + Vector3.up * 2f, 7f, ai.bossSoul.signature, 0.9f);
            Destroy(ai.gameObject, 3f);
        }

        /// <summary>Weighted random soul from this camp: rarer souls drop less often.</summary>
        SoulDef RollSoul()
        {
            float total = 0f;
            foreach (var s in anime.souls) total += Weight(s);
            float r = Random.value * total;
            foreach (var s in anime.souls)
            {
                r -= Weight(s);
                if (r <= 0f) return s;
            }
            return anime.souls[0];
        }

        static float Weight(SoulDef s)
        {
            switch (s.rarity)
            {
                case Rarity.Legendary: return 1f;
                case Rarity.Epic: return 2.2f;
                default: return 3.5f;
            }
        }
    }
}
