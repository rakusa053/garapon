namespace Garapon.Game
{
    public sealed class RecycleStation : Interactable
    {
        public override string Prompt => "ごみと日用品を換金する";

        public override void Interact()
        {
            var session = GameSession.Instance;
            var earned = session.Economy.SellTrash();
            foreach (var item in session.Data.Items)
            {
                if (item.SellPrice > 0)
                {
                    earned += session.Economy.SellItems(item.Id);
                }
            }

            session.Post(earned > 0 ? $"{earned:N0}円で換金しました" : "換金できるものがありません");
        }
    }
}
