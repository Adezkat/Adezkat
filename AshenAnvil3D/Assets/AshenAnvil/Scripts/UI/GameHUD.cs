using UnityEngine;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>In-battle interface: bars, touch controls, banners, pause and results.</summary>
    public class GameHUD : MonoBehaviour
    {
        GameSceneController game;
        RectTransform safe;
        Image hpFill, xpFill, bossFill;
        Text hpText, levelText, goldText, waveText, bannerText, bossName, buffText;
        GameObject bossPanel, pausePanel, resultsPanel, controls;
        ActionButton attackBtn, skillBtn, dodgeBtn, potionBtn;
        float bannerTime;
        EnemyAI boss;

        public static GameHUD Create(GameSceneController game)
        {
            var canvas = UIKit.CreateCanvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<GameHUD>();
            hud.game = game;
            hud.Build(canvas.transform);
            return hud;
        }

        void Build(Transform canvas)
        {
            HealthBarManager.Create(canvas);
            FloatingTextSpawner.Create(canvas);
            safe = UIKit.SafeArea(canvas);

            controls = UIKit.Rect("Controls", safe).gameObject;
            UIKit.Stretch((RectTransform)controls.transform);
            TouchLookArea.Create(controls.transform);
            VirtualJoystick.Create(controls.transform);
            BuildActionButtons(controls.transform);
            BuildTopLeft();
            BuildTopCenter();
            BuildTopRight();

            bannerText = UIKit.Outline(UIKit.Label(safe, "", 72, UIKit.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold, "Banner"), 3);
            UIKit.Place(bannerText.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1400, 140));

            buffText = UIKit.Outline(UIKit.Label(safe, "", 30, new Color(1f, 0.5f, 0.3f), TextAnchor.MiddleLeft, FontStyle.Bold, "Buff"));
            UIKit.Place(buffText.rectTransform, new Vector2(0, 1), new Vector2(30, -230), new Vector2(500, 40), new Vector2(0, 1));

            pausePanel = BuildPausePanel(canvas);
            pausePanel.SetActive(false);
        }

        void BuildTopLeft()
        {
            var panel = UIKit.Panel(safe, UIKit.PanelColor, "HeroPanel");
            UIKit.Place(panel.rectTransform, new Vector2(0, 1), new Vector2(24, -20), new Vector2(560, 180), new Vector2(0, 1));

            var hero = game.Hero;
            var portrait = UIKit.Panel(panel.transform, hero.look.primaryColor, "Portrait", UIKit.Circle);
            portrait.type = Image.Type.Simple;
            UIKit.Place(portrait.rectTransform, new Vector2(0, 0.5f), new Vector2(20, 0), Vector2.one * 130, new Vector2(0, 0.5f));
            var initial = UIKit.Outline(UIKit.Label(portrait.transform, hero.displayName.Substring(0, 1), 70, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            UIKit.Stretch(initial.rectTransform);

            var name = UIKit.Label(panel.transform, hero.displayName, 32, UIKit.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(name.rectTransform, new Vector2(0, 1), new Vector2(170, -14), new Vector2(370, 44), new Vector2(0, 1));
            levelText = UIKit.Label(panel.transform, "", 26, UIKit.Gold, TextAnchor.UpperRight, FontStyle.Bold);
            UIKit.Place(levelText.rectTransform, new Vector2(1, 1), new Vector2(-18, -18), new Vector2(150, 40), new Vector2(1, 1));

            var hp = UIKit.Bar(panel.transform, new Color(0, 0, 0, 0.6f), new Color(0.85f, 0.2f, 0.2f), out hpFill, "HP");
            UIKit.Place(hp.rectTransform, new Vector2(0, 1), new Vector2(170, -66), new Vector2(370, 42), new Vector2(0, 1));
            hpText = UIKit.Outline(UIKit.Label(hp.transform, "", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            UIKit.Stretch(hpText.rectTransform);

            var xp = UIKit.Bar(panel.transform, new Color(0, 0, 0, 0.6f), new Color(0.4f, 0.6f, 1f), out xpFill, "XP");
            UIKit.Place(xp.rectTransform, new Vector2(0, 1), new Vector2(170, -120), new Vector2(370, 18), new Vector2(0, 1));
        }

        void BuildTopCenter()
        {
            waveText = UIKit.Outline(UIKit.Label(safe, "", 36, UIKit.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold, "Wave"));
            UIKit.Place(waveText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(700, 50), new Vector2(0.5f, 1));
            var stageLabel = UIKit.Outline(UIKit.Label(safe, $"Stage {game.Stage.stageNumber}: {game.Stage.displayName}", 26, UIKit.TextDim, TextAnchor.MiddleCenter));
            UIKit.Place(stageLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(800, 40), new Vector2(0.5f, 1));

            var bp = UIKit.Panel(safe, UIKit.PanelColor, "BossPanel");
            bossPanel = bp.gameObject;
            UIKit.Place(bp.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(820, 90), new Vector2(0.5f, 1));
            bossName = UIKit.Label(bp.transform, "", 28, UIKit.Bad, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Stretch(bossName.rectTransform, 10, 6, 10, 40);
            var bar = UIKit.Bar(bp.transform, new Color(0, 0, 0, 0.6f), new Color(0.75f, 0.1f, 0.15f), out bossFill, "BossHP");
            UIKit.Anchor(bar.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 12), new Vector2(-20, 42));
            bossPanel.SetActive(false);
        }

        void BuildTopRight()
        {
            var goldPanel = UIKit.Panel(safe, UIKit.PanelColor, "Gold");
            UIKit.Place(goldPanel.rectTransform, new Vector2(1, 1), new Vector2(-150, -20), new Vector2(260, 80), new Vector2(1, 1));
            var coin = UIKit.Panel(goldPanel.transform, UIKit.Gold, "Coin", UIKit.Circle);
            coin.type = Image.Type.Simple;
            UIKit.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(18, 0), Vector2.one * 44, new Vector2(0, 0.5f));
            goldText = UIKit.Label(goldPanel.transform, "0", 34, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.Stretch(goldText.rectTransform, 70, 0, 18, 0);

            var pause = UIKit.Button(safe, "II", () => TogglePause(true), UIKit.PanelLight, 40, "Pause");
            UIKit.Place((RectTransform)pause.transform, new Vector2(1, 1), new Vector2(-24, -20), new Vector2(100, 80), new Vector2(1, 1));
        }

        void BuildActionButtons(Transform parent)
        {
            var hero = game.Hero;
            attackBtn = ActionButton.Create(parent, "ATTACK", new Vector2(-230, 230), 240, new Color(0.75f, 0.25f, 0.15f));
            attackBtn.onHold = held => PlayerInput.TouchAttackHeld = held;

            string skillName = hero.skill != null ? hero.skill.displayName : "SKILL";
            skillBtn = ActionButton.Create(parent, skillName.ToUpperInvariant(), new Vector2(-470, 170), 170,
                hero.skill != null ? hero.skill.vfxColor * 0.8f : UIKit.Accent);
            skillBtn.onPress = PlayerInput.QueueSkill;
            skillBtn.cooldown = () => game.Player != null && game.Player.Combat != null ? game.Player.Combat.SkillCooldownNormalized : 0f;

            dodgeBtn = ActionButton.Create(parent, "DODGE", new Vector2(-300, 470), 150, new Color(0.25f, 0.45f, 0.7f));
            dodgeBtn.onPress = PlayerInput.QueueDodge;
            dodgeBtn.cooldown = () => game.Player != null ? game.Player.DodgeCooldownNormalized : 0f;

            potionBtn = ActionButton.Create(parent, "POTION", new Vector2(-110, 470), 130, new Color(0.2f, 0.6f, 0.3f));
            potionBtn.onPress = PlayerInput.QueuePotion;
            potionBtn.cooldown = () => game.Player != null && game.Player.Combat != null ? game.Player.Combat.PotionCooldownNormalized : 0f;
        }

        GameObject BuildPausePanel(Transform canvas)
        {
            var dim = UIKit.Panel(canvas, new Color(0, 0, 0, 0.7f), "Pause", UIKit.Square);
            UIKit.Stretch(dim.rectTransform);
            var box = UIKit.Panel(dim.transform, UIKit.PanelColor, "Box");
            UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 620));
            var stack = UIKit.Rect("Stack", box.transform);
            UIKit.Stretch(stack, 50, 40, 50, 40);
            UIKit.VStack(stack, 24);
            UIKit.Layout(UIKit.Label(stack, "PAUSED", 60, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold), 100);
            UIKit.Layout(UIKit.Button(stack, "Resume", () => TogglePause(false), UIKit.AccentDark, 38), 110);
            UIKit.Layout(UIKit.Button(stack, "Restart Stage", () => game.Retry(), null, 38), 110);
            UIKit.Layout(UIKit.Button(stack, "Retreat to Camp", () => game.ExitToMenu(), null, 38), 110);
            return dim.gameObject;
        }

        public void TogglePause(bool paused)
        {
            if (game.Finished && paused) return;
            pausePanel.SetActive(paused);
            controls.SetActive(!paused);
            game.SetPaused(paused);
        }

        public void ShowBanner(string text, Color color)
        {
            bannerText.text = text;
            bannerText.color = color;
            bannerTime = 2f;
        }

        public void SetWaveText(string text) => waveText.text = text;

        public void ShowBoss(EnemyAI b)
        {
            boss = b;
            bossPanel.SetActive(true);
            bossName.text = b.data.displayName.ToUpperInvariant();
        }

        public void ShowResults(bool victory, int gold, int xp, ItemData bonus)
        {
            controls.SetActive(false);
            bossPanel.SetActive(false);
            var dim = UIKit.Panel(transform, new Color(0, 0, 0, 0.75f), "Results", UIKit.Square);
            UIKit.Stretch(dim.rectTransform);
            resultsPanel = dim.gameObject;
            var box = UIKit.Panel(dim.transform, UIKit.PanelColor, "Box");
            UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 760));
            var stack = UIKit.Rect("Stack", box.transform);
            UIKit.Stretch(stack, 60, 40, 60, 40);
            UIKit.VStack(stack, 16);

            UIKit.Layout(UIKit.Outline(UIKit.Label(stack, victory ? "VICTORY" : "DEFEATED", 84, victory ? UIKit.Gold : UIKit.Bad,
                TextAnchor.MiddleCenter, FontStyle.Bold), 3), 120);
            UIKit.Layout(UIKit.Label(stack, $"Stage {game.Stage.stageNumber}: {game.Stage.displayName}", 32, UIKit.TextDim, TextAnchor.MiddleCenter), 50);
            UIKit.Layout(UIKit.Label(stack, $"{UIKit.Colored("Gold", UIKit.Gold)}  +{gold}", 40, UIKit.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold), 60);
            UIKit.Layout(UIKit.Label(stack, $"{UIKit.Colored("Hero XP", new Color(0.5f, 0.7f, 1f))}  +{xp}", 40, UIKit.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold), 60);

            string loot = RunLoot.Items.Count == 0 ? "No items found" : "";
            int shown = 0;
            foreach (var item in RunLoot.Items)
            {
                if (shown++ >= 5) { loot += " ..."; break; }
                loot += (loot.Length > 0 ? ",  " : "") + UIKit.Colored(item.displayName, RarityUtil.GetColor(item.rarity));
            }
            UIKit.Layout(UIKit.Label(stack, loot, 28, UIKit.TextColor, TextAnchor.MiddleCenter), 80);
            if (bonus != null)
                UIKit.Layout(UIKit.Label(stack, "First clear reward: " + UIKit.Colored(bonus.displayName, RarityUtil.GetColor(bonus.rarity)),
                    30, UIKit.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold), 50);

            var row = UIKit.Rect("Buttons", stack);
            UIKit.HStack(row, 20);
            UIKit.Layout(row, 110);
            UIKit.Button(row, "Camp", game.ExitToMenu, null, 36);
            UIKit.Button(row, victory ? "Replay" : "Retry", game.Retry, null, 36);
            bool hasNext = victory && GameSession.StageIndex < GameDatabase.Instance.stages.Count - 1;
            if (hasNext) UIKit.Button(row, "Next Stage", game.NextStage, UIKit.AccentDark, 36);
        }

        void Update()
        {
            var player = game.Player;
            var profile = PlayerProfile.Instance;
            if (player != null)
            {
                var h = player.Health;
                hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, h.Normalized, 12f * Time.unscaledDeltaTime);
                hpText.text = $"{Mathf.CeilToInt(h.Current)} / {Mathf.CeilToInt(h.maxHealth)}";
                var prog = profile.GetProgress(game.Hero);
                levelText.text = "Lv " + prog.level;
                xpFill.fillAmount = prog.xp / (float)PlayerProfile.XpToNext(prog.level);

                var potion = player.Combat.FindPotion();
                potionBtn.SetBadge(potion != null ? profile.CountOf(potion).ToString() : "0");
                float buff = player.Combat.AttackBuffRemaining;
                buffText.text = buff > 0f ? $"Fury  {buff:0}s" : "";
            }
            goldText.text = profile.Gold.ToString("N0");

            if (bossPanel.activeSelf)
            {
                if (boss == null || boss.Health.IsDead) bossPanel.SetActive(false);
                else bossFill.fillAmount = boss.Health.Normalized;
            }

            if (bannerTime > 0f)
            {
                bannerTime -= Time.unscaledDeltaTime;
                var c = bannerText.color;
                c.a = Mathf.Clamp01(bannerTime / 0.5f);
                bannerText.color = c;
                bannerText.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, bannerTime - 1.7f));
            }

            if (Input.GetKeyDown(KeyCode.Escape) && resultsPanel == null) TogglePause(!pausePanel.activeSelf);
        }
    }
}
