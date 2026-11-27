using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Breakable prop (crate, barrel). Shatters into physics debris with an explosion force
    /// and drops gold or a potion.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Destructible : MonoBehaviour, IHitReactor
    {
        public float hitPoints = 30f;
        public int goldMin = 3;
        public int goldMax = 12;
        [Range(0f, 1f)] public float potionChance = 0.25f;
        public Color debrisColor = new Color(0.55f, 0.38f, 0.22f);
        public int debrisCount = 6;

        Health health;
        Rigidbody body;

        void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody>();
            health.Initialize(hitPoints, 0f, Team.Neutral);
            health.showHealthBar = false;
            health.uiHeight = 1.4f;
            health.Died += OnBroken;
        }

        public void OnHit(DamageInfo info)
        {
            if (body != null && !body.isKinematic)
                body.AddForce(info.knockback.Flat() * 1.5f + Vector3.up, ForceMode.VelocityChange);
        }

        void OnBroken(Health h)
        {
            Vector3 center = transform.position + Vector3.up * 0.5f;
            var mat = MaterialCache.Lit(debrisColor);
            for (int i = 0; i < debrisCount; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = "Debris";
                piece.layer = Layers.Pickup;
                piece.transform.position = center + Random.insideUnitSphere * 0.4f;
                piece.transform.rotation = Random.rotation;
                piece.transform.localScale = new Vector3(Random.Range(0.1f, 0.35f), Random.Range(0.05f, 0.15f), Random.Range(0.1f, 0.4f));
                piece.GetComponent<MeshRenderer>().sharedMaterial = mat;
                var rb = piece.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                rb.AddExplosionForce(6f, center - Vector3.up * 0.3f, 3f, 0.6f, ForceMode.VelocityChange);
                Destroy(piece, Random.Range(2.5f, 4f));
            }
            Vfx.Burst(center, 0.9f, new Color(1f, 0.85f, 0.6f, 0.5f), 0.25f);
            Sfx.Play(SfxId.Break, center);

            LootSystem.DropGold(transform.position, Random.Range(goldMin, goldMax + 1), 3);
            var db = GameDatabase.Instance;
            if (db != null && db.defaultPotion != null && Random.value < potionChance)
                Pickup.SpawnItem(transform.position, db.defaultPotion);
            Destroy(gameObject);
        }
    }
}
