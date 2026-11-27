using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Pooled projectile that sweeps a sphere through the physics scene every frame,
    /// so fast arrows never tunnel through thin colliders.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        static SimplePool<Projectile> pool;
        static Transform poolRoot;

        Team team;
        DamageInfo damage;
        Vector3 velocity;
        float radius;
        float explosionRadius;
        float life;
        float gravity;
        bool pierce;
        int piercesLeft;
        Color color;
        Transform visual;
        TrailRenderer trail;
        Health lastHit;

        public static Projectile Spawn(Vector3 position, Vector3 direction, float speed, DamageInfo damage, Color color,
            float radius = 0.25f, float explosionRadius = 0f, float lifetime = 3f, float gravity = 0f, int pierceCount = 0)
        {
            if (pool == null || poolRoot == null)
            {
                poolRoot = new GameObject("[Projectiles]").transform;
                pool = new SimplePool<Projectile>(Create, poolRoot);
            }
            var p = pool.Get();
            p.transform.SetParent(null, false);
            p.transform.position = position;
            p.transform.rotation = Quaternion.LookRotation(direction);
            p.team = damage.sourceTeam;
            p.damage = damage;
            p.velocity = direction.normalized * speed;
            p.radius = radius;
            p.explosionRadius = explosionRadius;
            p.life = lifetime;
            p.gravity = gravity;
            p.piercesLeft = pierceCount;
            p.pierce = pierceCount > 0;
            p.lastHit = null;
            p.SetColor(color, radius);
            return p;
        }

        static Projectile Create()
        {
            var go = new GameObject("Projectile");
            go.layer = Layers.Projectile;
            var p = go.AddComponent<Projectile>();
            var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.transform.SetParent(go.transform, false);
            vis.layer = Layers.Projectile;
            p.visual = vis.transform;
            var r = vis.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            p.trail = go.AddComponent<TrailRenderer>();
            p.trail.time = 0.18f;
            p.trail.minVertexDistance = 0.2f;
            p.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return p;
        }

        void SetColor(Color c, float r)
        {
            color = c;
            visual.localScale = Vector3.one * r * 2f;
            visual.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Lit(c, 2f);
            trail.sharedMaterial = MaterialCache.Vfx(new Color(c.r, c.g, c.b, 0.6f));
            trail.startWidth = r * 1.6f;
            trail.endWidth = 0f;
            trail.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Finish(false); return; }

            velocity += Vector3.down * gravity * dt;
            Vector3 step = velocity * dt;
            float dist = step.magnitude;
            if (dist > 0f &&
                Physics.SphereCast(transform.position, radius, step / dist, out var hit, dist, Layers.ProjectileHitMask, QueryTriggerInteraction.Ignore))
            {
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null && !CombatUtility.IsHostile(team, h.team))
                {
                    // Pass through friendly colliders.
                    transform.position += step;
                    return;
                }
                transform.position = hit.point;
                if (h != null && h != lastHit && explosionRadius <= 0f)
                {
                    var info = damage;
                    info.hitPoint = hit.point;
                    info.knockback = velocity.normalized * 3f + info.knockback;
                    CombatUtility.ApplyHit(h, info);
                    lastHit = h;
                    if (pierce && piercesLeft-- > 0) return;
                }
                Finish(true);
                return;
            }
            transform.position += step;
            if (velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
        }

        void Finish(bool impact)
        {
            if (impact)
            {
                if (explosionRadius > 0f)
                {
                    var center = transform.position;
                    var template = damage;
                    CombatUtility.DamageArea(center, explosionRadius, team, h =>
                    {
                        var d = template;
                        d.hitPoint = h.transform.position;
                        d.knockback = (h.transform.position - center).Flat().normalized * 6f;
                        return d;
                    });
                    Vfx.Burst(center, explosionRadius, color);
                    Sfx.Play(SfxId.Explosion, center);
                }
                else
                {
                    Vfx.Burst(transform.position, 0.6f, color, 0.2f);
                }
            }
            pool.Release(this);
        }
    }
}
