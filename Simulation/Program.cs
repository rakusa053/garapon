using System;
using System.Collections.Generic;
using System.Linq;
using Garapon.Core;

namespace Garapon.Simulation
{
    // A rough model of a reasonable player on the Town map, used to compare pacing between
    // balance changes. The absolute times are estimates; the comparisons are what matter.
    sealed class PlayerModel
    {
        // Map and machine estimates taken from GaraponWorldBuilder and GaraponMachine.
        const double TrashDensity = 40.0 / (30 * 24);
        const double SweepEfficiency = 0.5;
        const double BaseSpeed = 6;
        const double BasePickupRadius = 1.5;
        const double LoopWalkMeters = 150;
        const double LoopInteractSeconds = 10;
        const double CrankSeconds = 3;
        const double CollectSecondsPerLoop = 45;
        const double TicketBudgetShare = 0.5;
        const double ManualCrankSecondsPerLoop = 120;
        const double LuckySpawnMin = 60;
        const double LuckySpawnMax = 180;
        const double LuckyCatchChance = 0.6;

        public readonly GameData Data;
        public readonly GameState State;
        public readonly UpgradeSystem Upgrades;
        public readonly Economy Economy;
        public readonly Automation Automation;
        public readonly Lottery Lottery;
        public readonly Progression Progression;
        readonly Combo combo;
        readonly LuckyTrash lucky;
        readonly Random random;
        readonly SystemRandomSource luckyRandom;
        readonly TrashDef averageTrash;
        readonly bool legacy;
        readonly bool noFun;
        double nextLucky;

        // Switches for measuring each system on its own.
        public static bool NoCombo, NoLucky, NoStamps, NoStocking;

        public double Time;
        public readonly Dictionary<string, double> FirstPurchase = new Dictionary<string, double>();
        public readonly SortedDictionary<long, double> DrawMilestones = new SortedDictionary<long, double>();

        public PlayerModel(GameData data, GameState state, int seed, bool legacy, bool noFun)
        {
            Data = data;
            State = state;
            this.legacy = legacy;
            this.noFun = noFun;
            Upgrades = new UpgradeSystem(data, State);
            Economy = new Economy(data, State, Upgrades);
            Automation = new Automation(data, State, Upgrades, Economy);
            Lottery = new Lottery(State, new DrawResolver(data, new SystemRandomSource(seed)));
            Progression = new Progression(data, State, Upgrades);
            combo = new Combo(data, State);
            lucky = new LuckyTrash(data, State, Upgrades, Economy);
            random = new Random(seed * 7919);
            luckyRandom = new SystemRandomSource(seed * 104729);
            averageTrash = new TrashDef { Id = "average", Value = data.AverageTrashValue() };
            nextLucky = NextLuckyDelay();
        }

        double NextLuckyDelay() => LuckySpawnMin + random.NextDouble() * (LuckySpawnMax - LuckySpawnMin);

        // In legacy mode the five automation upgrades can be bought but do nothing, as before.
        long DrawsPerCrank => legacy ? 1 : Upgrades.DrawsPerCrank;
        double CrankTime => CrankSeconds / (legacy ? 1 : Upgrades.CrankSpeedMultiplier);
        bool AutoCrank => !legacy && Upgrades.HasAutoCrank;

        public void Run(double maxSeconds)
        {
            while (!State.Cleared && Time < maxSeconds)
            {
                Loop();
            }
        }

        void Loop()
        {
            var speed = BaseSpeed * (1 + Upgrades.GetValue(UpgradeStat.MoveSpeed));
            var radius = BasePickupRadius + Upgrades.GetValue(UpgradeStat.PickupRadius);
            var piecesPerSecond = 2 * radius * speed * TrashDensity * SweepEfficiency;

            // Collect piece by piece so combos, frenzies and lucky trash play out over time.
            var step = 1 / piecesPerSecond;
            for (var t = 0.0; t < CollectSecondsPerLoop; t += step)
            {
                var multiplier = noFun || NoCombo ? 1 : combo.Register(Time + t);
                Economy.CollectTrash(averageTrash, multiplier);
                PassTime(step, true);
            }

            var walkSeconds = LoopWalkMeters / speed + LoopInteractSeconds;
            PassTime(walkSeconds, false);
            if (AutoCrank)
            {
                Crank(CollectSecondsPerLoop + walkSeconds);
            }

            Advance(CollectSecondsPerLoop + walkSeconds);
            Economy.SellEverything();
            Shop();

            if (!AutoCrank)
            {
                // A player cranks for a couple of minutes and then goes back to shopping,
                // rather than standing at the machine until thousands of tickets are gone.
                var seconds = Crank(ManualCrankSecondsPerLoop);
                PassTime(seconds, false);
                Advance(seconds);
            }

            if (!noFun && !NoStamps)
            {
                StampCard.Update(State, Upgrades);
            }
        }

        void PassTime(double seconds, bool inField)
        {
            if (!legacy)
            {
                Automation.Tick(seconds);
            }

            if (noFun || NoLucky)
            {
                return;
            }

            lucky.Tick(seconds);
            nextLucky -= seconds;
            if (nextLucky <= 0)
            {
                nextLucky = NextLuckyDelay();
                if (inField && random.NextDouble() < LuckyCatchChance)
                {
                    lucky.Collect(luckyRandom);
                }
            }
        }

        void Advance(double seconds)
        {
            Time += seconds;
        }

        // Cranks for at most the given time and returns how long the cranking took.
        double Crank(double seconds)
        {
            var spent = 0.0;
            while (Lottery.CanDraw && spent + CrankTime <= seconds)
            {
                var before = State.TotalDraws;
                Lottery.DrawBatch(DrawsPerCrank);
                spent += CrankTime;
                foreach (var milestone in Progression.DrawMilestonesCrossed(before, State.TotalDraws))
                {
                    DrawMilestones[milestone] = Time + spent;
                }
            }

            return spent;
        }

        void Shop()
        {
            // Buy the cheapest affordable upgrade until nothing else fits.
            while (true)
            {
                var candidates = Data.Upgrades
                    .Where(upgrade => !Upgrades.IsMaxed(upgrade) && (noFun || NoStocking || Progression.IsRevealed(upgrade)))
                    .Select(upgrade => Data.FindProduct("upgrade_" + upgrade.Id))
                    .Where(product => Economy.CanAddToCart(new string[0], product.Id))
                    .OrderBy(product => Economy.GetPrice(product))
                    .ToList();
                var bought = false;
                foreach (var product in candidates)
                {
                    if (Economy.GetPrice(product) <= State.Money && Economy.Checkout(new[] { product.Id }).Success)
                    {
                        if (!FirstPurchase.ContainsKey(product.UpgradeId))
                        {
                            FirstPurchase[product.UpgradeId] = Time;
                        }

                        bought = true;
                        break;
                    }
                }

                if (!bought)
                {
                    break;
                }
            }

            // Spend part of what is left on groceries for tickets, biggest items first.
            var budget = (long)(State.Money * TicketBudgetShare) + VoucherValue();
            var groceries = Data.Products
                .Where(product => string.IsNullOrEmpty(product.UpgradeId) && (noFun || NoStocking || Progression.IsRevealed(product)))
                .OrderByDescending(product => product.Price)
                .ToList();
            var cart = new List<string>();
            foreach (var product in groceries)
            {
                while (product.Price <= budget && cart.Count < 50)
                {
                    cart.Add(product.Id);
                    budget -= product.Price;
                }
            }

            if (cart.Count > 0)
            {
                Economy.Checkout(cart);
            }
        }

        long VoucherValue()
        {
            return Data.Items.Where(item => item.VoucherValue > 0).Sum(item => State.GetItem(item.Id) * item.VoucherValue);
        }
    }

    static class Program
    {
        static string Clock(double seconds)
        {
            if (double.IsNaN(seconds))
            {
                return "-";
            }

            var span = TimeSpan.FromSeconds(seconds);
            return $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}";
        }

        static double Median(List<double> values)
        {
            values.Sort();
            return values.Count == 0 ? double.NaN : values[values.Count / 2];
        }

        static void Main(string[] args)
        {
            if (args.Contains("--write-asset"))
            {
                const string assetPath = "Assets/Data/GameData.asset";
                var text = System.IO.File.ReadAllText(assetPath).Replace("\r\n", "\n");
                System.IO.File.WriteAllText(assetPath, Garapon.Tools.GameDataYaml.ReplaceData(text, GameData.CreateDefault()));
                Console.WriteLine($"Wrote {assetPath} from GameData.CreateDefault().");
                return;
            }

            var legacy = args.Contains("--no-automation");
            var runs = 21;
            var maxHours = legacy ? 24 : 48;
            var data = GameData.CreateDefault();
            if (legacy)
            {
                // The original data: three groceries, no guarantee.
                data.GuaranteedGoldDraws = 0;
                data.Products.RemoveAll(product => string.IsNullOrEmpty(product.UpgradeId) && product.Price > 250);
            }

            var noFun = legacy || args.Contains("--no-fun");
            PlayerModel.NoCombo = args.Contains("--no-combo");
            PlayerModel.NoLucky = args.Contains("--no-lucky");
            PlayerModel.NoStamps = args.Contains("--no-stamps");
            PlayerModel.NoStocking = args.Contains("--no-stocking");
            var models = new List<PlayerModel>();
            var secondRuns = new List<PlayerModel>();
            for (var seed = 1; seed <= runs; seed++)
            {
                var model = new PlayerModel(data, new GameState(), seed, legacy, noFun);
                model.Run(maxHours * 3600.0);
                models.Add(model);

                if (model.State.Cleared && !noFun)
                {
                    model.State.PlaySeconds = model.Time;
                    var second = new PlayerModel(data, model.State.NextRun(), seed + 1000, legacy, noFun);
                    second.Run(maxHours * 3600.0);
                    secondRuns.Add(second);
                    if (args.Contains("--debug")) Console.WriteLine($"seed {seed}: first {Clock(model.Time)} second {Clock(second.Time)} draws {second.State.TotalDraws:N0} buys " + string.Join(" ", second.FirstPurchase.Select(p => p.Key + "@" + Clock(p.Value))));
                }
            }

            Console.WriteLine(legacy ? "== Automation upgrades disabled (as before this change) =="
                : noFun ? "== Without lucky trash, combos, stamps and stocking ==" : "== Current balance ==");
            Console.WriteLine($"{runs} runs, capped at {maxHours}h of play\n");

            Console.WriteLine("First purchase of each upgrade (median):");
            foreach (var upgrade in data.Upgrades)
            {
                var times = models.Where(m => m.FirstPurchase.ContainsKey(upgrade.Id)).Select(m => m.FirstPurchase[upgrade.Id]).ToList();
                Console.WriteLine($"  {upgrade.DisplayName,-10} {Clock(Median(times)),9}  ({times.Count}/{runs} runs)");
            }

            Console.WriteLine("\nDraw milestones (median time reached):");
            foreach (var milestone in new long[] { 10, 100, 1000, 10000, 100000, 500000, 1000000 })
            {
                var times = models.Where(m => m.DrawMilestones.ContainsKey(milestone)).Select(m => m.DrawMilestones[milestone]).ToList();
                Console.WriteLine($"  {milestone,9:N0} draws {Clock(Median(times)),9}  ({times.Count}/{runs} runs)");
            }

            var cleared = models.Where(m => m.State.Cleared).Select(m => m.Time).ToList();
            Console.WriteLine($"\nCleared: {cleared.Count}/{runs} runs");
            if (cleared.Count > 0)
            {
                cleared.Sort();
                Console.WriteLine($"  fastest {Clock(cleared.First())}, median {Clock(Median(cleared))}, slowest {Clock(cleared.Last())}");
            }

            if (!noFun)
            {
                Console.WriteLine($"\nStamps at first clear (median): {Median(models.Select(m => (double)m.State.Stamps.Count).ToList())}/{StampCard.All.Count}");
                Console.WriteLine($"Lucky trash caught (median): {Median(models.Select(m => (double)m.State.LuckyCollected).ToList())}");
                Console.WriteLine($"Best combo (median): {Median(models.Select(m => (double)m.State.BestCombo).ToList())}");
                var secondTimes = secondRuns.Where(m => m.State.Cleared).Select(m => m.Time).ToList();
                if (secondTimes.Count > 0)
                {
                    secondTimes.Sort();
                    Console.WriteLine($"Second run clear: fastest {Clock(secondTimes.First())}, median {Clock(Median(secondTimes))}, slowest {Clock(secondTimes.Last())}");
                }
            }

            var stuck = models.Where(m => !m.State.Cleared).ToList();
            if (stuck.Count > 0)
            {
                var draws = stuck.Average(m => (double)m.State.TotalDraws);
                Console.WriteLine($"  not cleared after {maxHours}h: average {draws:N0} draws, {draws / maxHours:N0} draws/hour");
            }
        }
    }
}
