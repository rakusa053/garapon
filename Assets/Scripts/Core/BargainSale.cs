using System.Collections.Generic;

namespace Garapon.Core
{
    // The supermarket's time sale: every few minutes one stocked product is on offer for a short
    // while. Upgrades are discounted and groceries give extra tickets, which gives a reason to drop
    // by the supermarket at the right moment.
    public sealed class BargainSale
    {
        readonly GameData data;
        readonly GameState state;
        readonly UpgradeSystem upgrades;
        readonly Progression progression;
        double untilNext = -1;

        public BargainSale(GameData data, GameState state, UpgradeSystem upgrades, Progression progression)
        {
            this.data = data;
            this.state = state;
            this.upgrades = upgrades;
            this.progression = progression;
        }

        public ProductDef Current => state.SaleSecondsLeft > 0 ? data.FindProduct(state.SaleProductId) : null;

        public double SecondsLeft => state.SaleSecondsLeft;

        // Advances the clock and returns the product whose sale has just started, if any.
        public ProductDef Tick(double seconds, IRandomSource random)
        {
            if (state.SaleSecondsLeft > 0)
            {
                state.SaleSecondsLeft -= seconds;
                return null;
            }

            if (untilNext < 0)
            {
                untilNext = NextDelay(random);
            }

            untilNext -= seconds;
            if (untilNext > 0)
            {
                return null;
            }

            untilNext = NextDelay(random);
            var candidates = new List<ProductDef>();
            foreach (var product in data.Products)
            {
                var upgrade = string.IsNullOrEmpty(product.UpgradeId) ? null : data.FindUpgrade(product.UpgradeId);
                if (progression.IsRevealed(product) && (upgrade == null || !upgrades.IsMaxed(upgrade)))
                {
                    candidates.Add(product);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var chosen = candidates[random.Next(candidates.Count)];
            state.SaleProductId = chosen.Id;
            state.SaleSecondsLeft = data.SaleSeconds;
            return chosen;
        }

        double NextDelay(IRandomSource random)
        {
            var spread = (int)System.Math.Max(1, (data.SaleIntervalMax - data.SaleIntervalMin) * 10);
            return data.SaleIntervalMin + random.Next(spread) / 10.0;
        }
    }
}
