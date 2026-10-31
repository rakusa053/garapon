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
                var price = GameSession.Instance.Economy.GetPrice(Product);
                var upgrade = Upgrade;
                var detail = upgrade != null
                    ? $"({UpgradeText.Effect(upgrade)})"
                    : $"(抽選券 約{NumberFormat.Japanese(price / GameSession.Instance.Data.MoneyPerTicket)}枚分)";
                return $"{Product.DisplayName} {NumberFormat.Yen(price)}{detail} をカゴへ";
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
            label.text = upgrade != null && session.Upgrades.IsMaxed(upgrade)
                ? $"{Product.DisplayName}\n売り切れ"
                : $"{Product.DisplayName}\n{NumberFormat.Yen(session.Economy.GetPrice(Product))}";
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
            }

            session.Cart.Add(productId);
            session.Post($"{Product.DisplayName}をカゴに入れました");
        }
    }
}
