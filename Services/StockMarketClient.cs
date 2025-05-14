using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using STIN_Burza.Models;
using Microsoft.Extensions.Configuration;

namespace STIN_Burza.Services
{
    public class StockMarketClient : IStockMarketClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public StockMarketClient(HttpClient http, IConfiguration config)
        {
            _http     = http;
            _apiKey   = config["StockApi:ApiKey"];
        }

        public async Task<IReadOnlyList<StockPricePoint>> GetDailyPricesAsync(string symbol, DateTime from, DateTime to)
        {
            // Např. AlphaVantage TIME_SERIES_DAILY
            var url = $"query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_apiKey}&outputsize=full";
            //var resp = await _http.GetFromJsonAsync<AlphaVantageResponse>(url);
            // z AlphaVantageResponse vyber jen body mezi from–to, převeď na List<StockPricePoint>
            // ... tady zpracujete resp.TimeSeriesDaily
            throw new NotImplementedException("Parsování z API");
        }
    }
}