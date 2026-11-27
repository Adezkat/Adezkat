using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>Floating thumbstick: appears where the thumb lands inside its zone.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public float radius = 130f;
        RectTransform zone, baseRing, knob;
        Vector2 restPosition;
        int pointerId = int.MinValue;

        public static VirtualJoystick Create(Transform parent)
        {
            var zone = UIKit.Rect("JoystickZone", parent);
            UIKit.Anchor(zone, new Vector2(0, 0), new Vector2(0.4f, 0.65f));
            var hit = zone.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            var j = zone.gameObject.AddComponent<VirtualJoystick>();
            j.zone = zone;
            var ring = UIKit.Panel(zone, new Color(1, 1, 1, 0.16f), "Base", UIKit.Circle);
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;
            j.baseRing = ring.rectTransform;
            j.baseRing.sizeDelta = Vector2.one * j.radius * 2f;
            j.baseRing.anchorMin = j.baseRing.anchorMax = Vector2.zero;
            j.restPosition = new Vector2(250, 250);
            j.baseRing.anchoredPosition = j.restPosition;

            var knobImg = UIKit.Panel(ring.transform, new Color(1, 1, 1, 0.55f), "Knob", UIKit.Circle);
            knobImg.type = Image.Type.Simple;
            knobImg.raycastTarget = false;
            j.knob = knobImg.rectTransform;
            j.knob.sizeDelta = Vector2.one * 110f;
            return j;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;
            pointerId = e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out var local);
            baseRing.anchoredPosition = local - zone.rect.min;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRing, e.position, e.pressEventCamera, out var local);
            var clamped = Vector2.ClampMagnitude(local, radius);
            knob.anchoredPosition = clamped;
            var v = clamped / radius;
            PlayerInput.TouchMove = v.magnitude < 0.12f ? Vector2.zero : v;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            pointerId = int.MinValue;
            knob.anchoredPosition = Vector2.zero;
            baseRing.anchoredPosition = restPosition;
            PlayerInput.TouchMove = Vector2.zero;
        }

        void OnDisable()
        {
            pointerId = int.MinValue;
            PlayerInput.TouchMove = Vector2.zero;
        }
    }
}
