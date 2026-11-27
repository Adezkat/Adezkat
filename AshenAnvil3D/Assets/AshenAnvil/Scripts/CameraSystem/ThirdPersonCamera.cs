using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Mobile-friendly orbit camera: follows the hero, rotates with right-side drag,
    /// avoids clipping through scenery with a sphere cast, and supports screen shake.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera Instance { get; private set; }

        public Transform target;
        public float distance = 9f;
        public float minPitch = 25f;
        public float maxPitch = 65f;
        public float pitch = 45f;
        public float yaw = 0f;
        public float followSharpness = 12f;
        public float lookSensitivity = 0.15f;
        public Vector3 pivotOffset = new Vector3(0f, 1.3f, 0f);

        public Vector3 PlanarForward
        {
            get
            {
                var f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                return f;
            }
        }

        Vector3 pivot;
        float currentDistance;
        float shake;

        void Awake()
        {
            Instance = this;
            currentDistance = distance;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetTarget(Transform t, bool snap = true)
        {
            target = t;
            if (snap && t != null)
            {
                yaw = t.eulerAngles.y;
                pivot = t.position + pivotOffset;
                UpdateTransform(1f);
            }
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void LateUpdate()
        {
            if (target == null) return;
            float sens = lookSensitivity * PlayerProfile.Instance.Data.settings.cameraSensitivity;
            Vector2 look = PlayerInput.Look;
            yaw += look.x * sens;
            pitch = Mathf.Clamp(pitch - look.y * sens * 0.5f, minPitch, maxPitch);

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            pivot = Vector3.Lerp(pivot, target.position + pivotOffset, t);
            UpdateTransform(t);
        }

        void UpdateTransform(float t)
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rot * Vector3.back;

            float wanted = distance;
            if (Physics.SphereCast(pivot, 0.3f, back, out var hit, distance, Layers.CameraObstacleMask, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(2f, hit.distance - 0.2f);
            currentDistance = wanted < currentDistance ? wanted : Mathf.Lerp(currentDistance, wanted, t);

            Vector3 pos = pivot + back * currentDistance;
            if (shake > 0f)
            {
                pos += Random.insideUnitSphere * shake * 0.3f;
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 2.5f);
            }
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
