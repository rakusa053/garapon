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
                default: return string.Empty;
            }
        }
    }
}
