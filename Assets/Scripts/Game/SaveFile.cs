using System;
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

            try
            {
                var state = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
                if (state != null)
                {
                    return state;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            // Keep the unreadable file so it can be inspected, and start over rather than crash on launch.
            var backupPath = SavePath + ".broken";
            File.Copy(SavePath, backupPath, true);
            Debug.LogWarning($"Save file could not be read and was copied to {backupPath}.");
            return new GameState();
        }

        public static void Save(GameState state)
        {
            state.LastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
