using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Persistent account state: gold, heroes, inventory, equipment and quests.
    /// Survives scene loads and saves automatically.
    /// </summary>
    public class PlayerProfile : MonoBehaviour
    {
        static PlayerProfile instance;
        public static PlayerProfile Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[PlayerProfile]");
                    instance = go.AddComponent<PlayerProfile>();
                    DontDestroyOnLoad(go);
                    instance.LoadOrCreate();
                }
                return instance;
            }
        }

        public static event Action Changed;
        public static event Action<HeroData, int> HeroLeveledUp;

        public SaveData Data { get; private set; }
        GameDatabase db;
        readonly List<StatModifier> modBuffer = new List<StatModifier>();

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
        }

        void LoadOrCreate()
        {
            db = GameDatabase.Instance;
            Data = SaveSystem.Load();
            if (Data == null) CreateNewSave();
            Sanitize();
        }

        void CreateNewSave()
        {
            Data = new SaveData { gold = db != null ? db.starterGold : 100 };
            if (db == null || db.heroes.Count == 0) return;
            var first = db.heroes[0];
            GetProgress(first).unlocked = true;
            Data.selectedHeroId = first.id;
            if (first.startingWeapon != null)
            {
                var stack = AddItem(first.startingWeapon, 1, 0, false);
                Data.equippedUids[(int)EquipmentSlot.Weapon] = stack.uid;
            }
            foreach (var item in db.starterItems)
                if (item != null) AddItem(item, item.IsStackable ? 3 : 1, 0, false);
            Save();
        }

        void Sanitize()
        {
            if (db == null) return;
            Data.inventory.RemoveAll(s => db.GetItem(s.itemId) == null || s.count <= 0);
            for (int i = 0; i < Data.equippedUids.Length; i++)
                if (Data.equippedUids[i] != 0 && FindStack(Data.equippedUids[i]) == null) Data.equippedUids[i] = 0;
            if (!IsUnlocked(SelectedHero) && db.heroes.Count > 0)
            {
                GetProgress(db.heroes[0]).unlocked = true;
                Data.selectedHeroId = db.heroes[0].id;
            }
        }

        public void Save() => SaveSystem.Save(Data);
        public static void NotifyChanged() => Changed?.Invoke();

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();

        public void ResetProgress()
        {
            SaveSystem.Delete();
            var settings = Data.settings;
            CreateNewSave();
            Data.settings = settings;
            Save();
            NotifyChanged();
        }

        // ---------------------------------------------------------------- Gold
        public int Gold => Data.gold;

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Data.gold += amount;
            Data.totalGoldEarned += amount;
            ReportQuest(QuestGoal.CollectGold, amount);
            NotifyChanged();
        }

        public bool SpendGold(int amount)
        {
            if (Data.gold < amount) return false;
            Data.gold -= amount;
            NotifyChanged();
            return true;
        }

        // ---------------------------------------------------------------- Heroes
        public HeroData SelectedHero => db.GetHero(Data.selectedHeroId);

        public HeroProgress GetProgress(HeroData hero)
        {
            var p = Data.heroes.Find(h => h.heroId == hero.id);
            if (p == null)
            {
                p = new HeroProgress { heroId = hero.id, unlocked = hero.unlockCost <= 0 };
                Data.heroes.Add(p);
            }
            return p;
        }

        public bool IsUnlocked(HeroData hero) => hero != null && GetProgress(hero).unlocked;

        public bool UnlockHero(HeroData hero)
        {
            if (IsUnlocked(hero) || !SpendGold(hero.unlockCost)) return false;
            GetProgress(hero).unlocked = true;
            if (hero.startingWeapon != null) AddItem(hero.startingWeapon, 1, 0, false);
            Save();
            NotifyChanged();
            return true;
        }

        public void SelectHero(HeroData hero)
        {
            if (!IsUnlocked(hero)) return;
            Data.selectedHeroId = hero.id;
            Save();
            NotifyChanged();
        }

        public static int XpToNext(int level) => Mathf.RoundToInt(60f + 40f * Mathf.Pow(level, 1.5f));
        public const int MaxHeroLevel = 60;

        /// <summary>Adds XP and returns the number of levels gained.</summary>
        public int AddHeroXp(HeroData hero, int xp)
        {
            var p = GetProgress(hero);
            int gained = 0;
            p.xp += xp;
            while (p.level < MaxHeroLevel && p.xp >= XpToNext(p.level))
            {
                p.xp -= XpToNext(p.level);
                p.level++;
                gained++;
            }
            if (p.level >= MaxHeroLevel) p.xp = 0;
            if (gained > 0)
            {
                ReportQuestMax(QuestGoal.ReachHeroLevel, p.level);
                HeroLeveledUp?.Invoke(hero, p.level);
            }
            NotifyChanged();
            return gained;
        }

        // ---------------------------------------------------------------- Inventory
        public ItemStack FindStack(int uid) => Data.inventory.Find(s => s.uid == uid);

        public ItemStack AddItem(ItemData item, int count = 1, int upgrade = 0, bool notify = true)
        {
            ItemStack stack = null;
            if (item.IsStackable)
            {
                stack = Data.inventory.Find(s => s.itemId == item.id);
                if (stack != null) stack.count = Mathf.Min(item.maxStack, stack.count + count);
            }
            if (stack == null)
            {
                stack = new ItemStack { uid = Data.nextUid++, itemId = item.id, count = item.IsStackable ? count : 1, upgrade = upgrade };
                Data.inventory.Add(stack);
                for (int i = 1; i < count && !item.IsStackable; i++)
                    Data.inventory.Add(new ItemStack { uid = Data.nextUid++, itemId = item.id, count = 1, upgrade = upgrade });
            }
            if (notify) NotifyChanged();
            return stack;
        }

        public int CountOf(ItemData item)
        {
            int n = 0;
            foreach (var s in Data.inventory) if (s.itemId == item.id) n += s.count;
            return n;
        }

        public bool ConsumeOne(ItemData item)
        {
            var stack = Data.inventory.Find(s => s.itemId == item.id);
            if (stack == null) return false;
            stack.count--;
            if (stack.count <= 0) Data.inventory.Remove(stack);
            NotifyChanged();
            return true;
        }

        public bool IsEquipped(int uid) => Array.IndexOf(Data.equippedUids, uid) >= 0;

        public ItemStack GetEquipped(EquipmentSlot slot)
        {
            int uid = Data.equippedUids[(int)slot];
            return uid == 0 ? null : FindStack(uid);
        }

        public static bool TryGetSlot(ItemData item, out EquipmentSlot slot)
        {
            if (item is WeaponData) { slot = EquipmentSlot.Weapon; return true; }
            if (item is EquipmentData e) { slot = e.slot; return true; }
            slot = EquipmentSlot.Weapon;
            return false;
        }

        public bool Equip(int uid)
        {
            var stack = FindStack(uid);
            if (stack == null) return false;
            if (!TryGetSlot(db.GetItem(stack.itemId), out var slot)) return false;
            Data.equippedUids[(int)slot] = uid;
            Save();
            NotifyChanged();
            return true;
        }

        public void Unequip(EquipmentSlot slot)
        {
            Data.equippedUids[(int)slot] = 0;
            NotifyChanged();
        }

        public static int UpgradeCost(ItemData item, int currentLevel) =>
            Mathf.RoundToInt((40 + 35 * currentLevel * currentLevel) * (1f + 0.5f * (int)item.rarity));

        public const int MaxUpgrade = 15;

        public bool UpgradeItem(int uid)
        {
            var stack = FindStack(uid);
            if (stack == null) return false;
            var item = db.GetItem(stack.itemId);
            if (item == null || item.IsStackable || stack.upgrade >= MaxUpgrade) return false;
            if (!SpendGold(UpgradeCost(item, stack.upgrade))) return false;
            stack.upgrade++;
            ReportQuest(QuestGoal.UpgradeItems, 1);
            Save();
            NotifyChanged();
            return true;
        }

        public int SellValue(ItemStack stack)
        {
            var item = db.GetItem(stack.itemId);
            return item == null ? 0 : Mathf.RoundToInt(item.sellPrice * (1f + 0.3f * stack.upgrade));
        }

        public bool SellItem(int uid)
        {
            var stack = FindStack(uid);
            if (stack == null || IsEquipped(uid)) return false;
            int value = SellValue(stack);
            stack.count--;
            if (stack.count <= 0) Data.inventory.Remove(stack);
            Data.gold += value;
            Save();
            NotifyChanged();
            return true;
        }

        // ---------------------------------------------------------------- Stats
        /// <summary>Final stats for a hero including level growth and equipped gear.</summary>
        public StatBlock ComputeStats(HeroData hero)
        {
            var stats = hero.baseStats.Clone();
            int level = GetProgress(hero).level;
            stats.AddScaled(hero.growthPerLevel, level - 1);

            modBuffer.Clear();
            bool hasWeapon = false;
            for (int i = 0; i < Data.equippedUids.Length; i++)
            {
                var stack = GetEquipped((EquipmentSlot)i);
                if (stack == null) continue;
                var item = db.GetItem(stack.itemId);
                if (item == null) continue;
                if (item is WeaponData) hasWeapon = true;
                item.CollectModifiers(stack.upgrade, modBuffer);
            }
            if (!hasWeapon && hero.startingWeapon != null) hero.startingWeapon.CollectModifiers(0, modBuffer);

            foreach (var m in modBuffer) if (!m.percent) stats.Add(m.stat, m.value);
            foreach (var m in modBuffer)
            {
                if (!m.percent) continue;
                if (StatBlock.IsPercentStat(m.stat)) stats.Add(m.stat, m.value);
                else stats.Set(m.stat, stats.Get(m.stat) * (1f + m.value));
            }
            stats.Clamp();
            return stats;
        }

        public WeaponData EquippedWeaponFor(HeroData hero)
        {
            var stack = GetEquipped(EquipmentSlot.Weapon);
            var w = stack != null ? db.GetItem(stack.itemId) as WeaponData : null;
            return w != null ? w : hero.startingWeapon;
        }

        // ---------------------------------------------------------------- Quests
        public QuestProgress GetQuest(QuestData q)
        {
            var p = Data.quests.Find(x => x.questId == q.id);
            if (p == null) { p = new QuestProgress { questId = q.id }; Data.quests.Add(p); }
            return p;
        }

        public void ReportQuest(QuestGoal goal, int amount)
        {
            if (db == null) return;
            foreach (var q in db.quests)
                if (q != null && q.goal == goal) { var p = GetQuest(q); p.progress = Mathf.Min(q.target, p.progress + amount); }
        }

        public void ReportQuestMax(QuestGoal goal, int value)
        {
            if (db == null) return;
            foreach (var q in db.quests)
                if (q != null && q.goal == goal) { var p = GetQuest(q); p.progress = Mathf.Min(q.target, Mathf.Max(p.progress, value)); }
        }

        public bool ClaimQuest(QuestData q)
        {
            var p = GetQuest(q);
            if (p.claimed || p.progress < q.target) return false;
            p.claimed = true;
            Data.gold += q.goldReward;
            if (q.rewardItem != null) AddItem(q.rewardItem, 1, 0, false);
            Save();
            NotifyChanged();
            return true;
        }

        public int ClaimableQuestCount()
        {
            int n = 0;
            if (db == null) return 0;
            foreach (var q in db.quests) { var p = GetQuest(q); if (!p.claimed && p.progress >= q.target) n++; }
            return n;
        }

        // ---------------------------------------------------------------- Stage results
        public void RecordMonsterKill(bool boss)
        {
            Data.monstersDefeated++;
            ReportQuest(QuestGoal.DefeatMonsters, 1);
            if (boss) { Data.bossesDefeated++; ReportQuest(QuestGoal.DefeatBosses, 1); }
        }

        public bool RecordStageClear(StageData stage)
        {
            bool first = stage.stageNumber > Data.highestStageCleared;
            if (first) Data.highestStageCleared = stage.stageNumber;
            ReportQuestMax(QuestGoal.ClearStage, Data.highestStageCleared);
            return first;
        }

        public bool IsStageUnlocked(StageData stage) => stage.stageNumber <= Data.highestStageCleared + 1;
    }
}
