using System;

namespace Garapon.Core
{
    // Souvenirs from the hot-spring trip: the prestige currency. Like heavenly chips or angel investors,
    // the total ever earned grows with the square root of lifetime earnings, so each one costs more than
    // the last, and they are spent on permanent perks at the souvenir exchange in town.
    public static class Prestige
    {
        public static long SouvenirsFor(GameData data, long lifetimeEarned)
        {
            if (lifetimeEarned <= 0 || data.SouvenirUnit <= 0)
            {
                return 0;
            }

            return (long)Math.Floor(Math.Sqrt(lifetimeEarned / data.SouvenirUnit) + 1e-9);
        }

        // Souvenirs the current run would bring home if it ended now.
        public static long Pending(GameData data, GameState state) =>
            Math.Max(0, SouvenirsFor(data, state.LifetimeEarned) - state.SouvenirsEarned);

        // Lifetime earnings needed for one more souvenir than the run would bring home now.
        public static long EarningsForNext(GameData data, GameState state)
        {
            var next = SouvenirsFor(data, state.LifetimeEarned) + 1;
            return (long)Math.Ceiling(next * (double)next * data.SouvenirUnit) - state.LifetimeEarned;
        }

        // The gold ball's trip ticket lets the player leave for the hot spring whenever they like.
        public static bool CanDepart(GameState state) => state.HasTripTicket && !state.Cleared;

        // Boards the bus to the hot spring, which ends the run. The souvenirs are granted by NextRun.
        public static bool Depart(GameState state)
        {
            if (!CanDepart(state))
            {
                return false;
            }

            state.Cleared = true;
            return true;
        }

        // Starts another run after clearing: grants the souvenirs and applies the start-of-run perks.
        public static GameState NextRun(GameData data, GameState state)
        {
            var granted = state.Cleared ? Pending(data, state) : 0;
            var next = state.NextRun();
            next.SouvenirsEarned += granted;
            next.Souvenirs += granted;

            next.Money += (long)PerkValue(data, next, PerkStat.StartMoney);
            var startLevels = (int)PerkValue(data, next, PerkStat.StartTrashValueLevels);
            foreach (var upgrade in data.Upgrades)
            {
                if (upgrade.Stat == UpgradeStat.TrashValue)
                {
                    for (var level = 0; level < Math.Min(startLevels, upgrade.MaxLevel); level++)
                    {
                        next.AddUpgradeLevel(upgrade.Id);
                    }
                }
            }

            return next;
        }

        public static double PerkValue(GameData data, GameState state, PerkStat stat)
        {
            var value = 0.0;
            foreach (var perk in data.Perks)
            {
                if (perk.Stat == stat)
                {
                    value += state.GetPerkLevel(perk.Id) * perk.ValuePerLevel;
                }
            }

            return value;
        }

        public static bool IsMaxed(GameState state, PerkDef perk) => state.GetPerkLevel(perk.Id) >= perk.MaxLevel;

        public static long Cost(GameState state, PerkDef perk) =>
            (long)Math.Round(perk.BaseCost * Math.Pow(perk.CostGrowth, state.GetPerkLevel(perk.Id)));

        public static bool CanBuy(GameState state, PerkDef perk) => !IsMaxed(state, perk) && state.Souvenirs >= Cost(state, perk);

        public static bool Buy(GameState state, PerkDef perk)
        {
            if (!CanBuy(state, perk))
            {
                return false;
            }

            state.Souvenirs -= Cost(state, perk);
            state.AddPerkLevel(perk.Id);
            return true;
        }

        // The draw count that guarantees gold, shortened by the golden-garapon perk.
        public static long GuaranteedGoldDraws(GameData data, GameState state)
        {
            var reduction = Math.Min(0.9, PerkValue(data, state, PerkStat.GuaranteeReduction));
            return (long)Math.Round(data.GuaranteedGoldDraws * (1 - reduction));
        }

        // Lucky trash shows up this many times as often.
        public static double LuckyRate(GameData data, GameState state) => 1 + PerkValue(data, state, PerkStat.LuckyRate);

        public static bool HasAutoShop(GameData data, GameState state) => PerkValue(data, state, PerkStat.AutoShop) >= 1;
    }
}
