using UnityEngine;

namespace AshenAnvil
{
    [CreateAssetMenu(menuName = "Ashen Anvil/Skill", fileName = "Skill_")]
    public class SkillData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public SkillType skillType;
        public float cooldown = 8f;
        public float damageMultiplier = 2f;
        public float radius = 4f;
        public float range = 10f;
        public int count = 1;
        public float projectileSpeed = 18f;
        [Range(0f, 1f)] public float healPercent = 0f;
        public float stunDuration = 0f;
        public float knockback = 6f;
        public Color vfxColor = new Color(1f, 0.55f, 0.15f);
    }
}
