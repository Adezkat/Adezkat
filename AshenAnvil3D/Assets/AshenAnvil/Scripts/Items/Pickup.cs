using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Physical loot: bounces out with a Rigidbody impulse, then magnetises to the player.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public enum Kind { Gold, Item }

        public Kind kind;
        public int amount;
        public ItemData item;

        const float MagnetRange = 4.5f;
        const float CollectRange = 0.9f;

        Rigidbody rb;
        float age;
        bool magnetised;
        Transform visual;

        public static Pickup SpawnGold(Vector3 pos, int amount)
        {
            var db = GameDatabase.Instance;
            var p = Create("Gold", db != null ? db.coinMesh : null, PrimitiveType.Cylinder, new Color(1f, 0.8f, 0.2f), 0.6f,
                new Vector3(0.35f, 0.05f, 0.35f));
            p.kind = Kind.Gold;
            p.amount = amount;
            p.Launch(pos);
            return p;
        }

        public static Pickup SpawnItem(Vector3 pos, ItemData item)
        {
            var db = GameDatabase.Instance;
            bool potion = item is ConsumableData;
            Mesh mesh = db == null ? null : potion ? db.potionMesh : db.lootMesh;
            var p = Create(item.displayName, mesh, potion ? PrimitiveType.Capsule : PrimitiveType.Cube,
                potion ? item.tint : RarityUtil.GetColor(item.rarity), 0.8f, Vector3.one * 0.4f);
            p.kind = Kind.Item;
            p.item = item;
            p.amount = 1;
            p.Launch(pos);
            // Rarity beacon.
            Vfx.Ring(pos, 0.2f, 1.2f, RarityUtil.GetColor(item.rarity), 1.2f, p.transform);
            return p;
        }

        static Pickup Create(string name, Mesh mesh, PrimitiveType fallback, Color color, float emission, Vector3 fallbackScale)
        {
            var go = new GameObject("Pickup_" + name);
            go.layer = Layers.Pickup;
            var p = go.AddComponent<Pickup>();

            GameObject vis;
            if (mesh != null)
            {
                vis = new GameObject("Visual");
                vis.AddComponent<MeshFilter>().sharedMesh = mesh;
                vis.AddComponent<MeshRenderer>();
                vis.transform.localScale = Vector3.one * 0.5f;
            }
            else
            {
                vis = GameObject.CreatePrimitive(fallback);
                DestroyImmediate(vis.GetComponent<Collider>());
                vis.transform.localScale = fallbackScale;
            }
            vis.layer = Layers.Pickup;
            vis.transform.SetParent(go.transform, false);
            var r = vis.GetComponent<MeshRenderer>();
            r.sharedMaterial = MaterialCache.Lit(color, emission);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            p.visual = vis.transform;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.25f;
            p.rb = go.AddComponent<Rigidbody>();
            p.rb.mass = 0.2f;
            p.rb.interpolation = RigidbodyInterpolation.Interpolate;
            return p;
        }

        void Launch(Vector3 pos)
        {
            transform.position = pos + Vector3.up * 1f;
            Vector2 r = Random.insideUnitCircle * 2.5f;
            rb.AddForce(new Vector3(r.x, Random.Range(4f, 6f), r.y), ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.VelocityChange);
        }

        void Update()
        {
            age += Time.deltaTime;
            visual.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead) return;

            Vector3 target = player.transform.position + Vector3.up * 1f;
            float dist = Vector3.Distance(transform.position, target);
            if (!magnetised && age > 0.6f && dist < MagnetRange)
            {
                magnetised = true;
                rb.isKinematic = true;
                GetComponent<Collider>().enabled = false;
            }
            if (magnetised)
            {
                float speed = Mathf.Lerp(8f, 22f, age / 3f);
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                if (dist < CollectRange) Collect();
            }
            if (age > 60f) Destroy(gameObject);
        }

        void Collect()
        {
            var profile = PlayerProfile.Instance;
            if (kind == Kind.Gold)
            {
                profile.AddGold(amount);
                RunLoot.Gold += amount;
                FloatingTextSpawner.Show(transform.position, "+" + amount, new Color(1f, 0.85f, 0.3f), 0.8f);
                Sfx.Play(SfxId.Coin, transform.position);
            }
            else if (item != null)
            {
                profile.AddItem(item);
                FloatingTextSpawner.Show(transform.position, item.displayName, RarityUtil.GetColor(item.rarity), 1f);
                Sfx.Play(SfxId.Loot, transform.position);
                RunLoot.Record(item);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>Items collected during the current stage (shown on the results screen).</summary>
    public static class RunLoot
    {
        public static readonly System.Collections.Generic.List<ItemData> Items = new System.Collections.Generic.List<ItemData>();
        public static int Gold;
        public static void Clear() { Items.Clear(); Gold = 0; }
        public static void Record(ItemData item) => Items.Add(item);
    }
}
