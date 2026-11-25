using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>One special offer: a shop discount on cosmetics and an optional free coin gift, for whole local days.</summary>
    public sealed class SpecialOffer
    {
        /// <summary>Stable id, unique per occurrence (gift receipts use it), e.g. "weekend_20261128".</summary>
        public string Id { get; }
        /// <summary>Short title, e.g. "WEEKEND SALE".</summary>
        public string Title { get; }
        /// <summary>One-line pitch used in the shop and notifications.</summary>
        public string Pitch { get; }
        /// <summary>First local day (date part only).</summary>
        public DateTime Start { get; }
        /// <summary>Last local day, inclusive.</summary>
        public DateTime End { get; }
        /// <summary>Percent off arrow styles, avatars, frames and sounds.</summary>
        public int DiscountPercent { get; }
        /// <summary>Free coins the player can claim once in the shop (0 = none).</summary>
        public int GiftCoins { get; }
        /// <summary>Creates an offer.</summary>
        public SpecialOffer(string id, string title, string pitch, DateTime start, DateTime end, int discount, int gift)
        { Id = id; Title = title; Pitch = pitch; Start = start.Date; End = end.Date; DiscountPercent = discount; GiftCoins = gift; }
        /// <summary>True on any local day from Start to End.</summary>
        public bool IsActiveOn(DateTime day) => day.Date >= Start && day.Date <= End;
    }

    /// <summary>
    /// Step 17: the built-in special-offer calendar. Every weekend has a sale, and festivals (New Year, Pongal, Holi,
    /// Diwali, Christmas) bring bigger discounts plus a free coin gift. A festival replaces the weekend sale on the days it
    /// covers. Pure date logic, so the shop and the notification planner agree everywhere.
    /// </summary>
    public static class OfferCalendar
    {
        /// <summary>Weekend sale discount.</summary>
        public const int WeekendDiscount = 25;
        // Moveable festivals (local dates). Fixed-date ones are generated per year.
        private static readonly (int y, int m, int d)[] Holi = { (2027, 3, 22), (2028, 3, 11), (2029, 3, 1) };
        private static readonly (int y, int m, int d)[] Diwali = { (2026, 11, 8), (2027, 10, 29), (2028, 10, 17), (2029, 11, 5) };

        /// <summary>The offer running on a local day, or null.</summary>
        public static SpecialOffer ActiveOn(DateTime day)
        {
            foreach (var f in FestivalsAround(day.Year)) if (f.IsActiveOn(day)) return f;
            return Weekend(day);
        }

        /// <summary>Offers that start after <paramref name="from"/> (exclusive) within <paramref name="days"/> days, in start order.</summary>
        public static List<SpecialOffer> Upcoming(DateTime from, int days)
        {
            var list = new List<SpecialOffer>(); string last = null;
            for (int i = 1; i <= days; i++)
            {
                var day = from.Date.AddDays(i); var offer = ActiveOn(day);
                if (offer == null || offer.Id == last) continue;
                last = offer.Id;
                if (offer.Start == day) list.Add(offer);
            }
            return list;
        }

        private static SpecialOffer Weekend(DateTime day)
        {
            var d = day.Date;
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) return null;
            var sat = d.DayOfWeek == DayOfWeek.Saturday ? d : d.AddDays(-1);
            // A festival that touches this weekend takes it over completely.
            foreach (var f in FestivalsAround(sat.Year)) if (f.IsActiveOn(sat) || f.IsActiveOn(sat.AddDays(1))) return null;
            return new SpecialOffer("weekend_" + Key(sat), "WEEKEND SALE", WeekendDiscount + "% off every arrow style, avatar, frame and sound", sat, sat.AddDays(1), WeekendDiscount, 0);
        }

        private static IEnumerable<SpecialOffer> FestivalsAround(int year)
        {
            for (int y = year - 1; y <= year + 1; y++)
            {
                yield return Make("newyear", "NEW YEAR SALE", "Happy New Year! 50% off and 500 free coins", new DateTime(y, 1, 1), 2, 50, 500);
                yield return Make("pongal", "PONGAL SALE", "Happy Pongal! 40% off and 300 free coins", new DateTime(y, 1, 14), 3, 40, 300);
                yield return Make("christmas", "CHRISTMAS SALE", "Merry Christmas! 40% off and 300 free coins", new DateTime(y, 12, 24), 3, 40, 300);
            }
            foreach (var (y, m, d) in Holi) if (Math.Abs(y - year) <= 1) yield return Make("holi", "HOLI SALE", "Happy Holi! 40% off and 300 free coins", new DateTime(y, m, d).AddDays(-1), 2, 40, 300);
            foreach (var (y, m, d) in Diwali) if (Math.Abs(y - year) <= 1) yield return Make("diwali", "DIWALI SALE", "Happy Diwali! 50% off and 500 free coins", new DateTime(y, m, d).AddDays(-1), 3, 50, 500);
        }
        private static SpecialOffer Make(string id, string title, string pitch, DateTime start, int days, int discount, int gift) =>
            new SpecialOffer(id + "_" + Key(start), title, pitch, start, start.AddDays(days - 1), discount, gift);
        /// <summary>yyyymmdd key of a date.</summary>
        public static int Key(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;

        /// <summary>Discounted coin price (rounded to the nearest 5, never below 5 for a paid item).</summary>
        public static int Discounted(int price, int percent)
        {
            if (price <= 0 || percent <= 0) return price;
            int p = (int)Math.Round(price * (100 - Math.Min(90, percent)) / 100.0 / 5.0, MidpointRounding.AwayFromZero) * 5;
            return Math.Max(5, p);
        }
    }
}
