using System;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Physics-driven monster brain. Behaviours: Melee, Ranged (kites), Charger (wind-up dash)
    /// and Boss (melee + telegraphed slam + one-time summon + enrage).
    /// Movement is applied through Rigidbody velocity so knockback and collisions are real physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Health))]
    public class EnemyAI : MonoBehaviour, IHitReactor
    {
        enum State { Idle, Chase, Attack, WindUp, Charge, Slam, Stunned, Dead }

        public MonsterData data;

        public static event Action<EnemyAI> Killed;
        public static event Action<EnemyAI, EnemyAI> Summoned; // boss, minion

        public StatBlock Stats { get; private set; }
        public Health Health { get; private set; }
        public bool IsBoss => data != null && data.IsBoss;

        Rigidbody rb;
        CharacterAnimation anim;
        State state = State.Idle;
        float stateTime;
        float attackTimer;
        float specialTimer;
        float stunTimer;
        float strafeSign = 1f;
        Vector3 knockback;
        Vector3 chargeDir;
        bool summoned, enraged, hitThisAction;
        float difficulty = 1f;
        Vfx telegraph;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            Health = GetComponent<Health>();
            anim = GetComponent<CharacterAnimation>();
        }

        public void Initialize(MonsterData monster, float diff)
        {
            data = monster;
            difficulty = Mathf.Max(0.1f, diff);
            Stats = monster.baseStats.Clone();
            Stats.maxHealth *= difficulty;
            Stats.attack *= Mathf.Lerp(1f, difficulty, 0.85f);
            Stats.defense *= Mathf.Lerp(1f, difficulty, 0.6f);
            Health.Initialize(Stats.maxHealth, Stats.defense, Team.Enemy);
            Health.Died += OnDied;
            var visual = transform.Find("Visual");
            if (visual != null) anim.Bind(visual.gameObject);
            attackTimer = UnityEngine.Random.Range(0.3f, 1f);
            specialTimer = IsBoss ? 6f : UnityEngine.Random.Range(3f, 6f);
            strafeSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            SetState(State.Idle);
            HealthBarRegistry.Register(Health, IsBoss);
        }

        Transform Target
        {
            get
            {
                var p = PlayerController.Instance;
                return p != null && !p.Health.IsDead ? p.transform : null;
            }
        }

        void SetState(State s)
        {
            state = s;
            stateTime = 0f;
            hitThisAction = false;
        }

        void Update()
        {
            if (state == State.Dead || data == null) return;
            float dt = Time.deltaTime;
            stateTime += dt;
            attackTimer -= dt;
            specialTimer -= dt;

            var target = Target;
            if (state == State.Stunned)
            {
                stunTimer -= dt;
                if (stunTimer <= 0f) SetState(State.Chase);
                return;
            }
            if (target == null) { if (state != State.Idle) SetState(State.Idle); return; }

            Vector3 to = (target.position - transform.position).Flat();
            float dist = to.magnitude;

            if (IsBoss) UpdateBossPhases();

            switch (state)
            {
                case State.Idle:
                    if (dist < data.aggroRange || IsBoss) SetState(State.Chase);
                    break;

                case State.Chase:
                    if (data.behaviour == MonsterBehaviour.Charger && specialTimer <= 0f && dist > 3.5f && dist < 14f)
                    {
                        SetState(State.WindUp);
                        telegraph = Vfx.Ring(transform.position, 0.5f, 1.6f, new Color(1f, 0.3f, 0.1f, 0.5f), 0.7f, transform);
                    }
                    else if (IsBoss && specialTimer <= 0f && dist < 9f)
                    {
                        SetState(State.Slam);
                        anim.Cast();
                        telegraph = Vfx.Ring(transform.position, 0.5f, SlamRadius, new Color(1f, 0.15f, 0.05f, 0.45f), 1.1f, transform);
                    }
                    else if (dist <= data.attackRange && attackTimer <= 0f)
                    {
                        SetState(State.Attack);
                        if (data.behaviour == MonsterBehaviour.Ranged) anim.Cast(); else anim.Attack(1f);
                    }
                    break;

                case State.Attack:
                    Face(to);
                    float hitTime = data.behaviour == MonsterBehaviour.Ranged ? 0.35f : 0.4f;
                    if (!hitThisAction && stateTime >= hitTime)
                    {
                        hitThisAction = true;
                        if (data.behaviour == MonsterBehaviour.Ranged) Shoot(target);
                        else MeleeHit(target, dist);
                    }
                    if (stateTime >= 0.8f)
                    {
                        attackTimer = data.attackCooldown * (enraged ? 0.65f : 1f) * UnityEngine.Random.Range(0.85f, 1.15f);
                        SetState(State.Chase);
                    }
                    break;

                case State.WindUp:
                    Face(to);
                    if (stateTime >= 0.7f)
                    {
                        chargeDir = to.normalized;
                        SetState(State.Charge);
                        anim.Attack(1.5f);
                    }
                    break;

                case State.Charge:
                    if (!hitThisAction && dist < 1.6f * data.look.scale + 0.6f)
                    {
                        hitThisAction = true;
                        var d = CombatUtility.Roll(Stats, 1.6f, gameObject, Team.Enemy);
                        d.knockback = chargeDir * 12f;
                        CombatUtility.ApplyHit(target.GetComponent<Health>(), d);
                    }
                    if (stateTime >= 0.6f)
                    {
                        specialTimer = UnityEngine.Random.Range(5f, 8f);
                        attackTimer = 0.8f;
                        SetState(State.Chase);
                    }
                    break;

                case State.Slam:
                    if (!hitThisAction && stateTime >= 1.1f)
                    {
                        hitThisAction = true;
                        var center = transform.position;
                        CombatUtility.DamageArea(center, SlamRadius, Team.Enemy, h =>
                        {
                            var d = CombatUtility.Roll(Stats, 2.2f, gameObject, Team.Enemy);
                            d.knockback = (h.transform.position - center).Flat().normalized * 14f;
                            return d;
                        });
                        Vfx.Burst(center + Vector3.up * 0.3f, SlamRadius * 0.6f, new Color(1f, 0.4f, 0.1f, 0.5f), 0.4f);
                        ThirdPersonCamera.Instance?.Shake(0.6f);
                        Sfx.Play(SfxId.Explosion, center);
                    }
                    if (stateTime >= 1.6f)
                    {
                        specialTimer = enraged ? 5f : 8f;
                        SetState(State.Chase);
                    }
                    break;
            }

            anim.SetSpeed(rb.GetVelocity().Flat().magnitude / Mathf.Max(1f, Stats.moveSpeed));
        }

        float SlamRadius => 4.5f * Mathf.Max(1f, data.look.scale * 0.6f);

        void UpdateBossPhases()
        {
            if (!summoned && Health.Normalized < 0.5f && data.summon != null)
            {
                summoned = true;
                for (int i = 0; i < 3; i++)
                {
                    Vector3 offset = Quaternion.Euler(0, i * 120f, 0) * Vector3.forward * 3f;
                    var minion = CharacterSpawner.SpawnEnemy(data.summon, transform.position + offset, difficulty);
                    Vfx.Burst(minion.transform.position + Vector3.up, 1.2f, new Color(0.6f, 0.2f, 1f, 0.6f), 0.4f);
                    Summoned?.Invoke(this, minion);
                }
            }
            if (!enraged && Health.Normalized < 0.3f)
            {
                enraged = true;
                Stats.moveSpeed *= 1.3f;
                Vfx.Ring(transform.position, 1f, 5f, new Color(1f, 0.1f, 0.1f, 0.6f), 0.8f, transform);
            }
        }

        void FixedUpdate()
        {
            if (state == State.Dead) return;
            Vector3 desired = Vector3.zero;
            var target = Target;

            if (target != null)
            {
                Vector3 to = (target.position - transform.position).Flat();
                float dist = to.magnitude;
                Vector3 dir = dist > 0.01f ? to / dist : transform.forward;
                switch (state)
                {
                    case State.Chase:
                        if (data.behaviour == MonsterBehaviour.Ranged)
                        {
                            // Keep at roughly 70% of attack range and strafe sideways.
                            float ideal = data.attackRange * 0.7f;
                            float approach = Mathf.Clamp((dist - ideal) / 2f, -1f, 1f);
                            Vector3 side = new Vector3(dir.z, 0, -dir.x) * strafeSign * 0.4f;
                            desired = (dir * approach + side) * Stats.moveSpeed;
                        }
                        else if (dist > data.attackRange * 0.85f)
                        {
                            desired = Avoid(dir) * Stats.moveSpeed;
                        }
                        Face(to);
                        break;
                    case State.Charge:
                        desired = chargeDir * Stats.moveSpeed * 3.2f;
                        Face(chargeDir);
                        break;
                }
            }

            knockback = Vector3.Lerp(knockback, Vector3.zero, 8f * Time.fixedDeltaTime);
            Vector3 v = rb.GetVelocity();
            Vector3 planar = desired + knockback;
            rb.SetVelocity(new Vector3(planar.x, v.y, planar.z));
        }

        /// <summary>Simple obstacle steering: probe ahead and slide along walls/rocks.</summary>
        Vector3 Avoid(Vector3 dir)
        {
            float probe = 1.6f * Mathf.Max(1f, data.look.scale);
            Vector3 origin = transform.position + Vector3.up * 0.6f;
            if (Physics.SphereCast(origin, 0.35f, dir, out var hit, probe, 1 << Layers.Environment | 1, QueryTriggerInteraction.Ignore)
                && hit.rigidbody == null)
            {
                Vector3 slide = Vector3.ProjectOnPlane(dir, hit.normal).Flat();
                if (slide.sqrMagnitude < 0.05f) slide = new Vector3(-hit.normal.z, 0, hit.normal.x) * strafeSign;
                return slide.normalized;
            }
            return dir;
        }

        void Face(Vector3 dir)
        {
            dir = dir.Flat();
            if (dir.sqrMagnitude < 0.001f) return;
            var targetRot = Quaternion.LookRotation(dir);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRot, 540f * Time.deltaTime);
        }

        void MeleeHit(Transform target, float dist)
        {
            if (dist > data.attackRange * 1.35f) return;
            var d = CombatUtility.Roll(Stats, 1f, gameObject, Team.Enemy);
            d.knockback = (target.position - transform.position).Flat().normalized * (IsBoss ? 8f : 3f);
            CombatUtility.ApplyHit(target.GetComponent<Health>(), d);
            Sfx.Play(SfxId.Hit, target.position);
        }

        void Shoot(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * 1.3f * data.look.scale + transform.forward * 0.6f;
            Vector3 aim = target.position + Vector3.up * 1f;
            var d = CombatUtility.Roll(Stats, 1f, gameObject, Team.Enemy);
            Projectile.Spawn(origin, aim - origin, data.projectileSpeed, d, data.projectileColor, 0.25f, 0f, 3f);
        }

        public void OnHit(DamageInfo info)
        {
            if (state == State.Dead) return;
            float resist = IsBoss ? 0.15f : 1f / Mathf.Max(1f, data.look.scale);
            knockback += info.knockback.Flat() * resist;
            if (info.knockback.y > 0f && !IsBoss)
            {
                Vector3 v = rb.GetVelocity();
                rb.SetVelocity(new Vector3(v.x, info.knockback.y, v.z));
            }
            if (info.stunDuration > 0f && !IsBoss)
            {
                stunTimer = info.stunDuration;
                SetState(State.Stunned);
                if (telegraph != null) telegraph = null;
            }
            if (state != State.Slam && state != State.Charge) anim.Hit();
            if (state == State.Idle) SetState(State.Chase);
        }

        void OnDied(Health h)
        {
            SetState(State.Dead);
            anim.Die();
            HealthBarRegistry.Unregister(Health);
            gameObject.layer = Layers.Pickup; // stop blocking the player while the body fades
            rb.SetVelocity(Vector3.zero);
            rb.isKinematic = true;
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
            LootSystem.DropFromMonster(data, transform.position);
            Killed?.Invoke(this);
            Sfx.Play(IsBoss ? SfxId.Explosion : SfxId.EnemyDeath, transform.position);
            Destroy(gameObject, 2.5f);
        }

        void OnDestroy()
        {
            if (Health != null) HealthBarRegistry.Unregister(Health);
        }
    }
}
