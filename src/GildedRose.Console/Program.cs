using System.Collections.Generic;

namespace GildedRose.Console
{
	public class Program
	{
		public const int MinQuality = 0, MaxQuality = 50;

		// Stores item-specific QualityStrategys indexed by the item name.
		// If I could change the Item class I'd add in a QualityStrategy field
		// instead of this. If an item's name is not found as a key in this map,
		// then the DefaultQualityStrategy is used.
		public static readonly IReadOnlyDictionary<string, QualityStrategy> QualityStrategyMap = new Dictionary
			<string, QualityStrategy>
		{
			{"Aged Brie", QualityStrategies.Standard(1, 2)},
			{"Sulfuras, Hand of Ragnaros", QualityStrategies.Legendary()},
			{"Backstage passes to a TAFKAL80ETC concert", QualityStrategies.BackstagePass()},
			{"Conjured Mana Cake", QualityStrategies.Standard(-2, -4)}
		};

		// Default QualityStrategy for Items whose names are not found in the above QualityStrategyMap map.
		public static readonly QualityStrategy DefaultQualityStrategy = QualityStrategies.Standard(-1, -2);

		// Stores item-specific SellInStrategys indexed by the item name.
		// If I could change the Item class I'd add in a SellInStrategy field
		// instead of this. If an item's name is not found as a key in this map,
		// then the DefaultSellInStrategy is used.
		public static readonly IReadOnlyDictionary<string, SellInStrategy> SellInStrategyMap = new Dictionary
			<string, SellInStrategy>
		{
			{"Sulfuras, Hand of Ragnaros", SellInStrategies.NoChange()}
		};

		// Default SellInStrategy for Items whose names are not found in the above SellInStrategyMap map.
		public static readonly SellInStrategy DefaultSellInStrategy = SellInStrategies.LinearChange(-1);

		public IList<Item> Items;

		private static void Main()
		{
			System.Console.WriteLine("OMGHAI!");

			var app = new Program
			{
				Items = new List<Item>
				{
					new Item {Name = "+5 Dexterity Vest", SellIn = 10, Quality = 20},
					new Item {Name = "Aged Brie", SellIn = 2, Quality = 0},
					new Item {Name = "Elixir of the Mongoose", SellIn = 5, Quality = 7},
					new Item {Name = "Sulfuras, Hand of Ragnaros", SellIn = 0, Quality = 80},
					new Item
					{
						Name = "Backstage passes to a TAFKAL80ETC concert",
						SellIn = 15,
						Quality = 20
					},
					new Item {Name = "Conjured Mana Cake", SellIn = 3, Quality = 6}
				}
			};

			app.UpdateQuality();

			System.Console.ReadKey();

		}

		public void UpdateQuality()
		{
			foreach (var item in Items)
			{
				GetQualityStrategy(item.Name)(item);
				GetSellInStrategy(item.Name)(item);
			}
		}

		private QualityStrategy GetQualityStrategy(string itemName)
		{
			QualityStrategy strategy;
			if (QualityStrategyMap != null && QualityStrategyMap.TryGetValue(itemName, out strategy))
				return strategy;
			return DefaultQualityStrategy;
		}

		private SellInStrategy GetSellInStrategy(string itemName)
		{
			SellInStrategy strategy;
			if (SellInStrategyMap != null && SellInStrategyMap.TryGetValue(itemName, out strategy))
				return strategy;
			return DefaultSellInStrategy;
		}
	}

	public class Item
	{
		public string Name { get; set; }

		public int SellIn { get; set; }

		public int Quality { get; set; }
	}

	/// <summary>
	/// Given a GildedRose item, updates the Quality value for that
	/// item. Assumes exactly one day has passed. Does not modify and
	/// Item fields except Quality.
	/// </summary>
	/// <param name="item">The item whose Quality value will be updated.</param>
	public delegate void QualityStrategy(Item item);

	public static class QualityStrategies
	{
		/// <summary>
		/// Produces a standard QualityStrategy that changes Item quality by
		/// a constant amount before and on the sell date and by another constant
		/// amount after the sell date has passed. Positive change rates increase
		/// the quality, negative change rates decrease the quality.
		/// </summary>
		/// <param name="changeRate">The rate at which Item quality changes
		/// before and during the sell date, in units of quantity per day.</param>
		/// <param name="pastSellDateChangeRate">The rate at which Item quality
		/// changes after the sell date has passed, in units of quantity per date.</param>
		public static QualityStrategy Standard(int changeRate, int pastSellDateChangeRate)
		{
			return BoundQualityHighLow(item =>
			{
				item.Quality += item.SellIn > 0 ? changeRate : pastSellDateChangeRate;
			});
		}

		/// <summary>
		/// Produces a QualityStrategy for legendary Items which do not change quality
		/// as time passes.
		/// </summary>
		public static QualityStrategy Legendary()
		{
			return BoundQualityLow(item => { });
		}

		/// <summary>
		/// Produces a QualityStrategy for backstage pass Items, which have
		/// special case behavior as the date of the concert (sell date) approaches.
		/// </summary>
		public static QualityStrategy BackstagePass()
		{
			return BoundQualityHighLow(item =>
			{
				if (item.SellIn <= 0)
					item.Quality = 0;
				else if (item.SellIn <= 5)
					item.Quality += 3;
				else if (item.SellIn <= 10)
					item.Quality += 2;
				else
					item.Quality += 1;
			});
		}

		private static QualityStrategy BoundQualityLow(QualityStrategy strategyToWrap)
		{
			return item =>
			{
				strategyToWrap(item);
				if (item.Quality < Program.MinQuality)
					item.Quality = Program.MinQuality;
			};
		}

		private static QualityStrategy BoundQualityHighLow(QualityStrategy strategyToWrap)
		{
			return BoundQualityLow(item =>
			{
				strategyToWrap(item);
				if (item.Quality > Program.MaxQuality)
					item.Quality = Program.MaxQuality;
			});
		}
	}

	/// <summary>
	/// Given a GildedRose item, updates the SellIn value for that
	/// item. Assumes exactly one day has passed. Does not modify and
	/// Item fields except SellIn.
	/// </summary>
	/// <param name="item">The item whose SellIn value will be updated.</param>
	public delegate void SellInStrategy(Item item);

	public static class SellInStrategies
	{
		/// <summary>
		/// Produces a SellInStrategy that changes Item SellIn by a constant
		/// amount.
		/// </summary>
		/// <param name="changeRate">The rate at which an Item's SellIn property
		/// will change, in units of days per day.</param>
		/// <returns></returns>
		public static SellInStrategy LinearChange(int changeRate)
		{
			return item => { item.SellIn += changeRate; };
		}

		/// <summary>
		/// Produces a SellInStrategy that does not change an Item's SellIn value.
		/// </summary>
		public static SellInStrategy NoChange()
		{
			return item => { };
		}
	}
}