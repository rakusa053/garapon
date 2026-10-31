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

        // The cheapest stocked upgrade that is not maxed yet, with how far the player is from buying it.
        public Goal NextGoal()
        {
            Goal best = null;
            foreach (var upgrade in data.Upgrades)
            {
                if (upgrades.IsMaxed(upgrade) || !IsRevealed(upgrade))
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

        // Groceries up to this price are on the shelves from the start.
        public const long StarterPrice = 300;

        // Shelves are stocked gradually so the supermarket keeps having something new: a product
        // appears once the player has earned half its price, and upgrades that cost an item also
        // wait until that item has turned up in the garapon.
        public bool IsRevealed(ProductDef product)
        {
            if (string.IsNullOrEmpty(product.UpgradeId))
            {
                return product.Price <= StarterPrice || state.TotalEarned * 2 >= product.Price;
            }

            return IsRevealed(data.FindUpgrade(product.UpgradeId));
        }

        public bool IsRevealed(UpgradeDef upgrade)
        {
            return state.GetUpgradeLevel(upgrade.Id) > 0 ||
                (state.TotalEarned * 2 >= upgrade.BaseCost && HasSeenItem(upgrade.ItemCostId));
        }

        // The next upgrade to be stocked and what it is waiting for, as a teaser.
        public UpgradeDef NextReveal()
        {
            UpgradeDef next = null;
            foreach (var upgrade in data.Upgrades)
            {
                if (!IsRevealed(upgrade) && (next == null || upgrade.BaseCost < next.BaseCost))
                {
                    next = upgrade;
                }
            }

            return next;
        }

        public string RevealHint(UpgradeDef upgrade)
        {
            var hints = new List<string>();
            if (state.TotalEarned * 2 < upgrade.BaseCost)
            {
                hints.Add($"累計{NumberFormat.Yen((upgrade.BaseCost + 1) / 2)}かせぐ");
            }

            if (!HasSeenItem(upgrade.ItemCostId))
            {
                var tier = data.PrizeTiers.Find(candidate => candidate.ItemId == upgrade.ItemCostId);
                hints.Add(tier != null ? $"{ColorName(tier.Color)}玉をひく" : $"{data.FindItem(upgrade.ItemCostId).DisplayName}を手に入れる");
            }

            return string.Join("・", hints);
        }

        // Products that are stocked now but were not in the given set, which is then updated.
        public List<ProductDef> NewlyRevealed(HashSet<string> known)
        {
            var revealed = new List<ProductDef>();
            foreach (var product in data.Products)
            {
                if (!known.Contains(product.Id) && IsRevealed(product))
                {
                    known.Add(product.Id);
                    revealed.Add(product);
                }
            }

            return revealed;
        }

        bool HasSeenItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || state.GetItem(itemId) > 0)
            {
                return true;
            }

            foreach (var tier in data.PrizeTiers)
            {
                if (tier.ItemId == itemId && state.GetDrawCount(tier.Color) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static string ColorName(PrizeColor color)
        {
            switch (color)
            {
                case PrizeColor.Gold: return "金";
                case PrizeColor.Silver: return "銀";
                case PrizeColor.Blue: return "青";
                case PrizeColor.Green: return "緑";
                case PrizeColor.Red: return "赤";
                default: return "白";
            }
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
