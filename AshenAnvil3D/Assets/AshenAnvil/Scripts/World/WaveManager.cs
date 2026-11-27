using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Runs a stage: sequential monster waves, then the boss encounter.</summary>
    public class WaveManager : MonoBehaviour
    {
        public event Action<int, int> WaveStarted;   // wave index (0-based), total waves
        public event Action<EnemyAI> BossSpawned;
        public event Action<EnemyAI> EnemyKilled;
        public event Action StageCleared;

        public int AliveCount => alive.Count;
        public int TotalWaves => stage != null ? stage.waves.Count : 0;

        readonly List<EnemyAI> alive = new List<EnemyAI>();
        readonly List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        SpawnPoint bossPoint;
        StageData stage;
        bool running;

        void OnEnable()
        {
            EnemyAI.Killed += OnKilled;
            EnemyAI.Summoned += OnSummoned;
        }

        void OnDisable()
        {
            EnemyAI.Killed -= OnKilled;
            EnemyAI.Summoned -= OnSummoned;
        }

        public void Begin(StageData s)
        {
            stage = s;
            spawnPoints.Clear();
            foreach (var p in Compat.FindAll<SpawnPoint>())
            {
                if (p.bossSpawn) bossPoint = p;
                else spawnPoints.Add(p);
            }
            running = true;
            StartCoroutine(Run());
        }

        public void Stop()
        {
            running = false;
            StopAllCoroutines();
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(1.5f);
            for (int w = 0; w < stage.waves.Count && running; w++)
            {
                WaveStarted?.Invoke(w, stage.waves.Count);
                yield return new WaitForSeconds(1.2f);
                foreach (var entry in stage.waves[w].entries)
                {
                    if (entry.monster == null) continue;
                    var point = PickSpawnPoint();
                    for (int i = 0; i < entry.count && running; i++)
                    {
                        Spawn(entry.monster, point);
                        yield return new WaitForSeconds(0.25f);
                    }
                }
                yield return new WaitUntil(() => alive.Count == 0 || !running);
                yield return new WaitForSeconds(1.2f);
            }

            if (!running) yield break;
            if (stage.boss != null)
            {
                WaveStarted?.Invoke(stage.waves.Count, stage.waves.Count);
                yield return new WaitForSeconds(2f);
                var point = bossPoint != null ? bossPoint : PickSpawnPoint();
                var boss = Spawn(stage.boss, point);
                Vfx.Ring(boss.transform.position, 1f, 6f, new Color(1f, 0.2f, 0.1f, 0.6f), 1.2f);
                ThirdPersonCamera.Instance?.Shake(0.5f);
                BossSpawned?.Invoke(boss);
                yield return new WaitUntil(() => alive.Count == 0 || !running);
            }
            if (running) StageCleared?.Invoke();
        }

        EnemyAI Spawn(MonsterData monster, SpawnPoint point)
        {
            Vector3 pos = point != null ? point.RandomPosition() : FallbackPosition();
            var e = CharacterSpawner.SpawnEnemy(monster, pos, stage.difficulty);
            var player = PlayerController.Instance;
            if (player != null)
            {
                var look = (player.transform.position - pos).Flat();
                if (look.sqrMagnitude > 0.01f) e.transform.rotation = Quaternion.LookRotation(look);
            }
            Vfx.Burst(pos + Vector3.up, 1f, new Color(0.5f, 0.15f, 0.8f, 0.5f), 0.35f);
            alive.Add(e);
            return e;
        }

        SpawnPoint PickSpawnPoint()
        {
            if (spawnPoints.Count == 0) return null;
            var player = PlayerController.Instance;
            var candidates = new List<SpawnPoint>();
            foreach (var p in spawnPoints)
                if (player == null || (p.transform.position - player.transform.position).sqrMagnitude > 64f) candidates.Add(p);
            if (candidates.Count == 0) candidates = spawnPoints;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        Vector3 FallbackPosition()
        {
            var player = PlayerController.Instance;
            Vector3 c = player != null ? player.transform.position : Vector3.zero;
            var r = UnityEngine.Random.insideUnitCircle.normalized * 14f;
            return c + new Vector3(r.x, 0.5f, r.y);
        }

        void OnKilled(EnemyAI e)
        {
            if (!alive.Remove(e)) return;
            EnemyKilled?.Invoke(e);
        }

        void OnSummoned(EnemyAI boss, EnemyAI minion)
        {
            if (!alive.Contains(minion)) alive.Add(minion);
        }
    }
}
