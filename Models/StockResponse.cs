using System;
using System.Collections.Generic;

namespace STIN_Burza.Models
{
    public class StockResponse
    {
        public DateTime Timestamp { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public List<StockData> Stocks { get; set; } = new();

    }
}
