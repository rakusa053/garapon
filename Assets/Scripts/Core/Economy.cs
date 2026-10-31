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

        public long GetPrice(ProductDef product)
        {
            return string.IsNullOrEmpty(product.UpgradeId)
                ? product.Price
                : upgrades.GetCost(data.FindUpgrade(product.UpgradeId));
        }

        public CheckoutResult Checkout(IReadOnlyList<string> productIds)
        {
            var upgradesInCart = new List<UpgradeDef>();
            var total = 0L;
            foreach (var productId in productIds)
            {
                var product = data.FindProduct(productId);
                total += GetPrice(product);
                if (string.IsNullOrEmpty(product.UpgradeId))
                {
                    continue;
                }

                var upgrade = data.FindUpgrade(product.UpgradeId);
                if (upgradesInCart.Contains(upgrade) || upgrades.IsMaxed(upgrade) || !upgrades.HasItemCost(upgrade))
                {
                    return default;
                }

                upgradesInCart.Add(upgrade);
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
