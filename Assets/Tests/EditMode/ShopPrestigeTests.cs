using System.Linq;
using Garapon.Core;
using NUnit.Framework;

namespace Garapon.Tests
{
    public class ShopPrestigeTests
    {
        sealed class FixedRandom : IRandomSource
        {
            readonly int value;
            public FixedRandom(int value) => this.value = value;
            public int Next(int maxExclusive) => System.Math.Min(value, maxExclusive - 1);
        }

        GameData data;
        GameState state;
        UpgradeSystem upgrades;
        Economy economy;
        Progression progression;

        [SetUp]
        public void SetUp()
        {
            data = GameData.CreateDefault();
            state = new GameState();
            upgrades = new UpgradeSystem(data, state);
            economy = new Economy(data, state, upgrades);
            progression = new Progression(data, state, upgrades);
        }

        void SetLevel(string upgradeId, int level)
        {
            while (state.GetUpgradeLevel(upgradeId) < level)
            {
                state.AddUpgradeLevel(upgradeId);
            }
        }

        [Test]
        public void BulkCartChecksOutEveryUnit()
        {
            state.Money = 1000;
            var cart = new ShoppingCart();
            cart.Add("bread", economy.MaxAffordable(cart, "bread"));

            Assert.AreEqual(1000 / 150, cart.Count);
            Assert.AreEqual(0, economy.MaxAffordable(cart, "bread"));

            var result = economy.Checkout(cart);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(150 * 6, result.Total);
            Assert.AreEqual(100, state.Money);
            Assert.AreEqual(9, state.Tickets);
        }

        [Test]
        public void MaxAffordableCountsVouchers()
        {
            state.Money = 100;
            state.AddItem("voucher", 2);

            Assert.AreEqual(2, economy.MaxAffordable(new ShoppingCart(), "bread"));
        }

        [Test]
        public void UpgradeCannotBeBulkBought()
        {
            state.Money = 1000000;
            var cart = new ShoppingCart();
            cart.Add("upgrade_shoes");

            Assert.IsFalse(economy.CanAddToCart(cart, "upgrade_shoes"));
            cart.Add("upgrade_shoes");
            Assert.IsFalse(economy.Checkout(cart).Success);
        }

        [Test]
        public void SaleDiscountsUpgradesAndDoublesGroceryTickets()
        {
            var shoes = data.FindProduct("upgrade_shoes");
            var bread = data.FindProduct("bread");
            var fullPrice = economy.GetPrice(shoes);

            state.SaleProductId = shoes.Id;
            state.SaleSecondsLeft = 10;
            Assert.AreEqual((long)System.Math.Round(fullPrice * (1 - data.SaleDiscount)), economy.GetPrice(shoes));

            state.SaleProductId = bread.Id;
            state.Money = 1000;
            var result = economy.Checkout(new[] { "bread", "bread" });
            Assert.AreEqual(300, result.Total);
            Assert.AreEqual(6, result.TicketsGranted);
            Assert.AreEqual(1, state.SaleBuys);
        }

        [Test]
        public void SaleStartsAfterIntervalOnAStockedProductAndEnds()
        {
            var sale = new BargainSale(data, state, upgrades, progression);
            var random = new FixedRandom(0);

            Assert.IsNull(sale.Tick(data.SaleIntervalMin - 1, random));
            var started = sale.Tick(1, random);

            Assert.IsNotNull(started);
            Assert.IsTrue(progression.IsRevealed(started));
            Assert.AreSame(started, sale.Current);
            Assert.IsTrue(economy.IsOnSale(started));

            sale.Tick(data.SaleSeconds, random);
            Assert.IsNull(sale.Current);
            Assert.IsFalse(economy.IsOnSale(started));
        }

        [Test]
        public void MilestonesMultiplyTheWholeEffect()
        {
            var bag = data.FindUpgrade("bag");
            SetLevel("bag", 9);
            Assert.AreEqual(9 * bag.ValuePerLevel, upgrades.GetValue(UpgradeStat.TrashValue), 1e-9);
            Assert.AreEqual(10, upgrades.NextMilestone(bag));
            Assert.IsFalse(upgrades.ReachedMilestone);

            SetLevel("bag", 10);
            Assert.AreEqual(10 * bag.ValuePerLevel * bag.MilestoneMultiplier, upgrades.GetValue(UpgradeStat.TrashValue), 1e-9);
            Assert.AreEqual(20, upgrades.NextMilestone(bag));
            Assert.IsTrue(upgrades.ReachedMilestone);

            SetLevel("bag", bag.MaxLevel);
            Assert.AreEqual(0, upgrades.NextMilestone(bag));
        }

        [Test]
        public void SynergyUpgradeWaitsForItsRequirementAndBoostsTheRobot()
        {
            var truck = data.FindUpgrade("truck");
            state.TotalEarned = truck.BaseCost;
            state.AddDrawCount(PrizeColor.Silver);
            SetLevel("robot", truck.RequiresLevel - 1);
            Assert.IsFalse(progression.IsRevealed(truck));
            StringAssert.Contains("Lv" + truck.RequiresLevel, progression.RevealHint(truck));

            SetLevel("robot", truck.RequiresLevel);
            Assert.IsTrue(progression.IsRevealed(truck));

            SetLevel("bag", 5);
            var before = upgrades.AutoCollectPerSecond;
            state.AddUpgradeLevel("truck");
            Assert.AreEqual(before * (1 + 5 * truck.ValuePerLevel), upgrades.AutoCollectPerSecond, 1e-9);
        }

        [Test]
        public void NameTagAddsTicketsPerStamp()
        {
            state.Stamps.AddRange(new[] { "a", "b", "c", "d", "e" });
            var before = upgrades.TicketMultiplier;
            state.AddUpgradeLevel("nametag");
            Assert.AreEqual(before * (1 + 5 * data.FindUpgrade("nametag").ValuePerLevel), upgrades.TicketMultiplier, 1e-9);
        }

        [Test]
        public void SouvenirsFollowTheSquareRootOfLifetimeEarnings()
        {
            Assert.AreEqual(0, Prestige.SouvenirsFor(data, 0));
            Assert.AreEqual(1, Prestige.SouvenirsFor(data, (long)data.SouvenirUnit));
            Assert.AreEqual(10, Prestige.SouvenirsFor(data, (long)(100 * data.SouvenirUnit)));
            Assert.AreEqual(9, Prestige.SouvenirsFor(data, (long)(100 * data.SouvenirUnit) - 1));

            state.LifetimeEarned = (long)(100 * data.SouvenirUnit) - 1;
            Assert.AreEqual(1, Prestige.EarningsForNext(data, state));
        }

        [Test]
        public void NextRunGrantsOnlyNewSouvenirsAndAppliesStartPerks()
        {
            state.AddEarnings((long)(100 * data.SouvenirUnit));
            state.SouvenirsEarned = 4;
            state.Souvenirs = 1;
            state.AddPerkLevel("manju");
            state.AddPerkLevel("my_bag");
            state.Cleared = true;

            var next = Prestige.NextRun(data, state);

            Assert.AreEqual(10, next.SouvenirsEarned);
            Assert.AreEqual(7, next.Souvenirs);
            Assert.AreEqual(state.LifetimeEarned, next.LifetimeEarned);
            Assert.AreEqual(0, next.TotalEarned);
            Assert.AreEqual((long)data.FindPerk("manju").ValuePerLevel, next.Money);
            Assert.AreEqual((int)data.FindPerk("my_bag").ValuePerLevel, next.GetUpgradeLevel("bag"));
            Assert.AreEqual(1, next.GetPerkLevel("manju"));

            // Changing the new run must not touch the old one.
            next.AddPerkLevel("charm");
            Assert.AreEqual(0, state.GetPerkLevel("charm"));
        }

        [Test]
        public void PerksCostSouvenirsAndStopAtMax()
        {
            var perk = data.FindPerk("net_super");
            state.Souvenirs = perk.BaseCost - 1;
            Assert.IsFalse(Prestige.Buy(state, perk));

            state.Souvenirs = perk.BaseCost * 10;
            Assert.IsTrue(Prestige.Buy(state, perk));
            Assert.AreEqual(perk.BaseCost * 9, state.Souvenirs);
            Assert.IsTrue(Prestige.IsMaxed(state, perk));
            Assert.IsFalse(Prestige.Buy(state, perk));
            Assert.IsTrue(Prestige.HasAutoShop(data, state));
        }

        [Test]
        public void GoldenGaraponShortensTheGuarantee()
        {
            var lottery = new Lottery(state, new DrawResolver(data, new FixedRandom(999999)));
            Assert.AreEqual(data.GuaranteedGoldDraws, lottery.DrawsUntilGuarantee);

            state.AddPerkLevel("golden");
            state.AddPerkLevel("golden");
            var reduction = 2 * data.FindPerk("golden").ValuePerLevel;
            Assert.AreEqual((long)System.Math.Round(data.GuaranteedGoldDraws * (1 - reduction)), lottery.DrawsUntilGuarantee);
        }

        [Test]
        public void CrankPerkStacksWithGrease()
        {
            state.AddUpgradeLevel("grease");
            var grease = 1 + data.FindUpgrade("grease").ValuePerLevel;
            Assert.AreEqual(grease, upgrades.CrankSpeedMultiplier, 1e-9);

            state.AddPerkLevel("master");
            Assert.AreEqual(grease * (1 + data.FindPerk("master").ValuePerLevel), upgrades.CrankSpeedMultiplier, 1e-9);
        }

        [Test]
        public void OnlineShoppingSpendsPartOfDeliverySalesOnTickets()
        {
            var automation = new Automation(data, state, upgrades, economy);
            state.AddUpgradeLevel("delivery");
            state.AddPerkLevel("net_super");
            state.TrashValue = 10000;

            var report = automation.Tick(Automation.DeliveryInterval);

            Assert.Greater(report.MoneyEarned, 0);
            var spent = (long)(report.MoneyEarned * data.AutoShopShare);
            Assert.AreEqual(report.MoneyEarned - spent, state.Money);
            Assert.AreEqual(spent / data.MoneyPerTicket, report.TicketsBought);
            Assert.AreEqual(report.TicketsBought, state.Tickets);
        }

        [Test]
        public void SaleAndPerkStampsAreOnTheCard()
        {
            var ids = StampCard.All.Select(stamp => stamp.Id).ToList();
            CollectionAssert.IsSubsetOf(new[] { "sale", "milestone", "perk" }, ids);
            CollectionAssert.AllItemsAreUnique(ids);
        }
    }
}
