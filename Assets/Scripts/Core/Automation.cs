using System;

namespace Garapon.Core
{
    public readonly struct AutomationReport
    {
        public readonly double Seconds;
        public readonly long TrashCollected;
        public readonly long MoneyEarned;

        public AutomationReport(double seconds, long trashCollected, long moneyEarned)
        {
            Seconds = seconds;
            TrashCollected = trashCollected;
            MoneyEarned = moneyEarned;
        }
    }

    // Everything that keeps working while the player is busy elsewhere or away:
    // the trash robot and the delivery service that sells on the player's behalf.
    public sealed class Automation
    {
        public const double DeliveryInterval = 5;

        readonly GameData data;
        readonly GameState state;
        readonly UpgradeSystem upgrades;
        readonly Economy economy;

        double collectCarry;
        double deliveryTimer;

        public Automation(GameData data, GameState state, UpgradeSystem upgrades, Economy economy)
        {
            this.data = data;
            this.state = state;
            this.upgrades = upgrades;
            this.economy = economy;
        }

        public static double OfflineSeconds(long lastSavedUnixSeconds, long nowUnixSeconds)
        {
            return lastSavedUnixSeconds <= 0 ? 0 : Math.Max(0, nowUnixSeconds - lastSavedUnixSeconds);
        }

        public AutomationReport Tick(double seconds)
        {
            if (seconds <= 0 || state.Cleared)
            {
                return default;
            }

            var collected = Collect(seconds);
            var earned = 0L;
            if (upgrades.HasDelivery)
            {
                deliveryTimer += seconds;
                if (deliveryTimer >= DeliveryInterval)
                {
                    deliveryTimer %= DeliveryInterval;
                    earned = economy.SellEverything();
                }
            }

            return new AutomationReport(seconds, collected, earned);
        }

        // Runs the automation for time spent away, capped and at reduced efficiency
        // so that coming back is a welcome-back bonus rather than a replacement for playing.
        public AutomationReport ApplyOffline(double seconds)
        {
            var capped = Math.Min(seconds, data.OfflineHoursCap * 3600);
            if (capped <= 0 || state.Cleared)
            {
                return default;
            }

            // A frenzy only lasts while someone is around to enjoy it.
            state.FrenzySecondsLeft = 0;
            var collected = Collect(capped * data.OfflineEfficiency);
            var earned = upgrades.HasDelivery ? economy.SellEverything() : 0;
            return new AutomationReport(capped, collected, earned);
        }

        long Collect(double seconds)
        {
            collectCarry += upgrades.AutoCollectPerSecond * data.AverageTrashValue() * economy.FrenzyMultiplier * seconds;
            // The tolerance stops many tiny frame ticks from summing to 9.999... and losing a yen.
            var whole = (long)Math.Floor(collectCarry + 1e-9);
            collectCarry -= whole;
            state.TrashValue += whole;
            return whole;
        }
    }
}
