using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AshenAnvil.EditorTools
{
    /// <summary>Generates the MainMenu (camp) and Game (battle arena) scenes and registers them for builds.</summary>
    public static class SceneBuilder
    {
        const float ArenaRadius = 34f;

        public static void BuildAll()
        {
            GeneratorPaths.Ensure(GeneratorPaths.Scenes);
            BuildGameScene();
            BuildMenuScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GeneratorPaths.MainMenuScene, true),
                new EditorBuildSettingsScene(GeneratorPaths.GameScene, true),
            };
        }

        // ------------------------------------------------------------------ battle arena
        static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting(new Color(0.35f, 0.28f, 0.25f), new Color(0.45f, 0.4f, 0.38f), 0.012f);

            var sun = CreateSun(new Vector3(50f, -35f, 0f), new Color(1f, 0.85f, 0.7f), 1.2f);
            var ground = CreateGround(140f);

            var cam = CreateCamera("Main Camera", new Vector3(0, 8, -24), Quaternion.Euler(45, 0, 0), 55f, 140f);
            var rig = cam.gameObject.AddComponent<ThirdPersonCamera>();

            var env = new GameObject("Environment").transform;
            var rng = new System.Random(1337);
            var occupied = new List<Vector4>(); // xz position + radius

            // Boundary: ring of rocks and trees with invisible walls behind them.
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f;
                float r = ArenaRadius + 2f + (float)rng.NextDouble() * 6f;
                var pos = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                string prop = i % 3 == 0 ? "RockA" : i % 3 == 1 ? "PineTree" : (rng.NextDouble() < 0.5 ? "DeadTree" : "RockB");
                Place(prop, pos, (float)rng.NextDouble() * 360f, 1f + (float)rng.NextDouble() * 0.8f, env);
            }
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                var wall = new GameObject("Boundary_" + i);
                wall.transform.SetParent(env);
                wall.transform.position = new Vector3(Mathf.Cos(a) * (ArenaRadius + 1.5f), 2.5f, Mathf.Sin(a) * (ArenaRadius + 1.5f));
                wall.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0);
                var box = wall.AddComponent<BoxCollider>();
                box.size = new Vector3(ArenaRadius * 2f * Mathf.PI / 24f + 1f, 5f, 1f);
                wall.layer = Layers.Environment;
            }

            // Central ruins.
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f + 0.2f;
                var pos = new Vector3(Mathf.Cos(a) * 12f, 0, Mathf.Sin(a) * 12f);
                if (i % 3 == 2) continue; // broken gaps
                Place("Pillar", pos, a * Mathf.Rad2Deg, 1f, env);
                occupied.Add(new Vector4(pos.x, 0, pos.z, 2f));
            }
            Place("Brazier", new Vector3(3, 0, -9), 0, 1f, env);
            Place("Brazier", new Vector3(-3, 0, -9), 0, 1f, env);
            occupied.Add(new Vector4(3, 0, -9, 1.5f));
            occupied.Add(new Vector4(-3, 0, -9, 1.5f));
            AddPointLight(new Vector3(3, 1.6f, -9), env);
            AddPointLight(new Vector3(-3, 1.6f, -9), env);

            // Scattered cover inside the arena.
            ScatterProps(rng, env, occupied, new[] { "RockA", "RockB", "DeadTree", "PineTree" }, 16, 15f, ArenaRadius - 4f, 3f);
            // Breakable crates and barrels.
            var breakables = new GameObject("Breakables").transform;
            ScatterProps(rng, breakables, occupied, new[] { "Crate", "Barrel" }, 14, 6f, ArenaRadius - 5f, 1.5f);

            // Decorative scorched patches.
            var deco = new GameObject("GroundDecals").transform;
            for (int i = 0; i < 18; i++)
            {
                var p = RandomInRing(rng, 2f, ArenaRadius);
                var patch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(patch.GetComponent<Collider>());
                patch.name = "Patch";
                patch.transform.SetParent(deco);
                patch.transform.position = new Vector3(p.x, 0.01f, p.z);
                float s = 2f + (float)rng.NextDouble() * 5f;
                patch.transform.localScale = new Vector3(s, 0.01f, s * (0.6f + (float)rng.NextDouble() * 0.6f));
                patch.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 180f, 0);
                patch.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Lit(new Color(0.2f, 0.17f, 0.15f));
                patch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(patch, StaticEditorFlags.BatchingStatic);
            }

            // Spawn points.
            var spawns = new GameObject("SpawnPoints").transform;
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f + Mathf.PI / 8f;
                var sp = new GameObject("Spawn_" + i).AddComponent<SpawnPoint>();
                sp.transform.SetParent(spawns);
                sp.transform.position = new Vector3(Mathf.Cos(a) * 22f, 0, Mathf.Sin(a) * 22f);
            }
            var boss = new GameObject("BossSpawn").AddComponent<SpawnPoint>();
            boss.transform.SetParent(spawns);
            boss.transform.position = new Vector3(0, 0, 20f);
            boss.bossSpawn = true;
            boss.radius = 0.5f;

            var playerSpawn = new GameObject("PlayerSpawn").AddComponent<PlayerSpawnPoint>();
            playerSpawn.transform.position = new Vector3(0, 0.05f, -14f);

            var controller = new GameObject("GameSceneController");
            var gsc = controller.AddComponent<GameSceneController>();
            gsc.sun = sun;
            gsc.groundRenderer = ground.GetComponent<Renderer>();
            gsc.cameraRig = rig;

            EditorSceneManager.SaveScene(scene, GeneratorPaths.GameScene);
        }

        static void ScatterProps(System.Random rng, Transform parent, List<Vector4> occupied, string[] props, int count, float minR, float maxR, float spacing)
        {
            int placed = 0, attempts = 0;
            while (placed < count && attempts++ < count * 40)
            {
                var p = RandomInRing(rng, minR, maxR);
                if (Mathf.Abs(p.x) < 4f && p.z < -6f) continue; // keep the player start clear
                bool blocked = false;
                foreach (var o in occupied)
                    if ((new Vector2(o.x - p.x, o.z - p.z)).sqrMagnitude < (o.w + spacing) * (o.w + spacing)) { blocked = true; break; }
                if (blocked) continue;
                string prop = props[rng.Next(props.Length)];
                Place(prop, p, (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble() * 0.5f, parent);
                occupied.Add(new Vector4(p.x, 0, p.z, spacing));
                placed++;
            }
        }

        static Vector3 RandomInRing(System.Random rng, float minR, float maxR)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Mathf.Lerp(minR * minR, maxR * maxR, (float)rng.NextDouble()));
            return new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
        }

        // ------------------------------------------------------------------ camp menu
        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting(new Color(0.08f, 0.06f, 0.07f), new Color(0.3f, 0.26f, 0.28f), 0.045f);
            CreateSun(new Vector3(35f, 150f, 0f), new Color(1f, 0.75f, 0.55f), 0.9f);
            CreateGround(60f);

            var cam = CreateCamera("Main Camera", new Vector3(1.55f, 1.7f, 5.4f), Quaternion.Euler(6f, 180f, 0f), 40f, 60f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.07f);

            var env = new GameObject("Camp").transform;
            Place("Pedestal", Vector3.zero, 0f, 1f, env);
            Place("Brazier", new Vector3(-2.2f, 0, -0.8f), 0, 1f, env);
            Place("Brazier", new Vector3(2.4f, 0, -1.2f), 0, 1f, env);
            AddPointLight(new Vector3(-2.2f, 1.7f, -0.8f), env, 2.2f);
            AddPointLight(new Vector3(2.4f, 1.7f, -1.2f), env, 2.2f);
            Place("Pillar", new Vector3(-4f, 0, -4f), 10f, 1.2f, env);
            Place("Pillar", new Vector3(5f, 0, -5f), 40f, 1.3f, env);
            Place("PineTree", new Vector3(-7f, 0, -9f), 0, 1.6f, env);
            Place("PineTree", new Vector3(8f, 0, -10f), 90, 1.8f, env);
            Place("DeadTree", new Vector3(-3f, 0, -10f), 30, 1.5f, env);
            Place("RockA", new Vector3(4f, 0, -3f), 50, 0.9f, env);
            Place("RockB", new Vector3(-5.5f, 0, -2f), 120, 1f, env);
            Place("Crate", new Vector3(3.2f, 0, 0.6f), 20, 1f, env, false);
            Place("Barrel", new Vector3(3.9f, 0, -0.2f), 0, 1f, env, false);

            var pedestalTop = PrefabBuilder.LoadMesh("pedestal");
            float top = pedestalTop != null ? pedestalTop.bounds.max.y : 0.3f;
            var preview = new GameObject("HeroPreview").AddComponent<MenuHeroPreview>();
            preview.transform.position = new Vector3(0, top, 0);
            preview.transform.rotation = Quaternion.Euler(0, 160f, 0);

            var ui = new GameObject("MainMenu").AddComponent<MainMenuUI>();
            ui.preview = preview;

            EditorSceneManager.SaveScene(scene, GeneratorPaths.MainMenuScene);
        }

        // ------------------------------------------------------------------ helpers
        static void SetupLighting(Color fog, Color ambient, float density)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = density;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.skybox = null;
            try
            {
                string path = GeneratorPaths.Settings + "/Lighting_Mobile.lighting";
                var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
                if (settings == null)
                {
                    settings = new LightingSettings { name = "Lighting_Mobile", bakedGI = false, realtimeGI = false };
                    GeneratorPaths.Ensure(GeneratorPaths.Settings);
                    AssetDatabase.CreateAsset(settings, path);
                }
                Lightmapping.lightingSettings = settings; // fully realtime lighting: nothing to bake
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Ashen Anvil] Could not assign lighting settings: " + e.Message);
            }
        }

        static Light CreateSun(Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject("Sun");
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            return light;
        }

        static GameObject CreateGround(float size)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(size, 1f, size);
            ground.GetComponent<MeshRenderer>().sharedMaterial = PrefabBuilder.GroundMaterial;
            ground.layer = Layers.Environment;
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic);
            return ground;
        }

        static Camera CreateCamera(string name, Vector3 pos, Quaternion rot, float fov, float far)
        {
            var go = new GameObject(name) { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(pos, rot);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = far;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            go.AddComponent<AudioListener>();
            return cam;
        }

        static void AddPointLight(Vector3 pos, Transform parent, float intensity = 1.6f)
        {
            var go = new GameObject("FireLight");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.55f, 0.2f);
            l.range = 7f;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
        }

        static GameObject Place(string prop, Vector3 pos, float yaw, float scale, Transform parent, bool physics = true)
        {
            if (!PrefabBuilder.PropPrefabs.TryGetValue(prop, out var prefab) || prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            if (!physics)
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }
            return go;
        }
    }
}
