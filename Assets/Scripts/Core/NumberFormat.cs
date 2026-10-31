namespace Garapon.Core
{
    public static class NumberFormat
    {
        static readonly string[] Units = { "万", "億", "兆", "京" };

        // Formats a count the way Japanese shops do: 9,999 stays as is, then 1.23万, 45.6億 and so on.
        // Rounds down so a price never looks more affordable than it is.
        public static string Japanese(long value)
        {
            if (value < 0)
            {
                return "-" + Japanese(-value);
            }

            if (value < 10000)
            {
                return value.ToString("N0");
            }

            var unit = -1;
            var scale = 1L;
            while (unit + 1 < Units.Length && value / scale >= 10000)
            {
                unit++;
                scale *= 10000;
            }

            var whole = value / scale;
            var remainder = value % scale;
            if (whole >= 1000)
            {
                return whole.ToString("N0") + Units[unit];
            }

            var decimals = whole >= 100 ? 0 : whole >= 10 ? 1 : 2;
            var fractionScale = decimals == 0 ? 1 : decimals == 1 ? 10 : 100;
            var fraction = remainder / (scale / fractionScale);
            var text = decimals == 0 ? whole.ToString() : whole + "." + fraction.ToString().PadLeft(decimals, '0');
            return text + Units[unit];
        }

        public static string Yen(long value) => Japanese(value) + "円";
    }
}
