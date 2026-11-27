using UnityEngine;

namespace AshenAnvil
{
    public enum Rarity { Common, Rare, Epic, Legendary, Mythic }
    public enum HeroClass { Warrior, Assassin, Mage, Archer, Support }
    public enum EquipmentSlot { Weapon, Helmet, Armor, Boots, Ring, Amulet }
    public enum WeaponType { Sword, Axe, Dagger, Staff, Bow, Hammer }
    public enum StatType { MaxHealth, Attack, Defense, MoveSpeed, CritChance, CritDamage, AttackSpeed, Lifesteal }
    public enum SkillType { GroundSlam, Whirlwind, Fireball, ArrowVolley, ShadowStrike, HealingNova }
    public enum MonsterBehaviour { Melee, Ranged, Charger, Boss }
    public enum ConsumableEffect { Heal, AttackBuff, SpeedBuff }
    public enum Team { Player, Enemy, Neutral }
    public enum HeadStyle { Hair, Hood, Helmet, Horns, Crown, WizardHat, Skull }
    public enum QuestGoal { DefeatMonsters, DefeatBosses, ClearStage, CollectGold, UpgradeItems, ReachHeroLevel }

    public static class RarityUtil
    {
        static readonly Color[] colors =
        {
            new Color(0.66f, 0.71f, 0.76f),
            new Color(0.25f, 0.64f, 1.00f),
            new Color(0.72f, 0.40f, 1.00f),
            new Color(1.00f, 0.71f, 0.15f),
            new Color(1.00f, 0.24f, 0.37f),
        };

        public static Color GetColor(Rarity r) => colors[Mathf.Clamp((int)r, 0, colors.Length - 1)];
        public static float StatMultiplier(Rarity r) => 1f + 0.25f * (int)r;
    }
}
