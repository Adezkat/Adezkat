using System;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Hit points, defense mitigation and death for any damageable object.</summary>
    public class Health : MonoBehaviour
    {
        public Team team = Team.Enemy;
        public float maxHealth = 100f;
        public float defense = 0f;
        [Tooltip("Height of the health bar / damage numbers above the pivot.")]
        public float uiHeight = 2.4f;
        public bool showHealthBar = true;

        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public bool Invulnerable { get; set; }
        public float Normalized => maxHealth <= 0 ? 0 : Current / maxHealth;
        public Vector3 UiAnchor => transform.position + Vector3.up * uiHeight;

        public event Action<DamageInfo, float> Damaged;
        public event Action<float> Healed;
        public event Action<Health> Died;

        bool initialized;

        void Awake()
        {
            if (!initialized) Current = maxHealth;
        }

        public void Initialize(float max, float def, Team t)
        {
            maxHealth = Mathf.Max(1f, max);
            defense = def;
            team = t;
            Current = maxHealth;
            IsDead = false;
            initialized = true;
        }

        /// <summary>Changes max health (e.g. on level up) keeping the same fill ratio.</summary>
        public void SetMaxHealth(float max, bool refill)
        {
            float ratio = Normalized;
            maxHealth = Mathf.Max(1f, max);
            Current = refill ? maxHealth : Mathf.Max(1f, maxHealth * ratio);
        }

        /// <summary>Applies damage after defense mitigation. Returns the damage actually dealt.</summary>
        public float TakeDamage(DamageInfo info)
        {
            if (IsDead || Invulnerable || info.amount <= 0f) return 0f;
            float mitigated = info.amount * (100f / (100f + Mathf.Max(0f, defense)));
            mitigated = Mathf.Max(1f, Mathf.Round(mitigated));
            Current -= mitigated;
            Damaged?.Invoke(info, mitigated);
            CombatEvents.RaiseDamaged(this, mitigated, info.isCrit);
            if (Current <= 0f)
            {
                Current = 0f;
                IsDead = true;
                Died?.Invoke(this);
                CombatEvents.RaiseDied(this);
            }
            return mitigated;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            float before = Current;
            Current = Mathf.Min(maxHealth, Current + amount);
            float healed = Current - before;
            if (healed > 0.5f)
            {
                Healed?.Invoke(healed);
                CombatEvents.RaiseHealed(this, healed);
            }
        }

        public void Revive(float fraction)
        {
            IsDead = false;
            Current = Mathf.Max(1f, maxHealth * fraction);
        }
    }
}
