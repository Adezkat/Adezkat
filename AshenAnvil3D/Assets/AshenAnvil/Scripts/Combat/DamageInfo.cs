using UnityEngine;

namespace AshenAnvil
{
    public struct DamageInfo
    {
        public float amount;
        public bool isCrit;
        public Team sourceTeam;
        public GameObject source;
        public Vector3 hitPoint;
        public Vector3 knockback;
        public float stunDuration;
    }

    /// <summary>Global combat notifications consumed by UI, audio and progression.</summary>
    public static class CombatEvents
    {
        public static event System.Action<Health, float, bool> Damaged;   // target, amount, crit
        public static event System.Action<Health, float> Healed;
        public static event System.Action<Health> Died;

        public static void RaiseDamaged(Health h, float amount, bool crit) => Damaged?.Invoke(h, amount, crit);
        public static void RaiseHealed(Health h, float amount) => Healed?.Invoke(h, amount);
        public static void RaiseDied(Health h) => Died?.Invoke(h);
    }
}
