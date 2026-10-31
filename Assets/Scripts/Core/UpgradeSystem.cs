using System;

namespace Garapon.Core
{
    public sealed class UpgradeSystem
    {
        readonly GameData data;
        readonly GameState state;

        public UpgradeSystem(GameData data, GameState state)
        {
            this.data = data;
            this.state = state;
        }

        public double GetValue(UpgradeStat stat)
        {
            var value = 0.0;
            foreach (var upgrade in data.Upgrades)
            {
                if (upgrade.Stat == stat)
                {
                    value += state.GetUpgradeLevel(upgrade.Id) * upgrade.ValuePerLevel;
                }
            }

            return value;
        }

        public bool IsMaxed(UpgradeDef upgrade) => state.GetUpgradeLevel(upgrade.Id) >= upgrade.MaxLevel;

        public long GetCost(UpgradeDef upgrade)
        {
            var level = state.GetUpgradeLevel(upgrade.Id);
            return (long)Math.Round(upgrade.BaseCost * Math.Pow(upgrade.CostGrowth, level));
        }

        public bool HasItemCost(UpgradeDef upgrade) =>
            string.IsNullOrEmpty(upgrade.ItemCostId) || state.GetItem(upgrade.ItemCostId) >= upgrade.ItemCostAmount;

        // The money cost is paid at the register, so this only applies the item cost and the level.
        public bool Apply(UpgradeDef upgrade)
        {
            if (IsMaxed(upgrade) || !HasItemCost(upgrade))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(upgrade.ItemCostId))
            {
                state.AddItem(upgrade.ItemCostId, -upgrade.ItemCostAmount);
            }

            state.AddUpgradeLevel(upgrade.Id);
            return true;
        }
    }
}
