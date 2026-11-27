using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Physics layers created by the project generator, with safe fallbacks.</summary>
    public static class Layers
    {
        public static int Player { get; private set; }
        public static int Enemy { get; private set; }
        public static int Pickup { get; private set; }
        public static int Projectile { get; private set; }
        public static int Destructible { get; private set; }
        public static int Environment { get; private set; }

        static Layers() => Refresh();

        /// <summary>Re-reads layer indices (the editor generator calls this after creating layers).</summary>
        public static void Refresh()
        {
            Player = Resolve("Player");
            Enemy = Resolve("Enemy");
            Pickup = Resolve("Pickup");
            Projectile = Resolve("Projectile");
            Destructible = Resolve("Destructible");
            Environment = Resolve("Environment");
        }

        static int Resolve(string name)
        {
            int l = LayerMask.NameToLayer(name);
            return l < 0 ? 0 : l;
        }

        static bool Configured => Enemy != 0;

        public static int DamageableMask =>
            Configured ? (1 << Player) | (1 << Enemy) | (1 << Destructible) : Physics.DefaultRaycastLayers;

        public static int ProjectileHitMask =>
            Configured ? DamageableMask | (1 << Environment) | 1 : Physics.DefaultRaycastLayers;

        public static int CameraObstacleMask => Configured ? (1 << Environment) | 1 : 1;

        /// <summary>Called once at startup so pickups and projectiles don't push characters around.</summary>
        public static void ConfigureCollisionMatrix()
        {
            if (!Configured) return;
            Physics.IgnoreLayerCollision(Pickup, Player, true);
            Physics.IgnoreLayerCollision(Pickup, Enemy, true);
            Physics.IgnoreLayerCollision(Pickup, Pickup, true);
            Physics.IgnoreLayerCollision(Projectile, Projectile, true);
            Physics.IgnoreLayerCollision(Projectile, Pickup, true);
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }
    }
}
