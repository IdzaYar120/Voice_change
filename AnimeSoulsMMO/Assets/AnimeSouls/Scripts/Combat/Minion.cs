using UnityEngine;

namespace AnimeSouls
{
    /// <summary>Temporary ally: shadow clones, summoned beasts, illusions. Chases the nearest enemy near its owner.</summary>
    public class Minion : MonoBehaviour, IMovable
    {
        SkillCaster _owner;
        Health _health;
        SkillCaster _caster;
        float _damage;
        float _expires;
        Element _element;
        Color _color;
        bool _sword;
        float _speed = 7f;

        public static Minion Spawn(SkillCaster owner, Appearance look, Vector3 pos, float damage, float duration, Element element, Color color)
        {
            var go = new GameObject(look.beast ? "Summon" : "Clone");
            go.transform.position = pos;
            go.transform.rotation = owner.transform.rotation;
            var health = go.AddComponent<Health>();
            health.team = owner.health.team;
            health.displayName = look.beast ? "Призов" : "Клон";
            health.Init(Mathf.Max(60f, owner.health.maxHp * 0.3f));
            health.rig = CharacterBuilder.Build(look, go.transform);
            var caster = go.AddComponent<SkillCaster>();
            caster.unlimitedEnergy = true;
            caster.appearance = look;
            var m = go.AddComponent<Minion>();
            m._owner = owner;
            m._health = health;
            m._caster = caster;
            caster.mover = m;
            m._damage = damage;
            m._expires = Time.time + duration;
            m._element = element;
            m._color = color;
            m._sword = look.Has(Accessory.Swords) || look.Has(Accessory.BigSword);
            health.OnDeath += _ => m.Vanish();
            return m;
        }

        public void ForceMove(Vector3 delta) { transform.position = World.OnGround(transform.position + delta); }
        public void Teleport(Vector3 position) { transform.position = World.OnGround(position); }

        void Vanish()
        {
            VFX.Smoke(transform.position, 1f);
            Destroy(gameObject);
        }

        void Update()
        {
            if (_health.IsDead) return;
            if (Time.time > _expires || _owner == null || _owner.health.IsDead) { Vanish(); return; }

            var rig = _health.rig;
            var ownerPos = _owner.transform.position;
            var target = SkillEffects.Nearest(transform.position, 22f, _health.team);
            if (target != null && Vector3.Distance(target.transform.position, ownerPos) > 35f) target = null;

            Vector3 goal;
            float stop;
            if (target != null) { goal = target.transform.position; stop = 1.8f + SkillEffects.Radius(target); }
            else { goal = ownerPos + (transform.position - ownerPos).normalized * 2.5f; stop = 1.5f; }

            var to = goal - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            float moving = 0f;
            if (!_health.IsStunned && dist > stop)
            {
                var step = to.normalized * Mathf.Min(dist - stop, _speed * Time.deltaTime);
                transform.position = World.OnGround(transform.position + step);
                moving = 1f;
            }
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 12f);
            if (rig != null) rig.moveAmount = moving;

            if (target != null && dist <= stop + 0.4f)
                _caster.TryBasicAttack(target.transform.position, _damage, _color, _element, _sword);
        }
    }
}
