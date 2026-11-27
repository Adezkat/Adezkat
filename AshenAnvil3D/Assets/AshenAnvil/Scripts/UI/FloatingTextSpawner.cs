using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>Pooled floating combat text (damage, heals, loot) projected from world space.</summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        class Entry
        {
            public Text text;
            public Vector3 world;
            public float age, life;
            public Vector2 drift;
        }

        static FloatingTextSpawner instance;
        readonly List<Entry> active = new List<Entry>();
        readonly Stack<Text> pool = new Stack<Text>();
        RectTransform container;
        Camera cam;

        public static FloatingTextSpawner Create(Transform parent)
        {
            var rt = UIKit.Rect("FloatingText", parent);
            UIKit.Stretch(rt);
            instance = rt.gameObject.AddComponent<FloatingTextSpawner>();
            instance.container = rt;
            return instance;
        }

        void OnEnable()
        {
            CombatEvents.Damaged += OnDamaged;
            CombatEvents.Healed += OnHealed;
        }

        void OnDisable()
        {
            CombatEvents.Damaged -= OnDamaged;
            CombatEvents.Healed -= OnHealed;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void OnDamaged(Health h, float amount, bool crit)
        {
            if (!PlayerProfile.Instance.Data.settings.showDamageNumbers) return;
            Color c = h.team == Team.Player ? UIKit.Bad : crit ? new Color(1f, 0.85f, 0.2f) : Color.white;
            string s = crit ? amount.ToString("0") + "!" : amount.ToString("0");
            Spawn(h.UiAnchor, s, c, crit ? 1.45f : 1f);
        }

        void OnHealed(Health h, float amount)
        {
            if (h.team != Team.Player) return;
            Spawn(h.UiAnchor, "+" + amount.ToString("0"), UIKit.Good, 1f);
        }

        public static void Show(Vector3 world, string text, Color color, float scale = 1f)
        {
            if (instance != null) instance.Spawn(world, text, color, scale);
        }

        void Spawn(Vector3 world, string s, Color color, float scale)
        {
            if (active.Count > 40) return;
            var t = pool.Count > 0 ? pool.Pop() : UIKit.Outline(UIKit.Label(container, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "Float"));
            t.gameObject.SetActive(true);
            t.text = s;
            t.color = color;
            t.rectTransform.sizeDelta = new Vector2(400, 80);
            t.rectTransform.localScale = Vector3.one * scale;
            active.Add(new Entry
            {
                text = t,
                world = world + Random.insideUnitSphere * 0.3f,
                life = 0.9f,
                drift = new Vector2(Random.Range(-40f, 40f), 0)
            });
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                e.age += Time.deltaTime;
                if (e.age >= e.life || cam == null)
                {
                    e.text.gameObject.SetActive(false);
                    pool.Push(e.text);
                    active.RemoveAt(i);
                    continue;
                }
                float t = e.age / e.life;
                Vector3 sp = cam.WorldToScreenPoint(e.world);
                if (sp.z < 0f) { e.text.enabled = false; continue; }
                e.text.enabled = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(container, sp, null, out var local);
                e.text.rectTransform.anchoredPosition = local + e.drift * t + Vector2.up * (140f * t);
                var c = e.text.color;
                c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                e.text.color = c;
            }
        }
    }
}
