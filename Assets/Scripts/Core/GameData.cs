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
        AutoCrank,
        // Synergies: the robot gets faster per level of trash-value upgrades, tickets grow per stamp.
        RobotPerTrashLevel,
        TicketPerStamp
    }

    // Permanent perks bought with souvenirs from clearing, which survive every reset.
    public enum PerkStat
    {
        StartMoney,
        StartTrashValueLevels,
        CrankSpeed,
        LuckyRate,
        GuaranteeReduction,
        AutoShop
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

        // Every MilestoneEvery levels the whole effect is multiplied by MilestoneMultiplier,
        // like the unlocks at 25, 50 and 100 owned in AdVenture Capitalist. 0 turns it off.
        public int MilestoneEvery;
        public double MilestoneMultiplier = 1;

        // Not stocked until another upgrade reaches a level, like Cookie Clicker's unlockable upgrades.
        public string RequiresId;
        public int RequiresLevel;
    }

    [Serializable]
    public class PerkDef
    {
        public string Id;
        public string DisplayName;
        public PerkStat Stat;
        public double ValuePerLevel;
        public int MaxLevel;
        public long BaseCost;
        public double CostGrowth = 2;
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
        public List<PerkDef> Perks = new List<PerkDef>();
        public long MoneyPerTicket = 100;

        // The draw that is guaranteed to be gold if luck has not delivered it sooner. 0 turns it off.
        public long GuaranteedGoldDraws = 1000000;

        // Offline progress: automation keeps running at this efficiency for at most this many hours.
        public double OfflineHoursCap = 8;
        public double OfflineEfficiency = 0.5;

        // Fun systems: lucky trash, pickup combos, the stamp card and the bonus for clearing.
        public double FrenzyMultiplier = 7;
        public double FrenzySeconds = 30;
        public double ComboWindowSeconds = 2.5;
        public double ComboStep = 0.01;
        public int ComboMaxSteps = 50;
        public double StampBonus = 0.03;
        public double ClearBonus = 1;

        // Time sales: now and then one stocked product is discounted (upgrades) or gives extra tickets (groceries).
        public double SaleIntervalMin = 90;
        public double SaleIntervalMax = 180;
        public double SaleSeconds = 45;
        public double SaleDiscount = 0.3;
        public double SaleTicketMultiplier = 2;

        // Souvenirs for clearing: the total ever earned is the square root of lifetime earnings divided by this.
        public double SouvenirUnit = 500000;

        // Share of delivery sales spent on tickets automatically once the online-shopping perk is owned.
        public double AutoShopShare = 0.5;

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
        public PerkDef FindPerk(string id) => Perks.Find(perk => perk.Id == id);

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
            data.Upgrades.Add(new UpgradeDef { Id = "bag", DisplayName = "分別袋", Stat = UpgradeStat.TrashValue, ValuePerLevel = 0.5, MaxLevel = 40, BaseCost = 500, CostGrowth = 1.36, MilestoneEvery = 10, MilestoneMultiplier = 1.25 });
            data.Upgrades.Add(new UpgradeDef { Id = "robot", DisplayName = "ごみ拾いロボ", Stat = UpgradeStat.AutoCollect, ValuePerLevel = 2, MaxLevel = 60, BaseCost = 2000, CostGrowth = 1.3, ItemCostId = "rare_parts", ItemCostAmount = 1, MilestoneEvery = 10, MilestoneMultiplier = 1.25 });
            data.Upgrades.Add(new UpgradeDef { Id = "card", DisplayName = "ポイントカード", Stat = UpgradeStat.TicketRate, ValuePerLevel = 0.1, MaxLevel = 30, BaseCost = 1000, CostGrowth = 1.4 });
            data.Upgrades.Add(new UpgradeDef { Id = "delivery", DisplayName = "出張買取", Stat = UpgradeStat.Delivery, ValuePerLevel = 1, MaxLevel = 1, BaseCost = 20000, ItemCostId = "rare_parts", ItemCostAmount = 3 });
            data.Upgrades.Add(new UpgradeDef { Id = "grease", DisplayName = "潤滑油", Stat = UpgradeStat.CrankSpeed, ValuePerLevel = 0.25, MaxLevel = 4, BaseCost = 800, CostGrowth = 3 });
            data.Upgrades.Add(new UpgradeDef { Id = "hopper", DisplayName = "大型ホッパー", Stat = UpgradeStat.DrawBatch, ValuePerLevel = 1, MaxLevel = 10, BaseCost = 3000, CostGrowth = 3.0, ItemCostId = "tool_parts", ItemCostAmount = 3 });
            data.Upgrades.Add(new UpgradeDef { Id = "motor", DisplayName = "自動モーター", Stat = UpgradeStat.AutoCrank, ValuePerLevel = 1, MaxLevel = 1, BaseCost = 10000, ItemCostId = "tool_parts", ItemCostAmount = 5 });
            data.Upgrades.Add(new UpgradeDef { Id = "truck", DisplayName = "ごみ収集車", Stat = UpgradeStat.RobotPerTrashLevel, ValuePerLevel = 0.03, MaxLevel = 1, BaseCost = 300000, ItemCostId = "rare_parts", ItemCostAmount = 5, RequiresId = "robot", RequiresLevel = 10 });
            data.Upgrades.Add(new UpgradeDef { Id = "nametag", DisplayName = "常連さんの名札", Stat = UpgradeStat.TicketPerStamp, ValuePerLevel = 0.02, MaxLevel = 1, BaseCost = 150000, RequiresId = "card", RequiresLevel = 10 });

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

            data.Perks.Add(new PerkDef { Id = "manju", DisplayName = "温泉まんじゅう", Stat = PerkStat.StartMoney, ValuePerLevel = 20000, MaxLevel = 5, BaseCost = 2 });
            data.Perks.Add(new PerkDef { Id = "my_bag", DisplayName = "マイ分別袋", Stat = PerkStat.StartTrashValueLevels, ValuePerLevel = 5, MaxLevel = 3, BaseCost = 4 });
            data.Perks.Add(new PerkDef { Id = "charm", DisplayName = "旅のお守り", Stat = PerkStat.LuckyRate, ValuePerLevel = 0.25, MaxLevel = 4, BaseCost = 3 });
            data.Perks.Add(new PerkDef { Id = "master", DisplayName = "ガラポン名人", Stat = PerkStat.CrankSpeed, ValuePerLevel = 0.2, MaxLevel = 5, BaseCost = 4, CostGrowth = 1.8 });
            data.Perks.Add(new PerkDef { Id = "golden", DisplayName = "金のガラポン", Stat = PerkStat.GuaranteeReduction, ValuePerLevel = 0.1, MaxLevel = 5, BaseCost = 6 });
            data.Perks.Add(new PerkDef { Id = "net_super", DisplayName = "ネットスーパー", Stat = PerkStat.AutoShop, ValuePerLevel = 1, MaxLevel = 1, BaseCost = 10 });

            return data;
        }
    }
}
