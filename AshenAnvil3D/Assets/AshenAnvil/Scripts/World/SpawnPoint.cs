using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Marks a location where monster waves may appear.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public bool bossSpawn;
        public float radius = 2.5f;

        public Vector3 RandomPosition()
        {
            var r = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(r.x, 0.1f, r.y);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = bossSpawn ? Color.red : new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
