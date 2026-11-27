using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>
    /// Draws enemy health bars on the HUD canvas (one overlay canvas instead of one
    /// world-space canvas per enemy keeps batching cheap on mobile).
    /// </summary>
    public class HealthBarManager : MonoBehaviour
    {
        class Bar { public RectTransform rt; public Image fill; public Image back; }

        readonly List<Bar> bars = new List<Bar>();
        RectTransform container;
        Camera cam;

        public static HealthBarManager Create(Transform parent)
        {
            var rt = UIKit.Rect("HealthBars", parent);
            UIKit.Stretch(rt);
            var m = rt.gameObject.AddComponent<HealthBarManager>();
            m.container = rt;
            return m;
        }

        Bar GetBar(int i)
        {
            while (bars.Count <= i)
            {
                var back = UIKit.Bar(container, new Color(0, 0, 0, 0.65f), UIKit.Bad, out var fill, "EnemyBar");
                back.rectTransform.sizeDelta = new Vector2(110, 16);
                bars.Add(new Bar { rt = back.rectTransform, fill = fill, back = back });
            }
            return bars[i];
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            int used = 0;
            if (cam != null)
            {
                var list = HealthBarRegistry.Tracked;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var h = list[i];
                    if (h == null) { list.RemoveAt(i); continue; }
                    if (h.IsDead) continue;
                    Vector3 sp = cam.WorldToScreenPoint(h.UiAnchor);
                    if (sp.z < 0f || sp.x < -50 || sp.x > Screen.width + 50 || sp.y < -50 || sp.y > Screen.height + 50) continue;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, sp, null, out var local)) continue;
                    var bar = GetBar(used++);
                    bar.rt.gameObject.SetActive(true);
                    bar.rt.anchoredPosition = local;
                    bar.fill.fillAmount = h.Normalized;
                }
            }
            for (int i = used; i < bars.Count; i++)
                if (bars[i].rt.gameObject.activeSelf) bars[i].rt.gameObject.SetActive(false);
        }
    }
}
