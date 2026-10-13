using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    public interface IMovable
    {
        void ForceMove(Vector3 delta);
        void Teleport(Vector3 position);
    }

    /// <summary>
    /// Energy, cooldowns and transformations for any fighter. Players, bosses and clones all cast through this,
    /// so every soul skill works the same no matter who uses it.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class SkillCaster : MonoBehaviour
    {
        public Health health;
        public Appearance appearance;
        public float baseDamageMultiplier = 1f;
        public float cooldownMultiplier = 1f;
        public float maxEnergy = 100f;
        public float energy = 100f;
        public float energyRegen = 10f;
        public bool unlimitedEnergy;

        public IMovable mover;
        public float rootedUntil;
        public float faceUntil;
        public Vector3 faceDirection = Vector3.forward;

        readonly Dictionary<SkillDef, float> _readyAt = new Dictionary<SkillDef, float>();
        float _basicReadyAt;
        int _basicCombo;

        SkillDef _buff;
        float _buffEnd;
        GameObject _aura;

        public event Action<SkillDef> OnCast;

        public CharacterRig Rig => health != null ? health.rig : null;
        public bool IsRooted => Time.time < rootedUntil;
        public bool BuffActive => _buff != null && Time.time < _buffEnd;
        public SkillDef ActiveBuff => BuffActive ? _buff : null;
        public float BuffRemaining => BuffActive ? _buffEnd - Time.time : 0f;
        public float DamageMultiplier => baseDamageMultiplier * (BuffActive ? _buff.buffDamage : 1f);
        public float SpeedMultiplier => BuffActive ? _buff.buffSpeed : 1f;
        public float Scale => Rig != null ? Rig.CurrentScale : 1f;

        void Awake()
        {
            health = GetComponent<Health>();
            mover = GetComponent<IMovable>();
        }

        void Update()
        {
            if (!unlimitedEnergy && energy < maxEnergy) energy = Mathf.Min(maxEnergy, energy + energyRegen * Time.deltaTime);
            if (_buff != null && Time.time >= _buffEnd) EndBuff();
        }

        public Vector3 Chest => transform.position + Vector3.up * 1.3f * Scale;

        public float CooldownRemaining(SkillDef s)
        {
            return s != null && _readyAt.TryGetValue(s, out var t) ? Mathf.Max(0f, t - Time.time) : 0f;
        }

        public float CooldownFraction(SkillDef s)
        {
            if (s == null) return 0f;
            float total = s.cooldown * cooldownMultiplier;
            return total <= 0f ? 0f : CooldownRemaining(s) / total;
        }

        public bool CanCast(SkillDef s)
        {
            if (s == null || health == null || health.IsDead || health.IsStunned) return false;
            if (CooldownRemaining(s) > 0f) return false;
            if (!unlimitedEnergy && energy < s.EnergyCost) return false;
            return true;
        }

        public void ResetCooldowns()
        {
            _readyAt.Clear();
        }

        public bool TryCast(SkillDef s, Vector3 aimPoint)
        {
            if (!CanCast(s)) return false;
            if (!unlimitedEnergy) energy -= s.EnergyCost;
            _readyAt[s] = Time.time + s.cooldown * cooldownMultiplier;
            FaceTowards(aimPoint, 0.5f);
            SkillEffects.Cast(this, s, aimPoint);
            OnCast?.Invoke(s);
            return true;
        }

        public bool TryBasicAttack(Vector3 aimPoint, float damage, Color color, Element element, bool sword)
        {
            if (health == null || health.IsDead || health.IsStunned || Time.time < _basicReadyAt) return false;
            _basicReadyAt = Time.time + (sword ? 0.5f : 0.4f) / Mathf.Max(0.5f, SpeedMultiplier);
            FaceTowards(aimPoint, 0.35f);
            _basicCombo++;
            SkillEffects.BasicAttack(this, damage, color, element, sword, _basicCombo);
            return true;
        }

        public void FaceTowards(Vector3 point, float seconds)
        {
            var d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            faceDirection = d.normalized;
            faceUntil = Time.time + seconds;
            transform.rotation = Quaternion.LookRotation(faceDirection);
        }

        public void ApplyBuff(SkillDef s)
        {
            EndBuff();
            _buff = s;
            _buffEnd = Time.time + s.duration;
            _aura = VFX.Aura(transform, s.color, 0.6f * Scale, 1f * Scale, 50f);
            if (Rig != null)
            {
                if (s.hasTint) Rig.SetHairTint(s.tint);
                Rig.SetScaleMultiplier(s.buffScale);
            }
        }

        public void EndBuff()
        {
            if (_buff == null) return;
            _buff = null;
            if (_aura != null) Destroy(_aura);
            if (Rig != null)
            {
                Rig.SetHairTint(null);
                Rig.SetScaleMultiplier(1f);
            }
        }

        /// <summary>Called after the visual model is rebuilt (soul swap) so active effects re-attach.</summary>
        public void OnRigRebuilt()
        {
            if (_buff != null && Rig != null)
            {
                if (_buff.hasTint) Rig.SetHairTint(_buff.tint);
                Rig.SetScaleMultiplier(_buff.buffScale);
            }
        }
    }
}
