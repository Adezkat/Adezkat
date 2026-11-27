using System;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Visual description used to assemble a modular low-poly character rig.</summary>
    [Serializable]
    public class CharacterLook
    {
        public Color primaryColor = new Color(0.45f, 0.48f, 0.55f);
        public Color secondaryColor = new Color(0.25f, 0.22f, 0.2f);
        public Color skinColor = new Color(0.88f, 0.69f, 0.55f);
        public Color hairColor = new Color(0.35f, 0.22f, 0.12f);
        public Color eyeColor = new Color(0.1f, 0.1f, 0.1f);
        public HeadStyle headStyle = HeadStyle.Hair;
        [Range(0.5f, 3f)] public float scale = 1f;
        [Range(0.6f, 1.8f)] public float bulk = 1f;
        public bool cape;
    }
}
