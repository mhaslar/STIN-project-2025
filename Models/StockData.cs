using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace STIN_Burza.Models
{
    public class StockData
    {
        [JsonPropertyName("Time Series (Daily)")]
        public Dictionary<string, Dictionary<string, string>> DailyTimeSeries { get; set; } = new();
    }
}
