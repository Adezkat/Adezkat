namespace AshenAnvil
{
    /// <summary>
    /// Bone names shared by the procedural model builder, the generated animation clips
    /// and the runtime fallback animator. Clips address bones by these exact paths.
    /// </summary>
    public static class CharacterRig
    {
        public const string Hips = "Hips";
        public const string Spine = "Hips/Spine";
        public const string Head = "Hips/Spine/Head";
        public const string ArmL = "Hips/Spine/ArmL";
        public const string ArmR = "Hips/Spine/ArmR";
        public const string LegL = "Hips/LegL";
        public const string LegR = "Hips/LegR";
        public const string WeaponSocket = "Hips/Spine/ArmR/HandR/WeaponSocket";

        public const float HipHeight = 1.0f;
    }
}
