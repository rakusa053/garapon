namespace Garapon.Core
{
    public static class UpgradeText
    {
        // What one more level of the upgrade does, for shelf prompts.
        public static string Effect(UpgradeDef upgrade)
        {
            var percent = (int)System.Math.Round(upgrade.ValuePerLevel * 100);
            switch (upgrade.Stat)
            {
                case UpgradeStat.MoveSpeed: return $"歩く速さ +{percent}%";
                case UpgradeStat.PickupRadius: return $"拾える範囲 +{upgrade.ValuePerLevel}m";
                case UpgradeStat.TrashValue: return $"ごみの買取額 +{percent}%";
                case UpgradeStat.AutoCollect: return $"ロボが毎秒{upgrade.ValuePerLevel}個ごみを拾う";
                case UpgradeStat.TicketRate: return $"もらえる抽選券 +{percent}%";
                case UpgradeStat.Delivery: return "ごみと日用品を自動で換金";
                case UpgradeStat.CrankSpeed: return $"回転速度 +{percent}%";
                case UpgradeStat.DrawBatch: return "1回転の抽選数が2倍";
                case UpgradeStat.AutoCrank: return "ガラポンを自動で回す";
                case UpgradeStat.RobotPerTrashLevel: return $"分別袋1レベルごとにロボ +{percent}%";
                case UpgradeStat.TicketPerStamp: return $"スタンプ1個ごとに抽選券 +{percent}%";
                default: return string.Empty;
            }
        }

        public static string Effect(PerkDef perk)
        {
            var percent = (int)System.Math.Round(perk.ValuePerLevel * 100);
            switch (perk.Stat)
            {
                case PerkStat.StartMoney: return $"次の周回から 開始時の所持金 +{NumberFormat.Yen((long)perk.ValuePerLevel)}";
                case PerkStat.StartTrashValueLevels: return $"次の周回から 分別袋Lv{perk.ValuePerLevel}でスタート";
                case PerkStat.CrankSpeed: return $"ガラポンの回転速度 +{percent}%";
                case PerkStat.LuckyRate: return $"ラッキーごみの出現 +{percent}%";
                case PerkStat.GuaranteeReduction: return $"金玉確定までの回数 -{percent}%";
                case PerkStat.AutoShop: return "出張買取の売上の半分で自動的に抽選券を買う";
                default: return string.Empty;
            }
        }
    }
}
