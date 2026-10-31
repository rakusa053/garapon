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

        // Balls drawn per crank. Each hopper level doubles it, which is what lets the
        // late game reach a million draws.
        public long DrawsPerCrank => 1L << (int)Math.Min(GetValue(UpgradeStat.DrawBatch), 40);

        public double CrankSpeedMultiplier => 1 + GetValue(UpgradeStat.CrankSpeed);

        // Pieces of trash the robot collects per second.
        public double AutoCollectPerSecond => GetValue(UpgradeStat.AutoCollect);

        public bool HasAutoCrank => GetValue(UpgradeStat.AutoCrank) >= 1;

        public bool HasDelivery => GetValue(UpgradeStat.Delivery) >= 1;

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
