using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>Round touch button with hold support and a radial cooldown overlay.</summary>
    public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action onPress;
        public Action<bool> onHold;
        public Func<float> cooldown; // 0 = ready, 1 = just used

        Image face, overlay;
        Text label, badge;
        RectTransform rt;
        bool held;

        public static ActionButton Create(Transform parent, string text, Vector2 anchoredPos, float size, Color color)
        {
            var img = UIKit.Panel(parent, color, "Action_" + text, UIKit.Circle);
            img.type = Image.Type.Simple;
            var b = img.gameObject.AddComponent<ActionButton>();
            b.face = img;
            b.rt = img.rectTransform;
            UIKit.Place(b.rt, new Vector2(1, 0), anchoredPos, Vector2.one * size, new Vector2(0.5f, 0.5f));

            var ring = UIKit.Panel(img.transform, new Color(1, 1, 1, 0.25f), "Rim", UIKit.Circle);
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;
            UIKit.Stretch(ring.rectTransform, -6, -6, -6, -6);
            ring.transform.SetAsFirstSibling();

            var ov = UIKit.Rect("Cooldown", img.transform);
            UIKit.Stretch(ov);
            b.overlay = ov.gameObject.AddComponent<Image>();
            b.overlay.sprite = UIKit.Circle;
            b.overlay.type = Image.Type.Filled;
            b.overlay.fillMethod = Image.FillMethod.Radial360;
            b.overlay.fillOrigin = (int)Image.Origin360.Top;
            b.overlay.fillClockwise = false;
            b.overlay.color = new Color(0, 0, 0, 0.6f);
            b.overlay.raycastTarget = false;
            b.overlay.fillAmount = 0f;

            b.label = UIKit.Outline(UIKit.Label(img.transform, text, Mathf.RoundToInt(size * 0.2f), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            UIKit.Stretch(b.label.rectTransform);

            b.badge = UIKit.Outline(UIKit.Label(img.transform, "", 30, UIKit.Gold, TextAnchor.LowerRight, FontStyle.Bold));
            UIKit.Stretch(b.badge.rectTransform, 0, 0, 6, 2);
            return b;
        }

        public void SetBadge(string text) => badge.text = text;

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            rt.localScale = Vector3.one * 0.92f;
            onPress?.Invoke();
            onHold?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) { if (held && !Input.touchSupported) Release(); }

        void Release()
        {
            if (!held) return;
            held = false;
            rt.localScale = Vector3.one;
            onHold?.Invoke(false);
        }

        void OnDisable() => Release();

        void Update()
        {
            if (cooldown == null) return;
            float c = cooldown();
            overlay.fillAmount = c;
            face.color = new Color(face.color.r, face.color.g, face.color.b, c > 0f ? 0.6f : 0.95f);
        }
    }
}
