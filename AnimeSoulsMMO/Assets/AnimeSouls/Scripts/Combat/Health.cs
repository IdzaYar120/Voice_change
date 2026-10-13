using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>Hit points, shields and status effects (stun, burn, slow, knockback) for anything that can fight.</summary>
    public class Health : MonoBehaviour
    {
        public static readonly List<Health> All = new List<Health>();

        /// <summary>victim, amount, colour, isCritical-ish (big hit)</summary>
        public static event Action<Health, float, Color, bool> AnyDamaged;
        /// <summary>victim, killer</summary>
        public static event Action<Health, Health> AnyDied;

        public Team team;
        public string displayName = "???";
        public int level = 1;
        public bool isBoss;
        public bool isPlayer;
        public float maxHp = 100f;
        public float hp = 100f;
        public float damageTakenMultiplier = 1f;
        public CharacterRig rig;

        public float shield;
        float _shieldEnd;
        GameObject _shieldFx;

        public float stunTimer;
        float _slowTimer, _slowFactor = 1f;
        float _burnTimer, _burnDps, _burnTick;
        Health _burnSource;
        public Vector3 knockback;

        public bool IsDead { get; private set; }
        public bool IsStunned => stunTimer > 0f;
        public float SpeedFactor => _slowTimer > 0f ? _slowFactor : 1f;
        public float Height => rig != null ? rig.height * (rig.CurrentScale / Mathf.Max(0.01f, rig.baseScale)) : 2f;
        public Vector3 Center => transform.position + Vector3.up * Height * 0.5f;

        public event Action<Health> OnDeath;
        public event Action<float, Health> OnHit;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Init(float max)
        {
            maxHp = max;
            hp = max;
            IsDead = false;
            shield = 0f;
            stunTimer = 0f;
            _burnTimer = 0f;
            _slowTimer = 0f;
            knockback = Vector3.zero;
        }

        public void Revive(float fraction = 1f)
        {
            IsDead = false;
            hp = maxHp * fraction;
            stunTimer = 0f;
            _burnTimer = 0f;
            _slowTimer = 0f;
            knockback = Vector3.zero;
        }

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Fire: return Hex.C("#FF8A3C");
                case Element.Lightning: return Hex.C("#FFF27A");
                case Element.Water: return Hex.C("#4FB4FF");
                case Element.Ice: return Hex.C("#BFEFFF");
                case Element.Dark: return Hex.C("#C08BFF");
                case Element.Light: return Hex.C("#FFF0A0");
                case Element.Earth: return Hex.C("#D9B77A");
                case Element.Spirit: return Hex.C("#8FE8FF");
                case Element.Blood: return Hex.C("#FF4F7A");
                case Element.Wind: return Hex.C("#B8FFD8");
                default: return Color.white;
            }
        }

        public void TakeDamage(float amount, Health source, Element element, Vector3 push)
        {
            if (IsDead || amount <= 0f) return;
            if (source != null && source.team == team) return;

            amount *= damageTakenMultiplier;
            float total = amount;
            if (shield > 0f)
            {
                float absorbed = Mathf.Min(shield, amount);
                shield -= absorbed;
                amount -= absorbed;
                if (shield <= 0f) BreakShield();
            }

            hp -= amount;
            knockback += isBoss ? push * 0.3f : push;
            if (source != null) _burnSource = source;

            switch (element)
            {
                case Element.Fire:
                    _burnTimer = 3f;
                    _burnDps = Mathf.Max(_burnDps * 0.5f, total * 0.08f);
                    break;
                case Element.Ice:
                    _slowTimer = 2.5f;
                    _slowFactor = 0.55f;
                    break;
            }

            if (rig != null) rig.Flash();
            AnyDamaged?.Invoke(this, total, ElementColor(element), total >= maxHp * 0.25f || total >= 120f);
            OnHit?.Invoke(total, source);
            if (hp <= 0f) Die(source);
        }

        public void ApplyStun(float seconds)
        {
            if (IsDead || seconds <= 0f) return;
            if (isBoss) seconds *= 0.4f;
            stunTimer = Mathf.Max(stunTimer, seconds);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            hp = Mathf.Min(maxHp, hp + amount);
            AnyDamaged?.Invoke(this, -amount, Hex.C("#8CFFB0"), false);
        }

        public void AddShield(float amount, float duration, Color color)
        {
            shield = Mathf.Max(shield, amount);
            _shieldEnd = Time.time + duration;
            if (_shieldFx != null) Destroy(_shieldFx);
            var mat = Mats.GlowInstance(color.WithAlpha(0.28f));
            _shieldFx = MeshFactory.Part("Shield", MeshFactory.LowSphere, mat, transform, Vector3.up * Height * 0.5f, Vector3.one * Height * 1.3f, null, false);
            _shieldFx.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 60, 0);
        }

        void BreakShield()
        {
            shield = 0f;
            if (_shieldFx != null)
            {
                VFX.Burst(_shieldFx.transform.position, Height * 0.7f, Hex.C("#DDF6FF"), 0.3f);
                var r = _shieldFx.GetComponent<Renderer>();
                if (r != null) Destroy(r.sharedMaterial);
                Destroy(_shieldFx);
            }
        }

        void Die(Health killer)
        {
            if (IsDead) return;
            IsDead = true;
            hp = 0f;
            BreakShield();
            if (rig != null) rig.Die();
            OnDeath?.Invoke(this);
            AnyDied?.Invoke(this, killer);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (stunTimer > 0f) stunTimer -= dt;
            if (_slowTimer > 0f) _slowTimer -= dt;
            if (shield > 0f && Time.time > _shieldEnd) BreakShield();
            if (rig != null) rig.stunned = IsStunned && !IsDead;

            if (_burnTimer > 0f && !IsDead)
            {
                _burnTimer -= dt;
                _burnTick -= dt;
                if (_burnTick <= 0f)
                {
                    _burnTick = 0.5f;
                    VFX.Particles(Center, Hex.C("#FF7A1A"), 5, 1.5f, 0.3f, 0.4f, -0.3f);
                    TakeDamage(_burnDps * 0.5f, _burnSource, Element.Physical, Vector3.zero);
                }
            }

            knockback = Vector3.MoveTowards(knockback, Vector3.zero, (12f + knockback.magnitude * 3f) * dt);
        }
    }
}
