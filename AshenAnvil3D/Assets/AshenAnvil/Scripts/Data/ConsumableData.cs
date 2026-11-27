using UnityEngine;

namespace AshenAnvil
{
    [CreateAssetMenu(menuName = "Ashen Anvil/Consumable", fileName = "Potion_")]
    public class ConsumableData : ItemData
    {
        [Header("Consumable")]
        public ConsumableEffect effect = ConsumableEffect.Heal;
        [Tooltip("Heal: fraction of max health. Buffs: bonus fraction (0.3 = +30%).")]
        public float amount = 0.35f;
        public float duration = 0f;

        public override string CategoryName => "Consumable";
    }
}
