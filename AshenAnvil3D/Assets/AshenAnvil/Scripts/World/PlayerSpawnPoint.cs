using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Marks where the player appears.</summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
        }
    }
}
