using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Garapon.Core;

namespace Garapon.Tools
{
    // Writes GameData the way Unity serializes it inside Assets/Data/GameData.asset, so the asset
    // can be regenerated from GameData.CreateDefault() without opening the Editor.
    public static class GameDataYaml
    {
        const string DataKey = "  Data:\n";

        public static string ReplaceData(string assetText, GameData data)
        {
            var start = assetText.IndexOf(DataKey, StringComparison.Ordinal);
            if (start < 0)
            {
                throw new FormatException("The asset has no Data block.");
            }

            return assetText.Substring(0, start) + Serialize(data);
        }

        public static string Serialize(GameData data)
        {
            var yaml = new StringBuilder(DataKey);
            List("PrizeTiers", data.PrizeTiers, yaml, (tier, add) =>
            {
                add("Color", Int((int)tier.Color));
                add("PartsPerMillion", Int(tier.PartsPerMillion));
                add("ItemId", Text(tier.ItemId));
                add("ItemAmount", Int(tier.ItemAmount));
            });
            List("Items", data.Items, yaml, (item, add) =>
            {
                add("Id", Text(item.Id));
                add("DisplayName", Text(item.DisplayName));
                add("SellPrice", Int(item.SellPrice));
                add("VoucherValue", Int(item.VoucherValue));
            });
            List("Trash", data.Trash, yaml, (trash, add) =>
            {
                add("Id", Text(trash.Id));
                add("DisplayName", Text(trash.DisplayName));
                add("Value", Int(trash.Value));
            });
            List("Upgrades", data.Upgrades, yaml, (upgrade, add) =>
            {
                add("Id", Text(upgrade.Id));
                add("DisplayName", Text(upgrade.DisplayName));
                add("Stat", Int((int)upgrade.Stat));
                add("ValuePerLevel", Number(upgrade.ValuePerLevel));
                add("MaxLevel", Int(upgrade.MaxLevel));
                add("BaseCost", Int(upgrade.BaseCost));
                add("CostGrowth", Number(upgrade.CostGrowth));
                add("ItemCostId", Text(upgrade.ItemCostId));
                add("ItemCostAmount", Int(upgrade.ItemCostAmount));
            });
            List("Products", data.Products, yaml, (product, add) =>
            {
                add("Id", Text(product.Id));
                add("DisplayName", Text(product.DisplayName));
                add("Price", Int(product.Price));
                add("UpgradeId", Text(product.UpgradeId));
            });
            yaml.Append("    MoneyPerTicket: ").Append(Int(data.MoneyPerTicket)).Append('\n');
            yaml.Append("    GuaranteedGoldDraws: ").Append(Int(data.GuaranteedGoldDraws)).Append('\n');
            yaml.Append("    OfflineHoursCap: ").Append(Number(data.OfflineHoursCap)).Append('\n');
            yaml.Append("    OfflineEfficiency: ").Append(Number(data.OfflineEfficiency)).Append('\n');
            return yaml.ToString();
        }

        static void List<T>(string name, List<T> entries, StringBuilder yaml, Action<T, Action<string, string>> write)
        {
            yaml.Append("    ").Append(name).Append(":\n");
            foreach (var entry in entries)
            {
                var first = true;
                write(entry, (key, value) =>
                {
                    yaml.Append(first ? "    - " : "      ").Append(key).Append(": ").Append(value).Append('\n');
                    first = false;
                });
            }
        }

        static string Int(long value) => value.ToString(CultureInfo.InvariantCulture);

        static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        static string Text(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var plain = true;
            foreach (var character in value)
            {
                if (character > 0x7E || character < 0x20 || character == '"' || character == '\\')
                {
                    plain = false;
                }
            }

            if (plain && !value.Contains(": ") && value.Trim() == value)
            {
                return value;
            }

            var quoted = new StringBuilder("\"");
            foreach (var character in value)
            {
                if (character > 0x7E || character < 0x20)
                {
                    quoted.Append("\\u").Append(((int)character).ToString("X4"));
                }
                else
                {
                    if (character == '"' || character == '\\')
                    {
                        quoted.Append('\\');
                    }

                    quoted.Append(character);
                }
            }

            return quoted.Append('"').ToString();
        }
    }
}
