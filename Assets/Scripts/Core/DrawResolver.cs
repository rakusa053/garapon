using System;

namespace Garapon.Core
{
    public interface IRandomSource
    {
        // Returns an integer in the range [0, maxExclusive).
        int Next(int maxExclusive);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        readonly Random random;

        public SystemRandomSource() => random = new Random();
        public SystemRandomSource(int seed) => random = new Random(seed);

        public int Next(int maxExclusive) => random.Next(maxExclusive);
    }

    public sealed class DrawResolver
    {
        readonly GameData data;
        readonly IRandomSource randomSource;

        public DrawResolver(GameData data, IRandomSource randomSource)
        {
            var total = 0;
            foreach (var tier in data.PrizeTiers)
            {
                total += tier.PartsPerMillion;
            }

            if (total != GameData.PartsPerMillionTotal)
            {
                throw new ArgumentException($"Prize tiers must add up to {GameData.PartsPerMillionTotal}, but add up to {total}.");
            }

            this.data = data;
            this.randomSource = randomSource;
        }

        public PrizeTierDef Resolve()
        {
            var roll = randomSource.Next(GameData.PartsPerMillionTotal);
            foreach (var tier in data.PrizeTiers)
            {
                roll -= tier.PartsPerMillion;
                if (roll < 0)
                {
                    return tier;
                }
            }

            throw new InvalidOperationException("Roll fell outside the prize tiers.");
        }
    }
}
