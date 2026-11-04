using System.Collections.Generic;
using System.Linq;
using Garapon.Core;
using NUnit.Framework;

namespace Garapon.Tests
{
    public class FunSystemsTests
    {
        sealed class FixedRandom : IRandomSource
        {
            readonly int value;
            public FixedRandom(int value) => this.value = value;
            public int Next(int maxExclusive) => value;
        }

        GameData data;
        GameState state;
        UpgradeSystem upgrades;
        Economy economy;
        Progression progression;
        LuckyTrash lucky;

        [SetUp]
        public void SetUp()
        {
            data = GameData.CreateDefault();
            state = new GameState();
            upgrades = new UpgradeSystem(data, state);
            economy = new Economy(data, state, upgrades);
            progression = new Progression(data, state, upgrades);
            lucky = new LuckyTrash(data, state, upgrades, economy);
        }

        [Test]
        public void ComboGrowsWithinWindowAndBreaksAfter()
        {
            var combo = new Combo(data, state);

            Assert.AreEqual(1.0, combo.Register(0), 1e-9);
            Assert.AreEqual(1 + data.ComboStep, combo.Register(1), 1e-9);
            Assert.AreEqual(1 + 2 * data.ComboStep, combo.Register(2), 1e-9);
            Assert.AreEqual(3, state.BestCombo);

            Assert.AreEqual(1.0, combo.Register(10), 1e-9);
            Assert.AreEqual(1, combo.Count);
            Assert.AreEqual(3, state.BestCombo);
        }

        [Test]
        public void ComboMultiplierIsCapped()
        {
            var combo = new Combo(data, state);
            for (var i = 0; i < 500; i++)
            {
                combo.Register(i * 0.1);
            }

            Assert.AreEqual(1 + data.ComboMaxSteps * data.ComboStep, combo.Multiplier, 1e-9);
        }

        [Test]
        public void CollectTrashAppliesComboAndFrenzy()
        {
            Assert.AreEqual(15, economy.CollectTrash(data.Trash[0], 1.5));

            state.FrenzySecondsLeft = 1;
            Assert.AreEqual(70, economy.CollectTrash(data.Trash[0]));
            Assert.AreEqual(85, state.TrashValue);
        }

        [Test]
        public void LuckyFrenzyRunsOut()
        {
            var reward = lucky.Collect(new FixedRandom(0));

            Assert.AreEqual(LuckyKind.Frenzy, reward.Kind);
            Assert.AreEqual(data.FrenzySeconds, state.FrenzySecondsLeft);
            Assert.AreEqual(data.FrenzyMultiplier, economy.FrenzyMultiplier);

            lucky.Tick(data.FrenzySeconds + 1);
            Assert.AreEqual(1, economy.FrenzyMultiplier);
            Assert.AreEqual(1, state.LuckyCollected);
        }

        [Test]
        public void LuckyWindfallHasAFloorAndScalesWithBank()
        {
            var early = lucky.Collect(new FixedRandom(LuckyTrash.FrenzyChancePercent));
            Assert.AreEqual(LuckyKind.Windfall, early.Kind);
            Assert.AreEqual(LuckyTrash.MinimumWindfall, early.Amount);

            state.Money = 1000000;
            state.AddUpgradeLevel("robot");
            var later = lucky.Collect(new FixedRandom(LuckyTrash.FrenzyChancePercent));
            Assert.Greater(later.Amount, LuckyTrash.MinimumWindfall);
            Assert.LessOrEqual(later.Amount, 1000300 * LuckyTrash.WindfallShareOfMoney);
        }

        [Test]
        public void LuckyTicketsScaleWithHopper()
        {
            state.AddUpgradeLevel("hopper");
            state.AddUpgradeLevel("hopper");

            var reward = lucky.Collect(new FixedRandom(99));

            Assert.AreEqual(LuckyKind.Tickets, reward.Kind);
            Assert.AreEqual(4 * LuckyTrash.TicketCranks, reward.Amount);
            Assert.AreEqual(reward.Amount, state.Tickets);
        }

        [Test]
        public void OfflineProgressDoesNotKeepAFrenzy()
        {
            var automation = new Automation(data, state, upgrades, economy);
            state.AddUpgradeLevel("robot");
            state.FrenzySecondsLeft = 30;

            var report = automation.ApplyOffline(100);

            Assert.AreEqual((long)(100 * data.OfflineEfficiency * upgrades.AutoCollectPerSecond * data.AverageTrashValue()), report.TrashCollected);
        }

        [Test]
        public void StampsAreEarnedOnceAndBoostSales()
        {
            state.TotalDraws = 1;
            state.AddDrawCount(PrizeColor.Red);

            var first = StampCard.Update(state, upgrades);
            var again = StampCard.Update(state, upgrades);

            CollectionAssert.AreEquivalent(new[] { "first_draw", "red" }, first.Select(stamp => stamp.Id));
            CollectionAssert.IsEmpty(again);

            state.TrashValue = 100;
            Assert.AreEqual((long)(100 * (1 + 2 * data.StampBonus)), economy.SellTrash());
        }

        [Test]
        public void StampIdsAreUnique()
        {
            var ids = StampCard.All.Select(stamp => stamp.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
        }

        [Test]
        public void SellingTracksLifetimeEarnings()
        {
            state.TrashValue = 100;
            state.AddItem("tissue", 2);
            economy.SellEverything();
            state.Money = 0;

            Assert.AreEqual(110, state.TotalEarned);
        }

        [Test]
        public void ShelvesAreStockedGradually()
        {
            var rice = data.FindProduct("rice");
            var shoes = data.FindProduct("upgrade_shoes");
            var tongs = data.FindProduct("upgrade_tongs");

            Assert.IsTrue(progression.IsRevealed(data.FindProduct("bread")));
            Assert.IsFalse(progression.IsRevealed(shoes));
            Assert.IsFalse(progression.IsRevealed(rice));

            state.TotalEarned = 300;
            Assert.IsTrue(progression.IsRevealed(shoes));
            Assert.IsFalse(progression.IsRevealed(tongs), "Tongs wait for a tool part to turn up.");

            state.AddDrawCount(PrizeColor.Blue);
            Assert.IsTrue(progression.IsRevealed(tongs));
        }

        [Test]
        public void BoughtUpgradesStayStocked()
        {
            state.AddUpgradeLevel("robot");
            Assert.IsTrue(progression.IsRevealed(data.FindUpgrade("robot")));
        }

        [Test]
        public void NewlyRevealedAnnouncesEachProductOnce()
        {
            var known = new HashSet<string>();
            var start = progression.NewlyRevealed(known);
            CollectionAssert.AreEquivalent(new[] { "bread", "milk", "eggs" }, start.Select(product => product.Id));

            state.TotalEarned = 100;
            CollectionAssert.AreEqual(new[] { "upgrade_shoes" }, progression.NewlyRevealed(known).Select(product => product.Id));
            CollectionAssert.IsEmpty(progression.NewlyRevealed(known));
        }

        [Test]
        public void NextRevealTeasesWhatIsMissing()
        {
            state.TotalEarned = 1000;
            var next = progression.NextReveal();

            Assert.AreEqual("tongs", next.Id);
            Assert.AreEqual("青玉をひく", progression.RevealHint(next));
        }

        [Test]
        public void NextRunKeepsClearsBestTimeAndStamps()
        {
            state.Cleared = true;
            state.PlaySeconds = 3600;
            state.Money = 999;
            state.Stamps.Add("red");

            var next = state.NextRun();

            Assert.AreEqual(1, next.Clears);
            Assert.AreEqual(3600, next.BestClearSeconds);
            CollectionAssert.AreEqual(new[] { "red" }, next.Stamps);
            Assert.AreEqual(0, next.Money);
            Assert.IsFalse(next.Cleared);

            next.Cleared = true;
            next.PlaySeconds = 7200;
            Assert.AreEqual(3600, next.NextRun().BestClearSeconds, "A slower clear keeps the best time.");
        }

        [Test]
        public void BestTimeIsTheTimeToTheGoldBall()
        {
            state.HasTripTicket = true;
            state.TripWonSeconds = 3000;
            state.PlaySeconds = 5000;
            state.Cleared = true;

            var next = state.NextRun();

            Assert.AreEqual(3000, next.BestClearSeconds, "Staying on after the ticket does not spoil the record.");
            Assert.IsFalse(next.HasTripTicket, "The ticket is used up by the trip.");
            Assert.AreEqual(0, next.TripWonSeconds);
        }

        [Test]
        public void EarlierClearsMultiplySales()
        {
            state.Clears = 1;
            state.TrashValue = 100;
            Assert.AreEqual(200, economy.SellTrash());
        }
    }
}
