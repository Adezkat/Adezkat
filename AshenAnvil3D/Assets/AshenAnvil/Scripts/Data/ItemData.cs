using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Base class for everything that can live in the inventory.</summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Rarity rarity;
        public Sprite icon;
        public Color tint = Color.white;

        [Header("Economy")]
        public int sellPrice = 10;
        public int maxStack = 1;

        public bool IsStackable => maxStack > 1;
        public abstract string CategoryName { get; }

        /// <summary>Stat bonuses this item grants at the given upgrade level (empty for non-gear).</summary>
        public virtual void CollectModifiers(int upgradeLevel, List<StatModifier> into) { }

        public static float UpgradeScale(int upgradeLevel) => 1f + 0.12f * upgradeLevel;
    }
}
