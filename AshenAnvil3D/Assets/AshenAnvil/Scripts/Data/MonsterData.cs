using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    [Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Range(0f, 1f)] public float chance = 0.1f;
    }

    [CreateAssetMenu(menuName = "Ashen Anvil/Monster", fileName = "Monster_")]
    public class MonsterData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public MonsterBehaviour behaviour;

        [Header("Combat")]
        public StatBlock baseStats = new StatBlock();
        public float aggroRange = 18f;
        public float attackRange = 2f;
        public float attackCooldown = 1.6f;
        public float projectileSpeed = 12f;
        public Color projectileColor = new Color(1f, 0.4f, 0.1f);
        [Tooltip("Boss only: minion summoned when below half health.")]
        public MonsterData summon;

        [Header("Rewards")]
        public int xpReward = 10;
        public int goldMin = 2;
        public int goldMax = 6;
        public List<LootEntry> loot = new List<LootEntry>();

        [Header("Visuals")]
        public CharacterLook look = new CharacterLook();
        public GameObject prefab;

        public bool IsBoss => behaviour == MonsterBehaviour.Boss;
    }
}
