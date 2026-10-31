using System.Collections.Generic;

namespace Garapon.Core
{
    // The outcome of one crank, which can draw many balls once the hopper is upgraded.
    public sealed class DrawBatchResult
    {
        readonly Dictionary<PrizeColor, long> counts = new Dictionary<PrizeColor, long>();

        public long Draws { get; private set; }

        // The rarest prize in the batch, which is the ball the machine shows.
        public PrizeTierDef Best { get; private set; }

        public long Count(PrizeColor color) => counts.TryGetValue(color, out var count) ? count : 0;

        internal void Add(PrizeTierDef tier)
        {
            Draws++;
            counts[tier.Color] = Count(tier.Color) + 1;
            if (Best == null || tier.PartsPerMillion < Best.PartsPerMillion)
            {
                Best = tier;
            }
        }
    }

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

        // Draws left until gold is guaranteed, or -1 when there is no guarantee.
        public long DrawsUntilGuarantee
        {
            get
            {
                var guarantee = Prestige.GuaranteedGoldDraws(resolver.Data, state);
                return guarantee > 0 ? System.Math.Max(0, guarantee - state.TotalDraws) : -1;
            }
        }

        // Consumes one ticket and grants the prize. Returns null when no draw can be made.
        public PrizeTierDef Draw()
        {
            if (!CanDraw)
            {
                return null;
            }

            state.Tickets--;
            state.TotalDraws++;

            var tier = DrawsUntilGuarantee == 0 ? resolver.Find(PrizeColor.Gold) : resolver.Resolve();
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

        // Draws up to maxDraws balls, stopping early when tickets run out or gold comes up.
        // Returns null when no draw can be made.
        public DrawBatchResult DrawBatch(long maxDraws)
        {
            if (!CanDraw || maxDraws <= 0)
            {
                return null;
            }

            var result = new DrawBatchResult();
            for (var i = 0L; i < maxDraws && CanDraw; i++)
            {
                result.Add(Draw());
            }

            return result;
        }
    }
}
