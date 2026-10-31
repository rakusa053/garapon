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
                return session.Cart.Count == 0 ? "カゴが空です" : $"レジで支払う {NumberFormat.Yen(session.CartTotal())}";
            }
        }

        public override void Interact()
        {
            var session = GameSession.Instance;
            if (session.Cart.Count == 0)
            {
                return;
            }

            var result = session.Economy.Checkout(session.Cart);
            session.Cart.Clear();
            session.Post(result.Success
                ? $"お買い上げ {NumberFormat.Yen(result.Total)}。抽選券を{NumberFormat.Japanese(result.TicketsGranted)}枚もらいました"
                : "支払えないので、商品を棚に戻しました");
        }
    }
}
