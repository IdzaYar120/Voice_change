using System.Collections;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Camp monster brain: wander -> aggro -> chase -> wind-up attack -> leash back home.
    /// Bosses ("echoes" of a soul) also cast that soul's real skills.
    /// </summary>
    public class MobAI : MonoBehaviour, IMovable
    {
        public MobCamp camp;
        public MobDef def;
        public SoulDef bossSoul;
        public Health health;
        public SkillCaster caster;

        Vector3 _home;
        Vector3 _wanderTarget;
        float _wanderTimer;
        Health _target;
        float _attackReady;
        float _skillReady;
        bool _returning;
        bool _attacking;
        float _damage;
        float _speed;
        float _reach;
        float _aggroRange;
        float _leash;

        public bool IsBoss => bossSoul != null;

        public void Setup(MobCamp owner, Vector3 home, float damage, float speed, float reach)
        {
            camp = owner;
            _home = home;
            _damage = damage;
            _speed = speed;
            _reach = reach;
            _aggroRange = IsBoss ? 22f : 14f;
            _leash = owner.anime.campRadius + 22f;
            _wanderTarget = home;
            _attackReady = Time.time + Random.Range(0.5f, 1.5f);
            _skillReady = Time.time + 2f;
            health.OnHit += OnHit;
        }

        void OnHit(float amount, Health source)
        {
            if (source != null && !source.IsDead && !_returning && source.team != health.team) _target = source;
        }

        public void ForceMove(Vector3 delta) { transform.position = World.OnGround(transform.position + delta); }
        public void Teleport(Vector3 position) { transform.position = World.OnGround(position); }

        void Update()
        {
            if (health.IsDead) return;
            var rig = health.rig;
            float dt = Time.deltaTime;

            if (health.knockback.sqrMagnitude > 0.01f)
            {
                var kb = health.knockback;
                kb.y = 0f;
                transform.position = World.OnGround(transform.position + kb * dt);
            }

            if (health.IsStunned || caster.IsRooted)
            {
                if (rig != null) rig.moveAmount = 0f;
                return;
            }

            float distHome = Flat(transform.position - _home).magnitude;
            if (!_returning && distHome > _leash) { _returning = true; _target = null; }

            if (_returning)
            {
                MoveTowards(_home, _speed * 1.6f, 1f, rig);
                health.hp = Mathf.Min(health.maxHp, health.hp + health.maxHp * 0.25f * dt);
                if (distHome < 3f) _returning = false;
                return;
            }

            if (_target == null || _target.IsDead || Flat(_target.transform.position - _home).magnitude > _leash)
                _target = Acquire();

            if (_target == null)
            {
                Wander(rig, dt);
                return;
            }

            float stop = _reach * 0.8f + SkillEffects.Radius(_target);
            float dist = Flat(_target.transform.position - transform.position).magnitude;

            if (IsBoss && Time.time > _skillReady) TryBossSkill(dist);

            if (_attacking)
            {
                if (rig != null) rig.moveAmount = 0f;
                Face(_target.transform.position);
                return;
            }

            if (dist > stop) MoveTowards(_target.transform.position, _speed * health.SpeedFactor, stop, rig);
            else
            {
                if (rig != null) rig.moveAmount = 0f;
                Face(_target.transform.position);
                if (Time.time > _attackReady) StartCoroutine(Attack(_target));
            }
        }

        Health Acquire()
        {
            Health best = null;
            float bestD = _aggroRange;
            foreach (var h in Health.All)
            {
                if (!SkillEffects.IsEnemy(h, health.team)) continue;
                float d = Vector3.Distance(h.transform.position, transform.position);
                if (d < bestD && Flat(h.transform.position - _home).magnitude < _leash) { bestD = d; best = h; }
            }
            return best;
        }

        void Wander(CharacterRig rig, float dt)
        {
            _wanderTimer -= dt;
            if (_wanderTimer <= 0f)
            {
                _wanderTimer = Random.Range(3f, 7f);
                var r = Random.insideUnitCircle * camp.anime.campRadius * 0.45f;
                _wanderTarget = camp.anime.campPosition + new Vector3(r.x, 0, r.y);
                if (IsBoss) _wanderTarget = camp.anime.campPosition + new Vector3(r.x, 0, r.y) * 0.2f;
            }
            MoveTowards(_wanderTarget, _speed * 0.35f, 0.8f, rig);
        }

        void MoveTowards(Vector3 goal, float speed, float stop, CharacterRig rig)
        {
            var to = Flat(goal - transform.position);
            float dist = to.magnitude;
            if (dist <= stop)
            {
                if (rig != null) rig.moveAmount = 0f;
                return;
            }
            var step = to.normalized * Mathf.Min(dist - stop, speed * Time.deltaTime);
            transform.position = World.OnGround(transform.position + step);
            Face(goal);
            if (rig != null) rig.moveAmount = Mathf.Clamp01(speed / 6f);
        }

        void Face(Vector3 p)
        {
            var d = Flat(p - transform.position);
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 10f);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        IEnumerator Attack(Health target)
        {
            _attacking = true;
            float windup = IsBoss ? 0.45f : 0.55f;
            var rig = health.rig;
            bool beastAttack = def != null && def.look.beast;
            if (rig != null) rig.PlayAttack(beastAttack ? 0 : (Random.value < 0.5f ? 0 : 3), windup + 0.15f);
            // Telegraph: a small warning flash so players can react.
            VFX.Particles(transform.position + Vector3.up * health.Height * 0.6f + transform.forward * 0.6f, Hex.C("#FF5A4A"), 6, 1.5f, 0.35f, windup);
            yield return new WaitForSeconds(windup);
            if (!health.IsDead && !health.IsStunned && target != null && !target.IsDead)
            {
                float dist = Flat(target.transform.position - transform.position).magnitude;
                if (dist <= _reach + SkillEffects.Radius(target) + 0.6f)
                {
                    target.TakeDamage(_damage, health, Element.Physical, Flat(target.transform.position - transform.position).normalized * 4f);
                    VFX.Particles(target.Center, Color.white, 8, 4f, 0.3f, 0.25f);
                }
            }
            _attackReady = Time.time + (IsBoss ? 1.2f : Random.Range(1.4f, 2.0f));
            _attacking = false;
        }

        void TryBossSkill(float dist)
        {
            _skillReady = Time.time + Random.Range(2.5f, 4.5f);
            var skills = bossSoul.skills;
            int start = Random.Range(0, skills.Length);
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[(start + i) % skills.Length];
                if (!caster.CanCast(s)) continue;
                if (!SkillUsable(s, dist)) continue;
                caster.TryCast(s, _target.transform.position);
                return;
            }
        }

        bool SkillUsable(SkillDef s, float dist)
        {
            switch (s.type)
            {
                case SkillType.Heal: return health.hp < health.maxHp * 0.6f;
                case SkillType.Shield: return health.hp < health.maxHp * 0.8f;
                case SkillType.Buff: return dist < 12f;
                case SkillType.Clones:
                case SkillType.Summon: return dist < 18f;
                case SkillType.Nova: return dist < s.radius + 1f;
                case SkillType.Slash: return dist < s.range + 1f;
                case SkillType.Dash: return dist < s.range + 2f;
                case SkillType.StretchPunch: return dist < s.range;
                case SkillType.Teleport: return dist > 6f;
                default: return dist < Mathf.Max(8f, s.range);
            }
        }
    }
}
