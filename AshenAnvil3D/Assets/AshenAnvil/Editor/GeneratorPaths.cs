using System.IO;
using UnityEditor;

namespace AshenAnvil.EditorTools
{
    public static class GeneratorPaths
    {
        public const string Root = "Assets/AshenAnvil";
        public const string Models = Root + "/Models";
        public const string Generated = Root + "/Generated";
        public const string Materials = Generated + "/Materials";
        public const string Palette = Generated + "/Materials/Palette";
        public const string Animations = Generated + "/Animations";
        public const string HeroPrefabs = Generated + "/Prefabs/Heroes";
        public const string MonsterPrefabs = Generated + "/Prefabs/Monsters";
        public const string WeaponPrefabs = Generated + "/Prefabs/Weapons";
        public const string PropPrefabs = Generated + "/Prefabs/Props";
        public const string Settings = Root + "/Settings";
        public const string Scenes = Root + "/Scenes";
        public const string MainMenuScene = Scenes + "/MainMenu.unity";
        public const string GameScene = Scenes + "/Game.unity";
        public const string Database = Root + "/Resources/GameDatabase.asset";
        public const string Icon = Root + "/Art/AppIcon.png";

        /// <summary>Creates a folder path (and parents) inside the AssetDatabase.</summary>
        public static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
