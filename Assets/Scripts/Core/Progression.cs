using System;
using System.Collections.Generic;

namespace Garapon.Core
{
    public sealed class Goal
    {
        public UpgradeDef Upgrade;
        public long Cost;
        public long MoneyShort;
        public ItemDef MissingItem;
        public long MissingItemCount;

        public bool Affordable => MoneyShort == 0 && MissingItemCount == 0;
    }

    // Helpers that tell the player what to aim for next and celebrate milestones.
    public sealed class Progression
    {
        readonly GameData data;
        readonly GameState state;
        readonly UpgradeSystem upgrades;

        public Progression(GameData data, GameState state, UpgradeSystem upgrades)
        {
            this.data = data;
            this.state = state;
            this.upgrades = upgrades;
        }

        // The cheapest upgrade that is not maxed yet, with how far the player is from buying it.
        // Upgrades that need an item the player has never seen still count, so the goal also
        // hints at which prize colour matters next.
        public Goal NextGoal()
        {
            Goal best = null;
            foreach (var upgrade in data.Upgrades)
            {
                if (upgrades.IsMaxed(upgrade))
                {
                    continue;
                }

                var cost = upgrades.GetCost(upgrade);
                if (best != null && cost >= best.Cost)
                {
                    continue;
                }

                var goal = new Goal
                {
                    Upgrade = upgrade,
                    Cost = cost,
                    MoneyShort = Math.Max(0, cost - state.Money)
                };
                if (!string.IsNullOrEmpty(upgrade.ItemCostId))
                {
                    goal.MissingItem = data.FindItem(upgrade.ItemCostId);
                    goal.MissingItemCount = Math.Max(0, upgrade.ItemCostAmount - state.GetItem(upgrade.ItemCostId));
                }

                best = goal;
            }

            return best;
        }

        // Draw counts worth celebrating: 10, 100, 1,000 and so on, plus 1/2 and 5 steps past 1,000.
        public static IEnumerable<long> DrawMilestonesCrossed(long before, long after)
        {
            for (var power = 10L; power <= after && power > 0; power *= 10)
            {
                foreach (var step in power >= 1000 ? new[] { 1L, 2L, 5L } : new[] { 1L })
                {
                    var milestone = power * step;
                    if (milestone > before && milestone <= after)
                    {
                        yield return milestone;
                    }
                }
            }
        }
    }
}
