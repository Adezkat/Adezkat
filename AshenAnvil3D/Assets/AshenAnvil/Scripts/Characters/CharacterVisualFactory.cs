using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Assembles modular low-poly characters (heroes and monsters) from primitive parts on a
    /// shared bone hierarchy, so every character can use the same Animator controller.
    /// Used at edit time to bake prefabs and at runtime as a fallback.
    /// </summary>
    public static class CharacterVisualFactory
    {
        public static GameObject Build(CharacterLook look, string name, Transform parent = null)
        {
            var root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * look.scale;

            float bulk = look.bulk;
            var primary = MaterialCache.Lit(look.primaryColor);
            var secondary = MaterialCache.Lit(look.secondaryColor);
            var skin = MaterialCache.Lit(look.skinColor);
            var hair = MaterialCache.Lit(look.hairColor);
            bool glowEyes = look.headStyle == HeadStyle.Skull || look.eyeColor.maxColorComponent > 0.6f;
            var eyes = MaterialCache.Lit(look.eyeColor, glowEyes ? 1.5f : 0f);

            var hips = Bone("Hips", root.transform, new Vector3(0, CharacterRig.HipHeight, 0));
            Part(PrimitiveType.Cube, hips, new Vector3(0, 0.02f, 0), new Vector3(0.44f * bulk, 0.22f, 0.27f), secondary);

            var spine = Bone("Spine", hips, new Vector3(0, 0.1f, 0));
            Part(PrimitiveType.Cube, spine, new Vector3(0, 0.3f, 0), new Vector3(0.52f * bulk, 0.58f, 0.32f), primary);
            Part(PrimitiveType.Cube, spine, new Vector3(0, 0.05f, 0), new Vector3(0.54f * bulk, 0.08f, 0.34f), secondary); // belt
            Part(PrimitiveType.Cube, spine, new Vector3(0, 0.55f, 0), new Vector3(0.6f * bulk, 0.14f, 0.36f), secondary);  // shoulders
            if (look.cape)
                Part(PrimitiveType.Cube, spine, new Vector3(0, 0.15f, -0.2f), new Vector3(0.5f * bulk, 0.85f, 0.04f), secondary, new Vector3(-8f, 0, 0));

            var head = Bone("Head", spine, new Vector3(0, 0.64f, 0));
            BuildHead(head, look, skin, hair, primary, secondary, eyes);

            BuildArm("ArmL", spine, new Vector3(-0.34f * bulk, 0.52f, 0), primary, skin, bulk, false);
            BuildArm("ArmR", spine, new Vector3(0.34f * bulk, 0.52f, 0), primary, skin, bulk, true);

            BuildLeg("LegL", hips, new Vector3(-0.12f * bulk, -0.06f, 0), secondary, primary, bulk);
            BuildLeg("LegR", hips, new Vector3(0.12f * bulk, -0.06f, 0), secondary, primary, bulk);

            var animator = root.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var db = GameDatabase.Instance;
            if (db != null) animator.runtimeAnimatorController = db.characterAnimator;
            return root;
        }

        static void BuildHead(Transform head, CharacterLook look, Material skin, Material hair, Material primary, Material secondary, Material eyes)
        {
            Part(PrimitiveType.Sphere, head, new Vector3(0, 0.2f, 0), new Vector3(0.36f, 0.38f, 0.36f), skin);
            Part(PrimitiveType.Sphere, head, new Vector3(-0.075f, 0.23f, 0.16f), Vector3.one * 0.065f, eyes);
            Part(PrimitiveType.Sphere, head, new Vector3(0.075f, 0.23f, 0.16f), Vector3.one * 0.065f, eyes);

            switch (look.headStyle)
            {
                case HeadStyle.Hair:
                    Part(PrimitiveType.Sphere, head, new Vector3(0, 0.31f, -0.03f), new Vector3(0.39f, 0.24f, 0.39f), hair);
                    Part(PrimitiveType.Cube, head, new Vector3(0, 0.2f, -0.15f), new Vector3(0.34f, 0.26f, 0.1f), hair);
                    break;
                case HeadStyle.Hood:
                    Part(PrimitiveType.Sphere, head, new Vector3(0, 0.24f, -0.04f), new Vector3(0.44f, 0.46f, 0.42f), primary);
                    Part(PrimitiveType.Cube, head, new Vector3(0, 0.08f, 0.13f), new Vector3(0.3f, 0.1f, 0.06f), secondary); // mask
                    break;
                case HeadStyle.Helmet:
                    Part(PrimitiveType.Sphere, head, new Vector3(0, 0.26f, 0), new Vector3(0.42f, 0.4f, 0.42f), secondary);
                    Part(PrimitiveType.Cube, head, new Vector3(0, 0.24f, 0.19f), new Vector3(0.3f, 0.05f, 0.04f), primary);   // visor slit
                    Part(PrimitiveType.Cube, head, new Vector3(0, 0.47f, -0.02f), new Vector3(0.05f, 0.12f, 0.34f), hair);    // crest
                    break;
                case HeadStyle.Horns:
                    Part(PrimitiveType.Sphere, head, new Vector3(0, 0.3f, -0.03f), new Vector3(0.38f, 0.2f, 0.38f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(-0.2f, 0.42f, 0), new Vector3(0.07f, 0.14f, 0.07f), secondary, new Vector3(0, 0, 35f));
                    Part(PrimitiveType.Capsule, head, new Vector3(0.2f, 0.42f, 0), new Vector3(0.07f, 0.14f, 0.07f), secondary, new Vector3(0, 0, -35f));
                    break;
                case HeadStyle.Crown:
                    Part(PrimitiveType.Sphere, head, new Vector3(0, 0.3f, -0.03f), new Vector3(0.38f, 0.2f, 0.38f), hair);
                    Part(PrimitiveType.Cylinder, head, new Vector3(0, 0.43f, 0), new Vector3(0.32f, 0.06f, 0.32f), MaterialCache.Lit(new Color(1f, 0.78f, 0.25f), 0.3f));
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 2f / 5f;
                        Part(PrimitiveType.Cube, head, new Vector3(Mathf.Sin(a) * 0.14f, 0.52f, Mathf.Cos(a) * 0.14f),
                            new Vector3(0.05f, 0.1f, 0.05f), MaterialCache.Lit(new Color(1f, 0.78f, 0.25f), 0.3f), new Vector3(0, a * Mathf.Rad2Deg, 45f));
                    }
                    break;
                case HeadStyle.WizardHat:
                    Part(PrimitiveType.Cylinder, head, new Vector3(0, 0.37f, 0), new Vector3(0.56f, 0.02f, 0.56f), primary);
                    Part(PrimitiveType.Cylinder, head, new Vector3(0, 0.47f, -0.01f), new Vector3(0.3f, 0.1f, 0.3f), primary);
                    Part(PrimitiveType.Cylinder, head, new Vector3(0, 0.62f, -0.04f), new Vector3(0.2f, 0.08f, 0.2f), primary, new Vector3(-8f, 0, 0));
                    Part(PrimitiveType.Cylinder, head, new Vector3(0, 0.75f, -0.09f), new Vector3(0.1f, 0.07f, 0.1f), primary, new Vector3(-18f, 0, 0));
                    Part(PrimitiveType.Cylinder, head, new Vector3(0, 0.45f, 0), new Vector3(0.31f, 0.03f, 0.31f), secondary);
                    break;
                case HeadStyle.Skull:
                    Part(PrimitiveType.Cube, head, new Vector3(0, 0.06f, 0.06f), new Vector3(0.22f, 0.08f, 0.2f), skin); // jaw
                    break;
            }
        }

        static void BuildArm(string name, Transform spine, Vector3 pos, Material sleeve, Material skin, float bulk, bool right)
        {
            var arm = Bone(name, spine, pos);
            float thick = 0.15f * Mathf.Lerp(1f, bulk, 0.6f);
            Part(PrimitiveType.Capsule, arm, new Vector3(0, -0.28f, 0), new Vector3(thick, 0.3f, thick), sleeve);
            var hand = Bone(right ? "HandR" : "HandL", arm, new Vector3(0, -0.62f, 0.02f));
            Part(PrimitiveType.Sphere, hand, Vector3.zero, Vector3.one * 0.15f, skin);
            if (right)
            {
                var socket = Bone("WeaponSocket", hand, Vector3.zero);
                socket.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        static void BuildLeg(string name, Transform hips, Vector3 pos, Material pants, Material boots, float bulk)
        {
            var leg = Bone(name, hips, pos);
            float thick = 0.19f * Mathf.Lerp(1f, bulk, 0.5f);
            Part(PrimitiveType.Capsule, leg, new Vector3(0, -0.42f, 0), new Vector3(thick, 0.42f, thick), pants);
            Part(PrimitiveType.Cube, leg, new Vector3(0, -0.88f, 0.05f), new Vector3(thick * 1.05f, 0.14f, 0.3f), boots);
        }

        // ------------------------------------------------------------------ weapons

        /// <summary>Attaches a weapon model to the character's right-hand socket, replacing any previous one.</summary>
        public static GameObject AttachWeapon(GameObject character, WeaponData weapon)
        {
            var socket = character.transform.Find(CharacterRig.WeaponSocket);
            if (socket == null || weapon == null) return null;
            for (int i = socket.childCount - 1; i >= 0; i--)
            {
                var child = socket.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child); else Object.DestroyImmediate(child);
            }

            GameObject model;
            if (weapon.modelPrefab != null)
            {
                model = Object.Instantiate(weapon.modelPrefab, socket, false);
                ApplyWeaponTint(model, weapon.tint);
            }
            else
            {
                model = BuildPrimitiveWeapon(weapon.weaponType, weapon.tint);
                model.transform.SetParent(socket, false);
            }
            model.name = "Weapon";
            foreach (var c in model.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
            }
            if (weapon.weaponType == WeaponType.Bow)
                model.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
            return model;
        }

        static void ApplyWeaponTint(GameObject model, Color tint)
        {
            var handle = MaterialCache.Lit(new Color(0.32f, 0.22f, 0.14f));
            var blade = MaterialCache.Lit(tint, tint.maxColorComponent > 0.95f ? 0.25f : 0f);
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = i == 0 ? blade : handle;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>Fallback weapon geometry if the imported model is unavailable.</summary>
        public static GameObject BuildPrimitiveWeapon(WeaponType type, Color tint)
        {
            var root = new GameObject("Weapon_" + type).transform;
            var metal = MaterialCache.Lit(tint);
            var wood = MaterialCache.Lit(new Color(0.35f, 0.24f, 0.15f));
            switch (type)
            {
                case WeaponType.Sword:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.05f, 0), new Vector3(0.05f, 0.1f, 0.05f), wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.17f, 0), new Vector3(0.28f, 0.04f, 0.06f), wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.62f, 0), new Vector3(0.09f, 0.85f, 0.025f), metal);
                    break;
                case WeaponType.Axe:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.4f, 0), new Vector3(0.06f, 0.45f, 0.06f), wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0.14f, 0.75f, 0), new Vector3(0.26f, 0.26f, 0.04f), metal);
                    break;
                case WeaponType.Dagger:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.04f, 0), new Vector3(0.04f, 0.07f, 0.04f), wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.3f, 0), new Vector3(0.07f, 0.42f, 0.02f), metal);
                    break;
                case WeaponType.Staff:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.55f, 0), new Vector3(0.05f, 0.75f, 0.05f), wood);
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 1.35f, 0), Vector3.one * 0.18f, MaterialCache.Lit(tint, 1.5f));
                    break;
                case WeaponType.Bow:
                    Part(PrimitiveType.Capsule, root, new Vector3(0, 0.35f, 0.08f), new Vector3(0.05f, 0.3f, 0.05f), wood, new Vector3(20f, 0, 0));
                    Part(PrimitiveType.Capsule, root, new Vector3(0, -0.15f, 0.08f), new Vector3(0.05f, 0.3f, 0.05f), wood, new Vector3(-20f, 0, 0));
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.1f, -0.03f), new Vector3(0.01f, 0.95f, 0.01f), metal);
                    break;
                case WeaponType.Hammer:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.4f, 0), new Vector3(0.06f, 0.45f, 0.06f), wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.85f, 0), new Vector3(0.36f, 0.2f, 0.2f), metal);
                    break;
            }
            return root.gameObject;
        }

        // ------------------------------------------------------------------ helpers
        static Transform Bone(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        public static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = type.ToString();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return go;
        }
    }
}
