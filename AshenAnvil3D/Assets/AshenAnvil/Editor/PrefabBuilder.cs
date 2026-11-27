using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenAnvil.EditorTools
{
    /// <summary>
    /// Creates the URP materials, weapon / prop prefabs from the bundled OBJ models, and bakes
    /// a prefab for every hero and monster defined in the GameDatabase.
    /// </summary>
    public static class PrefabBuilder
    {
        public enum ColliderKind { None, Box, Capsule, Mesh }

        public class PropSpec
        {
            public string name, model;
            public Color[] colors;
            public ColliderKind collider;
            public bool destructible;
            public float mass;
            public float colliderRadius = -1f;
            public float emission;
            public PrimitiveType fallback = PrimitiveType.Cube;
            public Vector3 fallbackScale = Vector3.one;
        }

        static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f);
        static readonly Color Iron = new Color(0.32f, 0.32f, 0.35f);
        static readonly Color Stone = new Color(0.45f, 0.43f, 0.41f);
        static readonly Color Ember = new Color(1f, 0.45f, 0.1f);

        public static readonly PropSpec[] Props =
        {
            new PropSpec { name = "Crate", model = "crate", colors = new[] { Wood, Iron }, collider = ColliderKind.Box, destructible = true, mass = 25f },
            new PropSpec { name = "Barrel", model = "barrel", colors = new[] { new Color(0.5f, 0.31f, 0.17f), Iron }, collider = ColliderKind.Capsule, destructible = true, mass = 30f, fallback = PrimitiveType.Cylinder, fallbackScale = new Vector3(0.8f, 0.55f, 0.8f) },
            new PropSpec { name = "RockA", model = "rock_a", colors = new[] { Stone }, collider = ColliderKind.Mesh, fallback = PrimitiveType.Sphere, fallbackScale = new Vector3(2f, 1.2f, 1.8f) },
            new PropSpec { name = "RockB", model = "rock_b", colors = new[] { new Color(0.38f, 0.36f, 0.35f) }, collider = ColliderKind.Mesh, fallback = PrimitiveType.Sphere, fallbackScale = new Vector3(1.4f, 2f, 1.4f) },
            new PropSpec { name = "PineTree", model = "tree_pine", colors = new[] { new Color(0.33f, 0.22f, 0.14f), new Color(0.2f, 0.33f, 0.2f) }, collider = ColliderKind.Capsule, colliderRadius = 0.35f, fallback = PrimitiveType.Capsule, fallbackScale = new Vector3(1.5f, 2.5f, 1.5f) },
            new PropSpec { name = "DeadTree", model = "tree_dead", colors = new[] { new Color(0.26f, 0.2f, 0.17f) }, collider = ColliderKind.Capsule, colliderRadius = 0.3f, fallback = PrimitiveType.Cylinder, fallbackScale = new Vector3(0.5f, 2f, 0.5f) },
            new PropSpec { name = "Pillar", model = "pillar", colors = new[] { new Color(0.56f, 0.53f, 0.49f) }, collider = ColliderKind.Box, fallback = PrimitiveType.Cylinder, fallbackScale = new Vector3(1f, 2f, 1f) },
            new PropSpec { name = "Pedestal", model = "pedestal", colors = new[] { Stone, Ember }, collider = ColliderKind.Box, emission = 1.2f, fallback = PrimitiveType.Cylinder, fallbackScale = new Vector3(2f, 0.15f, 2f) },
            new PropSpec { name = "Brazier", model = "brazier", colors = new[] { new Color(0.2f, 0.19f, 0.2f), Ember }, collider = ColliderKind.Capsule, colliderRadius = 0.35f, emission = 2f, fallback = PrimitiveType.Cylinder, fallbackScale = new Vector3(0.6f, 0.6f, 0.6f) },
        };

        public static Material LitMaterial { get; private set; }
        public static Material VfxMaterial { get; private set; }
        public static Material GroundMaterial { get; private set; }
        public static readonly Dictionary<string, GameObject> PropPrefabs = new Dictionary<string, GameObject>();

        // ------------------------------------------------------------------ materials
        public static void BuildMaterials(GameDatabase db)
        {
            GeneratorPaths.Ensure(GeneratorPaths.Palette);
            var litShader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit");
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

            LitMaterial = LoadOrCreate(GeneratorPaths.Materials + "/M_Lit.mat", litShader);
            LitMaterial.color = Color.white;
            LitMaterial.SetColor("_BaseColor", Color.white);
            if (LitMaterial.HasProperty("_Smoothness")) LitMaterial.SetFloat("_Smoothness", 0.15f);
            if (LitMaterial.HasProperty("_SpecColor")) LitMaterial.SetColor("_SpecColor", new Color(0.2f, 0.2f, 0.2f));
            LitMaterial.enableInstancing = true;
            EditorUtility.SetDirty(LitMaterial);

            VfxMaterial = LoadOrCreate(GeneratorPaths.Materials + "/M_Vfx.mat", unlitShader);
            MakeTransparent(VfxMaterial);
            VfxMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0.6f));
            EditorUtility.SetDirty(VfxMaterial);

            GroundMaterial = LoadOrCreate(GeneratorPaths.Materials + "/M_Ground.mat", litShader);
            GroundMaterial.SetColor("_BaseColor", new Color(0.32f, 0.3f, 0.26f));
            if (GroundMaterial.HasProperty("_Smoothness")) GroundMaterial.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(GroundMaterial);

            db.litMaterial = LitMaterial;
            db.vfxMaterial = VfxMaterial;
            EditorUtility.SetDirty(db);

            // Every colour the factories request is saved as a real material asset so prefabs keep it.
            MaterialCache.Clear();
            MaterialCache.PersistHook = m =>
            {
                string path = $"{GeneratorPaths.Palette}/{m.name}.mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing != null)
                {
                    existing.CopyPropertiesFromMaterial(m);
                    existing.shaderKeywords = m.shaderKeywords;
                    EditorUtility.SetDirty(existing);
                    return existing;
                }
                AssetDatabase.CreateAsset(m, path);
                return m;
            };
            AssetDatabase.SaveAssets();
        }

        public static void EndMaterialPersistence()
        {
            MaterialCache.PersistHook = null;
            MaterialCache.Clear();
        }

        static Material LoadOrCreate(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            return m;
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.enableInstancing = true;
        }

        // ------------------------------------------------------------------ models
        /// <summary>Forces 1:1 scale, imported flat normals and readable meshes on the bundled OBJ files.</summary>
        public static void ConfigureModelImporters()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { GeneratorPaths.Models }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter imp)) continue;
                bool dirty = imp.useFileScale || !Mathf.Approximately(imp.globalScale, 1f) || !imp.isReadable ||
                             imp.materialImportMode != ModelImporterMaterialImportMode.None || imp.importAnimation;
                if (!dirty) continue;
                imp.useFileScale = false;
                imp.globalScale = 1f;
                imp.isReadable = true; // MeshColliders on rocks need CPU access in player builds
                imp.importNormals = ModelImporterNormals.Import;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = false;
                imp.importBlendShapes = false;
                imp.addCollider = false;
                imp.SaveAndReimport();
            }
        }

        public static Mesh LoadMesh(string model)
        {
            string path = $"{GeneratorPaths.Models}/{model}.obj";
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
        }

        static GameObject MeshObject(string name, Mesh mesh, Color[] colors, float emission)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var mats = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (int i = 0; i < mats.Length; i++)
            {
                var c = colors[Mathf.Min(i, colors.Length - 1)];
                bool glow = emission > 0f && i == mats.Length - 1 && mats.Length > 1;
                mats[i] = MaterialCache.Lit(c, glow ? emission : 0f);
            }
            r.sharedMaterials = mats;
            return go;
        }

        public static void BuildWeaponPrefabs(GameDatabase db)
        {
            GeneratorPaths.Ensure(GeneratorPaths.WeaponPrefabs);
            var byType = new Dictionary<WeaponType, GameObject>();
            foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
            {
                var mesh = LoadMesh("weapon_" + type.ToString().ToLowerInvariant());
                GameObject go = mesh != null
                    ? MeshObject("W_" + type, mesh, new[] { Color.white, new Color(0.32f, 0.22f, 0.14f) }, 0f)
                    : CharacterVisualFactory.BuildPrimitiveWeapon(type, Color.white);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                byType[type] = SavePrefab(go, $"{GeneratorPaths.WeaponPrefabs}/W_{type}.prefab");
            }

            foreach (var item in db.items.OfType<WeaponData>().Concat(db.heroes.Where(h => h.startingWeapon != null).Select(h => h.startingWeapon)).Distinct())
            {
                item.modelPrefab = byType[item.weaponType];
                EditorUtility.SetDirty(item);
            }
        }

        public static void BuildPropPrefabs(GameDatabase db)
        {
            GeneratorPaths.Ensure(GeneratorPaths.PropPrefabs);
            PropPrefabs.Clear();
            foreach (var spec in Props)
            {
                var mesh = LoadMesh(spec.model);
                GameObject go;
                if (mesh != null) go = MeshObject("P_" + spec.name, mesh, spec.colors, spec.emission);
                else
                {
                    go = GameObject.CreatePrimitive(spec.fallback);
                    Object.DestroyImmediate(go.GetComponent<Collider>());
                    go.name = "P_" + spec.name;
                    go.transform.localScale = spec.fallbackScale;
                    go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Lit(spec.colors[0]);
                }
                var bounds = go.GetComponent<MeshFilter>().sharedMesh.bounds;
                switch (spec.collider)
                {
                    case ColliderKind.Box:
                        var box = go.AddComponent<BoxCollider>();
                        box.center = bounds.center;
                        box.size = bounds.size;
                        break;
                    case ColliderKind.Capsule:
                        var cap = go.AddComponent<CapsuleCollider>();
                        cap.center = bounds.center;
                        cap.height = bounds.size.y;
                        cap.radius = spec.colliderRadius > 0 ? spec.colliderRadius : Mathf.Max(bounds.extents.x, bounds.extents.z);
                        break;
                    case ColliderKind.Mesh:
                        go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                        break;
                }

                if (spec.destructible)
                {
                    var rb = go.AddComponent<Rigidbody>();
                    rb.mass = spec.mass;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    var h = go.AddComponent<Health>();
                    h.team = Team.Neutral;
                    h.showHealthBar = false;
                    var d = go.AddComponent<Destructible>();
                    d.debrisColor = spec.colors[0];
                    go.layer = Layers.Destructible;
                }
                else
                {
                    go.layer = Layers.Environment;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                }
                PropPrefabs[spec.name] = SavePrefab(go, $"{GeneratorPaths.PropPrefabs}/{go.name}.prefab");
            }

            db.coinMesh = LoadMesh("coin");
            db.lootMesh = LoadMesh("loot_chest");
            db.potionMesh = LoadMesh("potion");
            EditorUtility.SetDirty(db);
        }

        // ------------------------------------------------------------------ characters
        public static void BuildCharacterPrefabs(GameDatabase db)
        {
            GeneratorPaths.Ensure(GeneratorPaths.HeroPrefabs);
            GeneratorPaths.Ensure(GeneratorPaths.MonsterPrefabs);

            foreach (var hero in db.heroes.Where(h => h != null))
            {
                var visual = CharacterVisualFactory.Build(hero.look, "Hero_" + hero.id);
                hero.prefab = SavePrefab(visual, $"{GeneratorPaths.HeroPrefabs}/Hero_{hero.id}.prefab");
                EditorUtility.SetDirty(hero);
            }

            foreach (var monster in db.monsters.Where(m => m != null))
            {
                monster.prefab = null; // build fresh, not from a stale prefab
                var go = CharacterSpawner.BuildEnemy(monster);
                go.name = "Monster_" + monster.id;
                monster.prefab = SavePrefab(go, $"{GeneratorPaths.MonsterPrefabs}/Monster_{monster.id}.prefab");
                EditorUtility.SetDirty(monster);
            }
        }

        public static GameObject SavePrefab(GameObject instance, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }
    }
}
