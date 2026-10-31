using Garapon.Core;
using NUnit.Framework;

namespace Garapon.Tests
{
    public class ProgressionTests
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
        Automation automation;
        Progression progression;

        [SetUp]
        public void SetUp()
        {
            data = GameData.CreateDefault();
            state = new GameState();
            upgrades = new UpgradeSystem(data, state);
            economy = new Economy(data, state, upgrades);
            automation = new Automation(data, state, upgrades, economy);
            progression = new Progression(data, state, upgrades);
        }

        void SetLevel(string upgradeId, int level)
        {
            for (var i = 0; i < level; i++)
            {
                state.AddUpgradeLevel(upgradeId);
            }
        }

        [Test]
        public void HopperDoublesDrawsPerCrank()
        {
            Assert.AreEqual(1, upgrades.DrawsPerCrank);
            SetLevel("hopper", 3);
            Assert.AreEqual(8, upgrades.DrawsPerCrank);
        }

        [Test]
        public void BatchDrawStopsWhenTicketsRunOut()
        {
            state.Tickets = 5;
            var lottery = new Lottery(state, new DrawResolver(data, new FixedRandom(999999)));

            var result = lottery.DrawBatch(8);

            Assert.AreEqual(5, result.Draws);
            Assert.AreEqual(5, result.Count(PrizeColor.White));
            Assert.AreEqual(0, state.Tickets);
            Assert.AreEqual(5, state.GetItem("tissue"));
            Assert.IsNull(lottery.DrawBatch(8));
        }

        [Test]
        public void BatchDrawReportsRarestPrize()
        {
            state.Tickets = 3;
            var rolls = new SequenceRandom(999999, 5000, 300000);
            var lottery = new Lottery(state, new DrawResolver(data, rolls));

            var result = lottery.DrawBatch(3);

            Assert.AreEqual(PrizeColor.Silver, result.Best.Color);
        }

        [Test]
        public void GoldIsGuaranteedOnTheConfiguredDraw()
        {
            data.GuaranteedGoldDraws = 10;
            state.Tickets = 100;
            var lottery = new Lottery(state, new DrawResolver(data, new FixedRandom(999999)));

            var result = lottery.DrawBatch(100);

            Assert.AreEqual(10, result.Draws);
            Assert.AreEqual(PrizeColor.Gold, result.Best.Color);
            Assert.IsTrue(state.Cleared);
            Assert.AreEqual(90, state.Tickets);
        }

        [Test]
        public void DrawsUntilGuaranteeCountsDown()
        {
            data.GuaranteedGoldDraws = 10;
            state.TotalDraws = 4;
            var lottery = new Lottery(state, new DrawResolver(data, new FixedRandom(999999)));
            Assert.AreEqual(6, lottery.DrawsUntilGuarantee);

            data.GuaranteedGoldDraws = 0;
            Assert.AreEqual(-1, lottery.DrawsUntilGuarantee);
        }

        [Test]
        public void RobotCollectsTrashOverTime()
        {
            SetLevel("robot", 2);
            var expected = (long)(upgrades.AutoCollectPerSecond * 1.5 * data.AverageTrashValue());

            var report = automation.Tick(1.5);

            Assert.Greater(expected, 0);
            Assert.AreEqual(expected, report.TrashCollected);
            Assert.AreEqual(expected, state.TrashValue);
        }

        [Test]
        public void RobotKeepsFractionsBetweenTicks()
        {
            SetLevel("robot", 1);
            for (var i = 0; i < 60; i++)
            {
                automation.Tick(1.0 / 60);
            }

            Assert.AreEqual((long)(upgrades.AutoCollectPerSecond * data.AverageTrashValue()), state.TrashValue);
        }

        [Test]
        public void DeliverySellsEverythingPeriodically()
        {
            SetLevel("delivery", 1);
            state.TrashValue = 100;
            state.AddItem("tissue", 2);

            Assert.AreEqual(0, automation.Tick(Automation.DeliveryInterval - 0.1).MoneyEarned);
            Assert.AreEqual(110, automation.Tick(0.2).MoneyEarned);
            Assert.AreEqual(110, state.Money);
            Assert.AreEqual(0, state.TrashValue);
            Assert.AreEqual(0, state.GetItem("tissue"));
        }

        [Test]
        public void WithoutDeliveryNothingIsSoldAutomatically()
        {
            state.TrashValue = 100;
            automation.Tick(60);
            Assert.AreEqual(0, state.Money);
        }

        [Test]
        public void OfflineProgressIsCappedAndReduced()
        {
            SetLevel("robot", 1);
            SetLevel("delivery", 1);

            var report = automation.ApplyOffline(24 * 3600);

            Assert.AreEqual(data.OfflineHoursCap * 3600, report.Seconds);
            var expected = (long)(data.OfflineHoursCap * 3600 * data.OfflineEfficiency * upgrades.AutoCollectPerSecond * data.AverageTrashValue());
            Assert.AreEqual(expected, report.TrashCollected);
            Assert.AreEqual(expected, report.MoneyEarned);
        }

        [Test]
        public void OfflineSecondsIgnoresMissingOrFutureTimestamps()
        {
            Assert.AreEqual(0, Automation.OfflineSeconds(0, 1000));
            Assert.AreEqual(0, Automation.OfflineSeconds(2000, 1000));
            Assert.AreEqual(500, Automation.OfflineSeconds(500, 1000));
        }

        [Test]
        public void SellEverythingSellsTrashAndGoods()
        {
            state.TrashValue = 10;
            state.AddItem("detergent", 1);
            state.AddItem("voucher", 1);

            Assert.AreEqual(40, economy.SellEverything());
            Assert.AreEqual(1, state.GetItem("voucher"));
        }

        [TestCase(0, "0")]
        [TestCase(9999, "9,999")]
        [TestCase(10000, "1.00万")]
        [TestCase(12345, "1.23万")]
        [TestCase(123456, "12.3万")]
        [TestCase(1234567, "123万")]
        [TestCase(12345678, "1,234万")]
        [TestCase(199999999, "1.99億")]
        [TestCase(1000000000000, "1.00兆")]
        [TestCase(-12345, "-1.23万")]
        public void JapaneseNumbersUseMyriadUnitsAndRoundDown(long value, string expected)
        {
            Assert.AreEqual(expected, NumberFormat.Japanese(value));
        }

        [Test]
        public void NextGoalIsCheapestUpgradeWithShortfall()
        {
            state.Money = 100;

            var goal = progression.NextGoal();

            Assert.AreEqual("shoes", goal.Upgrade.Id);
            Assert.AreEqual(200, goal.Cost);
            Assert.AreEqual(100, goal.MoneyShort);
            Assert.IsFalse(goal.Affordable);
        }

        [Test]
        public void NextGoalReportsMissingItems()
        {
            state.Money = 1000;
            SetLevel("shoes", 10);

            var goal = progression.NextGoal();

            Assert.AreEqual("tongs", goal.Upgrade.Id);
            Assert.AreEqual("tool_parts", goal.MissingItem.Id);
            Assert.AreEqual(1, goal.MissingItemCount);
        }

        [Test]
        public void NextGoalIsNullWhenEverythingIsMaxed()
        {
            foreach (var upgrade in data.Upgrades)
            {
                SetLevel(upgrade.Id, upgrade.MaxLevel);
            }

            Assert.IsNull(progression.NextGoal());
        }

        [Test]
        public void DrawMilestonesAreReportedOnce()
        {
            CollectionAssert.AreEqual(new long[] { 10, 100 }, Progression.DrawMilestonesCrossed(5, 150));
            CollectionAssert.AreEqual(new long[] { 1000, 2000, 5000, 10000 }, Progression.DrawMilestonesCrossed(999, 10000));
            CollectionAssert.IsEmpty(Progression.DrawMilestonesCrossed(10, 99));
        }

        [Test]
        public void EveryUpgradeHasAnEffectText()
        {
            foreach (var upgrade in data.Upgrades)
            {
                Assert.IsNotEmpty(UpgradeText.Effect(upgrade), upgrade.Id);
            }

            Assert.AreEqual("歩く速さ +25%", UpgradeText.Effect(data.FindUpgrade("shoes")));
        }

        sealed class SequenceRandom : IRandomSource
        {
            readonly int[] values;
            int index;
            public SequenceRandom(params int[] values) => this.values = values;
            public int Next(int maxExclusive) => values[index++ % values.Length];
        }
    }
}
