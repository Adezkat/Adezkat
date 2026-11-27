using System;
using System.Collections.Generic;

namespace AshenAnvil
{
    [Serializable]
    public class HeroProgress
    {
        public string heroId;
        public int level = 1;
        public int xp;
        public bool unlocked;
    }

    [Serializable]
    public class ItemStack
    {
        public int uid;
        public string itemId;
        public int count = 1;
        public int upgrade;
    }

    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public int progress;
        public bool claimed;
    }

    [Serializable]
    public class GameSettings
    {
        public int qualityLevel = 1;   // 0 low, 1 medium, 2 high
        public int targetFps = 60;
        public float sfxVolume = 0.8f;
        public float cameraSensitivity = 1f;
        public bool showDamageNumbers = true;
    }

    /// <summary>Everything persisted between sessions. Serialized with JsonUtility.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int gold;
        public string selectedHeroId;
        public int highestStageCleared;
        public int nextUid = 1;
        public List<HeroProgress> heroes = new List<HeroProgress>();
        public List<ItemStack> inventory = new List<ItemStack>();
        public int[] equippedUids = new int[6];
        public List<QuestProgress> quests = new List<QuestProgress>();
        public int monstersDefeated;
        public int bossesDefeated;
        public int totalGoldEarned;
        public GameSettings settings = new GameSettings();
    }
}
