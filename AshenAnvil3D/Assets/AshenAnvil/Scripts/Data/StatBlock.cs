using System;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Complete set of combat stats used by heroes, monsters and items.</summary>
    [Serializable]
    public class StatBlock
    {
        public float maxHealth = 100f;
        public float attack = 10f;
        public float defense = 5f;
        public float moveSpeed = 5f;
        [Range(0f, 1f)] public float critChance = 0.05f;
        public float critDamage = 0.5f;
        public float attackSpeed = 1f;
        [Range(0f, 1f)] public float lifesteal = 0f;

        public static StatBlock Zero() => new StatBlock
        {
            maxHealth = 0, attack = 0, defense = 0, moveSpeed = 0,
            critChance = 0, critDamage = 0, attackSpeed = 0, lifesteal = 0
        };

        public StatBlock Clone() => (StatBlock)MemberwiseClone();

        public float Get(StatType t)
        {
            switch (t)
            {
                case StatType.MaxHealth: return maxHealth;
                case StatType.Attack: return attack;
                case StatType.Defense: return defense;
                case StatType.MoveSpeed: return moveSpeed;
                case StatType.CritChance: return critChance;
                case StatType.CritDamage: return critDamage;
                case StatType.AttackSpeed: return attackSpeed;
                case StatType.Lifesteal: return lifesteal;
            }
            return 0f;
        }

        public void Set(StatType t, float v)
        {
            switch (t)
            {
                case StatType.MaxHealth: maxHealth = v; break;
                case StatType.Attack: attack = v; break;
                case StatType.Defense: defense = v; break;
                case StatType.MoveSpeed: moveSpeed = v; break;
                case StatType.CritChance: critChance = v; break;
                case StatType.CritDamage: critDamage = v; break;
                case StatType.AttackSpeed: attackSpeed = v; break;
                case StatType.Lifesteal: lifesteal = v; break;
            }
        }

        public void Add(StatType t, float v) => Set(t, Get(t) + v);

        public void AddScaled(StatBlock other, float scale)
        {
            foreach (StatType t in Enum.GetValues(typeof(StatType)))
                Add(t, other.Get(t) * scale);
        }

        public void Clamp()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            attack = Mathf.Max(1f, attack);
            defense = Mathf.Max(0f, defense);
            moveSpeed = Mathf.Clamp(moveSpeed, 1f, 12f);
            critChance = Mathf.Clamp01(critChance);
            critDamage = Mathf.Max(0f, critDamage);
            attackSpeed = Mathf.Clamp(attackSpeed, 0.3f, 3f);
            lifesteal = Mathf.Clamp(lifesteal, 0f, 0.5f);
        }

        /// <summary>Single number used for "recommended power" comparisons.</summary>
        public int Power => Mathf.RoundToInt(maxHealth * 0.1f + attack * 1.6f + defense * 1.2f +
                                             critChance * 250f + critDamage * 60f + (attackSpeed - 1f) * 120f + lifesteal * 300f);

        public static string Label(StatType t)
        {
            switch (t)
            {
                case StatType.MaxHealth: return "Health";
                case StatType.Attack: return "Attack";
                case StatType.Defense: return "Defense";
                case StatType.MoveSpeed: return "Move Speed";
                case StatType.CritChance: return "Crit Chance";
                case StatType.CritDamage: return "Crit Damage";
                case StatType.AttackSpeed: return "Attack Speed";
                case StatType.Lifesteal: return "Lifesteal";
            }
            return t.ToString();
        }

        public static bool IsPercentStat(StatType t) =>
            t == StatType.CritChance || t == StatType.CritDamage || t == StatType.AttackSpeed || t == StatType.Lifesteal;
    }

    /// <summary>A bonus granted by an item. Percent modifiers multiply the hero's total after flat bonuses.</summary>
    [Serializable]
    public class StatModifier
    {
        public StatType stat;
        public float value;
        public bool percent;

        public string Describe(float scale = 1f)
        {
            float v = value * scale;
            if (percent) return $"+{v * 100f:0.#}% {StatBlock.Label(stat)}";
            if (StatBlock.IsPercentStat(stat)) return $"+{v * 100f:0.#}% {StatBlock.Label(stat)}";
            return $"+{v:0.#} {StatBlock.Label(stat)}";
        }
    }
}
