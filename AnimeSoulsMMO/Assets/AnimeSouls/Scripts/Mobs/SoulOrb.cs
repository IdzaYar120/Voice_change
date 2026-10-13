using UnityEngine;

namespace AnimeSouls
{
    /// <summary>A soul fragment dropped by a mob. Flies to the player when nearby and feeds the soul inventory.</summary>
    public class SoulOrb : MonoBehaviour
    {
        public SoulDef soul;
        public int amount;
        float _born;
        Vector3 _velocity;
        float _speed;

        public static SoulOrb Spawn(Vector3 pos, SoulDef soul, int amount)
        {
            var go = new GameObject("SoulOrb_" + soul.name);
            go.transform.position = pos;
            var orb = go.AddComponent<SoulOrb>();
            orb.soul = soul;
            orb.amount = amount;
            orb._born = Time.time;
            orb._velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(4f, 6f), Random.Range(-2f, 2f));
            float size = 0.45f + 0.08f * amount;
            VFX.Orb(Vector3.zero, size, soul.signature, go.transform);
            VFX.Orb(Vector3.zero, size * 0.55f, Color.white, go.transform);
            VFX.Aura(go.transform, soul.signature, size * 0.6f, 0f, 18f);
            VFX.Trail(go, soul.signature, size * 0.6f, 0.4f);
            return orb;
        }

        void Update()
        {
            float age = Time.time - _born;
            if (age > 120f) { Destroy(gameObject); return; }

            var player = Game.I != null ? Game.I.player : null;
            Vector3 target = Vector3.zero;
            bool homing = false;
            if (player != null && !player.health.IsDead && age > 0.7f)
            {
                target = player.transform.position + Vector3.up * 1.2f;
                homing = Vector3.Distance(transform.position, target) < 12f;
            }

            if (homing)
            {
                _speed = Mathf.Min(30f, _speed + 40f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
                if (Vector3.Distance(transform.position, target) < 0.7f)
                {
                    player.inventory.AddFragments(soul, amount);
                    VFX.Burst(target, 1.2f, soul.signature, 0.35f);
                    Destroy(gameObject);
                }
                return;
            }

            // Pop out of the body, then hover above the ground.
            _velocity += Vector3.down * 12f * Time.deltaTime;
            var p = transform.position + _velocity * Time.deltaTime;
            float floor = World.GroundHeight(p) + 1.1f;
            if (p.y < floor)
            {
                p.y = floor;
                _velocity = Vector3.zero;
                p.y += Mathf.Sin(Time.time * 3f + _born) * 0.003f;
            }
            transform.position = p;
        }
    }
}
