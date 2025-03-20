using System;
using System.Collections.Generic;

namespace STIN_Burza.Models
{
	public class StockRequest
	{
		public DateTime Timestamp { get; set; }
		public DateTime DateFrom { get; set; }
		public DateTime DateTo { get; set; }
		public List<StockDataEntry> Stocks { get; set; } = new();

	}

	public class StockDataEntry
	{
		public string Name { get; set; }
		public int? Rating { get; set; }
		public bool? Sell { get; set; }
	}
}
