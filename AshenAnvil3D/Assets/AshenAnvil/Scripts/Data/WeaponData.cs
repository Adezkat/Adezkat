using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    [CreateAssetMenu(menuName = "Ashen Anvil/Weapon", fileName = "Weapon_")]
    public class WeaponData : ItemData
    {
        [Header("Weapon")]
        public WeaponType weaponType;
        public float baseAttack = 10f;
        [Tooltip("Multiplier applied to the hero's attack speed.")]
        public float attackSpeed = 1f;
        public List<StatModifier> bonusStats = new List<StatModifier>();
        [Tooltip("Model shown in the hero's hand. Assigned by the project generator.")]
        public GameObject modelPrefab;

        public override string CategoryName => weaponType.ToString();

        public override void CollectModifiers(int upgradeLevel, List<StatModifier> into)
        {
            float s = UpgradeScale(upgradeLevel);
            into.Add(new StatModifier { stat = StatType.Attack, value = baseAttack * s });
            if (!Mathf.Approximately(attackSpeed, 1f))
                into.Add(new StatModifier { stat = StatType.AttackSpeed, value = attackSpeed - 1f, percent = true });
            foreach (var m in bonusStats)
                into.Add(new StatModifier { stat = m.stat, value = m.value * s, percent = m.percent });
        }
    }
}
