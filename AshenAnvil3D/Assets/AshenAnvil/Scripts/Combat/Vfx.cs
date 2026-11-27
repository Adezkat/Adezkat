using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Lightweight pooled visual effects: expanding bursts and ground telegraph rings.</summary>
    public class Vfx : MonoBehaviour
    {
        static SimplePool<Vfx> spheres;
        static SimplePool<Vfx> discs;
        static Transform root;
        static MaterialPropertyBlock block;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        MeshRenderer meshRenderer;
        SimplePool<Vfx> owner;
        float duration, elapsed, startSize, endSize;
        Color color;
        Transform follow;
        bool flat;

        static void EnsurePools()
        {
            if (spheres != null && root != null) return;
            root = new GameObject("[Vfx]").transform;
            block = new MaterialPropertyBlock();
            spheres = new SimplePool<Vfx>(() => Create(PrimitiveType.Sphere, false), root);
            discs = new SimplePool<Vfx>(() => Create(PrimitiveType.Cylinder, true), root);
        }

        static Vfx Create(PrimitiveType type, bool isFlat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Vfx_" + type;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var v = go.AddComponent<Vfx>();
            v.meshRenderer = go.GetComponent<MeshRenderer>();
            v.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            v.meshRenderer.receiveShadows = false;
            v.meshRenderer.sharedMaterial = MaterialCache.Vfx(Color.white);
            v.flat = isFlat;
            v.owner = isFlat ? discs : spheres;
            return v;
        }

        /// <summary>Expanding translucent sphere (explosions, hits, skills).</summary>
        public static void Burst(Vector3 pos, float radius, Color color, float duration = 0.35f)
        {
            EnsurePools();
            var v = spheres.Get();
            v.owner = spheres;
            v.Play(pos, radius * 0.2f, radius * 2f, color, duration, null);
        }

        /// <summary>Flat ring on the ground (telegraphs, shockwaves, heals).</summary>
        public static Vfx Ring(Vector3 pos, float startRadius, float endRadius, Color color, float duration, Transform follow = null)
        {
            EnsurePools();
            var v = discs.Get();
            v.owner = discs;
            v.Play(pos + Vector3.up * 0.05f, startRadius * 2f, endRadius * 2f, color, duration, follow);
            return v;
        }

        void Play(Vector3 pos, float from, float to, Color c, float dur, Transform followTarget)
        {
            transform.SetParent(null, false);
            transform.position = pos;
            startSize = from;
            endSize = to;
            color = c;
            duration = Mathf.Max(0.05f, dur);
            elapsed = 0f;
            follow = followTarget;
            Apply(0f);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (t >= 1f) { owner.Release(this); return; }
            if (follow != null) transform.position = follow.position + Vector3.up * 0.05f;
            Apply(t);
        }

        void Apply(float t)
        {
            float ease = 1f - (1f - t) * (1f - t);
            float s = Mathf.Lerp(startSize, endSize, ease);
            transform.localScale = flat ? new Vector3(s, 0.02f, s) : Vector3.one * s;
            var c = color;
            c.a = color.a * (1f - t * t);
            block.SetColor(BaseColor, c);
            block.SetColor(ColorId, c);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
