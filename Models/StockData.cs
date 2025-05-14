using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

namespace STIN_Burza.Models
{
    public class StockData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        [JsonPropertyName("sell")]
        public bool? Sell { get; set; }
    }
}
