using System;
using System.Collections.Generic;

namespace Garapon.Core
{
    [Serializable]
    public class CountEntry
    {
        public string Id;
        public long Count;
    }

    [Serializable]
    public class GameState
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public long Money;
        public long Tickets;
        public long TrashValue;
        public long SpendRemainder;
        public long TotalDraws;
        public double PlaySeconds;
        public bool Cleared;
        public long LastSavedUnixSeconds;

        // Lifetime money from selling, which drives which shelves have been stocked.
        public long TotalEarned;
        public long LuckyCollected;
        public long BestCombo;
        public double FrenzySecondsLeft;

        // Carried over between runs.
        public int Clears;
        public double BestClearSeconds;
        public List<string> Stamps = new List<string>();
        public List<CountEntry> Items = new List<CountEntry>();
        public List<CountEntry> UpgradeLevels = new List<CountEntry>();
        public List<CountEntry> DrawCounts = new List<CountEntry>();

        public bool HasStamp(string id) => Stamps.Contains(id);

        // The state for another run after clearing: progress resets, but clears, the best time
        // and the stamp card carry over.
        public GameState NextRun()
        {
            var best = Cleared && (BestClearSeconds <= 0 || PlaySeconds < BestClearSeconds) ? PlaySeconds : BestClearSeconds;
            return new GameState
            {
                Clears = Clears + (Cleared ? 1 : 0),
                BestClearSeconds = best,
                Stamps = new List<string>(Stamps)
            };
        }

        public long GetItem(string id) => Get(Items, id);
        public void AddItem(string id, long amount) => Add(Items, id, amount);
        public int GetUpgradeLevel(string id) => (int)Get(UpgradeLevels, id);
        public void AddUpgradeLevel(string id) => Add(UpgradeLevels, id, 1);
        public long GetDrawCount(PrizeColor color) => Get(DrawCounts, color.ToString());
        public void AddDrawCount(PrizeColor color) => Add(DrawCounts, color.ToString(), 1);

        static long Get(List<CountEntry> entries, string id)
        {
            var entry = entries.Find(candidate => candidate.Id == id);
            return entry?.Count ?? 0;
        }

        static void Add(List<CountEntry> entries, string id, long amount)
        {
            var entry = entries.Find(candidate => candidate.Id == id);
            if (entry == null)
            {
                entry = new CountEntry { Id = id };
                entries.Add(entry);
            }

            entry.Count += amount;
        }
    }
}
