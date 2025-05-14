using Microsoft.Extensions.Caching.Memory;
using STIN_Burza.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STIN_Burza.Services;

namespace STIN_Burza.Services
{
    public class StockService
    {
        private readonly Dictionary<string, StockRating> _stocks = new()
        {
            { "MSFT", new StockRating { Name = "Microsoft", Date = DateTime.UtcNow, Rating = null, Sell = null } },
            { "GOOGL", new StockRating { Name = "Google", Date = DateTime.UtcNow, Rating = null, Sell = null } }
        };

        private readonly ZpravyApiClient _zpravyApiClient;

        public StockService(ZpravyApiClient zpravyApiClient)
        {
            _zpravyApiClient = zpravyApiClient;
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

        public async Task<StockResponse> GetRatingsFromZpravyAsync(DateTime dateFrom, DateTime dateTo)
        {
            var request = new StockRequest
            {
                Timestamp = DateTime.UtcNow,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Stocks = GetAllStocks()
                    .Select(s => new StockData { Name = s.Name, Rating = null, Sell = null })
                    .ToList()
            };
            return await _zpravyApiClient.GetRatingAsync(request);
        }

        public async Task SendSellRecommendationsAsync(DateTime dateFrom, DateTime dateTo, int threshold)
        {
            // nejprve stáhneme ohodnocení
            var ratedResponse = await GetRatingsFromZpravyAsync(dateFrom, dateTo);
            var recRequest = new StockRequest
            {
                Timestamp = DateTime.UtcNow,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Stocks = ratedResponse.Stocks
                    .Select(s => new StockData
                    {
                        Name = s.Name,
                        Rating = null,
                        Sell = s.Rating.HasValue && s.Rating.Value > threshold
                    })
                    .ToList()
            };
            await _zpravyApiClient.SendRecommendationsAsync(recRequest);
        }
    }
}
