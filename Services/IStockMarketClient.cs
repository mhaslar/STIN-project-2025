using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using STIN_Burza.Models;

namespace STIN_Burza.Services
{
    /// <summary>
    /// Stáhne list cen akcie od–do (close price).
    /// </summary>
    public interface IStockMarketClient
    {
        Task<IReadOnlyList<StockPricePoint>> GetDailyPricesAsync(string symbol, DateTime from, DateTime to);
    }
}