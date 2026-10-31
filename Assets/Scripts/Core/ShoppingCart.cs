using System.Collections.Generic;

namespace Garapon.Core
{
    public sealed class CartLine
    {
        public string ProductId;
        public long Quantity;
    }

    // The basket carried around the supermarket. Lines hold quantities so that bulk buying
    // ("buy max" in AdVenture Capitalist) can put thousands of loaves in at once.
    public sealed class ShoppingCart
    {
        readonly List<CartLine> lines = new List<CartLine>();

        public IReadOnlyList<CartLine> Lines => lines;

        public bool IsEmpty => lines.Count == 0;

        public long Count
        {
            get
            {
                var count = 0L;
                foreach (var line in lines)
                {
                    count += line.Quantity;
                }

                return count;
            }
        }

        public static ShoppingCart Of(IEnumerable<string> productIds)
        {
            var cart = new ShoppingCart();
            foreach (var productId in productIds)
            {
                cart.Add(productId);
            }

            return cart;
        }

        public long QuantityOf(string productId) => Find(productId)?.Quantity ?? 0;

        public bool Contains(string productId) => QuantityOf(productId) > 0;

        public void Add(string productId, long quantity = 1)
        {
            if (quantity <= 0)
            {
                return;
            }

            var line = Find(productId);
            if (line == null)
            {
                lines.Add(new CartLine { ProductId = productId, Quantity = quantity });
            }
            else
            {
                line.Quantity += quantity;
            }
        }

        public ShoppingCart With(string productId, long quantity = 1)
        {
            var copy = new ShoppingCart();
            foreach (var line in lines)
            {
                copy.Add(line.ProductId, line.Quantity);
            }

            copy.Add(productId, quantity);
            return copy;
        }

        public void Clear() => lines.Clear();

        CartLine Find(string productId) => lines.Find(line => line.ProductId == productId);
    }
}
