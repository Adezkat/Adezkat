using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Camera-relative movement on a CharacterController with gravity, dodge roll (with
    /// invulnerability frames) and physical pushing of loose rigidbodies.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IHitReactor
    {
        public static PlayerController Instance { get; private set; }

        public float rotationSpeed = 900f;
        public float gravity = 25f;
        public float dodgeDistance = 5.5f;
        public float dodgeDuration = 0.32f;
        public float dodgeCooldown = 0.9f;
        public float pushPower = 2.5f;

        public CharacterController Controller { get; private set; }
        public Health Health { get; private set; }
        public CharacterAnimation Anim { get; private set; }
        public PlayerCombat Combat => combat != null ? combat : combat = GetComponent<PlayerCombat>();
        public float MoveSpeed { get; set; } = 6f;
        public float DodgeCooldownNormalized => Mathf.Clamp01(dodgeTimer / dodgeCooldown);
        public bool IsDodging => dodgeTime > 0f;

        PlayerCombat combat;
        float verticalVelocity;
        float dodgeTime, dodgeTimer;
        Vector3 dodgeDir;
        Vector3 knockback;
        float speedBuff = 1f, speedBuffTime;

        void Awake()
        {
            Instance = this;
            Controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Anim = GetComponent<CharacterAnimation>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ApplySpeedBuff(float bonus, float duration)
        {
            speedBuff = 1f + bonus;
            speedBuffTime = duration;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Health.IsDead)
            {
                Anim.SetSpeed(0f);
                ApplyGravityOnly(dt);
                return;
            }

            if (speedBuffTime > 0f && (speedBuffTime -= dt) <= 0f) speedBuff = 1f;
            dodgeTimer = Mathf.Max(0f, dodgeTimer - dt);

            Vector2 input = PlayerInput.Move;
            Vector3 camForward = Vector3.forward, camRight = Vector3.right;
            if (ThirdPersonCamera.Instance != null)
            {
                camForward = ThirdPersonCamera.Instance.PlanarForward;
                camRight = new Vector3(camForward.z, 0f, -camForward.x);
            }
            Vector3 desired = Vector3.ClampMagnitude(camRight * input.x + camForward * input.y, 1f);

            if (PlayerInput.ConsumeDodge() && dodgeTimer <= 0f) StartDodge(desired);

            Vector3 planar;
            if (dodgeTime > 0f)
            {
                dodgeTime -= dt;
                planar = dodgeDir * (dodgeDistance / dodgeDuration);
                if (dodgeTime <= 0f) Health.Invulnerable = false;
            }
            else
            {
                float slow = Combat != null && Combat.IsActing ? 0.35f : 1f;
                planar = desired * MoveSpeed * speedBuff * slow;
            }

            knockback = Vector3.MoveTowards(knockback, Vector3.zero, 30f * dt);
            planar += knockback;

            if (Controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= gravity * dt;
            Controller.Move((planar + Vector3.up * verticalVelocity) * dt);

            // Facing: combat target wins, otherwise movement direction.
            Vector3 face = Combat != null && Combat.FacingTarget.HasValue ? Combat.FacingTarget.Value : desired;
            if (dodgeTime > 0f) face = dodgeDir;
            if (face.sqrMagnitude > 0.01f)
            {
                var targetRot = Quaternion.LookRotation(face.Flat());
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * dt);
            }

            Anim.SetSpeed(dodgeTime > 0f ? 1f : desired.magnitude);
        }

        void ApplyGravityOnly(float dt)
        {
            if (!Controller.enabled) return;
            verticalVelocity = Controller.isGrounded ? -2f : verticalVelocity - gravity * dt;
            Controller.Move(Vector3.up * verticalVelocity * dt);
        }

        void StartDodge(Vector3 desired)
        {
            dodgeDir = desired.sqrMagnitude > 0.01f ? desired.normalized : transform.forward;
            dodgeTime = dodgeDuration;
            dodgeTimer = dodgeCooldown;
            Health.Invulnerable = true;
            Vfx.Ring(transform.position, 0.3f, 1.2f, new Color(0.7f, 0.9f, 1f, 0.5f), 0.3f);
            Sfx.Play(SfxId.Dodge, transform.position);
        }

        public void OnHit(DamageInfo info)
        {
            knockback += info.knockback.Flat() * 0.6f;
            Anim.Hit();
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(info.isCrit ? 0.35f : 0.2f);
            Sfx.Play(SfxId.PlayerHurt, transform.position);
        }

        /// <summary>Pushes crates, barrels and loot with real physics forces.</summary>
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic || hit.moveDirection.y < -0.3f) return;
            Vector3 push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z) * pushPower;
            Vector3 v = body.GetVelocity();
            body.SetVelocity(new Vector3(push.x, v.y, push.z));
        }

        public void Teleport(Vector3 position)
        {
            Controller.enabled = false;
            transform.position = position;
            Controller.enabled = true;
        }
    }
}
