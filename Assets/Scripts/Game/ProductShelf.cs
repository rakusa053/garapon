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

        public override string Prompt => $"{Product.DisplayName} {GameSession.Instance.Economy.GetPrice(Product):N0}円 をカゴへ";

        void Update()
        {
            var session = GameSession.Instance;
            var upgrade = Upgrade;
            label.text = upgrade != null && session.Upgrades.IsMaxed(upgrade)
                ? $"{Product.DisplayName}\n売り切れ"
                : $"{Product.DisplayName}\n{session.Economy.GetPrice(Product):N0}円";
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
            }

            session.Cart.Add(productId);
            session.Post($"{Product.DisplayName}をカゴに入れました");
        }
    }
}
