using Garapon.Core;

namespace Garapon.Game
{
    public sealed class RecycleStation : Interactable
    {
        public override string Prompt => "ごみと日用品を換金する";

        public override void Interact()
        {
            var session = GameSession.Instance;
            var earned = session.Economy.SellEverything();
            session.Post(earned > 0 ? $"{NumberFormat.Yen(earned)}で換金しました" : "換金できるものがありません");
        }
    }
}
