using System;
using System.IO;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>JSON save file in Application.persistentDataPath with an atomic write and a backup.</summary>
    public static class SaveSystem
    {
        const string FileName = "ashen_anvil_save.json";
        static string PathMain => Path.Combine(Application.persistentDataPath, FileName);
        static string PathBackup => PathMain + ".bak";

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                string tmp = PathMain + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(PathMain)) File.Copy(PathMain, PathBackup, true);
                if (File.Exists(PathMain)) File.Delete(PathMain);
                File.Move(tmp, PathMain);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save failed: " + e.Message);
            }
        }

        public static SaveData Load()
        {
            var data = TryRead(PathMain) ?? TryRead(PathBackup);
            return data;
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null) return null;
                if (data.equippedUids == null || data.equippedUids.Length != 6)
                {
                    var fixedArr = new int[6];
                    if (data.equippedUids != null)
                        Array.Copy(data.equippedUids, fixedArr, Mathf.Min(6, data.equippedUids.Length));
                    data.equippedUids = fixedArr;
                }
                if (data.settings == null) data.settings = new GameSettings();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not read save {path}: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (File.Exists(PathMain)) File.Delete(PathMain);
            if (File.Exists(PathBackup)) File.Delete(PathBackup);
        }
    }
}
