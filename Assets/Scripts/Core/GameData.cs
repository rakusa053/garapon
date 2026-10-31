using System;
using System.Collections.Generic;

namespace Garapon.Core
{
    public enum PrizeColor
    {
        Gold,
        Silver,
        Blue,
        Green,
        Red,
        White
    }

    public enum UpgradeStat
    {
        MoveSpeed,
        PickupRadius,
        TrashValue,
        AutoCollect,
        TicketRate,
        Delivery,
        CrankSpeed,
        DrawBatch,
        AutoCrank
    }

    [Serializable]
    public class PrizeTierDef
    {
        public PrizeColor Color;
        public int PartsPerMillion;
        public string ItemId;
        public int ItemAmount = 1;
    }

    [Serializable]
    public class ItemDef
    {
        public string Id;
        public string DisplayName;
        public long SellPrice;
        public long VoucherValue;
    }

    [Serializable]
    public class TrashDef
    {
        public string Id;
        public string DisplayName;
        public long Value;
    }

    [Serializable]
    public class UpgradeDef
    {
        public string Id;
        public string DisplayName;
        public UpgradeStat Stat;
        public double ValuePerLevel;
        public int MaxLevel;
        public long BaseCost;
        public double CostGrowth = 1.5;
        public string ItemCostId;
        public int ItemCostAmount;
    }

    [Serializable]
    public class ProductDef
    {
        public string Id;
        public string DisplayName;
        public long Price;
        public string UpgradeId;
    }

    [Serializable]
    public class GameData
    {
        public const int PartsPerMillionTotal = 1000000;

        public List<PrizeTierDef> PrizeTiers = new List<PrizeTierDef>();
        public List<ItemDef> Items = new List<ItemDef>();
        public List<TrashDef> Trash = new List<TrashDef>();
        public List<UpgradeDef> Upgrades = new List<UpgradeDef>();
        public List<ProductDef> Products = new List<ProductDef>();
        public long MoneyPerTicket = 100;

        // The draw that is guaranteed to be gold if luck has not delivered it sooner. 0 turns it off.
        public long GuaranteedGoldDraws = 1000000;

        // Offline progress: automation keeps running at this efficiency for at most this many hours.
        public double OfflineHoursCap = 8;
        public double OfflineEfficiency = 0.5;

        public long AverageTrashValue()
        {
            if (Trash.Count == 0)
            {
                return 0;
            }

            var total = 0L;
            foreach (var trash in Trash)
            {
                total += trash.Value;
            }

            return total / Trash.Count;
        }

        public ItemDef FindItem(string id) => Items.Find(item => item.Id == id);
        public UpgradeDef FindUpgrade(string id) => Upgrades.Find(upgrade => upgrade.Id == id);
        public ProductDef FindProduct(string id) => Products.Find(product => product.Id == id);

        public static GameData CreateDefault()
        {
            var data = new GameData();

            data.PrizeTiers.Add(new PrizeTierDef { Color = PrizeColor.Gold, PartsPerMillion = 1 });
            data.PrizeTiers.Add(new PrizeTierDef { Color = PrizeColor.Silver, PartsPerMillion = 5000, ItemId = "rare_parts" });
            data.PrizeTiers.Add(new PrizeTierDef { Color = PrizeColor.Blue, PartsPerMillion = 30000, ItemId = "tool_parts" });
            data.PrizeTiers.Add(new PrizeTierDef { Color = PrizeColor.Green, PartsPerMillion = 80000, ItemId = "voucher" });
            data.PrizeTiers.Add(new PrizeTierDef { Color = PrizeColor.Red, PartsPerMillion = 200000, ItemId = "detergent" });
            data.PrizeTiers.Add(new PrizeTierDef { Color = PrizeColor.White, PartsPerMillion = 684999, ItemId = "tissue" });

            data.Items.Add(new ItemDef { Id = "rare_parts", DisplayName = "希少な部品" });
            data.Items.Add(new ItemDef { Id = "tool_parts", DisplayName = "道具の部品" });
            data.Items.Add(new ItemDef { Id = "voucher", DisplayName = "商品券", VoucherValue = 100 });
            data.Items.Add(new ItemDef { Id = "detergent", DisplayName = "洗剤", SellPrice = 30 });
            data.Items.Add(new ItemDef { Id = "tissue", DisplayName = "ティッシュ", SellPrice = 5 });

            data.Trash.Add(new TrashDef { Id = "can", DisplayName = "空き缶", Value = 10 });
            data.Trash.Add(new TrashDef { Id = "bottle", DisplayName = "ペットボトル", Value = 15 });
            data.Trash.Add(new TrashDef { Id = "paper", DisplayName = "紙くず", Value = 5 });

            data.Upgrades.Add(new UpgradeDef { Id = "shoes", DisplayName = "運動靴", Stat = UpgradeStat.MoveSpeed, ValuePerLevel = 0.25, MaxLevel = 10, BaseCost = 200 });
            data.Upgrades.Add(new UpgradeDef { Id = "tongs", DisplayName = "長いトング", Stat = UpgradeStat.PickupRadius, ValuePerLevel = 0.5, MaxLevel = 10, BaseCost = 300, ItemCostId = "tool_parts", ItemCostAmount = 1 });
            data.Upgrades.Add(new UpgradeDef { Id = "bag", DisplayName = "分別袋", Stat = UpgradeStat.TrashValue, ValuePerLevel = 0.5, MaxLevel = 40, BaseCost = 500, CostGrowth = 1.3 });
            data.Upgrades.Add(new UpgradeDef { Id = "robot", DisplayName = "ごみ拾いロボ", Stat = UpgradeStat.AutoCollect, ValuePerLevel = 2, MaxLevel = 60, BaseCost = 2000, CostGrowth = 1.25, ItemCostId = "rare_parts", ItemCostAmount = 1 });
            data.Upgrades.Add(new UpgradeDef { Id = "card", DisplayName = "ポイントカード", Stat = UpgradeStat.TicketRate, ValuePerLevel = 0.1, MaxLevel = 30, BaseCost = 1000, CostGrowth = 1.3 });
            data.Upgrades.Add(new UpgradeDef { Id = "delivery", DisplayName = "出張買取", Stat = UpgradeStat.Delivery, ValuePerLevel = 1, MaxLevel = 1, BaseCost = 20000, ItemCostId = "rare_parts", ItemCostAmount = 3 });
            data.Upgrades.Add(new UpgradeDef { Id = "grease", DisplayName = "潤滑油", Stat = UpgradeStat.CrankSpeed, ValuePerLevel = 0.25, MaxLevel = 4, BaseCost = 800, CostGrowth = 3 });
            data.Upgrades.Add(new UpgradeDef { Id = "hopper", DisplayName = "大型ホッパー", Stat = UpgradeStat.DrawBatch, ValuePerLevel = 1, MaxLevel = 10, BaseCost = 3000, CostGrowth = 2.2, ItemCostId = "tool_parts", ItemCostAmount = 3 });
            data.Upgrades.Add(new UpgradeDef { Id = "motor", DisplayName = "自動モーター", Stat = UpgradeStat.AutoCrank, ValuePerLevel = 1, MaxLevel = 1, BaseCost = 10000, ItemCostId = "tool_parts", ItemCostAmount = 5 });

            data.Products.Add(new ProductDef { Id = "bread", DisplayName = "食パン", Price = 150 });
            data.Products.Add(new ProductDef { Id = "milk", DisplayName = "牛乳", Price = 200 });
            data.Products.Add(new ProductDef { Id = "eggs", DisplayName = "卵", Price = 250 });
            // Bigger purchases keep the number of trips to the register sane once a crank draws hundreds of balls.
            data.Products.Add(new ProductDef { Id = "rice", DisplayName = "お米 10kg", Price = 4000 });
            data.Products.Add(new ProductDef { Id = "beef", DisplayName = "すき焼き肉", Price = 20000 });
            data.Products.Add(new ProductDef { Id = "rice_cooker", DisplayName = "炊飯器", Price = 100000 });
            data.Products.Add(new ProductDef { Id = "television", DisplayName = "テレビ", Price = 500000 });
            foreach (var upgrade in data.Upgrades)
            {
                data.Products.Add(new ProductDef { Id = "upgrade_" + upgrade.Id, DisplayName = upgrade.DisplayName, UpgradeId = upgrade.Id });
            }

            return data;
        }
    }
}
