using System;
using System.Collections.Generic;
using Garapon.Core;
using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Garapon.Tests
{
    public class GameLogicTests
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

        [SetUp]
        public void SetUp()
        {
            data = GameData.CreateDefault();
            state = new GameState();
            upgrades = new UpgradeSystem(data, state);
            economy = new Economy(data, state, upgrades);
        }

        [Test]
        public void DefaultPrizeTiersAddUpToOneMillion()
        {
            Assert.DoesNotThrow(() => new DrawResolver(data, new FixedRandom(0)));
        }

        [Test]
        public void ResolverRejectsTiersThatDoNotAddUp()
        {
            data.PrizeTiers[0].PartsPerMillion = 2;
            Assert.Throws<ArgumentException>(() => new DrawResolver(data, new FixedRandom(0)));
        }

        [TestCase(0, PrizeColor.Gold)]
        [TestCase(1, PrizeColor.Silver)]
        [TestCase(5000, PrizeColor.Silver)]
        [TestCase(5001, PrizeColor.Blue)]
        [TestCase(999999, PrizeColor.White)]
        public void ResolverMapsRollsToTierBoundaries(int roll, PrizeColor expected)
        {
            Assert.AreEqual(expected, new DrawResolver(data, new FixedRandom(roll)).Resolve().Color);
        }

        [Test]
        public void ResolverMatchesConfiguredProbabilities()
        {
            const int draws = 2000000;
            var resolver = new DrawResolver(data, new SystemRandomSource(12345));
            var counts = new Dictionary<PrizeColor, int>();
            for (var i = 0; i < draws; i++)
            {
                var color = resolver.Resolve().Color;
                counts.TryGetValue(color, out var count);
                counts[color] = count + 1;
            }

            foreach (var tier in data.PrizeTiers)
            {
                counts.TryGetValue(tier.Color, out var actual);
                var probability = tier.PartsPerMillion / 1000000.0;
                var expected = draws * probability;
                var fiveSigma = 5 * Math.Sqrt(draws * probability * (1 - probability));
                Assert.That(actual, Is.EqualTo(expected).Within(fiveSigma), tier.Color.ToString());
            }
        }

        [Test]
        public void GoldDrawClearsGameAndStopsFurtherDraws()
        {
            state.Tickets = 2;
            var lottery = new Lottery(state, new DrawResolver(data, new FixedRandom(0)));

            Assert.AreEqual(PrizeColor.Gold, lottery.Draw().Color);
            Assert.IsTrue(state.Cleared);
            Assert.IsNull(lottery.Draw());
            Assert.AreEqual(1, state.Tickets);
        }

        [Test]
        public void NormalDrawConsumesTicketAndGrantsItem()
        {
            state.Tickets = 1;
            var lottery = new Lottery(state, new DrawResolver(data, new FixedRandom(999999)));

            lottery.Draw();

            Assert.AreEqual(0, state.Tickets);
            Assert.AreEqual(1, state.GetItem("tissue"));
            Assert.AreEqual(1, state.GetDrawCount(PrizeColor.White));
            Assert.IsFalse(state.Cleared);
        }

        [Test]
        public void DrawWithoutTicketDoesNothing()
        {
            var lottery = new Lottery(state, new DrawResolver(data, new FixedRandom(0)));

            Assert.IsNull(lottery.Draw());
            Assert.AreEqual(0, state.TotalDraws);
        }

        [Test]
        public void SellingTrashAppliesUpgradeMultiplier()
        {
            state.AddUpgradeLevel("bag");
            economy.CollectTrash(data.Trash[0]);

            Assert.AreEqual(15, economy.SellTrash());
            Assert.AreEqual(15, state.Money);
            Assert.AreEqual(0, state.TrashValue);
        }

        [Test]
        public void SellingItemsEmptiesStock()
        {
            state.AddItem("tissue", 3);

            Assert.AreEqual(15, economy.SellItems("tissue"));
            Assert.AreEqual(0, state.GetItem("tissue"));
        }

        [Test]
        public void CheckoutGrantsTicketsAndCarriesRemainder()
        {
            state.Money = 1000;

            var first = economy.Checkout(new[] { "bread" });
            var second = economy.Checkout(new[] { "bread" });

            Assert.AreEqual(1, first.TicketsGranted);
            Assert.AreEqual(2, second.TicketsGranted);
            Assert.AreEqual(3, state.Tickets);
            Assert.AreEqual(700, state.Money);
        }

        [Test]
        public void CheckoutFailsWithoutEnoughMoneyAndChangesNothing()
        {
            state.Money = 100;

            Assert.IsFalse(economy.Checkout(new[] { "bread" }).Success);
            Assert.AreEqual(100, state.Money);
            Assert.AreEqual(0, state.Tickets);
        }

        [Test]
        public void VouchersPayBeforeMoney()
        {
            state.Money = 50;
            state.AddItem("voucher", 1);

            var result = economy.Checkout(new[] { "bread" });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(100, result.VoucherPaid);
            Assert.AreEqual(0, state.Money);
            Assert.AreEqual(0, state.GetItem("voucher"));
        }

        [Test]
        public void UpgradeProductIsPricedByLevelAndAppliesEffect()
        {
            state.Money = 1000;
            var product = data.FindProduct("upgrade_shoes");

            Assert.AreEqual(200, economy.GetPrice(product));
            Assert.IsTrue(economy.Checkout(new[] { product.Id }).Success);
            Assert.AreEqual(300, economy.GetPrice(product));
            Assert.AreEqual(0.25, upgrades.GetValue(UpgradeStat.MoveSpeed), 1e-9);
        }

        [Test]
        public void UpgradeRequiringItemIsRejectedWithoutItem()
        {
            state.Money = 100000;

            Assert.IsFalse(economy.Checkout(new[] { "upgrade_motor" }).Success);
            Assert.AreEqual(100000, state.Money);

            state.AddItem("tool_parts", 5);
            Assert.IsTrue(economy.Checkout(new[] { "upgrade_motor" }).Success);
            Assert.AreEqual(0, state.GetItem("tool_parts"));
        }

        [Test]
        public void UpgradesSharingAnItemCostNeedEnoughItemsForAll()
        {
            // Tongs need 1 tool part and the hopper needs 3, so 3 parts cannot pay for both.
            state.Money = 100000;
            state.AddItem("tool_parts", 3);

            Assert.IsFalse(economy.Checkout(new[] { "upgrade_tongs", "upgrade_hopper" }).Success);
            Assert.AreEqual(100000, state.Money);
            Assert.AreEqual(3, state.GetItem("tool_parts"));
            Assert.AreEqual(0, state.GetUpgradeLevel("tongs"));
            Assert.AreEqual(0, state.GetUpgradeLevel("hopper"));

            state.AddItem("tool_parts", 1);
            Assert.IsTrue(economy.Checkout(new[] { "upgrade_tongs", "upgrade_hopper" }).Success);
            Assert.AreEqual(0, state.GetItem("tool_parts"));
            Assert.AreEqual(1, state.GetUpgradeLevel("tongs"));
            Assert.AreEqual(1, state.GetUpgradeLevel("hopper"));
        }

        [Test]
        public void CanAddToCartChecksItemCostsAlreadyInCart()
        {
            state.AddItem("rare_parts", 3);

            // The robot needs 1 rare part and the delivery service needs 3.
            Assert.IsTrue(economy.CanAddToCart(new string[0], "upgrade_delivery"));
            Assert.IsFalse(economy.CanAddToCart(new[] { "upgrade_robot" }, "upgrade_delivery"));
            Assert.IsFalse(economy.CanAddToCart(new[] { "upgrade_delivery" }, "upgrade_delivery"));
            Assert.IsTrue(economy.CanAddToCart(new[] { "upgrade_robot" }, "bread"));
        }

        [Test]
        public void MaxedUpgradeCannotBePurchased()
        {
            state.Money = 1000000;
            state.AddItem("rare_parts", 10);

            Assert.IsTrue(economy.Checkout(new[] { "upgrade_delivery" }).Success);
            Assert.IsFalse(economy.Checkout(new[] { "upgrade_delivery" }).Success);
        }

#if UNITY_5_3_OR_NEWER
        // JsonUtility only exists inside Unity, so the standalone .NET test run skips this test.
        [Test]
        public void StateSurvivesJsonRoundTrip()
        {
            state.Money = 1234567890123L;
            state.Tickets = 42;
            state.Cleared = true;
            state.AddItem("tissue", 7);
            state.AddUpgradeLevel("shoes");
            state.AddDrawCount(PrizeColor.Red);

            var restored = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));

            Assert.AreEqual(state.Money, restored.Money);
            Assert.AreEqual(42, restored.Tickets);
            Assert.IsTrue(restored.Cleared);
            Assert.AreEqual(7, restored.GetItem("tissue"));
            Assert.AreEqual(1, restored.GetUpgradeLevel("shoes"));
            Assert.AreEqual(1, restored.GetDrawCount(PrizeColor.Red));
        }
#endif
    }
}
