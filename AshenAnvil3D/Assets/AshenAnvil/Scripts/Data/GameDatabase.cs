using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Central registry of all game content. Lives at Resources/GameDatabase.asset so the
    /// runtime can find it without scene references.
    /// </summary>
    [CreateAssetMenu(menuName = "Ashen Anvil/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Content")]
        public List<HeroData> heroes = new List<HeroData>();
        public List<ItemData> items = new List<ItemData>();
        public List<SkillData> skills = new List<SkillData>();
        public List<MonsterData> monsters = new List<MonsterData>();
        public List<StageData> stages = new List<StageData>();
        public List<QuestData> quests = new List<QuestData>();

        [Header("New player")]
        public List<ItemData> starterItems = new List<ItemData>();
        public int starterGold = 150;
        public ConsumableData defaultPotion;

        [Header("Generated assets (filled by the project generator)")]
        public RuntimeAnimatorController characterAnimator;
        public Material litMaterial;
        public Material vfxMaterial;
        public Mesh coinMesh;
        public Mesh lootMesh;
        public Mesh potionMesh;

        static GameDatabase instance;
        Dictionary<string, ItemData> itemLookup;
        Dictionary<string, HeroData> heroLookup;

        public static GameDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameDatabase>("GameDatabase");
                    if (instance == null)
                        Debug.LogError("GameDatabase missing at Resources/GameDatabase.asset");
                }
                return instance;
            }
        }

        void OnEnable()
        {
            itemLookup = null;
            heroLookup = null;
        }

        public ItemData GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (itemLookup == null)
            {
                itemLookup = new Dictionary<string, ItemData>();
                foreach (var i in items) if (i != null && !string.IsNullOrEmpty(i.id)) itemLookup[i.id] = i;
                foreach (var h in heroes) if (h != null && h.startingWeapon != null) itemLookup[h.startingWeapon.id] = h.startingWeapon;
            }
            itemLookup.TryGetValue(id, out var item);
            return item;
        }

        public HeroData GetHero(string id)
        {
            if (heroLookup == null)
            {
                heroLookup = new Dictionary<string, HeroData>();
                foreach (var h in heroes) if (h != null) heroLookup[h.id] = h;
            }
            if (id != null && heroLookup.TryGetValue(id, out var hero)) return hero;
            return heroes.Count > 0 ? heroes[0] : null;
        }
    }
}
