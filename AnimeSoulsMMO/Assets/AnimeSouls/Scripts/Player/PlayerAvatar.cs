using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// The soul hunter: movement, combat input, soul swapping and respawning.
    /// Without classes, everything the player can do comes from the currently channelled soul.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAvatar : MonoBehaviour, IMovable
    {
        public Health health;
        public SkillCaster caster;
        public SoulInventory inventory;
        public ThirdPersonCamera cam;

        public const float WalkSpeed = 7f;
        public const float SprintSpeed = 11f;
        const float Gravity = -26f;
        const float JumpSpeed = 9f;
        public const float BaseHp = 300f;
        public static readonly KeyCode[] SkillKeys = { KeyCode.Q, KeyCode.E, KeyCode.R };

        CharacterController _cc;
        float _vy;
        bool _grounded;
        float _swapReadyAt;
        float _respawnAt = -1f;
        string _modelSoulId = "__none__";

        public float RespawnIn => _respawnAt > 0f ? Mathf.Max(0f, _respawnAt - Time.time) : 0f;
        public Vector3 AimPoint { get; private set; }
        public Health AimTarget { get; private set; }

        public static Appearance WandererLook => new Appearance
        {
            hair = Hex.C("#C8CCD6"), hairStyle = HairStyle.Messy, outfit = Hex.C("#3A3F5A"), pants = Hex.C("#2A2E44"),
            eyes = Hex.C("#3FC8E8"), accent = Hex.C("#3FC8E8"), accessories = new[] { Accessory.Scarf }
        };

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cc.height = 1.9f;
            _cc.radius = 0.38f;
            _cc.center = new Vector3(0, 0.95f, 0);
            _cc.slopeLimit = 50f;
            _cc.stepOffset = 0.5f;
            health = GetComponent<Health>();
            caster = GetComponent<SkillCaster>();
            inventory = GetComponent<SoulInventory>();
            caster.mover = this;
        }

        void Start()
        {
            health.team = Team.Player;
            health.isPlayer = true;
            health.displayName = "Мисливець на Душі";
            inventory.Load();
            inventory.Changed += OnInventoryChanged;
            inventory.SoulAbsorbed += OnSoulAbsorbed;
            health.OnDeath += OnDied;
            Health.AnyDied += OnAnyDied;
            RefreshStats(true);
            RebuildModel(false);
        }

        void OnDestroy()
        {
            Health.AnyDied -= OnAnyDied;
        }

        // ------------------------------------------------------------------ souls

        void OnInventoryChanged()
        {
            RefreshStats(false);
            var active = inventory.ActiveSoul;
            if ((active != null ? active.id : null) != _modelSoulId) RebuildModel(true);
        }

        void OnSoulAbsorbed(SoulDef s)
        {
            health.hp = health.maxHp;
            caster.energy = caster.maxEnergy;
            VFX.Burst(transform.position + Vector3.up, 6f, s.signature, 1f);
            VFX.Particles(transform.position + Vector3.up, s.signature, 60, 8f, 0.6f, 1.2f, -0.5f);
        }

        void RefreshStats(bool fill)
        {
            float frac = health.maxHp > 0f ? health.hp / health.maxHp : 1f;
            health.maxHp = BaseHp + inventory.HpBonus;
            health.hp = fill ? health.maxHp : Mathf.Clamp(frac * health.maxHp, 1f, health.maxHp);
            caster.baseDamageMultiplier = inventory.PowerMultiplier;
            caster.maxEnergy = 100f + inventory.UnlockedCount * 4f;
            caster.energyRegen = 10f + inventory.UnlockedCount * 0.3f;
            if (fill) caster.energy = caster.maxEnergy;
        }

        void RebuildModel(bool withEffect)
        {
            var soul = inventory.ActiveSoul;
            _modelSoulId = soul != null ? soul.id : null;
            var look = soul != null ? soul.look : WandererLook;
            if (health.rig != null) Destroy(health.rig.gameObject);
            health.rig = CharacterBuilder.Build(look, transform);
            caster.appearance = look;
            caster.OnRigRebuilt();
            if (withEffect)
            {
                var c = soul != null ? soul.signature : Hex.C("#8FE8FF");
                VFX.Burst(transform.position + Vector3.up, 2.5f, c, 0.4f);
                VFX.Smoke(transform.position, 1.2f);
            }
        }

        public void SwapToSlot(int slot)
        {
            if (Time.time < _swapReadyAt || inventory.InSlot(slot) == null || slot == inventory.activeSlot) return;
            _swapReadyAt = Time.time + 1f;
            caster.EndBuff();
            inventory.SetActiveSlot(slot);
        }

        void OnAnyDied(Health victim, Health killer)
        {
            if (victim == null || victim.team != Team.Enemy || killer == null || killer.team != Team.Player) return;
            if (victim.GetComponent<MobAI>() == null) return;
            inventory.RegisterKill(victim.isBoss);
        }

        // ------------------------------------------------------------------ life cycle

        void OnDied(Health h)
        {
            _respawnAt = Time.time + 4f;
            caster.EndBuff();
            HUD.Notify("Ви загинули. Відродження в Нексусі Душ...", Hex.C("#FF5A5A"));
        }

        void Respawn()
        {
            _respawnAt = -1f;
            Teleport(World.HubPosition + new Vector3(0, 0.2f, -8f));
            health.Revive(1f);
            caster.energy = caster.maxEnergy;
            RebuildModel(true);
        }

        public void TravelTo(Vector3 pos)
        {
            VFX.Burst(transform.position + Vector3.up, 2f, Hex.C("#8FE8FF"), 0.4f);
            Teleport(pos);
            VFX.Burst(transform.position + Vector3.up, 2f, Hex.C("#8FE8FF"), 0.4f);
            if (cam != null) cam.SnapBehind();
        }

        // ------------------------------------------------------------------ IMovable

        public void ForceMove(Vector3 delta)
        {
            var next = transform.position + delta;
            if (World.GroundHeight(next) < World.WaterLevel - 1.5f) return;
            _cc.Move(delta);
        }

        public void Teleport(Vector3 position)
        {
            position.y = Mathf.Max(position.y, World.GroundHeight(position) + 0.05f);
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = true;
            _vy = 0f;
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            if (health.IsDead)
            {
                if (_respawnAt > 0f && Time.time >= _respawnAt) Respawn();
                return;
            }

            UpdateAim();
            UpdateMovement();
            if (!HUD.BlocksGameplayInput) UpdateCombat();

            // Safety net: never fall through the world.
            float ground = World.GroundHeight(transform.position);
            if (transform.position.y < ground - 2f) Teleport(new Vector3(transform.position.x, ground + 0.5f, transform.position.z));
        }

        void UpdateMovement()
        {
            float dt = Time.deltaTime;
            var input = Vector3.zero;
            if (!HUD.BlocksGameplayInput)
            {
                if (Inp.Key(KeyCode.W)) input.z += 1;
                if (Inp.Key(KeyCode.S)) input.z -= 1;
                if (Inp.Key(KeyCode.A)) input.x -= 1;
                if (Inp.Key(KeyCode.D)) input.x += 1;
            }
            bool canAct = !health.IsStunned && !caster.IsRooted;
            if (!canAct) input = Vector3.zero;

            var camYaw = cam != null ? Quaternion.Euler(0, cam.yaw, 0) : Quaternion.identity;
            var move = camYaw * Vector3.ClampMagnitude(input, 1f);
            bool sprint = Inp.Key(KeyCode.LeftShift);
            float speed = (sprint ? SprintSpeed : WalkSpeed) * caster.SpeedMultiplier * health.SpeedFactor;
            var horizontal = move * speed;

            var kb = health.knockback;
            kb.y = 0f;
            horizontal += kb;

            _grounded = _cc.isGrounded;
            if (_grounded && _vy < 0f) _vy = -2f;
            if (_grounded && canAct && !HUD.BlocksGameplayInput && Inp.Down(KeyCode.Space))
            {
                _vy = JumpSpeed;
                VFX.Particles(transform.position, new Color(1, 1, 1, 0.6f), 8, 2f, 0.4f, 0.4f);
            }
            _vy += Gravity * dt;

            var delta = horizontal * dt;
            var next = transform.position + delta;
            if (World.GroundHeight(next) < World.WaterLevel - 1.5f) delta = Vector3.zero; // the ocean is a wall
            delta.y = _vy * dt;
            _cc.Move(delta);

            if (Time.time < caster.faceUntil)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(caster.faceDirection), dt * 25f);
            else if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), dt * 14f);

            var rig = health.rig;
            if (rig != null)
            {
                rig.moveAmount = move.magnitude * (sprint ? 1f : 0.75f);
                rig.grounded = _grounded || _vy < 0f && _vy > -4f;
            }
        }

        void UpdateAim()
        {
            var camera = cam != null ? cam.Camera : Camera.main;
            if (camera == null) { AimPoint = transform.position + transform.forward * 10f; return; }
            var ray = camera.ScreenPointToRay(Inp.MousePosition);

            // Soft lock: an enemy close to the cursor ray wins over the ground point.
            AimTarget = null;
            float best = 1.6f;
            foreach (var h in Health.All)
            {
                if (!SkillEffects.IsEnemy(h, health.team)) continue;
                var c = h.Center;
                float t = Vector3.Dot(c - ray.origin, ray.direction);
                if (t < 0f || t > 120f) continue;
                float d = Vector3.Distance(ray.origin + ray.direction * t, c) - SkillEffects.Radius(h) - h.Height * 0.25f;
                if (d < best) { best = d; AimTarget = h; }
            }
            if (AimTarget != null) { AimPoint = AimTarget.transform.position; return; }

            if (Physics.Raycast(ray, out var hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                AimPoint = hit.point;
            else
            {
                var plane = new Plane(Vector3.up, transform.position);
                AimPoint = plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : transform.position + transform.forward * 10f;
            }
        }

        void UpdateCombat()
        {
            for (int i = 0; i < SoulInventory.Slots; i++)
                if (Inp.Down(KeyCode.Alpha1 + i)) SwapToSlot(i);

            var soul = inventory.ActiveSoul;
            if (Inp.Mouse(0))
            {
                var color = soul != null ? soul.signature : Hex.C("#8FE8FF");
                var element = soul != null ? soul.element : Element.Spirit;
                bool sword = soul != null && soul.swordBasic;
                caster.TryBasicAttack(AimPoint, sword ? 18f : 15f, color, element, sword);
            }

            if (soul == null) return;
            for (int i = 0; i < SkillKeys.Length && i < soul.skills.Length; i++)
            {
                if (!Inp.Down(SkillKeys[i])) continue;
                var s = soul.skills[i];
                if (!caster.TryCast(s, AimPoint))
                {
                    if (caster.CooldownRemaining(s) > 0f) HUD.Notify($"{s.name}: перезарядка {caster.CooldownRemaining(s):0.0}с", Hex.C("#FFB0B0"), 1f);
                    else if (caster.energy < s.EnergyCost) HUD.Notify("Недостатньо енергії душі", Hex.C("#8FB8FF"), 1f);
                }
                else if (s.ultimate && cam != null) cam.Shake(0.35f);
            }
        }
    }
}
