using Microsoft.Extensions.Caching.Memory;
using STIN_Burza.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace STIN_Burza.Services
{
    public class StockService
    {
        private readonly Dictionary<string, StockRating> _stocks = new()
        {
            { "MSFT", new StockRating { Name = "Microsoft", Date = DateTime.UtcNow, Rating = null, Sell = null } },
            { "GOOGL", new StockRating { Name = "Google", Date = DateTime.UtcNow, Rating = null, Sell = null } }
        };

        private readonly HttpClient _httpClient;

        public StockService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        //Vrátí seznam všech akcií pouze s `name`, `rating` a `sell` jsou `null`
        public List<StockRating> GetAllStocks()
        {
            var result = new List<StockRating>();

            foreach (var stock in _stocks)
            {
                result.Add(new StockRating
                {
                    Name = stock.Key,
                    Date = stock.Value.Date,
                    Rating = null,
                    Sell = null
                });
            }

            return result;
        }

        //Prodej akcie: nastaví `sell`, ale `rating` zůstává `null`
        public StockRating? SellStock(string name)
        {
            if (_stocks.ContainsKey(name))
            {
                _stocks[name].Sell = true;
                return new StockRating
                {
                    Name = name,
                    Date = _stocks[name].Date,
                    Rating = null,
                    Sell = true
                };
            }

            return null;
        }

        // Napojení na /api/getrating přes Basic Auth
        public async Task<StockResponse?> GetRatingsFromZpravyAsync(StockRequest request)
        {
            var apiUrl = "https://novinky.zumepro.cz:8000/api/getrating";
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("burza:velmitajneheslo"));

            using var message = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            message.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(message);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<StockResponse>();
        }
    }
}
