using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Creates fully wired player and enemy characters from ScriptableObject data.</summary>
    public static class CharacterSpawner
    {
        // ---------------------------------------------------------------- player
        public static PlayerController SpawnPlayer(HeroData hero, Vector3 position, Quaternion rotation)
        {
            var profile = PlayerProfile.Instance;
            var root = new GameObject("Player_" + hero.id);
            root.transform.SetPositionAndRotation(position, rotation);

            float s = hero.look.scale;
            var cc = root.AddComponent<CharacterController>();
            cc.height = 2f * s;
            cc.radius = 0.4f * s;
            cc.center = new Vector3(0, cc.height * 0.5f, 0);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.05f;

            var health = root.AddComponent<Health>();
            health.uiHeight = 2.3f * s;
            health.showHealthBar = false;
            health.Initialize(100, 0, Team.Player);

            var anim = root.AddComponent<CharacterAnimation>();
            root.AddComponent<PlayerController>();
            var combat = root.AddComponent<PlayerCombat>();

            var visual = InstantiateVisual(hero.prefab, hero.look, root.transform, "Visual");
            anim.Bind(visual);
            var weapon = profile.EquippedWeaponFor(hero);
            CharacterVisualFactory.AttachWeapon(visual, weapon);
            Layers.SetLayerRecursive(root, Layers.Player);

            combat.Setup(hero, weapon, profile.ComputeStats(hero));
            return root.GetComponent<PlayerController>();
        }

        // ---------------------------------------------------------------- enemies
        public static EnemyAI SpawnEnemy(MonsterData data, Vector3 position, float difficulty)
        {
            GameObject go;
            if (data.prefab != null)
            {
                go = Object.Instantiate(data.prefab, position, Quaternion.identity);
                go.name = data.displayName;
            }
            else
            {
                go = BuildEnemy(data);
                go.transform.position = position;
            }
            var ai = go.GetComponent<EnemyAI>();
            ai.Initialize(data, difficulty);
            return ai;
        }

        /// <summary>Builds a complete enemy GameObject (used for prefab baking and as runtime fallback).</summary>
        public static GameObject BuildEnemy(MonsterData data)
        {
            var root = new GameObject(data.displayName);
            float s = data.look.scale;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 50f * s * s;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 2f * s;
            col.radius = 0.42f * s * Mathf.Lerp(1f, data.look.bulk, 0.5f);
            col.center = new Vector3(0, col.height * 0.5f, 0);

            var health = root.AddComponent<Health>();
            health.team = Team.Enemy;
            health.uiHeight = 2.35f * s;
            root.AddComponent<CharacterAnimation>();
            var ai = root.AddComponent<EnemyAI>();
            ai.data = data;

            InstantiateVisual(null, data.look, root.transform, "Visual");
            Layers.SetLayerRecursive(root, Layers.Enemy);
            return root;
        }

        public static GameObject InstantiateVisual(GameObject prefab, CharacterLook look, Transform parent, string name)
        {
            GameObject visual;
            if (prefab != null)
            {
                visual = Object.Instantiate(prefab, parent, false);
                visual.name = name;
                var animator = visual.GetComponent<Animator>();
                var db = GameDatabase.Instance;
                if (animator != null && animator.runtimeAnimatorController == null && db != null)
                    animator.runtimeAnimatorController = db.characterAnimator;
            }
            else
            {
                visual = CharacterVisualFactory.Build(look, name, parent);
            }
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return visual;
        }
    }
}
