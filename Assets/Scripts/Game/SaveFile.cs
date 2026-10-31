using System.IO;
using Garapon.Core;
using UnityEngine;

namespace Garapon.Game
{
    public static class SaveFile
    {
        // Tests point this at a temporary file so they never touch a real save.
        public static string PathOverride;

        static string SavePath => PathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");

        public static GameState Load()
        {
            if (!File.Exists(SavePath))
            {
                return new GameState();
            }

            return JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
        }

        public static void Save(GameState state)
        {
            var temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state));
            File.Copy(temporaryPath, SavePath, true);
            File.Delete(temporaryPath);
        }

        public static void Delete()
        {
            File.Delete(SavePath);
        }
    }
}
