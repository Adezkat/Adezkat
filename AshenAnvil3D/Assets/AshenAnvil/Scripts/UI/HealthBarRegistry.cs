using System.Collections.Generic;

namespace AshenAnvil
{
    /// <summary>Tracks which characters should display overhead health bars.</summary>
    public static class HealthBarRegistry
    {
        public static readonly List<Health> Tracked = new List<Health>();
        public static Health Boss { get; private set; }

        public static void Register(Health h, bool boss)
        {
            if (h == null) return;
            if (boss) Boss = h;
            else if (h.showHealthBar && !Tracked.Contains(h)) Tracked.Add(h);
        }

        public static void Unregister(Health h)
        {
            Tracked.Remove(h);
            if (Boss == h) Boss = null;
        }

        public static void Clear()
        {
            Tracked.Clear();
            Boss = null;
        }
    }
}
