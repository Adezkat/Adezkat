using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Thin facade over the Unity Animator (generated controller: Locomotion blend tree,
    /// Attack, Cast, Hit, Die). If no controller is assigned it drives the rig procedurally,
    /// so characters still animate before the project generator has run.
    /// </summary>
    public class CharacterAnimation : MonoBehaviour
    {
        public static readonly int SpeedId = Animator.StringToHash("Speed");
        public static readonly int AttackId = Animator.StringToHash("Attack");
        public static readonly int CastId = Animator.StringToHash("Cast");
        public static readonly int HitId = Animator.StringToHash("Hit");
        public static readonly int DeadId = Animator.StringToHash("Dead");
        public static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");

        Animator animator;
        Transform visualRoot;
        bool useAnimator;

        // Procedural fallback state.
        Transform hips, spine, armL, armR, legL, legR;
        float speed, phase, actionTimer, actionLength, hitTimer;
        bool dead, casting;
        float deathT;

        public Transform VisualRoot => visualRoot;

        public void Bind(GameObject visual)
        {
            visualRoot = visual.transform;
            animator = visual.GetComponent<Animator>();
            useAnimator = animator != null && animator.runtimeAnimatorController != null;
            hips = visualRoot.Find(CharacterRig.Hips);
            spine = visualRoot.Find(CharacterRig.Spine);
            armL = visualRoot.Find(CharacterRig.ArmL);
            armR = visualRoot.Find(CharacterRig.ArmR);
            legL = visualRoot.Find(CharacterRig.LegL);
            legR = visualRoot.Find(CharacterRig.LegR);
            dead = false;
            if (useAnimator)
            {
                animator.Rebind();
                animator.SetFloat(AttackSpeedId, 1f);
            }
        }

        /// <summary>Normalised locomotion speed 0..1.</summary>
        public void SetSpeed(float normalized)
        {
            speed = Mathf.Clamp01(normalized);
            if (useAnimator) animator.SetFloat(SpeedId, speed, 0.08f, Time.deltaTime);
        }

        public void Attack(float attackSpeed = 1f)
        {
            if (dead) return;
            if (useAnimator)
            {
                animator.SetFloat(AttackSpeedId, attackSpeed);
                animator.SetTrigger(AttackId);
            }
            else StartAction(0.45f / attackSpeed, false);
        }

        public void Cast()
        {
            if (dead) return;
            if (useAnimator) animator.SetTrigger(CastId);
            else StartAction(0.6f, true);
        }

        public void Hit()
        {
            if (dead) return;
            if (useAnimator) animator.SetTrigger(HitId);
            else hitTimer = 0.2f;
        }

        public void Die()
        {
            dead = true;
            if (useAnimator) animator.SetBool(DeadId, true);
        }

        public void Revive()
        {
            dead = false;
            deathT = 0f;
            if (useAnimator) animator.SetBool(DeadId, false);
        }

        void StartAction(float length, bool cast)
        {
            actionLength = Mathf.Max(0.1f, length);
            actionTimer = actionLength;
            casting = cast;
        }

        // ---------------------------------------------------------------- fallback
        void LateUpdate()
        {
            if (useAnimator || visualRoot == null || hips == null) return;
            float dt = Time.deltaTime;

            if (dead)
            {
                deathT = Mathf.Min(1f, deathT + dt * 3f);
                hips.localRotation = Quaternion.Euler(-85f * deathT, 0, 0);
                hips.localPosition = new Vector3(0, Mathf.Lerp(CharacterRig.HipHeight, 0.25f, deathT), 0);
                return;
            }

            phase += dt * Mathf.Lerp(2f, 11f, speed);
            float swing = Mathf.Sin(phase) * 38f * speed;
            float bob = speed > 0.05f ? Mathf.Abs(Mathf.Sin(phase)) * 0.06f : Mathf.Sin(phase * 0.5f) * 0.015f;

            hips.localPosition = new Vector3(0, CharacterRig.HipHeight + bob, 0);
            hips.localRotation = Quaternion.identity;
            if (legL) legL.localRotation = Quaternion.Euler(swing, 0, 0);
            if (legR) legR.localRotation = Quaternion.Euler(-swing, 0, 0);
            if (armL) armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0, -6f);
            float armRAngle = swing * 0.8f;

            if (actionTimer > 0f)
            {
                actionTimer -= dt;
                float t = 1f - actionTimer / actionLength;
                if (casting)
                {
                    float raise = Mathf.Sin(t * Mathf.PI) * -150f;
                    armRAngle = raise;
                    if (armL) armL.localRotation = Quaternion.Euler(raise, 0, -10f);
                }
                else
                {
                    armRAngle = t < 0.35f ? Mathf.Lerp(0f, -160f, t / 0.35f) : Mathf.Lerp(-160f, -20f, (t - 0.35f) / 0.65f);
                }
            }
            if (armR) armR.localRotation = Quaternion.Euler(armRAngle, 0, 6f);

            if (spine)
            {
                float lean = hitTimer > 0f ? -18f : speed * 8f;
                spine.localRotation = Quaternion.Euler(lean, 0, 0);
            }
            hitTimer -= dt;
        }
    }
}
