using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>Drag anywhere on the right side of the screen to orbit the camera.</summary>
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        int pointerId = int.MinValue;

        public static TouchLookArea Create(Transform parent)
        {
            var rt = UIKit.Rect("LookArea", parent);
            UIKit.Anchor(rt, new Vector2(0.4f, 0f), new Vector2(1f, 0.85f));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            return rt.gameObject.AddComponent<TouchLookArea>();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId == int.MinValue) pointerId = e.pointerId;
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            // Normalise by screen height so sensitivity is resolution independent.
            PlayerInput.TouchLook += e.delta * (1080f / Mathf.Max(1, Screen.height));
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) pointerId = int.MinValue;
        }
    }
}
