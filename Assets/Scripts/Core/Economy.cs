using System;
using System.Collections.Generic;

namespace Garapon.Core
{
    public readonly struct CheckoutResult
    {
        public readonly bool Success;
        public readonly long Total;
        public readonly long VoucherPaid;
        public readonly long TicketsGranted;

        public CheckoutResult(bool success, long total, long voucherPaid, long ticketsGranted)
        {
            Success = success;
            Total = total;
            VoucherPaid = voucherPaid;
            TicketsGranted = ticketsGranted;
        }
    }

    public sealed class Economy
    {
        readonly GameData data;
        readonly GameState state;
        readonly UpgradeSystem upgrades;

        public Economy(GameData data, GameState state, UpgradeSystem upgrades)
        {
            this.data = data;
            this.state = state;
            this.upgrades = upgrades;
        }

        public void CollectTrash(TrashDef trash)
        {
            state.TrashValue += trash.Value;
        }

        public long SellTrash()
        {
            if (state.TrashValue == 0)
            {
                return 0;
            }

            var multiplier = 1 + upgrades.GetValue(UpgradeStat.TrashValue);
            var earned = (long)Math.Round(state.TrashValue * multiplier);
            state.Money += earned;
            state.TrashValue = 0;
            return earned;
        }

        public long SellItems(string itemId)
        {
            var item = data.FindItem(itemId);
            var earned = state.GetItem(itemId) * item.SellPrice;
            if (earned == 0)
            {
                return 0;
            }

            state.Money += earned;
            state.AddItem(itemId, -state.GetItem(itemId));
            return earned;
        }

        // Sells the collected trash and every daily good that has a resale price.
        public long SellEverything()
        {
            var earned = SellTrash();
            foreach (var item in data.Items)
            {
                if (item.SellPrice > 0)
                {
                    earned += SellItems(item.Id);
                }
            }

            return earned;
        }

        public long GetPrice(ProductDef product)
        {
            return string.IsNullOrEmpty(product.UpgradeId)
                ? product.Price
                : upgrades.GetCost(data.FindUpgrade(product.UpgradeId));
        }

        // Whether productId can join the cart: an upgrade may appear only once, must not be maxed,
        // and every upgrade in the cart together must not need more items than the player owns.
        public bool CanAddToCart(IReadOnlyList<string> cart, string productId)
        {
            var products = new List<string>(cart) { productId };
            return TryGetUpgrades(products, out _);
        }

        public CheckoutResult Checkout(IReadOnlyList<string> productIds)
        {
            if (!TryGetUpgrades(productIds, out var upgradesInCart))
            {
                return default;
            }

            var total = 0L;
            foreach (var productId in productIds)
            {
                total += GetPrice(data.FindProduct(productId));
            }

            var voucherPaid = PayableByVouchers(total);
            if (total - voucherPaid > state.Money)
            {
                return default;
            }

            state.Money -= total - voucherPaid;
            SpendVouchers(voucherPaid);
            foreach (var upgrade in upgradesInCart)
            {
                upgrades.Apply(upgrade);
            }

            return new CheckoutResult(true, total, voucherPaid, GrantTickets(total));
        }

        bool TryGetUpgrades(IReadOnlyList<string> productIds, out List<UpgradeDef> upgradesInCart)
        {
            upgradesInCart = new List<UpgradeDef>();
            var itemCosts = new Dictionary<string, long>();
            foreach (var productId in productIds)
            {
                var product = data.FindProduct(productId);
                if (string.IsNullOrEmpty(product.UpgradeId))
                {
                    continue;
                }

                var upgrade = data.FindUpgrade(product.UpgradeId);
                if (upgradesInCart.Contains(upgrade) || upgrades.IsMaxed(upgrade))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(upgrade.ItemCostId))
                {
                    itemCosts.TryGetValue(upgrade.ItemCostId, out var needed);
                    needed += upgrade.ItemCostAmount;
                    if (state.GetItem(upgrade.ItemCostId) < needed)
                    {
                        return false;
                    }

                    itemCosts[upgrade.ItemCostId] = needed;
                }

                upgradesInCart.Add(upgrade);
            }

            return true;
        }

        long PayableByVouchers(long total)
        {
            var paid = 0L;
            foreach (var item in data.Items)
            {
                if (item.VoucherValue > 0)
                {
                    paid += Math.Min(state.GetItem(item.Id), total / item.VoucherValue) * item.VoucherValue;
                }
            }

            return Math.Min(paid, total);
        }

        void SpendVouchers(long amount)
        {
            foreach (var item in data.Items)
            {
                if (item.VoucherValue > 0 && amount > 0)
                {
                    var used = Math.Min(state.GetItem(item.Id), amount / item.VoucherValue);
                    state.AddItem(item.Id, -used);
                    amount -= used * item.VoucherValue;
                }
            }
        }

        long GrantTickets(long total)
        {
            var rate = 1 + upgrades.GetValue(UpgradeStat.TicketRate);
            var earned = state.SpendRemainder + (long)Math.Round(total * rate);
            var tickets = earned / data.MoneyPerTicket;
            state.SpendRemainder = earned % data.MoneyPerTicket;
            state.Tickets += tickets;
            return tickets;
        }
    }
}
