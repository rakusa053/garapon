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
                    value += Effect(upgrade, state.GetUpgradeLevel(upgrade.Id));
                }
            }

            return value;
        }

        // The total effect of an upgrade at a level, including its milestone multipliers.
        public static double Effect(UpgradeDef upgrade, int level) =>
            level * upgrade.ValuePerLevel * MilestoneFactor(upgrade, level);

        public static double MilestoneFactor(UpgradeDef upgrade, int level) =>
            upgrade.MilestoneEvery > 0 ? Math.Pow(upgrade.MilestoneMultiplier, level / upgrade.MilestoneEvery) : 1;

        // The next level that multiplies the upgrade's effect, or 0 when there is none left.
        public int NextMilestone(UpgradeDef upgrade)
        {
            if (upgrade.MilestoneEvery <= 0)
            {
                return 0;
            }

            var next = (state.GetUpgradeLevel(upgrade.Id) / upgrade.MilestoneEvery + 1) * upgrade.MilestoneEvery;
            return next <= upgrade.MaxLevel ? next : 0;
        }

        public bool ReachedMilestone
        {
            get
            {
                foreach (var upgrade in data.Upgrades)
                {
                    if (MilestoneFactor(upgrade, state.GetUpgradeLevel(upgrade.Id)) > 1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Levels bought across every upgrade that raises the trash price, which the garbage truck builds on.
        public int TrashValueLevels
        {
            get
            {
                var levels = 0;
                foreach (var upgrade in data.Upgrades)
                {
                    if (upgrade.Stat == UpgradeStat.TrashValue)
                    {
                        levels += state.GetUpgradeLevel(upgrade.Id);
                    }
                }

                return levels;
            }
        }

        // Every source of extra tickets: the point card and the name tag per stamp.
        public double TicketMultiplier =>
            (1 + GetValue(UpgradeStat.TicketRate)) *
            (1 + state.Stamps.Count * GetValue(UpgradeStat.TicketPerStamp));

        // Balls drawn per crank. Each hopper level doubles it, which is what lets the
        // late game reach a million draws.
        public long DrawsPerCrank => 1L << (int)Math.Min(GetValue(UpgradeStat.DrawBatch), 40);

        // Grease and the souvenir perk both speed up the crank.
        public double CrankSpeedMultiplier =>
            (1 + GetValue(UpgradeStat.CrankSpeed)) * (1 + Prestige.PerkValue(data, state, PerkStat.CrankSpeed));

        // Pieces of trash the robot collects per second.
        public double AutoCollectPerSecond =>
            GetValue(UpgradeStat.AutoCollect) * (1 + TrashValueLevels * GetValue(UpgradeStat.RobotPerTrashLevel));

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
