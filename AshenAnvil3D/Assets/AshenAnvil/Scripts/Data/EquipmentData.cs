using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    [CreateAssetMenu(menuName = "Ashen Anvil/Equipment", fileName = "Equip_")]
    public class EquipmentData : ItemData
    {
        [Header("Equipment")]
        public EquipmentSlot slot = EquipmentSlot.Armor;
        public List<StatModifier> stats = new List<StatModifier>();

        public override string CategoryName => slot.ToString();

        public override void CollectModifiers(int upgradeLevel, List<StatModifier> into)
        {
            float s = UpgradeScale(upgradeLevel);
            foreach (var m in stats)
                into.Add(new StatModifier { stat = m.stat, value = m.value * s, percent = m.percent });
        }
    }
}
