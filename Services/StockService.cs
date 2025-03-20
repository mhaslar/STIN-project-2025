using Microsoft.Extensions.Caching.Memory;
using STIN_Burza.Models;
using System;
using System.Collections.Generic;
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

        // ✅ Vrátí seznam všech akcií pouze s `name`, `rating` a `sell` jsou `null`
        public List<StockRating> GetAllStocks()
        {
            var result = new List<StockRating>();

            foreach (var stock in _stocks.Values)
            {
                result.Add(new StockRating
                {
                    Name = stock.Name,
                    Date = stock.Date,
                    Rating = null,
                    Sell = null
                });
            }

            return result;
        }

        // ✅ Prodej akcie: nastaví `sell`, ale `rating` zůstává `null`
        public StockRating SellStock(string name)
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
    }
}
