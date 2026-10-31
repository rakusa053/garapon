using Garapon.Core;
using UnityEngine;

namespace Garapon.Game
{
    public static class PrizeColors
    {
        public static Color ToColor(PrizeColor color)
        {
            switch (color)
            {
                case PrizeColor.Gold: return new Color(1f, 0.78f, 0.1f);
                case PrizeColor.Silver: return new Color(0.75f, 0.78f, 0.85f);
                case PrizeColor.Blue: return new Color(0.15f, 0.4f, 0.95f);
                case PrizeColor.Green: return new Color(0.15f, 0.75f, 0.3f);
                case PrizeColor.Red: return new Color(0.9f, 0.15f, 0.15f);
                default: return Color.white;
            }
        }

        public static string DisplayName(PrizeColor color)
        {
            switch (color)
            {
                case PrizeColor.Gold: return "金";
                case PrizeColor.Silver: return "銀";
                case PrizeColor.Blue: return "青";
                case PrizeColor.Green: return "緑";
                case PrizeColor.Red: return "赤";
                default: return "白";
            }
        }
    }
}
