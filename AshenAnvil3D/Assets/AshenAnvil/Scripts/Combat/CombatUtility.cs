using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    public static class CombatUtility
    {
        static readonly Collider[] buffer = new Collider[96];
        static readonly HashSet<Health> unique = new HashSet<Health>();
        static readonly List<Health> results = new List<Health>();

        public static bool IsHostile(Team attacker, Team target)
        {
            if (attacker == target) return false;
            if (target == Team.Neutral) return attacker == Team.Player;
            return attacker != Team.Neutral;
        }

        /// <summary>Rolls crit and variance for an attack.</summary>
        public static DamageInfo Roll(StatBlock stats, float multiplier, GameObject source, Team team)
        {
            bool crit = UnityEngine.Random.value < stats.critChance;
            float amount = stats.attack * multiplier * UnityEngine.Random.Range(0.92f, 1.08f);
            if (crit) amount *= 1f + stats.critDamage;
            return new DamageInfo
            {
                amount = amount,
                isCrit = crit,
                source = source,
                sourceTeam = team,
                hitPoint = source != null ? source.transform.position : Vector3.zero
            };
        }

        /// <summary>
        /// Returns every hostile Health within radius (optionally inside a forward arc).
        /// The returned list is reused; copy it if you need to keep it.
        /// </summary>
        public static List<Health> FindTargets(Vector3 center, float radius, Team attacker,
            Vector3 forward = default, float arcDegrees = 360f)
        {
            results.Clear();
            unique.Clear();
            int n = Physics.OverlapSphereNonAlloc(center, radius, buffer, Layers.DamageableMask, QueryTriggerInteraction.Ignore);
            bool useArc = arcDegrees < 359f && forward.sqrMagnitude > 0.01f;
            float cosHalf = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.IsDead || !IsHostile(attacker, h.team) || !unique.Add(h)) continue;
                if (useArc)
                {
                    Vector3 to = (h.transform.position - center).Flat();
                    if (to.sqrMagnitude > 0.25f && Vector3.Dot(to.normalized, forward.Flat().normalized) < cosHalf) continue;
                }
                results.Add(h);
            }
            return results;
        }

        public static Health FindNearest(Vector3 position, float radius, Team attacker, bool includeNeutral = false)
        {
            Health best = null;
            float bestSq = float.MaxValue;
            foreach (var h in FindTargets(position, radius, attacker))
            {
                if (!includeNeutral && h.team == Team.Neutral) continue;
                float d = (h.transform.position - position).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = h; }
            }
            return best;
        }

        /// <summary>Damages every target in the area. Returns total damage dealt.</summary>
        public static float DamageArea(Vector3 center, float radius, Team attacker, Func<Health, DamageInfo> makeDamage,
            Vector3 forward = default, float arcDegrees = 360f)
        {
            float total = 0f;
            var targets = new List<Health>(FindTargets(center, radius, attacker, forward, arcDegrees));
            foreach (var h in targets)
                total += ApplyHit(h, makeDamage(h));
            return total;
        }

        /// <summary>Applies damage plus physical reactions (knockback, stun).</summary>
        public static float ApplyHit(Health target, DamageInfo info)
        {
            if (target == null) return 0f;
            float dealt = target.TakeDamage(info);
            if (dealt <= 0f) return 0f;
            var reactor = target.GetComponent<IHitReactor>();
            reactor?.OnHit(info);
            return dealt;
        }
    }

    /// <summary>Implemented by characters that react physically to hits.</summary>
    public interface IHitReactor
    {
        void OnHit(DamageInfo info);
    }
}
