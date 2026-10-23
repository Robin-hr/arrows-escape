using System;
namespace ArrowEscape.Core
{
    /// <summary>Calendar days as yyyymmdd integers (the player's local date).</summary>
    public static class DailyCalendar
    {
        private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        /// <summary>Key for a date.</summary>
        public static int Key(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;
        /// <summary>Date for a key, or MinValue when invalid.</summary>
        public static DateTime FromKey(int key)
        {
            int y = key / 10000, m = key / 100 % 100, d = key % 100;
            if (y < 1 || m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(y, m)) return DateTime.MinValue;
            return new DateTime(y, m, d);
        }
        /// <summary>Key shifted by whole days.</summary>
        public static int AddDays(int key, int days) { var date = FromKey(key); return date == DateTime.MinValue ? 0 : Key(date.AddDays(days)); }
        /// <summary>"23 Oct 2026".</summary>
        public static string Pretty(int key) { var date = FromKey(key); return date == DateTime.MinValue ? "" : date.Day + " " + Months[date.Month - 1] + " " + date.Year; }
    }
}
