using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Visual body of a character plus its procedural animation (walk cycle, attacks, casting, stun, death).
    /// Lives on the "Model" child; the parent object handles movement and facing.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        public Transform hips, torso, head, armL, armR, legL, legR;
        public Transform legBL, legBR; // beasts only (front legs use legL/legR)
        public Transform handR;
        public bool beast;
        public float baseScale = 1f;
        public float height = 1.8f;

        [HideInInspector] public float moveAmount;   // 0 idle .. 1 run, set by the controller
        [HideInInspector] public bool grounded = true;
        [HideInInspector] public bool stunned;

        public readonly List<Renderer> renderers = new List<Renderer>();
        public Material hairMaterial;
        Color _hairBase;

        float _scaleMul = 1f, _targetScaleMul = 1f;
        float _phase;
        float _attackT = -1f, _attackDur = 0.25f;
        int _attackKind;
        float _flashT;
        bool _dead;
        float _deadT;
        MaterialPropertyBlock _mpb;

        public float CurrentScale => baseScale * _scaleMul;

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        public void RegisterHair(Material m)
        {
            hairMaterial = m;
            _hairBase = m.GetColor("_Color");
        }

        public void SetHairTint(Color? tint)
        {
            if (hairMaterial == null) return;
            hairMaterial.SetColor("_Color", tint ?? _hairBase);
            if (hairMaterial.HasProperty("_Emission")) hairMaterial.SetFloat("_Emission", tint.HasValue ? 0.35f : 0f);
        }

        public void SetScaleMultiplier(float m) { _targetScaleMul = m; }

        /// <summary>0 = right punch, 1 = left punch, 2 = two-hand cast, 3 = overhead slam, 4 = sword swing.</summary>
        public void PlayAttack(int kind, float duration = 0.25f)
        {
            _attackKind = kind;
            _attackDur = Mathf.Max(0.05f, duration);
            _attackT = 0f;
        }

        public void Flash()
        {
            _flashT = 0.09f;
            ApplyFlash(true);
        }

        void ApplyFlash(bool on)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (on)
                {
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor("_Color", Color.white);
                    _mpb.SetFloat("_Emission", 0.9f);
                    r.SetPropertyBlock(_mpb);
                }
                else r.SetPropertyBlock(null);
            }
        }

        void OnDestroy()
        {
            // Hair uses a per-character material instance (so transformations can tint it).
            if (hairMaterial != null) Destroy(hairMaterial);
        }

        public void Die()
        {
            _dead = true;
            _deadT = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            _scaleMul = Mathf.MoveTowards(_scaleMul, _targetScaleMul, dt * 2.5f);
            transform.localScale = Vector3.one * CurrentScale;

            if (_flashT > 0f)
            {
                _flashT -= dt;
                if (_flashT <= 0f) ApplyFlash(false);
            }

            if (_dead)
            {
                _deadT += dt;
                float k = Mathf.Clamp01(_deadT / 0.5f);
                transform.localRotation = Quaternion.Euler(-85f * k, 0, 0);
                transform.localPosition = new Vector3(0, -Mathf.Max(0, _deadT - 1.2f) * 0.8f, 0);
                return;
            }

            float move = stunned ? 0f : moveAmount;
            _phase += dt * Mathf.Lerp(2f, 11f, Mathf.Clamp01(move));
            float swing = Mathf.Sin(_phase);
            float amount = Mathf.Clamp01(move) * (grounded ? 1f : 0.3f);

            if (beast)
            {
                float a = 35f * amount;
                if (legL) legL.localRotation = Quaternion.Euler(swing * a, 0, 0);
                if (legBR) legBR.localRotation = Quaternion.Euler(swing * a, 0, 0);
                if (legR) legR.localRotation = Quaternion.Euler(-swing * a, 0, 0);
                if (legBL) legBL.localRotation = Quaternion.Euler(-swing * a, 0, 0);
                if (hips) hips.localPosition = Vector3.up * Mathf.Abs(swing) * 0.05f * amount;
                if (head) head.localRotation = Quaternion.Euler(Mathf.Sin(_phase * 0.5f) * 4f + (_attackT >= 0 ? -25f : 0f), 0, 0);
                if (_attackT >= 0f)
                {
                    _attackT += dt;
                    if (_attackT > _attackDur) _attackT = -1f;
                }
                return;
            }

            float legA = 38f * amount;
            if (legL) legL.localRotation = Quaternion.Euler(swing * legA, 0, 0);
            if (legR) legR.localRotation = Quaternion.Euler(-swing * legA, 0, 0);

            float armA = 32f * amount;
            var armLRot = Quaternion.Euler(-swing * armA, 0, -6f);
            var armRRot = Quaternion.Euler(swing * armA, 0, 6f);
            if (!grounded)
            {
                armLRot = Quaternion.Euler(-30f, 0, -35f);
                armRRot = Quaternion.Euler(-30f, 0, 35f);
            }

            if (_attackT >= 0f)
            {
                _attackT += dt;
                float k = Mathf.Clamp01(_attackT / _attackDur);
                float strike = k < 0.35f ? k / 0.35f : 1f - (k - 0.35f) / 0.65f;
                switch (_attackKind)
                {
                    case 0: armRRot = Quaternion.Slerp(armRRot, Quaternion.Euler(-95f, 0, -10f), strike); break;
                    case 1: armLRot = Quaternion.Slerp(armLRot, Quaternion.Euler(-95f, 0, 10f), strike); break;
                    case 2:
                        armLRot = Quaternion.Slerp(armLRot, Quaternion.Euler(-90f, 0, 15f), strike);
                        armRRot = Quaternion.Slerp(armRRot, Quaternion.Euler(-90f, 0, -15f), strike);
                        break;
                    case 3:
                        armLRot = Quaternion.Slerp(armLRot, Quaternion.Euler(-170f + 120f * k, 0, 0), strike);
                        armRRot = Quaternion.Slerp(armRRot, Quaternion.Euler(-170f + 120f * k, 0, 0), strike);
                        break;
                    case 4:
                        armRRot = Quaternion.Euler(Mathf.Lerp(-150f, -20f, k), Mathf.Lerp(-40f, 40f, k), 30f);
                        break;
                }
                if (torso && _attackKind != 2) torso.localRotation = Quaternion.Euler(0, Mathf.Sin(k * Mathf.PI) * (_attackKind == 1 ? 20f : -20f), 0);
                if (_attackT > _attackDur)
                {
                    _attackT = -1f;
                    if (torso) torso.localRotation = Quaternion.identity;
                }
            }

            if (armL) armL.localRotation = Quaternion.Slerp(armL.localRotation, armLRot, 1f - Mathf.Exp(-dt * 30f));
            if (armR) armR.localRotation = Quaternion.Slerp(armR.localRotation, armRRot, 1f - Mathf.Exp(-dt * 30f));

            float bob = grounded ? Mathf.Abs(Mathf.Sin(_phase)) * 0.06f * amount + Mathf.Sin(Time.time * 2f) * 0.01f : 0f;
            if (hips) hips.localPosition = new Vector3(0, bob, 0);

            if (head)
            {
                head.localRotation = stunned
                    ? Quaternion.Euler(10f, Mathf.Sin(Time.time * 6f) * 20f, Mathf.Sin(Time.time * 6f) * 10f)
                    : Quaternion.Euler(Mathf.Sin(Time.time * 1.7f) * 2f, 0, 0);
            }
        }
    }
}
