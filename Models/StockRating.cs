using System;

namespace STIN_Burza.Models
{
    public class StockRating
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public int? Rating { get; set; }  // Nullable rating
        public bool? Sell { get; set; }  // Nullable sell
    }
}
