using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AshenAnvil
{
    /// <summary>
    /// Entry point of the battle scene: applies the stage atmosphere, spawns the selected hero,
    /// builds the HUD, runs the waves and pays out rewards.
    /// </summary>
    [RequireComponent(typeof(WaveManager))]
    public class GameSceneController : MonoBehaviour
    {
        public Light sun;
        public Renderer groundRenderer;
        public ThirdPersonCamera cameraRig;

        public StageData Stage { get; private set; }
        public HeroData Hero { get; private set; }
        public PlayerController Player { get; private set; }
        public bool Finished { get; private set; }
        public int XpEarned { get; private set; }

        WaveManager waves;
        GameHUD hud;
        float stageXpScale = 1f;

        void Start()
        {
            var db = GameDatabase.Instance;
            var profile = PlayerProfile.Instance;
            Time.timeScale = 1f;
            PlayerInput.Reset();
            HealthBarRegistry.Clear();
            RunLoot.Clear();

            int index = Mathf.Clamp(GameSession.StageIndex, 0, db.stages.Count - 1);
            Stage = db.stages[index];
            Hero = profile.SelectedHero;
            stageXpScale = Mathf.Max(1f, Stage.difficulty);

            ApplyAtmosphere();

            var spawn = Compat.FindOne<PlayerSpawnPoint>();
            Vector3 pos = spawn != null ? spawn.transform.position : Vector3.up * 0.1f;
            Quaternion rot = spawn != null ? spawn.transform.rotation : Quaternion.identity;
            Player = CharacterSpawner.SpawnPlayer(Hero, pos, rot);
            Player.Health.Died += OnPlayerDied;

            if (cameraRig == null) cameraRig = Compat.FindOne<ThirdPersonCamera>();
            if (cameraRig == null && Camera.main != null) cameraRig = Camera.main.gameObject.AddComponent<ThirdPersonCamera>();
            cameraRig.SetTarget(Player.transform);

            hud = GameHUD.Create(this);

            waves = GetComponent<WaveManager>();
            waves.WaveStarted += OnWaveStarted;
            waves.BossSpawned += OnBossSpawned;
            waves.EnemyKilled += OnEnemyKilled;
            waves.StageCleared += OnStageCleared;
            waves.Begin(Stage);
        }

        void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Stage.fogColor;
            RenderSettings.fogDensity = Stage.fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Stage.ambientColor;
            if (sun != null)
            {
                sun.color = Stage.sunColor;
                sun.intensity = Stage.sunIntensity;
            }
            if (groundRenderer != null) groundRenderer.sharedMaterial = MaterialCache.Lit(Stage.groundColor);
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Stage.fogColor;
            }
        }

        void OnWaveStarted(int index, int total)
        {
            if (index < total) hud.ShowBanner($"Wave {index + 1} / {total}", UIKit.TextColor);
            else hud.ShowBanner("BOSS INCOMING", UIKit.Bad);
            hud.SetWaveText(index < total ? $"Wave {index + 1}/{total}" : "Boss");
        }

        void OnBossSpawned(EnemyAI boss) => hud.ShowBoss(boss);

        void OnEnemyKilled(EnemyAI e)
        {
            var profile = PlayerProfile.Instance;
            profile.RecordMonsterKill(e.IsBoss);
            int xp = Mathf.RoundToInt(e.data.xpReward * stageXpScale);
            XpEarned += xp;
            int levels = profile.AddHeroXp(Hero, xp);
            if (levels > 0) hud.ShowBanner($"LEVEL UP!  Lv {profile.GetProgress(Hero).level}", UIKit.Gold);
        }

        void OnStageCleared()
        {
            if (Finished) return;
            Finished = true;
            StartCoroutine(Victory());
        }

        IEnumerator Victory()
        {
            yield return new WaitForSeconds(2.5f); // let the loot magnet finish
            var profile = PlayerProfile.Instance;
            bool firstClear = profile.RecordStageClear(Stage);
            int gold = firstClear ? Stage.goldReward : Stage.goldReward / 2;
            profile.AddGold(gold);
            int xp = Stage.xpReward;
            profile.AddHeroXp(Hero, xp);
            XpEarned += xp;
            ItemData bonus = null;
            if (firstClear && Stage.firstClearReward != null)
            {
                bonus = Stage.firstClearReward;
                profile.AddItem(bonus);
            }
            profile.Save();
            Sfx.Play(SfxId.Victory);
            hud.ShowResults(true, gold + RunLoot.Gold, XpEarned, bonus);
        }

        void OnPlayerDied(Health h)
        {
            if (Finished) return;
            Finished = true;
            Player.Anim.Die();
            waves.Stop();
            StartCoroutine(Defeat());
        }

        IEnumerator Defeat()
        {
            yield return new WaitForSeconds(2f);
            PlayerProfile.Instance.Save();
            Sfx.Play(SfxId.Defeat);
            hud.ShowResults(false, RunLoot.Gold, XpEarned, null);
        }

        public void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            PlayerInput.Reset();
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            PlayerProfile.Instance.Save();
            SceneManager.LoadScene(SceneNames.Game);
        }

        public void NextStage()
        {
            GameSession.StageIndex = Mathf.Min(GameSession.StageIndex + 1, GameDatabase.Instance.stages.Count - 1);
            Retry();
        }

        public void ExitToMenu()
        {
            Time.timeScale = 1f;
            PlayerProfile.Instance.Save();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (waves != null)
            {
                waves.WaveStarted -= OnWaveStarted;
                waves.BossSpawned -= OnBossSpawned;
                waves.EnemyKilled -= OnEnemyKilled;
                waves.StageCleared -= OnStageCleared;
            }
        }
    }
}
