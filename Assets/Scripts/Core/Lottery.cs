namespace Garapon.Core
{
    public sealed class Lottery
    {
        readonly GameState state;
        readonly DrawResolver resolver;

        public Lottery(GameState state, DrawResolver resolver)
        {
            this.state = state;
            this.resolver = resolver;
        }

        public bool CanDraw => state.Tickets > 0 && !state.Cleared;

        // Consumes one ticket and grants the prize. Returns null when no draw can be made.
        public PrizeTierDef Draw()
        {
            if (!CanDraw)
            {
                return null;
            }

            state.Tickets--;
            state.TotalDraws++;

            var tier = resolver.Resolve();
            state.AddDrawCount(tier.Color);
            if (tier.Color == PrizeColor.Gold)
            {
                state.Cleared = true;
            }
            else if (!string.IsNullOrEmpty(tier.ItemId))
            {
                state.AddItem(tier.ItemId, tier.ItemAmount);
            }

            return tier;
        }
    }
}
