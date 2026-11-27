using UnityEngine;

namespace AshenAnvil
{
    /// <summary>
    /// Merges on-screen touch controls with keyboard/mouse (for editor testing) into one input state.
    /// Touch widgets write into the Touch* fields; gameplay reads the merged properties.
    /// </summary>
    public static class PlayerInput
    {
        public static Vector2 TouchMove;
        public static bool TouchAttackHeld;
        public static Vector2 TouchLook;

        static bool skillQueued, dodgeQueued, potionQueued;
        static int lastFrame = -1;
        static Vector2 move;
        static bool attackHeld;
        static Vector2 look;

        public static Vector2 Move { get { Refresh(); return move; } }
        public static bool AttackHeld { get { Refresh(); return attackHeld; } }
        public static Vector2 Look { get { Refresh(); return look; } }

        public static void QueueSkill() => skillQueued = true;
        public static void QueueDodge() => dodgeQueued = true;
        public static void QueuePotion() => potionQueued = true;

        public static bool ConsumeSkill() { Refresh(); bool v = skillQueued; skillQueued = false; return v; }
        public static bool ConsumeDodge() { Refresh(); bool v = dodgeQueued; dodgeQueued = false; return v; }
        public static bool ConsumePotion() { Refresh(); bool v = potionQueued; potionQueued = false; return v; }

        public static void Reset()
        {
            TouchMove = Vector2.zero;
            TouchLook = Vector2.zero;
            TouchAttackHeld = false;
            skillQueued = dodgeQueued = potionQueued = false;
        }

        static void Refresh()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;

            Vector2 kb = Vector2.zero;
            kb.x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            kb.y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            move = TouchMove.sqrMagnitude > kb.sqrMagnitude ? TouchMove : Vector2.ClampMagnitude(kb, 1f);

            attackHeld = TouchAttackHeld || Input.GetKey(KeyCode.J);
            if (Input.GetKeyDown(KeyCode.K)) skillQueued = true;
            if (Input.GetKeyDown(KeyCode.Space)) dodgeQueued = true;
            if (Input.GetKeyDown(KeyCode.Q)) potionQueued = true;

            look = TouchLook;
            TouchLook = Vector2.zero; // drag deltas are consumed once per frame
            if (Input.touchCount == 0 && Input.GetMouseButton(1))
                look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 8f;
        }
    }
}
