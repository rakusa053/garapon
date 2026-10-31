using System;

namespace Garapon.Core
{
    public enum LuckyKind
    {
        Frenzy,
        Windfall,
        Tickets
    }

    public readonly struct LuckyReward
    {
        public readonly LuckyKind Kind;
        public readonly long Amount;

        public LuckyReward(LuckyKind kind, long amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }

    // A golden can that shows up in the field now and then, in the spirit of Cookie Clicker's
    // golden cookie: a surprise that rewards paying attention, with a random effect.
    public sealed class LuckyTrash
    {
        public const int FrenzyChancePercent = 50;
        public const int WindfallChancePercent = 35;
        public const long MinimumWindfall = 300;
        public const double WindfallShareOfMoney = 0.15;
        public const double WindfallRobotSeconds = 900;
        public const long MinimumTickets = 10;
        public const long TicketCranks = 10;

        readonly GameData data;
        readonly GameState state;
        readonly UpgradeSystem upgrades;
        readonly Economy economy;

        public LuckyTrash(GameData data, GameState state, UpgradeSystem upgrades, Economy economy)
        {
            this.data = data;
            this.state = state;
            this.upgrades = upgrades;
            this.economy = economy;
        }

        public LuckyReward Collect(IRandomSource random)
        {
            state.LuckyCollected++;
            var roll = random.Next(100);
            if (roll < FrenzyChancePercent)
            {
                state.FrenzySecondsLeft = data.FrenzySeconds;
                return new LuckyReward(LuckyKind.Frenzy, (long)data.FrenzySeconds);
            }

            if (roll < FrenzyChancePercent + WindfallChancePercent)
            {
                // Like Cookie Clicker's "Lucky": a share of the bank, capped by what the robot makes in 15 minutes.
                var robotIncome = upgrades.AutoCollectPerSecond * data.AverageTrashValue() * economy.SaleMultiplier * WindfallRobotSeconds;
                var amount = Math.Max(MinimumWindfall, (long)Math.Min(state.Money * WindfallShareOfMoney, robotIncome));
                state.AddEarnings(amount);
                return new LuckyReward(LuckyKind.Windfall, amount);
            }

            var tickets = Math.Max(MinimumTickets, upgrades.DrawsPerCrank * TicketCranks);
            state.Tickets += tickets;
            return new LuckyReward(LuckyKind.Tickets, tickets);
        }

        public void Tick(double seconds)
        {
            state.FrenzySecondsLeft = Math.Max(0, state.FrenzySecondsLeft - seconds);
        }
    }
}
