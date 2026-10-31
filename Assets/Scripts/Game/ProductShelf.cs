using Garapon.Core;
using TMPro;
using UnityEngine;

namespace Garapon.Game
{
    public sealed class ProductShelf : Interactable
    {
        [SerializeField] string productId;
        [SerializeField] TMP_Text label;

        ProductDef Product => GameSession.Instance.Data.FindProduct(productId);
        UpgradeDef Upgrade => string.IsNullOrEmpty(Product.UpgradeId) ? null : GameSession.Instance.Data.FindUpgrade(Product.UpgradeId);

        public override string Prompt
        {
            get
            {
                var session = GameSession.Instance;
                var price = session.Economy.GetPrice(Product);
                var upgrade = Upgrade;
                if (upgrade != null)
                {
                    var level = session.State.GetUpgradeLevel(upgrade.Id);
                    var milestone = session.Upgrades.NextMilestone(upgrade);
                    var milestoneText = milestone > 0 ? $" / Lv{milestone}で効果×{upgrade.MilestoneMultiplier}" : string.Empty;
                    return $"{Product.DisplayName} Lv{level}→{level + 1} {NumberFormat.Yen(price)}({UpgradeText.Effect(upgrade)}{milestoneText}) をカゴへ";
                }

                var quantity = session.BulkQuantity(productId);
                var tickets = price * quantity / session.Data.MoneyPerTicket;
                if (session.Economy.IsOnSale(Product))
                {
                    tickets = (long)(tickets * session.Data.SaleTicketMultiplier);
                }

                return $"{Product.DisplayName} ×{NumberFormat.Japanese(quantity)} {NumberFormat.Yen(price * quantity)}(抽選券 約{NumberFormat.Japanese(tickets)}枚分) をカゴへ  [Q] {session.BuyModeName}";
            }
        }

        // Shelves are stocked as the game goes on; until then they tease what is coming.
        public override bool IsAvailable => GameSession.Instance.Progression.IsRevealed(Product);

        void Update()
        {
            var session = GameSession.Instance;
            if (!IsAvailable)
            {
                label.text = "？？？\n入荷待ち";
                return;
            }

            var upgrade = Upgrade;
            var sale = session.Economy.IsOnSale(Product) ? "<color=#E03030>特売!</color> " : string.Empty;
            if (upgrade == null)
            {
                label.text = $"{sale}{Product.DisplayName}\n{NumberFormat.Yen(session.Economy.GetPrice(Product))}";
                return;
            }

            var level = session.State.GetUpgradeLevel(upgrade.Id);
            label.text = session.Upgrades.IsMaxed(upgrade)
                ? $"{Product.DisplayName}\n売り切れ"
                : $"{sale}{Product.DisplayName} Lv{level}\n{NumberFormat.Yen(session.Economy.GetPrice(Product))}";
        }

        public override void Interact()
        {
            var session = GameSession.Instance;
            var upgrade = Upgrade;
            if (upgrade != null)
            {
                if (session.Upgrades.IsMaxed(upgrade) || session.Cart.Contains(productId))
                {
                    session.Post("これ以上は買えません");
                    return;
                }

                if (!session.Upgrades.HasItemCost(upgrade))
                {
                    var item = session.Data.FindItem(upgrade.ItemCostId);
                    session.Post($"{item.DisplayName}が{upgrade.ItemCostAmount}個必要です");
                    return;
                }

                if (!session.Economy.CanAddToCart(session.Cart, productId))
                {
                    var item = session.Data.FindItem(upgrade.ItemCostId);
                    session.Post($"カゴの商品と合わせると{item.DisplayName}が足りません");
                    return;
                }

                session.Cart.Add(productId);
                session.Post($"{Product.DisplayName}をカゴに入れました");
                return;
            }

            var quantity = session.BulkQuantity(productId);
            session.Cart.Add(productId, quantity);
            session.Post($"{Product.DisplayName}を{NumberFormat.Japanese(quantity)}個カゴに入れました");
        }
    }
}
