using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>
    /// The camp menu: Campaign, Heroes, Inventory (equip / forge / sell), Quests and Settings.
    /// Built entirely with Unity UI at runtime and refreshed whenever the profile changes.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        enum Screen { Campaign, Heroes, Inventory, Quests, Settings }

        public MenuHeroPreview preview;

        GameDatabase db;
        PlayerProfile profile;
        RectTransform content;
        Text goldText, powerText, heroLabel;
        readonly Dictionary<Screen, Button> navButtons = new Dictionary<Screen, Button>();
        Screen current = Screen.Campaign;
        HeroData viewedHero;
        int selectedUid;
        bool dirty;
        bool confirmReset;

        void Start()
        {
            db = GameDatabase.Instance;
            profile = PlayerProfile.Instance;
            Time.timeScale = 1f;
            if (preview == null) preview = Compat.FindOne<MenuHeroPreview>();
            viewedHero = profile.SelectedHero;
            BuildFrame();
            ShowScreen(Screen.Campaign);
            PlayerProfile.Changed += MarkDirty;
        }

        void OnDestroy() => PlayerProfile.Changed -= MarkDirty;
        void MarkDirty() => dirty = true;

        void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;
            Refresh();
        }

        // ------------------------------------------------------------------ frame
        void BuildFrame()
        {
            var canvas = UIKit.CreateCanvas("MenuCanvas");
            var safe = UIKit.SafeArea(canvas.transform);

            var top = UIKit.Panel(safe, UIKit.Bg, "TopBar", UIKit.Square);
            UIKit.Anchor(top.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -110), Vector2.zero);
            var title = UIKit.Label(top.transform, "ASHEN ANVIL  <size=30><color=#b8a89a>Legends</color></size>", 54, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Stretch(title.rectTransform, 36, 0, 900, 0);
            goldText = UIKit.Label(top.transform, "", 40, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.Stretch(goldText.rectTransform, 900, 0, 36, 0);
            powerText = UIKit.Label(top.transform, "", 32, UIKit.TextColor, TextAnchor.MiddleRight);
            UIKit.Stretch(powerText.rectTransform, 700, 0, 380, 0);

            var nav = UIKit.Panel(safe, UIKit.Bg, "Nav", UIKit.Square);
            UIKit.Anchor(nav.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(270, -110));
            var navStack = UIKit.Rect("Stack", nav.transform);
            UIKit.Stretch(navStack, 20, 30, 20, 30);
            UIKit.VStack(navStack, 18);
            AddNav(navStack, Screen.Campaign, "Campaign");
            AddNav(navStack, Screen.Heroes, "Heroes");
            AddNav(navStack, Screen.Inventory, "Inventory");
            AddNav(navStack, Screen.Quests, "Quests");
            AddNav(navStack, Screen.Settings, "Settings");

            heroLabel = UIKit.Outline(UIKit.Label(safe, "", 34, UIKit.TextColor, TextAnchor.LowerCenter, FontStyle.Bold, "HeroLabel"), 2);
            UIKit.Anchor(heroLabel.rectTransform, new Vector2(0, 0), new Vector2(0.45f, 0), new Vector2(270, 30), new Vector2(0, 140));

            var contentBg = UIKit.Panel(safe, UIKit.PanelColor, "Content");
            UIKit.Anchor(contentBg.rectTransform, new Vector2(0.45f, 0), new Vector2(1, 1), new Vector2(0, 24), new Vector2(-24, -130));
            content = UIKit.Rect("Inner", contentBg.transform);
            UIKit.Stretch(content, 30, 30, 30, 30);
        }

        void AddNav(RectTransform parent, Screen s, string label)
        {
            var b = UIKit.Button(parent, label, () => ShowScreen(s), UIKit.ButtonColor, 34);
            UIKit.Layout(b, 110);
            navButtons[s] = b;
        }

        void ShowScreen(Screen s)
        {
            current = s;
            confirmReset = false;
            foreach (var kv in navButtons)
                kv.Value.GetComponent<Image>().color = kv.Key == s ? UIKit.AccentDark : UIKit.ButtonColor;
            Refresh();
        }

        void Refresh()
        {
            goldText.text = "Gold  " + profile.Gold.ToString("N0");
            var hero = profile.SelectedHero;
            powerText.text = $"Power {profile.ComputeStats(hero).Power:N0}";
            int claimable = profile.ClaimableQuestCount();
            UIKit.SetButtonLabel(navButtons[Screen.Quests], claimable > 0 ? $"Quests ({claimable})" : "Quests");

            var shownHero = current == Screen.Heroes ? viewedHero : hero;
            if (preview != null) preview.Show(shownHero);
            var p = profile.GetProgress(shownHero);
            heroLabel.text = $"{UIKit.Colored(shownHero.displayName, RarityUtil.GetColor(shownHero.rarity))}\n<size=26>{shownHero.title}" +
                             (p.unlocked ? $"  -  Lv {p.level}" : "  -  Locked") + "</size>";

            UIKit.Clear(content);
            switch (current)
            {
                case Screen.Campaign: BuildCampaign(); break;
                case Screen.Heroes: BuildHeroes(); break;
                case Screen.Inventory: BuildInventory(); break;
                case Screen.Quests: BuildQuests(); break;
                case Screen.Settings: BuildSettings(); break;
            }
        }

        RectTransform Header(string text, string sub = null)
        {
            var t = UIKit.Label(content, text, 46, UIKit.Accent, TextAnchor.UpperLeft, FontStyle.Bold, "Header");
            UIKit.Anchor(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), Vector2.zero);
            if (sub != null)
            {
                var s = UIKit.Label(content, sub, 26, UIKit.TextDim, TextAnchor.UpperRight);
                UIKit.Anchor(s.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, -10));
            }
            var body = UIKit.Rect("Body", content);
            UIKit.Stretch(body, 0, 76, 0, 0);
            return body;
        }

        // ------------------------------------------------------------------ campaign
        void BuildCampaign()
        {
            int power = profile.ComputeStats(profile.SelectedHero).Power;
            var body = Header("Campaign", $"Cleared {profile.Data.highestStageCleared}/{db.stages.Count}");
            var list = UIKit.ScrollList(body, 14, 12);
            for (int i = 0; i < db.stages.Count; i++)
            {
                var stage = db.stages[i];
                int index = i;
                bool unlocked = profile.IsStageUnlocked(stage);
                bool cleared = stage.stageNumber <= profile.Data.highestStageCleared;

                var card = UIKit.Panel(list, unlocked ? UIKit.PanelLight : new Color(0.1f, 0.09f, 0.09f, 0.9f), "Stage" + stage.stageNumber);
                UIKit.Layout(card, 190);
                var swatch = UIKit.Panel(card.transform, stage.groundColor, "Swatch");
                UIKit.Anchor(swatch.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(14, 14), new Vector2(34, -14));

                string status = cleared ? UIKit.Colored("  CLEARED", UIKit.Good) : unlocked ? "" : UIKit.Colored("  LOCKED", UIKit.TextDim);
                var name = UIKit.Label(card.transform, $"{stage.stageNumber}. {stage.displayName}{status}", 36, UIKit.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Stretch(name.rectTransform, 56, 16, 280, 100);
                var desc = UIKit.Label(card.transform, stage.description, 24, UIKit.TextDim, TextAnchor.UpperLeft);
                UIKit.Stretch(desc.rectTransform, 56, 66, 280, 50);
                Color pc = power >= stage.recommendedPower ? UIKit.Good : power >= stage.recommendedPower * 0.75f ? UIKit.Gold : UIKit.Bad;
                string bossName = stage.boss != null ? stage.boss.displayName : "-";
                var info = UIKit.Label(card.transform,
                    $"Recommended power {UIKit.Colored(stage.recommendedPower.ToString("N0"), pc)}   Waves {stage.waves.Count}   Boss: {bossName}",
                    24, UIKit.TextColor, TextAnchor.LowerLeft);
                UIKit.Stretch(info.rectTransform, 56, 120, 280, 16);

                var play = UIKit.Button(card.transform, unlocked ? "FIGHT" : "LOCKED", () => StartStage(index), unlocked ? UIKit.AccentDark : UIKit.ButtonColor, 38);
                play.interactable = unlocked;
                UIKit.Place((RectTransform)play.transform, new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(230, 110), new Vector2(1, 0.5f));
            }
        }

        void StartStage(int index)
        {
            GameSession.StageIndex = index;
            profile.Save();
            SceneManager.LoadScene(SceneNames.Game);
        }

        // ------------------------------------------------------------------ heroes
        void BuildHeroes()
        {
            var body = Header("Heroes", $"{db.heroes.FindAll(h => profile.IsUnlocked(h)).Count}/{db.heroes.Count} recruited");
            var listArea = UIKit.Rect("List", body);
            UIKit.Anchor(listArea, Vector2.zero, new Vector2(0.42f, 1));
            var list = UIKit.ScrollList(listArea, 10, 10);
            foreach (var hero in db.heroes)
            {
                var h = hero;
                var p = profile.GetProgress(h);
                bool selected = h == profile.SelectedHero;
                string label = $"{UIKit.Colored(h.displayName, RarityUtil.GetColor(h.rarity))}\n<size=22>{h.heroClass}  -  " +
                               (p.unlocked ? $"Lv {p.level}" : $"{h.unlockCost:N0} gold") + (selected ? "  -  ACTIVE" : "") + "</size>";
                var b = UIKit.Button(list, "", () => { viewedHero = h; Refresh(); }, h == viewedHero ? UIKit.AccentDark : UIKit.ButtonColor, 30);
                var t = b.GetComponentInChildren<Text>();
                t.text = label;
                t.alignment = TextAnchor.MiddleLeft;
                t.fontStyle = FontStyle.Normal;
                t.rectTransform.offsetMin = new Vector2(24, 4);
                UIKit.Layout(b, 120);
            }

            var detail = UIKit.Panel(body, new Color(0, 0, 0, 0.25f), "Detail");
            UIKit.Anchor(detail.rectTransform, new Vector2(0.44f, 0), Vector2.one);
            var stack = UIKit.Rect("Stack", detail.transform);
            UIKit.Stretch(stack, 26, 20, 26, 20);
            UIKit.VStack(stack, 8);

            var vh = viewedHero;
            var prog = profile.GetProgress(vh);
            var stats = prog.unlocked ? profile.ComputeStats(vh) : vh.baseStats;
            UIKit.Layout(UIKit.Label(stack, $"{UIKit.Colored(vh.rarity.ToString(), RarityUtil.GetColor(vh.rarity))} {vh.heroClass}", 28, UIKit.TextColor), 40);
            if (prog.unlocked)
                UIKit.Layout(UIKit.Label(stack, $"Level {prog.level}   XP {prog.xp}/{PlayerProfile.XpToNext(prog.level)}", 26, UIKit.TextDim), 36);
            var sb = new StringBuilder();
            sb.AppendLine($"Health {stats.maxHealth:N0}    Attack {stats.attack:N0}    Defense {stats.defense:N0}");
            sb.AppendLine($"Crit {stats.critChance * 100f:0}%  x{1f + stats.critDamage:0.0#}    Speed {stats.moveSpeed:0.0}    Atk Spd {stats.attackSpeed:0.00}");
            if (stats.lifesteal > 0) sb.AppendLine($"Lifesteal {stats.lifesteal * 100f:0}%");
            UIKit.Layout(UIKit.Label(stack, sb.ToString().TrimEnd(), 26, UIKit.TextColor), 90);
            if (vh.skill != null)
                UIKit.Layout(UIKit.Label(stack, $"{UIKit.Colored("Skill: " + vh.skill.displayName, UIKit.Accent)}  ({vh.skill.cooldown:0}s)\n{vh.skill.description}", 25, UIKit.TextColor), 110);
            UIKit.Layout(UIKit.Label(stack, $"<i>{vh.lore}</i>", 23, UIKit.TextDim), 110);

            Button action;
            if (!prog.unlocked)
            {
                bool afford = profile.Gold >= vh.unlockCost;
                action = UIKit.Button(stack, $"Recruit  ({vh.unlockCost:N0} gold)", () => profile.UnlockHero(vh), afford ? UIKit.AccentDark : UIKit.ButtonColor, 34);
                action.interactable = afford;
            }
            else if (vh == profile.SelectedHero)
            {
                action = UIKit.Button(stack, "Active Hero", null, UIKit.ButtonColor, 34);
                action.interactable = false;
            }
            else action = UIKit.Button(stack, "Set as Active Hero", () => profile.SelectHero(vh), UIKit.AccentDark, 34);
            UIKit.Layout(action, 100);
        }

        // ------------------------------------------------------------------ inventory
        void BuildInventory()
        {
            var hero = profile.SelectedHero;
            var body = Header("Inventory & Forge", $"{profile.Data.inventory.Count} items");

            var slotsRow = UIKit.Rect("Slots", body);
            UIKit.Anchor(slotsRow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), Vector2.zero);
            UIKit.HStack(slotsRow, 10);
            for (int i = 0; i < 6; i++)
            {
                var slot = (EquipmentSlot)i;
                var stack = profile.GetEquipped(slot);
                var item = stack != null ? db.GetItem(stack.itemId) : null;
                if (slot == EquipmentSlot.Weapon && item == null) item = hero.startingWeapon;
                string text = $"<size=20>{slot}</size>\n" + (item != null
                    ? UIKit.Colored(Short(item.displayName), RarityUtil.GetColor(item.rarity)) + (stack != null && stack.upgrade > 0 ? $" +{stack.upgrade}" : "")
                    : UIKit.Colored("empty", UIKit.TextDim));
                var b = UIKit.Button(slotsRow, "", () => { if (stack != null) { selectedUid = stack.uid; Refresh(); } }, stack != null && stack.uid == selectedUid ? UIKit.AccentDark : UIKit.ButtonColor, 22);
                var t = b.GetComponentInChildren<Text>();
                t.text = text;
                t.fontStyle = FontStyle.Normal;
            }

            var listArea = UIKit.Rect("List", body);
            UIKit.Anchor(listArea, Vector2.zero, new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, -135));
            var list = UIKit.ScrollList(listArea, 8, 8);
            var sorted = new List<ItemStack>(profile.Data.inventory);
            sorted.Sort((a, b) =>
            {
                var ia = db.GetItem(a.itemId); var ib = db.GetItem(b.itemId);
                int r = ib.rarity.CompareTo(ia.rarity);
                return r != 0 ? r : string.CompareOrdinal(ia.displayName, ib.displayName);
            });
            if (selectedUid != 0 && profile.FindStack(selectedUid) == null) selectedUid = 0;
            if (selectedUid == 0 && sorted.Count > 0) selectedUid = sorted[0].uid;
            foreach (var s in sorted)
            {
                var item = db.GetItem(s.itemId);
                int uid = s.uid;
                string equipped = profile.IsEquipped(uid) ? UIKit.Colored("  [E]", UIKit.Good) : "";
                string count = item.IsStackable ? $"  x{s.count}" : s.upgrade > 0 ? $"  +{s.upgrade}" : "";
                var b = UIKit.Button(list, "", () => { selectedUid = uid; Refresh(); }, uid == selectedUid ? UIKit.AccentDark : UIKit.ButtonColor, 26);
                var t = b.GetComponentInChildren<Text>();
                t.text = $"{UIKit.Colored(item.displayName, RarityUtil.GetColor(item.rarity))}{count}{equipped}\n<size=20>{item.rarity} {item.CategoryName}</size>";
                t.alignment = TextAnchor.MiddleLeft;
                t.fontStyle = FontStyle.Normal;
                t.rectTransform.offsetMin = new Vector2(20, 4);
                UIKit.Layout(b, 92);
            }

            var detail = UIKit.Panel(body, new Color(0, 0, 0, 0.25f), "Detail");
            UIKit.Anchor(detail.rectTransform, new Vector2(0.52f, 0), Vector2.one, Vector2.zero, new Vector2(0, -135));
            var dstack = UIKit.Rect("Stack", detail.transform);
            UIKit.Stretch(dstack, 24, 18, 24, 18);
            UIKit.VStack(dstack, 10);

            var sel = profile.FindStack(selectedUid);
            if (sel == null)
            {
                UIKit.Layout(UIKit.Label(dstack, "Your pack is empty. Defeat monsters to find loot.", 28, UIKit.TextDim), 80);
                return;
            }
            var selItem = db.GetItem(sel.itemId);
            UIKit.Layout(UIKit.Label(dstack, UIKit.Colored(selItem.displayName + (sel.upgrade > 0 ? $" +{sel.upgrade}" : ""), RarityUtil.GetColor(selItem.rarity)), 36, UIKit.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold), 50);
            UIKit.Layout(UIKit.Label(dstack, $"{selItem.rarity} {selItem.CategoryName}", 24, UIKit.TextDim), 32);

            var mods = new List<StatModifier>();
            selItem.CollectModifiers(sel.upgrade, mods);
            var sb = new StringBuilder();
            foreach (var m in mods) sb.AppendLine(m.Describe());
            if (selItem is ConsumableData c)
                sb.AppendLine(c.effect == ConsumableEffect.Heal ? $"Restores {c.amount * 100f:0}% health" : $"+{c.amount * 100f:0}% {(c.effect == ConsumableEffect.AttackBuff ? "attack" : "move speed")} for {c.duration:0}s");
            UIKit.Layout(UIKit.Label(dstack, sb.ToString().TrimEnd(), 26, UIKit.Good), Mathf.Max(40, 32 * (mods.Count + 1)));
            UIKit.Layout(UIKit.Label(dstack, $"<i>{selItem.description}</i>", 23, UIKit.TextDim), 70);

            var row = UIKit.Rect("Actions", dstack);
            UIKit.HStack(row, 12);
            UIKit.Layout(row, 96);
            bool gear = PlayerProfile.TryGetSlot(selItem, out var selSlot);
            if (gear)
            {
                if (profile.IsEquipped(sel.uid)) UIKit.Button(row, "Unequip", () => profile.Unequip(selSlot), null, 30);
                else UIKit.Button(row, "Equip", () => profile.Equip(sel.uid), UIKit.AccentDark, 30);

                if (sel.upgrade < PlayerProfile.MaxUpgrade)
                {
                    int cost = PlayerProfile.UpgradeCost(selItem, sel.upgrade);
                    var up = UIKit.Button(row, $"Forge +1\n<size=20>{cost:N0} gold</size>", () =>
                    {
                        if (profile.UpgradeItem(sel.uid)) Sfx.Play(SfxId.LevelUp);
                    }, null, 28);
                    up.interactable = profile.Gold >= cost;
                }
            }
            var sell = UIKit.Button(row, $"Sell\n<size=20>{profile.SellValue(sel)} gold</size>", () => profile.SellItem(sel.uid), null, 28);
            sell.interactable = !profile.IsEquipped(sel.uid);
        }

        static string Short(string s) => s.Length > 16 ? s.Substring(0, 15) + "." : s;

        // ------------------------------------------------------------------ quests
        void BuildQuests()
        {
            var body = Header("Quests");
            var list = UIKit.ScrollList(body, 12, 12);
            foreach (var q in db.quests)
            {
                var quest = q;
                var p = profile.GetQuest(q);
                var card = UIKit.Panel(list, UIKit.PanelLight, "Quest");
                UIKit.Layout(card, 150);
                var title = UIKit.Label(card.transform, q.title, 32, p.claimed ? UIKit.TextDim : UIKit.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Stretch(title.rectTransform, 24, 14, 300, 90);
                var desc = UIKit.Label(card.transform, q.description, 22, UIKit.TextDim, TextAnchor.UpperLeft);
                UIKit.Stretch(desc.rectTransform, 24, 58, 300, 50);
                var bar = UIKit.Bar(card.transform, new Color(0, 0, 0, 0.5f), UIKit.Accent, out var fill);
                UIKit.Anchor(bar.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(24, 16), new Vector2(-300, 44));
                fill.fillAmount = p.progress / (float)Mathf.Max(1, q.target);
                var pt = UIKit.Outline(UIKit.Label(bar.transform, $"{p.progress}/{q.target}", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
                UIKit.Stretch(pt.rectTransform);

                string reward = $"{q.goldReward} gold" + (q.rewardItem != null ? " + " + q.rewardItem.displayName : "");
                bool ready = !p.claimed && p.progress >= q.target;
                var b = UIKit.Button(card.transform, p.claimed ? "Claimed" : ready ? "Claim" : reward, () => profile.ClaimQuest(quest),
                    ready ? UIKit.AccentDark : UIKit.ButtonColor, ready ? 32 : 20);
                b.interactable = ready;
                UIKit.Place((RectTransform)b.transform, new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(260, 100), new Vector2(1, 0.5f));
            }
        }

        // ------------------------------------------------------------------ settings
        void BuildSettings()
        {
            var s = profile.Data.settings;
            var body = Header("Settings");
            var stack = UIKit.Rect("Stack", body);
            UIKit.Stretch(stack);
            UIKit.VStack(stack, 18);

            UIKit.Layout(UIKit.Label(stack, "Graphics quality", 30, UIKit.TextColor), 40);
            var q = UIKit.Rect("Quality", stack);
            UIKit.HStack(q, 12);
            UIKit.Layout(q, 90);
            string[] names = { "Low", "Medium", "High" };
            for (int i = 0; i < 3; i++)
            {
                int level = i;
                UIKit.Button(q, names[i], () => { s.qualityLevel = level; ApplySettings(); }, s.qualityLevel == i ? UIKit.AccentDark : UIKit.ButtonColor, 30);
            }

            UIKit.Layout(UIKit.Label(stack, "Frame rate", 30, UIKit.TextColor), 40);
            var f = UIKit.Rect("Fps", stack);
            UIKit.HStack(f, 12);
            UIKit.Layout(f, 90);
            foreach (int fps in new[] { 30, 60 })
            {
                int v = fps;
                UIKit.Button(f, fps + " FPS", () => { s.targetFps = v; ApplySettings(); }, s.targetFps == fps ? UIKit.AccentDark : UIKit.ButtonColor, 30);
            }

            UIKit.Layout(UIKit.Label(stack, "Sound volume", 30, UIKit.TextColor), 40);
            UIKit.Layout(UIKit.Slider(stack, 0f, 1f, s.sfxVolume, v => s.sfxVolume = v), 60);
            UIKit.Layout(UIKit.Label(stack, "Camera sensitivity", 30, UIKit.TextColor), 40);
            UIKit.Layout(UIKit.Slider(stack, 0.3f, 2.5f, s.cameraSensitivity, v => s.cameraSensitivity = v), 60);

            var row = UIKit.Rect("Row", stack);
            UIKit.HStack(row, 12);
            UIKit.Layout(row, 90);
            UIKit.Button(row, "Damage numbers: " + (s.showDamageNumbers ? "On" : "Off"), () => { s.showDamageNumbers = !s.showDamageNumbers; ApplySettings(); }, null, 28);
            UIKit.Button(row, confirmReset ? "Tap again to ERASE" : "Reset progress", () =>
            {
                if (confirmReset) { profile.ResetProgress(); viewedHero = profile.SelectedHero; confirmReset = false; }
                else { confirmReset = true; Refresh(); }
            }, confirmReset ? UIKit.Bad : (Color?)null, 28);

            UIKit.Layout(UIKit.Label(stack, $"Version {Application.version}   Unity {Application.unityVersion}", 22, UIKit.TextDim, TextAnchor.LowerLeft), 40);
        }

        void ApplySettings()
        {
            MobilePerformance.Apply(profile.Data.settings);
            profile.Save();
            Refresh();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) profile?.Save();
        }
    }
}
