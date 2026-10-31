using System.Collections.Generic;
using Garapon.Core;

namespace Garapon.Game
{
    public sealed class CashRegister : Interactable
    {
        public override string Prompt
        {
            get
            {
                var session = GameSession.Instance;
                return session.Cart.IsEmpty ? "カゴが空です" : $"レジで支払う {NumberFormat.Yen(session.CartTotal())}";
            }
        }

        public override void Interact()
        {
            var session = GameSession.Instance;
            if (session.Cart.IsEmpty)
            {
                return;
            }

            var levelsBefore = new Dictionary<string, int>();
            foreach (var upgrade in session.Data.Upgrades)
            {
                levelsBefore[upgrade.Id] = session.State.GetUpgradeLevel(upgrade.Id);
            }

            var result = session.Economy.Checkout(session.Cart);
            session.Cart.Clear();
            session.Post(result.Success
                ? $"お買い上げ {NumberFormat.Yen(result.Total)}。抽選券を{NumberFormat.Japanese(result.TicketsGranted)}枚もらいました"
                : "支払えないので、商品を棚に戻しました");
            if (result.Success)
            {
                session.AnnounceMilestones(levelsBefore);
            }
        }
    }
}
