using System.Linq;
using System.Text.RegularExpressions;
using ArrowEscape.Core;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Permanent ids, exact counts, category costs and color payloads.</summary>
    public sealed class CatalogTests
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void CountsAndUniquePermanentIds()
        {
            Assert.That(ShopCatalog.Items.Count,Is.EqualTo(73));Assert.That(ShopCatalog.Items.Count(i=>i.Slot==ShopSlot.Arrow),Is.EqualTo(20));Assert.That(ShopCatalog.Items.Count(i=>i.Slot==ShopSlot.Theme),Is.EqualTo(10));Assert.That(ShopCatalog.Items.Count(i=>i.Slot==ShopSlot.Avatar),Is.EqualTo(22));Assert.That(ShopCatalog.Items.Count(i=>i.Slot==ShopSlot.Frame),Is.EqualTo(10));Assert.That(ShopCatalog.Items.Select(i=>i.Id).Distinct().Count(),Is.EqualTo(73));
            foreach(var item in ShopCatalog.Items){Assert.That(Regex.IsMatch(item.Id,"^[a-z][a-z_]*$"),Is.True);Assert.That(ColorUtility.TryParseHtmlString(item.PrimaryColor,out _),Is.True);Assert.That(ColorUtility.TryParseHtmlString(item.SecondaryColor,out _),Is.True);}
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [TestCase("Elemental & Magic",4,150)] [TestCase("Rare & Premium",5,300)] [TestCase("Tactical & Sci-Fi",4,400)] [TestCase("Organic & Strange",3,450)] [TestCase("Legendary",3,1000)]
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void ArrowCategory(string category,int count,int price){var items=ShopCatalog.Items.Where(i=>i.Category==category).ToArray();Assert.That(items.Length,Is.EqualTo(count));foreach(var item in items){Assert.That(item.Price,Is.EqualTo(price));Assert.That(item.IsAvailable,Is.True);}}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void ThemePricesAndAvailability(){string[] ids={"grad_midnight","grad_aurora","grad_sunset","grad_lagoon","grad_emerald","grad_rose","geo_crystal","geo_neon","geo_gold"};int[] costs={150,200,250,300,350,400,900,1200,1500};for(int i=0;i<ids.Length;i++){var item=ShopCatalog.Find(ids[i]);Assert.That(item.Price,Is.EqualTo(costs[i]));Assert.That(item.IsAvailable,Is.True);}Assert.That(ShopCatalog.Find("classic").Price,Is.Zero);Assert.That(ShopCatalog.Find("paper").Price,Is.Zero);}
    }
}

