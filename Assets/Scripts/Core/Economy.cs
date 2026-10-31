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

        // Lucky trash can start a frenzy that multiplies every piece collected for a while.
        public double FrenzyMultiplier => state.FrenzySecondsLeft > 0 ? data.FrenzyMultiplier : 1;

        // Everything that multiplies trash at the recycle station: bags, stamps and earlier clears.
        public double SaleMultiplier =>
            (1 + upgrades.GetValue(UpgradeStat.TrashValue)) *
            (1 + state.Stamps.Count * data.StampBonus) *
            (1 + state.Clears * data.ClearBonus);

        // Returns the value added to the trash bag.
        public long CollectTrash(TrashDef trash, double comboMultiplier = 1)
        {
            var value = (long)Math.Round(trash.Value * comboMultiplier * FrenzyMultiplier);
            state.TrashValue += value;
            return value;
        }

        public long SellTrash()
        {
            if (state.TrashValue == 0)
            {
                return 0;
            }

            var earned = (long)Math.Round(state.TrashValue * SaleMultiplier);
            state.AddEarnings(earned);
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

            state.AddEarnings(earned);
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

        public bool IsOnSale(ProductDef product) => state.SaleSecondsLeft > 0 && state.SaleProductId == product.Id;

        // Upgrades on sale are discounted; groceries on sale keep their price but give extra tickets.
        public long GetPrice(ProductDef product)
        {
            if (string.IsNullOrEmpty(product.UpgradeId))
            {
                return product.Price;
            }

            var cost = upgrades.GetCost(data.FindUpgrade(product.UpgradeId));
            return IsOnSale(product) ? (long)Math.Round(cost * (1 - data.SaleDiscount)) : cost;
        }

        public long Total(ShoppingCart cart)
        {
            var total = 0L;
            foreach (var line in cart.Lines)
            {
                total += GetPrice(data.FindProduct(line.ProductId)) * line.Quantity;
            }

            return total;
        }

        // How many more of a product the player could pay for, with cash and vouchers, on top of the cart.
        public long MaxAffordable(ShoppingCart cart, string productId)
        {
            var price = GetPrice(data.FindProduct(productId));
            if (price <= 0)
            {
                return 0;
            }

            return Math.Max(0, (state.Money + VoucherBalance() - Total(cart)) / price);
        }

        public long VoucherBalance()
        {
            var balance = 0L;
            foreach (var item in data.Items)
            {
                balance += state.GetItem(item.Id) * item.VoucherValue;
            }

            return balance;
        }

        // Whether productId can join the cart: an upgrade may appear only once, must not be maxed,
        // and every upgrade in the cart together must not need more items than the player owns.
        public bool CanAddToCart(ShoppingCart cart, string productId) => TryGetUpgrades(cart.With(productId), out _);

        public bool CanAddToCart(IReadOnlyList<string> cart, string productId) => CanAddToCart(ShoppingCart.Of(cart), productId);

        public CheckoutResult Checkout(IReadOnlyList<string> productIds) => Checkout(ShoppingCart.Of(productIds));

        public CheckoutResult Checkout(ShoppingCart cart)
        {
            if (cart.IsEmpty || !TryGetUpgrades(cart, out var upgradesInCart))
            {
                return default;
            }

            // Prices and sales are read before the upgrades level up and change them.
            var total = Total(cart);
            var ticketYen = TicketYen(cart);
            var voucherPaid = PayableByVouchers(total);
            if (total - voucherPaid > state.Money)
            {
                return default;
            }

            foreach (var line in cart.Lines)
            {
                if (IsOnSale(data.FindProduct(line.ProductId)))
                {
                    state.SaleBuys++;
                    break;
                }
            }

            state.Money -= total - voucherPaid;
            SpendVouchers(voucherPaid);
            foreach (var upgrade in upgradesInCart)
            {
                upgrades.Apply(upgrade);
            }

            return new CheckoutResult(true, total, voucherPaid, GrantTickets(ticketYen));
        }

        // Spends money straight on tickets, as the online-shopping perk does. Returns the tickets granted.
        public long BuyTickets(long yen)
        {
            yen = Math.Min(yen, state.Money);
            if (yen <= 0)
            {
                return 0;
            }

            state.Money -= yen;
            return GrantTickets(yen);
        }

        long TicketYen(ShoppingCart cart)
        {
            var yen = 0L;
            foreach (var line in cart.Lines)
            {
                var product = data.FindProduct(line.ProductId);
                var spent = GetPrice(product) * line.Quantity;
                var grocerySale = string.IsNullOrEmpty(product.UpgradeId) && IsOnSale(product);
                yen += grocerySale ? (long)Math.Round(spent * data.SaleTicketMultiplier) : spent;
            }

            return yen;
        }

        bool TryGetUpgrades(ShoppingCart cart, out List<UpgradeDef> upgradesInCart)
        {
            upgradesInCart = new List<UpgradeDef>();
            var itemCosts = new Dictionary<string, long>();
            foreach (var line in cart.Lines)
            {
                var product = data.FindProduct(line.ProductId);
                if (string.IsNullOrEmpty(product.UpgradeId))
                {
                    continue;
                }

                var upgrade = data.FindUpgrade(product.UpgradeId);
                if (line.Quantity > 1 || upgradesInCart.Contains(upgrade) || upgrades.IsMaxed(upgrade))
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

        long GrantTickets(long yen)
        {
            var earned = state.SpendRemainder + (long)Math.Round(yen * upgrades.TicketMultiplier);
            var tickets = earned / data.MoneyPerTicket;
            state.SpendRemainder = earned % data.MoneyPerTicket;
            state.Tickets += tickets;
            return tickets;
        }
    }
}
