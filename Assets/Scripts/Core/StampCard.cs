using System;
using System.Collections.Generic;

namespace Garapon.Core
{
    public sealed class Stamp
    {
        public string Id;
        public string Name;
        public Func<GameState, UpgradeSystem, bool> IsEarned;
    }

    // Achievements as a shopping-street stamp card. Each stamp permanently raises trash sales,
    // so collecting them matters, and the card carries over into later runs.
    public static class StampCard
    {
        public static readonly IReadOnlyList<Stamp> All = new List<Stamp>
        {
            Make("first_draw", "はじめての抽選", (s, u) => s.TotalDraws >= 1),
            Make("draws_100", "抽選100回", (s, u) => s.TotalDraws >= 100),
            Make("draws_10k", "抽選1万回", (s, u) => s.TotalDraws >= 10000),
            Make("draws_100k", "抽選10万回", (s, u) => s.TotalDraws >= 100000),
            Make("red", "赤玉をひいた", (s, u) => s.GetDrawCount(PrizeColor.Red) > 0),
            Make("green", "緑玉をひいた", (s, u) => s.GetDrawCount(PrizeColor.Green) > 0),
            Make("blue", "青玉をひいた", (s, u) => s.GetDrawCount(PrizeColor.Blue) > 0),
            Make("silver", "銀玉をひいた", (s, u) => s.GetDrawCount(PrizeColor.Silver) > 0),
            Make("silver_100", "銀玉100個", (s, u) => s.GetDrawCount(PrizeColor.Silver) >= 100),
            Make("earned_10k", "累計1万円かせいだ", (s, u) => s.TotalEarned >= 10000),
            Make("earned_1m", "累計100万円かせいだ", (s, u) => s.TotalEarned >= 1000000),
            Make("earned_100m", "累計1億円かせいだ", (s, u) => s.TotalEarned >= 100000000),
            Make("robot", "ロボを雇った", (s, u) => u.AutoCollectPerSecond > 0),
            Make("motor", "モーターを付けた", (s, u) => u.HasAutoCrank),
            Make("hopper_max", "ホッパーを極めた", (s, u) => u.DrawsPerCrank >= 1024),
            Make("lucky_1", "ラッキーごみを拾った", (s, u) => s.LuckyCollected >= 1),
            Make("lucky_20", "ラッキーごみ20個", (s, u) => s.LuckyCollected >= 20),
            Make("combo_30", "30コンボ", (s, u) => s.BestCombo >= 30),
            Make("combo_100", "100コンボ", (s, u) => s.BestCombo >= 100),
            Make("cleared", "温泉旅行に行った", (s, u) => s.Clears >= 1)
        };

        // Adds every newly earned stamp to the card and returns them, so they can be announced.
        public static List<Stamp> Update(GameState state, UpgradeSystem upgrades)
        {
            var earned = new List<Stamp>();
            foreach (var stamp in All)
            {
                if (!state.HasStamp(stamp.Id) && stamp.IsEarned(state, upgrades))
                {
                    state.Stamps.Add(stamp.Id);
                    earned.Add(stamp);
                }
            }

            return earned;
        }

        static Stamp Make(string id, string name, Func<GameState, UpgradeSystem, bool> isEarned) =>
            new Stamp { Id = id, Name = name, IsEarned = isEarned };
    }
}
