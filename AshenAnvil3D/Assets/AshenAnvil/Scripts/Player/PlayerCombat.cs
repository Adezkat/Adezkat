using System.Collections;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Basic attacks, hero skills, potions and buffs for the controlled hero.</summary>
    public class PlayerCombat : MonoBehaviour
    {
        public HeroData Hero { get; private set; }
        public WeaponData Weapon { get; private set; }
        public StatBlock Stats { get; private set; }
        public bool IsActing => actingTime > 0f;
        public Vector3? FacingTarget => facingTime > 0f ? facingDir : (Vector3?)null;
        public float SkillCooldownNormalized => Hero != null && Hero.skill != null ? Mathf.Clamp01(skillTimer / Hero.skill.cooldown) : 0f;
        public float PotionCooldownNormalized => Mathf.Clamp01(potionTimer / PotionCooldown);
        public float AttackBuffRemaining => attackBuffTime;

        const float PotionCooldown = 4f;
        const float MeleeAutoTargetRange = 6f;

        PlayerController controller;
        Health health;
        CharacterAnimation anim;
        float attackTimer, skillTimer, potionTimer, actingTime, facingTime;
        float attackBuff = 0f, attackBuffTime;
        Vector3 facingDir;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            anim = GetComponent<CharacterAnimation>();
        }

        void OnEnable() => PlayerProfile.HeroLeveledUp += OnHeroLeveledUp;
        void OnDisable() => PlayerProfile.HeroLeveledUp -= OnHeroLeveledUp;

        public void Setup(HeroData hero, WeaponData weapon, StatBlock stats)
        {
            Hero = hero;
            Weapon = weapon;
            ApplyStats(stats, true);
        }

        void ApplyStats(StatBlock stats, bool refill)
        {
            Stats = stats;
            health.defense = stats.defense;
            health.SetMaxHealth(stats.maxHealth, refill);
            controller.MoveSpeed = stats.moveSpeed;
        }

        void OnHeroLeveledUp(HeroData hero, int level)
        {
            if (hero != Hero) return;
            ApplyStats(PlayerProfile.Instance.ComputeStats(hero), true);
            Vfx.Ring(transform.position, 0.5f, 3f, new Color(1f, 0.85f, 0.3f, 0.7f), 0.8f, transform);
            Sfx.Play(SfxId.LevelUp, transform.position);
        }

        StatBlock Current
        {
            get
            {
                if (attackBuffTime <= 0f) return Stats;
                var s = Stats.Clone();
                s.attack *= 1f + attackBuff;
                return s;
            }
        }

        void Update()
        {
            if (Hero == null || health.IsDead) return;
            float dt = Time.deltaTime;
            attackTimer -= dt;
            skillTimer = Mathf.Max(0f, skillTimer - dt);
            potionTimer = Mathf.Max(0f, potionTimer - dt);
            actingTime -= dt;
            facingTime -= dt;
            if (attackBuffTime > 0f) attackBuffTime -= dt;

            if (controller.IsDodging) return;

            if (PlayerInput.ConsumeSkill()) TryUseSkill();
            if (PlayerInput.ConsumePotion()) TryUsePotion();
            if (PlayerInput.AttackHeld && attackTimer <= 0f && actingTime <= 0f) BasicAttack();
        }

        // ---------------------------------------------------------------- basic attack
        void BasicAttack()
        {
            var stats = Current;
            float interval = (Hero.IsRanged ? 0.75f : 0.62f) / stats.attackSpeed;
            attackTimer = interval;
            actingTime = interval * 0.55f;

            float searchRange = Hero.IsRanged ? Hero.AttackRange : MeleeAutoTargetRange;
            var target = CombatUtility.FindNearest(transform.position, searchRange, Team.Player, true);
            if (target != null) Face(target.transform.position - transform.position, interval);

            anim.Attack(stats.attackSpeed);
            Sfx.Play(SfxId.Swing, transform.position);
            StartCoroutine(AttackHit(target, 0.22f / stats.attackSpeed));
        }

        IEnumerator AttackHit(Health target, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (health.IsDead) yield break;
            var stats = Current;

            if (Hero.heroClass == HeroClass.Archer)
            {
                FireAt(target, stats, 1f, 30f, 0.15f, 0f, new Color(0.85f, 1f, 0.6f));
            }
            else if (Hero.heroClass == HeroClass.Mage)
            {
                FireAt(target, stats, 0.9f, 20f, 0.28f, 1.3f, Weapon != null ? Weapon.tint : new Color(0.4f, 0.7f, 1f));
            }
            else
            {
                float reach = Hero.heroClass == HeroClass.Assassin ? 1.6f : 2f;
                Vector3 center = transform.position + transform.forward * (reach * 0.6f) + Vector3.up;
                float dealt = CombatUtility.DamageArea(center, reach, Team.Player, h =>
                {
                    var d = CombatUtility.Roll(stats, 1f, gameObject, Team.Player);
                    d.hitPoint = h.transform.position + Vector3.up;
                    d.knockback = transform.forward * 4f;
                    return d;
                }, transform.forward, 160f);
                if (dealt > 0f)
                {
                    Sfx.Play(SfxId.Hit, center);
                    Vfx.Burst(center, 0.5f, new Color(1f, 0.9f, 0.6f, 0.6f), 0.15f);
                    Lifesteal(dealt, stats);
                }
            }
        }

        void FireAt(Health target, StatBlock stats, float mult, float speed, float radius, float explosion, Color color)
        {
            Vector3 origin = transform.position + Vector3.up * 1.3f + transform.forward * 0.6f;
            Vector3 aim = target != null ? target.transform.position + Vector3.up * 1f : origin + transform.forward * 10f;
            var dmg = CombatUtility.Roll(stats, mult, gameObject, Team.Player);
            Projectile.Spawn(origin, aim - origin, speed, dmg, color, radius, explosion, 2.5f);
        }

        void Lifesteal(float dealt, StatBlock stats)
        {
            if (stats.lifesteal > 0f) health.Heal(dealt * stats.lifesteal);
        }

        void Face(Vector3 dir, float time)
        {
            dir = dir.Flat();
            if (dir.sqrMagnitude < 0.001f) return;
            facingDir = dir.normalized;
            facingTime = time;
        }

        // ---------------------------------------------------------------- skills
        void TryUseSkill()
        {
            var skill = Hero.skill;
            if (skill == null || skillTimer > 0f) return;
            skillTimer = skill.cooldown;
            Sfx.Play(SfxId.Skill, transform.position);
            switch (skill.skillType)
            {
                case SkillType.GroundSlam: StartCoroutine(GroundSlam(skill)); break;
                case SkillType.Whirlwind: StartCoroutine(Whirlwind(skill)); break;
                case SkillType.Fireball: StartCoroutine(Fireball(skill)); break;
                case SkillType.ArrowVolley: StartCoroutine(ArrowVolley(skill)); break;
                case SkillType.ShadowStrike: StartCoroutine(ShadowStrike(skill)); break;
                case SkillType.HealingNova: StartCoroutine(HealingNova(skill)); break;
            }
        }

        IEnumerator GroundSlam(SkillData skill)
        {
            actingTime = 0.6f;
            anim.Cast();
            yield return new WaitForSeconds(0.35f);
            Vector3 center = transform.position + transform.forward * 1.2f;
            var stats = Current;
            float dealt = CombatUtility.DamageArea(center, skill.radius, Team.Player, h =>
            {
                var d = CombatUtility.Roll(stats, skill.damageMultiplier, gameObject, Team.Player);
                d.knockback = (h.transform.position - center).Flat().normalized * skill.knockback + Vector3.up * 2f;
                d.stunDuration = skill.stunDuration;
                return d;
            });
            Vfx.Ring(center, 0.4f, skill.radius, skill.vfxColor, 0.45f);
            Vfx.Burst(center, 1.2f, skill.vfxColor, 0.3f);
            ThirdPersonCamera.Instance?.Shake(0.45f);
            Sfx.Play(SfxId.Explosion, center);
            Lifesteal(dealt, stats);
        }

        IEnumerator Whirlwind(SkillData skill)
        {
            int ticks = Mathf.Max(1, skill.count);
            for (int i = 0; i < ticks && !health.IsDead; i++)
            {
                anim.Attack(2f);
                var stats = Current;
                float dealt = CombatUtility.DamageArea(transform.position + Vector3.up, skill.radius, Team.Player, h =>
                {
                    var d = CombatUtility.Roll(stats, skill.damageMultiplier, gameObject, Team.Player);
                    d.knockback = (h.transform.position - transform.position).Flat().normalized * skill.knockback;
                    return d;
                });
                Vfx.Ring(transform.position, skill.radius * 0.5f, skill.radius, skill.vfxColor, 0.25f, transform);
                Sfx.Play(SfxId.Swing, transform.position);
                Lifesteal(dealt, stats);
                yield return new WaitForSeconds(0.28f);
            }
        }

        IEnumerator Fireball(SkillData skill)
        {
            actingTime = 0.5f;
            anim.Cast();
            var target = CombatUtility.FindNearest(transform.position, skill.range, Team.Player);
            if (target != null) Face(target.transform.position - transform.position, 0.6f);
            yield return new WaitForSeconds(0.3f);
            var stats = Current;
            Vector3 origin = transform.position + Vector3.up * 1.4f + transform.forward * 0.8f;
            Vector3 baseDir = target != null ? (target.transform.position + Vector3.up - origin) : transform.forward;
            int count = Mathf.Max(1, skill.count);
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-18f, 18f, i / (float)(count - 1));
                var dir = Quaternion.Euler(0, angle, 0) * baseDir;
                var dmg = CombatUtility.Roll(stats, skill.damageMultiplier, gameObject, Team.Player);
                dmg.stunDuration = skill.stunDuration;
                Projectile.Spawn(origin, dir, skill.projectileSpeed, dmg, skill.vfxColor, 0.45f, skill.radius, 3f);
            }
        }

        IEnumerator ArrowVolley(SkillData skill)
        {
            actingTime = 0.4f;
            anim.Attack(1.5f);
            var target = CombatUtility.FindNearest(transform.position, skill.range, Team.Player);
            if (target != null) Face(target.transform.position - transform.position, 0.5f);
            yield return new WaitForSeconds(0.15f);
            var stats = Current;
            Vector3 origin = transform.position + Vector3.up * 1.3f;
            Vector3 baseDir = target != null ? (target.transform.position + Vector3.up - origin).Flat() : transform.forward;
            int count = Mathf.Max(1, skill.count);
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-25f, 25f, i / (float)(count - 1));
                var dmg = CombatUtility.Roll(stats, skill.damageMultiplier, gameObject, Team.Player);
                Projectile.Spawn(origin, Quaternion.Euler(0, angle, 0) * baseDir, skill.projectileSpeed, dmg, skill.vfxColor, 0.18f, 0f, 2f, 0f, 2);
            }
        }

        IEnumerator ShadowStrike(SkillData skill)
        {
            var target = CombatUtility.FindNearest(transform.position, skill.range, Team.Player);
            if (target == null) { skillTimer = 0.5f; yield break; }
            health.Invulnerable = true;
            Vfx.Burst(transform.position + Vector3.up, 1f, skill.vfxColor, 0.3f);
            Vector3 behind = target.transform.position - target.transform.forward * 1.3f;
            controller.Teleport(behind);
            Face(target.transform.position - behind, 0.6f);
            transform.rotation = Quaternion.LookRotation((target.transform.position - behind).Flat().normalized + transform.forward * 0.001f);
            anim.Attack(1.6f);
            yield return new WaitForSeconds(0.12f);
            var stats = Current.Clone();
            stats.critChance = 1f;
            float dealt = CombatUtility.DamageArea(target.transform.position + Vector3.up, skill.radius, Team.Player, h =>
            {
                var d = CombatUtility.Roll(stats, h == target ? skill.damageMultiplier : skill.damageMultiplier * 0.5f, gameObject, Team.Player);
                d.knockback = transform.forward * skill.knockback;
                d.stunDuration = skill.stunDuration;
                return d;
            });
            Vfx.Burst(target.transform.position + Vector3.up, skill.radius, skill.vfxColor, 0.3f);
            Sfx.Play(SfxId.Hit, transform.position);
            Lifesteal(dealt, stats);
            yield return new WaitForSeconds(0.2f);
            health.Invulnerable = false;
        }

        IEnumerator HealingNova(SkillData skill)
        {
            actingTime = 0.5f;
            anim.Cast();
            yield return new WaitForSeconds(0.25f);
            health.Heal(health.maxHealth * skill.healPercent);
            var stats = Current;
            CombatUtility.DamageArea(transform.position + Vector3.up, skill.radius, Team.Player, h =>
            {
                var d = CombatUtility.Roll(stats, skill.damageMultiplier, gameObject, Team.Player);
                d.knockback = (h.transform.position - transform.position).Flat().normalized * skill.knockback;
                d.stunDuration = skill.stunDuration;
                return d;
            });
            Vfx.Ring(transform.position, 0.5f, skill.radius, skill.vfxColor, 0.6f, transform);
            Vfx.Burst(transform.position + Vector3.up, 1.5f, new Color(skill.vfxColor.r, skill.vfxColor.g, skill.vfxColor.b, 0.4f), 0.5f);
            Sfx.Play(SfxId.Heal, transform.position);
        }

        // ---------------------------------------------------------------- potions
        public ConsumableData FindPotion()
        {
            var profile = PlayerProfile.Instance;
            var db = GameDatabase.Instance;
            if (db.defaultPotion != null && profile.CountOf(db.defaultPotion) > 0) return db.defaultPotion;
            foreach (var stack in profile.Data.inventory)
                if (db.GetItem(stack.itemId) is ConsumableData c && c.effect == ConsumableEffect.Heal) return c;
            foreach (var stack in profile.Data.inventory)
                if (db.GetItem(stack.itemId) is ConsumableData c) return c;
            return null;
        }

        void TryUsePotion()
        {
            if (potionTimer > 0f) return;
            var potion = FindPotion();
            if (potion == null) return;
            if (potion.effect == ConsumableEffect.Heal && health.Normalized >= 0.999f) return;
            if (!PlayerProfile.Instance.ConsumeOne(potion)) return;
            potionTimer = PotionCooldown;
            UseConsumable(potion);
        }

        public void UseConsumable(ConsumableData c)
        {
            switch (c.effect)
            {
                case ConsumableEffect.Heal:
                    health.Heal(health.maxHealth * c.amount);
                    Vfx.Ring(transform.position, 0.3f, 1.8f, new Color(0.3f, 1f, 0.45f, 0.6f), 0.5f, transform);
                    Sfx.Play(SfxId.Heal, transform.position);
                    break;
                case ConsumableEffect.AttackBuff:
                    attackBuff = c.amount;
                    attackBuffTime = c.duration;
                    Vfx.Ring(transform.position, 0.3f, 1.8f, new Color(1f, 0.35f, 0.2f, 0.6f), 0.5f, transform);
                    Sfx.Play(SfxId.Skill, transform.position);
                    break;
                case ConsumableEffect.SpeedBuff:
                    controller.ApplySpeedBuff(c.amount, c.duration);
                    Vfx.Ring(transform.position, 0.3f, 1.8f, new Color(0.4f, 0.8f, 1f, 0.6f), 0.5f, transform);
                    Sfx.Play(SfxId.Skill, transform.position);
                    break;
            }
        }
    }
}
