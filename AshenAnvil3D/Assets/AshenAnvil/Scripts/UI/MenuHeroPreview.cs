using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Shows the selected hero on a pedestal in the menu scene, slowly turning.</summary>
    public class MenuHeroPreview : MonoBehaviour
    {
        public float spinSpeed = 18f;
        GameObject current;
        HeroData shown;
        WeaponData shownWeapon;
        float idleTimer;
        CharacterAnimation anim;

        public void Show(HeroData hero, bool force = false)
        {
            if (hero == null) return;
            var weapon = PlayerProfile.Instance.IsUnlocked(hero) ? PlayerProfile.Instance.EquippedWeaponFor(hero) : hero.startingWeapon;
            if (!force && hero == shown && weapon == shownWeapon) return;
            shown = hero;
            shownWeapon = weapon;
            if (current != null) Destroy(current);

            current = new GameObject("Preview_" + hero.id);
            current.transform.SetParent(transform, false);
            var visual = CharacterSpawner.InstantiateVisual(hero.prefab, hero.look, current.transform, "Visual");
            CharacterVisualFactory.AttachWeapon(visual, weapon);
            anim = current.AddComponent<CharacterAnimation>();
            anim.Bind(visual);
            anim.SetSpeed(0f);
            idleTimer = 1.5f;
            Vfx.Ring(transform.position, 0.3f, 1.6f, RarityUtil.GetColor(hero.rarity), 0.6f);
        }

        void Update()
        {
            if (current == null) return;
            current.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f && anim != null)
            {
                idleTimer = Random.Range(4f, 7f);
                if (Random.value < 0.5f) anim.Attack(1f); else anim.Cast();
            }
        }
    }
}
