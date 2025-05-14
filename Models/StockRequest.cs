using System;
using System.Collections.Generic;

namespace STIN_Burza.Models
{
	public class StockRequest
	{
		public DateTime Timestamp { get; set; }
		public DateTime DateFrom { get; set; }
		public DateTime DateTo { get; set; }
		public List<StockData> Stocks { get; set; } = new();

		// Removed unnecessary implicit operator to avoid converting StockRequest to itself
    }
}
