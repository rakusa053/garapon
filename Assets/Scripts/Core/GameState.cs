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
        public List<CountEntry> Items = new List<CountEntry>();
        public List<CountEntry> UpgradeLevels = new List<CountEntry>();
        public List<CountEntry> DrawCounts = new List<CountEntry>();

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
