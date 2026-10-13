using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Implementation of every skill archetype. Skills are pure data (SkillDef); this class turns them into
    /// hits, movement and visuals. Hit tests use Health.All instead of physics so props never block combat.
    /// </summary>
    public static class SkillEffects
    {
        public const float TickInterval = 0.25f;

        // ------------------------------------------------------------------ targeting

        public static float Radius(Health h) => 0.45f * (h.rig != null ? h.rig.CurrentScale : 1f);

        public static float DistanceTo(Health h, Vector3 p)
        {
            var b = h.transform.position;
            float y = Mathf.Clamp(p.y, b.y, b.y + h.Height);
            return Vector3.Distance(p, new Vector3(b.x, y, b.z)) - Radius(h);
        }

        public static bool IsEnemy(Health h, Team team) => h != null && !h.IsDead && h.team != team;

        public static List<Health> InSphere(Vector3 c, float r, Team team)
        {
            var list = new List<Health>();
            foreach (var h in Health.All)
                if (IsEnemy(h, team) && DistanceTo(h, c) <= r) list.Add(h);
            return list;
        }

        public static List<Health> InCone(Vector3 origin, Vector3 fwd, float range, float angle, Team team)
        {
            var list = new List<Health>();
            fwd.y = 0f;
            fwd.Normalize();
            foreach (var h in Health.All)
            {
                if (!IsEnemy(h, team)) continue;
                if (DistanceTo(h, origin) > range) continue;
                var to = h.transform.position - origin;
                to.y = 0f;
                if (angle < 359f && to.sqrMagnitude > 0.25f && Vector3.Angle(fwd, to) > angle * 0.5f) continue;
                list.Add(h);
            }
            return list;
        }

        public static List<Health> AlongLine(Vector3 from, Vector3 dir, float length, float width, Team team)
        {
            var hits = new List<KeyValuePair<float, Health>>();
            dir.Normalize();
            foreach (var h in Health.All)
            {
                if (!IsEnemy(h, team)) continue;
                float t = Mathf.Clamp(Vector3.Dot(h.Center - from, dir), 0f, length);
                if (DistanceTo(h, from + dir * t) <= width) hits.Add(new KeyValuePair<float, Health>(t, h));
            }
            hits.Sort((a, b) => a.Key.CompareTo(b.Key));
            var list = new List<Health>(hits.Count);
            foreach (var kv in hits) list.Add(kv.Value);
            return list;
        }

        public static Health Nearest(Vector3 p, float maxDist, Team team)
        {
            Health best = null;
            float bestD = maxDist;
            foreach (var h in Health.All)
            {
                if (!IsEnemy(h, team)) continue;
                float d = Vector3.Distance(h.transform.position, p);
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward; }

        static Vector3 AimDir(SkillCaster c, Vector3 aim)
        {
            var d = aim - c.transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.25f ? d.normalized : c.transform.forward;
        }

        static Vector3 ClampToRange(SkillCaster c, Vector3 aim, float range)
        {
            var o = c.transform.position;
            var d = aim - o;
            d.y = 0f;
            if (d.magnitude > range) d = d.normalized * range;
            return World.OnGround(o + d);
        }

        public static void Hit(SkillCaster c, Health target, float baseDamage, SkillDef s, Vector3 pushDir)
        {
            if (target == null || target.IsDead) return;
            float dmg = baseDamage * (c != null ? c.DamageMultiplier : 1f);
            var src = c != null ? c.health : null;
            target.TakeDamage(dmg, src, s != null ? s.element : Element.Physical, Flat(pushDir) * (s != null ? s.knockback : 3f) + Vector3.up * 0.5f);
            if (s != null && s.stun > 0f) target.ApplyStun(s.stun);
        }

        static void Mover(SkillCaster c, Vector3 delta)
        {
            if (c.mover != null) c.mover.ForceMove(delta);
            else c.transform.position = World.OnGround(c.transform.position + delta);
        }

        // ------------------------------------------------------------------ dispatch

        public static void Cast(SkillCaster c, SkillDef s, Vector3 aim)
        {
            var rig = c.Rig;
            switch (s.type)
            {
                case SkillType.Projectile:
                    rig?.PlayAttack(2, 0.3f);
                    Projectile.Spawn(c, s, c.Chest + AimDir(c, aim) * 0.8f, AimDir(c, aim));
                    break;
                case SkillType.MultiProjectile: c.StartCoroutine(MultiRoutine(c, s, aim)); break;
                case SkillType.Nova: rig?.PlayAttack(2, 0.35f); Nova(c, s, c.transform.position, s.radius, s.damage); break;
                case SkillType.GroundSlam: c.StartCoroutine(SlamRoutine(c, s, aim)); break;
                case SkillType.Dash: c.StartCoroutine(DashRoutine(c, s, aim)); break;
                case SkillType.Clones: SpawnMinions(c, s, false); break;
                case SkillType.Summon: SpawnMinions(c, s, true); break;
                case SkillType.Beam: c.StartCoroutine(BeamRoutine(c, s, aim)); break;
                case SkillType.StretchPunch: c.StartCoroutine(PunchRoutine(c, s, aim)); break;
                case SkillType.Slash: c.StartCoroutine(SlashRoutine(c, s, aim)); break;
                case SkillType.Buff:
                    rig?.PlayAttack(2, 0.5f);
                    c.ApplyBuff(s);
                    VFX.Burst(c.Chest, 2.5f * c.Scale, s.color, 0.5f);
                    if (s.damage > 0f) Nova(c, s, c.transform.position, s.radius, s.damage);
                    break;
                case SkillType.Heal:
                    rig?.PlayAttack(2, 0.4f);
                    c.health.Heal(s.damage * (0.5f + 0.5f * c.DamageMultiplier));
                    VFX.Particles(c.transform.position + Vector3.up, s.color, 30, 3f, 0.4f, 1f, -0.6f);
                    VFX.Ring(c.transform.position + Vector3.up * 0.1f, 2.5f, s.color, 0.6f);
                    break;
                case SkillType.Shield:
                    rig?.PlayAttack(2, 0.4f);
                    c.health.AddShield(s.damage * (0.5f + 0.5f * c.DamageMultiplier), s.duration, s.color);
                    break;
                case SkillType.Teleport: Teleport(c, s, aim); break;
                case SkillType.Rain: c.StartCoroutine(RainRoutine(c, s, aim)); break;
                case SkillType.Pull: c.StartCoroutine(PullRoutine(c, s, aim)); break;
            }
        }

        // ------------------------------------------------------------------ basic attack

        public static void BasicAttack(SkillCaster c, float damage, Color color, Element element, bool sword, int combo)
        {
            float range = sword ? 3.3f : 2.6f;
            var fwd = c.transform.forward;
            c.Rig?.PlayAttack(sword ? 4 : (combo % 2 == 0 ? 0 : 1), sword ? 0.3f : 0.22f);
            var origin = c.transform.position + Vector3.up * 1.1f * c.Scale;
            var targets = InCone(c.transform.position + Vector3.up * c.Scale, fwd, range * c.Scale, 120f, c.health.team);
            var s = new SkillDef { element = element, knockback = 2.5f };
            if (sword) VFX.Slash(origin + fwd * 0.5f, Quaternion.LookRotation(fwd) * Quaternion.Euler(0, 0, combo % 2 == 0 ? 25 : -25), range * c.Scale, 140f, color, 0.18f);
            foreach (var t in targets)
            {
                Hit(c, t, damage, s, t.transform.position - c.transform.position);
                VFX.Particles(t.Center, color, 8, 4f, 0.25f, 0.3f);
            }
            if (!sword && targets.Count == 0) VFX.Particles(origin + fwd * range * 0.6f, color.WithAlpha(0.6f), 4, 2f, 0.2f, 0.2f);
        }

        // ------------------------------------------------------------------ archetypes

        static void Nova(SkillCaster c, SkillDef s, Vector3 centre, float radius, float damage)
        {
            radius *= Mathf.Max(1f, c.Scale * 0.7f);
            var mid = centre + Vector3.up * 1f;
            VFX.Burst(mid, radius, s.color, 0.55f);
            if (radius >= 9f)
            {
                // Domain-sized burst: a big translucent dome.
                var mat = Mats.GlowInstance(s.color.WithAlpha(0.35f));
                mat.SetFloat("_Core", 0f);
                var dome = MeshFactory.Part("Domain", MeshFactory.Sphere, mat, null, Vector3.zero, Vector3.one, null, false);
                dome.transform.position = centre;
                dome.AddComponent<FadeOut>().Init(1.1f, mat, Vector3.one * radius * 2.1f);
            }
            foreach (var t in InSphere(mid, radius, c.health.team))
                Hit(c, t, damage, s, t.transform.position - centre);
        }

        static IEnumerator MultiRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            c.Rig?.PlayAttack(2, 0.35f);
            var dir = AimDir(c, aim);
            float spread = Mathf.Min(70f, 9f * (s.count - 1));
            for (int i = 0; i < s.count; i++)
            {
                if (c == null || c.health.IsDead) yield break;
                float yaw = s.count > 1 ? Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (float)(s.count - 1)) : 0f;
                var d = Quaternion.Euler(0, yaw, 0) * dir;
                Projectile.Spawn(c, s, c.Chest + d * 0.8f, d);
                yield return new WaitForSeconds(0.04f);
            }
        }

        static IEnumerator SlamRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            c.Rig?.PlayAttack(3, 0.4f);
            var target = s.range <= 6f
                ? World.OnGround(c.transform.position + AimDir(c, aim) * s.range * Mathf.Max(1f, c.Scale * 0.6f))
                : ClampToRange(c, aim, s.range);
            float radius = s.radius * Mathf.Max(1f, c.Scale * 0.6f);
            VFX.Telegraph(target, radius, s.color, 0.4f);
            yield return new WaitForSeconds(0.32f);
            if (c == null) yield break;
            if (s.element == Element.Lightning) VFX.Lightning(target + Vector3.up * 25f, target, s.color, 0.3f);
            if (s.element == Element.Earth || s.element == Element.Ice)
            {
                var spikeMat = Mats.Toon(s.color);
                for (int i = 0; i < 7; i++)
                {
                    var p = target + Random.insideUnitSphere * radius * 0.7f;
                    p = World.OnGround(p);
                    var spike = MeshFactory.Part("Spike", MeshFactory.Cone(5), spikeMat, null, Vector3.zero,
                        new Vector3(0.8f, Random.Range(1.5f, 3f), 0.8f), null);
                    spike.transform.position = p;
                    spike.transform.rotation = Quaternion.Euler(Random.Range(-20, 20), Random.Range(0, 360), Random.Range(-20, 20));
                    Object.Destroy(spike, 1.2f);
                }
            }
            VFX.Burst(target + Vector3.up * 0.5f, radius, s.color, 0.5f);
            foreach (var t in InSphere(target + Vector3.up * 0.8f, radius, c.health.team))
                Hit(c, t, s.damage, s, t.transform.position - target);
        }

        static IEnumerator DashRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            var dir = AimDir(c, aim);
            float dist = s.range;
            const float speed = 45f;
            float dur = Mathf.Max(0.12f, dist / speed);
            c.rootedUntil = Time.time + dur + 0.1f;
            c.FaceTowards(c.transform.position + dir, dur + 0.2f);
            c.Rig?.PlayAttack(s.radius > 0 ? 2 : 0, dur + 0.1f);
            var already = new HashSet<Health>();
            float t = 0f;
            var orb = VFX.Orb(new Vector3(0, 1.1f, 0.6f), 0.6f * Mathf.Max(0.6f, s.size * 2f), s.color, c.transform);
            VFX.Trail(orb, s.color, 0.7f, 0.3f);
            while (t < dur)
            {
                if (c == null || c.health.IsDead) break;
                float dt = Time.deltaTime;
                t += dt;
                Mover(c, dir * speed * dt);
                foreach (var h in InSphere(c.Chest, 1.6f * c.Scale, c.health.team))
                {
                    if (!already.Add(h)) continue;
                    Hit(c, h, s.radius > 0 ? s.damage * 0.4f : s.damage, s, dir);
                }
                if (s.element == Element.Lightning && Random.value < 0.5f)
                    VFX.Lightning(c.Chest, c.Chest + Random.insideUnitSphere * 1.5f, s.color, 0.1f);
                yield return null;
            }
            if (orb != null) Object.Destroy(orb);
            if (c != null && s.radius > 0f && !c.health.IsDead)
            {
                var end = c.transform.position + dir * 1.2f;
                VFX.Burst(end + Vector3.up, s.radius, s.color, 0.45f);
                foreach (var h in InSphere(end + Vector3.up, s.radius, c.health.team))
                    Hit(c, h, s.damage, s, h.transform.position - c.transform.position);
            }
        }

        static void SpawnMinions(SkillCaster c, SkillDef s, bool beast)
        {
            c.Rig?.PlayAttack(2, 0.35f);
            for (int i = 0; i < s.count; i++)
            {
                float ang = (i / (float)s.count) * 360f + 30f;
                var pos = World.OnGround(c.transform.position + Quaternion.Euler(0, ang, 0) * c.transform.forward * 1.8f);
                Appearance look;
                if (beast)
                {
                    look = new Appearance
                    {
                        beast = true, outfit = s.color, skin = s.color.Brighten(0.35f), hair = s.color.Darken(0.25f),
                        eyes = Hex.C("#FFD21F"), scale = 1f
                    };
                }
                else
                {
                    look = (c.appearance ?? new Appearance()).Clone();
                    look.scale *= 0.95f;
                }
                VFX.Smoke(pos, 1.2f);
                Minion.Spawn(c, look, pos, s.damage * c.DamageMultiplier, s.duration, s.element, s.color);
            }
        }

        static IEnumerator BeamRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            var dir = AimDir(c, aim);
            c.rootedUntil = Time.time + s.duration + 0.25f;
            c.FaceTowards(c.transform.position + dir, s.duration + 0.3f);
            c.Rig?.PlayAttack(2, s.duration + 0.25f);
            // Short charge-up orb at the hands.
            var charge = VFX.Orb(c.Chest + dir * 0.7f, 0.2f, s.color, null);
            for (float k = 0; k < 0.22f; k += Time.deltaTime)
            {
                if (c == null || charge == null) yield break;
                charge.transform.position = c.Chest + dir * 0.7f;
                charge.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.2f * s.radius, k / 0.22f);
                yield return null;
            }
            if (charge != null) Object.Destroy(charge);
            if (c == null || c.health.IsDead) yield break;

            var from = c.Chest + dir * 0.6f;
            float width = s.radius * Mathf.Max(1f, c.Scale * 0.6f);
            VFX.Beam(from, dir, s.range, width * 1.6f, s.color, s.duration + 0.15f);
            VFX.Orb(from, width * 2f, s.color, null).AddComponent<FadeOut>().Init(s.duration + 0.15f, null, Vector3.one * width * 2.5f);
            bool first = true;
            for (float t = 0; t < s.duration; t += TickInterval)
            {
                if (c == null || c.health.IsDead) yield break;
                foreach (var h in AlongLine(from, dir, s.range, width, c.health.team))
                {
                    Hit(c, h, s.damage, first ? s : NoStun(s), dir);
                    VFX.Particles(h.Center, s.color, 6, 4f, 0.3f, 0.3f);
                }
                first = false;
                yield return new WaitForSeconds(TickInterval);
            }
        }

        static SkillDef _noStunCache;
        static SkillDef NoStun(SkillDef s)
        {
            if (s.stun <= 0f) return s;
            _noStunCache = _noStunCache ?? new SkillDef();
            _noStunCache.element = s.element;
            _noStunCache.knockback = s.knockback * 0.3f;
            _noStunCache.stun = 0f;
            return _noStunCache;
        }

        static IEnumerator PunchRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            var dir = AimDir(c, aim);
            float range = s.range * Mathf.Max(1f, c.Scale * 0.6f);
            var armMat = Mats.Toon(s.color);
            if (s.count <= 1)
            {
                c.rootedUntil = Time.time + 0.4f;
                c.Rig?.PlayAttack(0, 0.4f);
                var from = c.Chest + c.transform.right * 0.3f * c.Scale;
                var targets = AlongLine(from, dir, range, 0.9f * c.Scale, c.health.team);
                float reach = targets.Count > 0 ? Mathf.Min(range, Vector3.Distance(from, targets[0].Center)) : range;
                yield return StretchArm(from, dir, reach, armMat, 0.22f * c.Scale, 0.1f, 0.12f, c);
                if (c == null) yield break;
                if (targets.Count > 0)
                {
                    var t = targets[0];
                    var hitPos = from + dir * reach;
                    VFX.Burst(hitPos, Mathf.Max(1.2f, s.radius), s.color.Brighten(0.3f), 0.35f);
                    Hit(c, t, s.damage, s, dir);
                    if (s.radius > 0f)
                        foreach (var h in InSphere(hitPos, s.radius, c.health.team))
                            if (h != t) Hit(c, h, s.damage * 0.6f, s, h.transform.position - hitPos);
                }
            }
            else
            {
                c.rootedUntil = Time.time + s.count * 0.06f + 0.2f;
                for (int i = 0; i < s.count; i++)
                {
                    if (c == null || c.health.IsDead) yield break;
                    c.Rig?.PlayAttack(i % 2, 0.08f);
                    var off = c.transform.right * Random.Range(-0.5f, 0.5f) + Vector3.up * Random.Range(-0.35f, 0.35f);
                    var from = c.Chest + off;
                    var targets = AlongLine(from, dir, range, 1.1f * c.Scale, c.health.team);
                    float reach = targets.Count > 0 ? Mathf.Min(range, Vector3.Distance(from, targets[0].Center)) : range;
                    c.StartCoroutine(StretchArm(from, dir, reach, armMat, 0.18f * c.Scale, 0.03f, 0.05f, c));
                    if (targets.Count > 0)
                    {
                        Hit(c, targets[0], s.damage, s, dir * 0.3f);
                        VFX.Particles(from + dir * reach, Color.white, 4, 3f, 0.25f, 0.2f);
                    }
                    yield return new WaitForSeconds(0.06f);
                }
            }
        }

        static IEnumerator StretchArm(Vector3 from, Vector3 dir, float reach, Material mat, float thickness, float outTime, float backTime, SkillCaster c)
        {
            var arm = MeshFactory.Part("StretchArm", MeshFactory.Cylinder, mat, null, Vector3.zero, Vector3.one * thickness, Quaternion.FromToRotation(Vector3.up, dir), false);
            var fist = MeshFactory.Part("Fist", MeshFactory.Sphere, mat, arm.transform.parent, Vector3.zero, Vector3.one * thickness * 2.2f, null, false);
            float t = 0f, total = outTime + backTime;
            while (t < total)
            {
                if (c == null) break;
                t += Time.deltaTime;
                float k = t < outTime ? t / outTime : 1f - (t - outTime) / backTime;
                float len = Mathf.Max(0.05f, reach * Mathf.Clamp01(k));
                arm.transform.position = from + dir * len * 0.5f;
                arm.transform.localScale = new Vector3(thickness, len * 0.5f, thickness);
                fist.transform.position = from + dir * len;
                yield return null;
            }
            Object.Destroy(arm);
            Object.Destroy(fist);
        }

        static IEnumerator SlashRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            var dir = AimDir(c, aim);
            float range = s.range * Mathf.Max(1f, c.Scale * 0.6f);
            c.rootedUntil = Time.time + s.count * 0.12f + 0.1f;
            for (int i = 0; i < s.count; i++)
            {
                if (c == null || c.health.IsDead) yield break;
                c.Rig?.PlayAttack(4, 0.12f);
                float spin = s.angle >= 359f ? i * 67f : (i % 2 == 0 ? 20f : -20f);
                var rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, s.angle >= 359f ? spin : 0f, spin * (s.angle >= 359f ? 0.2f : 1f));
                VFX.Slash(c.Chest + dir * 0.3f, rot, range, s.angle, s.color, 0.22f);
                foreach (var t in InCone(c.transform.position + Vector3.up * c.Scale, dir, range, s.angle, c.health.team))
                    Hit(c, t, s.damage, s, t.transform.position - c.transform.position);
                yield return new WaitForSeconds(0.12f);
            }
        }

        static void Teleport(SkillCaster c, SkillDef s, Vector3 aim)
        {
            var target = ClampToRange(c, aim, s.range);
            VFX.Burst(c.Chest, 1.2f, s.color, 0.3f);
            VFX.Particles(c.Chest, s.color.Darken(0.2f), 20, 5f, 0.4f, 0.5f);
            if (c.mover != null) c.mover.Teleport(target);
            else c.transform.position = target;
            c.Rig?.PlayAttack(3, 0.3f);
            VFX.Burst(target + Vector3.up, Mathf.Max(1.5f, s.radius), s.color, 0.4f);
            if (s.damage > 0f)
                foreach (var h in InSphere(target + Vector3.up, Mathf.Max(2f, s.radius), c.health.team))
                    Hit(c, h, s.damage, s, h.transform.position - target);
        }

        static IEnumerator RainRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            c.Rig?.PlayAttack(3, 0.5f);
            var centre = ClampToRange(c, aim, s.range);
            VFX.Telegraph(centre, s.radius, s.color, 0.3f + s.count * 0.09f);
            float strikeRadius = Mathf.Clamp(s.radius * 0.35f, 1.8f, 3.2f);
            for (int i = 0; i < s.count; i++)
            {
                if (c == null) yield break;
                var off = Random.insideUnitCircle * s.radius;
                var p = World.OnGround(centre + new Vector3(off.x, 0, off.y));
                FallingStrike.Spawn(c, s, p, strikeRadius);
                yield return new WaitForSeconds(0.09f);
            }
        }

        static IEnumerator PullRoutine(SkillCaster c, SkillDef s, Vector3 aim)
        {
            c.Rig?.PlayAttack(2, 0.4f);
            var centre = ClampToRange(c, aim, s.range) + Vector3.up * 1.2f;
            var orb = VFX.Orb(centre, 0.4f, s.color, null);
            orb.AddComponent<Spin>().degreesPerSecond = new Vector3(90, 360, 0);
            var ringMat = Mats.GlowInstance(s.color.WithAlpha(0.8f));
            var ring = MeshFactory.Part("Vortex", MeshFactory.Ring, ringMat, null, Vector3.zero, Vector3.one * s.radius, null, false);
            ring.transform.position = centre - Vector3.up * 1.1f;
            ring.AddComponent<Spin>().degreesPerSecond = new Vector3(0, -400, 0);
            var aura = VFX.Aura(orb.transform, s.color, s.radius * 0.6f, 0f, 80f);
            bool first = true;
            float tick = 0f;
            for (float t = 0; t < s.duration; t += Time.deltaTime)
            {
                if (c == null) break;
                orb.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.6f, t / s.duration);
                tick -= Time.deltaTime;
                foreach (var h in InSphere(centre, s.radius * 1.4f, c.health.team))
                {
                    var to = centre - h.transform.position;
                    to.y = 0f;
                    if (!h.isBoss) h.knockback = to.normalized * Mathf.Min(9f, to.magnitude * 4f);
                    if (tick <= 0f) Hit(c, h, s.damage, first ? s : NoStun(s), Vector3.zero);
                }
                if (tick <= 0f) { tick = TickInterval; first = false; }
                yield return null;
            }
            VFX.Burst(centre, s.radius * 0.7f, s.color, 0.4f);
            Object.Destroy(aura);
            Object.Destroy(orb);
            Object.Destroy(ring);
            Object.Destroy(ringMat);
        }
    }

    // ---------------------------------------------------------------------- moving pieces

    public class Projectile : MonoBehaviour
    {
        SkillCaster _caster;
        SkillDef _s;
        Vector3 _dir;
        float _travelled;
        Team _team;
        readonly HashSet<Health> _hit = new HashSet<Health>();

        public static void Spawn(SkillCaster c, SkillDef s, Vector3 pos, Vector3 dir)
        {
            float size = s.size * Mathf.Max(1f, c.Scale * 0.6f);
            var go = new GameObject("Projectile_" + s.name);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir);
            var p = go.AddComponent<Projectile>();
            p._caster = c;
            p._s = s;
            p._dir = dir.normalized;
            p._team = c.health.team;
            VFX.Orb(Vector3.zero, size, s.color, go.transform);
            VFX.Trail(go, s.color, size * 0.9f, 0.25f);
            if (size >= 1.2f)
            {
                // Big techniques get a spinning energy ring (Rasenshuriken, Supernova...).
                var ring = MeshFactory.Part("Ring", MeshFactory.Ring, Mats.Glow(s.color.Brighten(0.3f)), go.transform, Vector3.zero, Vector3.one * size * 1.4f, null, false);
                ring.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 720, 0);
                var l = go.AddComponent<Light>();
                l.color = s.color;
                l.range = size * 6f;
                l.intensity = 2f;
            }
        }

        void Update()
        {
            float step = _s.speed * Time.deltaTime;
            transform.position += _dir * step;
            _travelled += step;
            float size = _s.size * 0.5f + 0.35f;

            foreach (var h in SkillEffects.InSphere(transform.position, size, _team))
            {
                if (_hit.Contains(h)) continue;
                _hit.Add(h);
                SkillEffects.Hit(_caster, h, _s.damage, _s, _dir);
                if (!_s.pierce) { Explode(); return; }
                VFX.Particles(h.Center, _s.color, 6, 4f, 0.3f, 0.3f);
            }

            if (transform.position.y < World.GroundHeight(transform.position) + 0.1f || _travelled >= _s.range)
                Explode();
        }

        void Explode()
        {
            var pos = transform.position;
            if (_s.radius > 0f)
            {
                VFX.Burst(pos, _s.radius, _s.color, 0.5f);
                if (_caster != null)
                    foreach (var h in SkillEffects.InSphere(pos, _s.radius, _team))
                        if (!_hit.Contains(h)) SkillEffects.Hit(_caster, h, _s.damage * 0.75f, _s, h.transform.position - pos);
            }
            else VFX.Particles(pos, _s.color, 10, 4f, 0.3f, 0.35f);
            Destroy(gameObject);
        }
    }

    public class FallingStrike : MonoBehaviour
    {
        SkillCaster _caster;
        SkillDef _s;
        Vector3 _target;
        float _radius;
        Team _team;

        public static void Spawn(SkillCaster c, SkillDef s, Vector3 target, float radius)
        {
            if (s.element == Element.Lightning)
            {
                VFX.Lightning(target + Vector3.up * 22f, target, s.color, 0.25f);
                Impact(c, s, target, radius, c.health.team);
                return;
            }
            var go = new GameObject("Strike");
            go.transform.position = target + Vector3.up * 16f + Random.insideUnitSphere * 2f;
            var f = go.AddComponent<FallingStrike>();
            f._caster = c;
            f._s = s;
            f._target = target;
            f._radius = radius;
            f._team = c.health.team;
            VFX.Orb(Vector3.zero, 0.7f, s.color, go.transform);
            VFX.Trail(go, s.color, 0.6f, 0.2f);
        }

        void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, _target, 45f * Time.deltaTime);
            if ((transform.position - _target).sqrMagnitude < 0.05f)
            {
                Impact(_caster, _s, _target, _radius, _team);
                Destroy(gameObject);
            }
        }

        static void Impact(SkillCaster c, SkillDef s, Vector3 p, float radius, Team team)
        {
            VFX.Burst(p + Vector3.up * 0.4f, radius, s.color, 0.4f);
            if (c == null) return;
            foreach (var h in SkillEffects.InSphere(p + Vector3.up * 0.8f, radius, team))
                SkillEffects.Hit(c, h, s.damage, s, h.transform.position - p);
        }
    }
}
