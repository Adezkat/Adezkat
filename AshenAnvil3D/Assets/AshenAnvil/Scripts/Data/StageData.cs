using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    [Serializable]
    public class WaveEntry
    {
        public MonsterData monster;
        public int count = 3;
    }

    [Serializable]
    public class WaveData
    {
        public List<WaveEntry> entries = new List<WaveEntry>();
    }

    [CreateAssetMenu(menuName = "Ashen Anvil/Stage", fileName = "Stage_")]
    public class StageData : ScriptableObject
    {
        [Header("Identity")]
        public int stageNumber = 1;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public int recommendedPower = 100;

        [Header("Encounters")]
        [Tooltip("Monster stats are multiplied by this value.")]
        public float difficulty = 1f;
        public List<WaveData> waves = new List<WaveData>();
        public MonsterData boss;

        [Header("Rewards")]
        public int goldReward = 100;
        public int xpReward = 50;
        public ItemData firstClearReward;

        [Header("Atmosphere")]
        public Color groundColor = new Color(0.32f, 0.3f, 0.26f);
        public Color fogColor = new Color(0.35f, 0.28f, 0.25f);
        public float fogDensity = 0.012f;
        public Color ambientColor = new Color(0.45f, 0.4f, 0.38f);
        public Color sunColor = new Color(1f, 0.85f, 0.7f);
        public float sunIntensity = 1.2f;
    }
}
