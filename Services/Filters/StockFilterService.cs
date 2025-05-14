using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STIN_Burza.Models;

namespace STIN_Burza.Services.Filters
{
    public class BusinessDayHelper
    {
        public static DateTime SubtractBusinessDays(DateTime date, int days)
        {
            var result = date;
            for (int i = 0; i < days; i++)
            {
                do
                {
                    result = result.AddDays(-1);
                } while (result.DayOfWeek == DayOfWeek.Saturday || result.DayOfWeek == DayOfWeek.Sunday);
            }
            return result;
        }
    }
    public class StockFilterService : IStockFilterService
    {
        private readonly IStockMarketClient _market;

        public StockFilterService(IStockMarketClient market) =>
            _market = market;

        public async Task<IReadOnlyList<string>> FilterAlwaysDownAsync(
            IEnumerable<string> symbols, DateTime to, int workingDays)
        {
            var result = new List<string>();
            var from = BusinessDayHelper.SubtractBusinessDays(to, workingDays);
            foreach (var s in symbols)
            {
                var series = (await _market.GetDailyPricesAsync(s, from, to))
                             .OrderBy(p => p.Date)
                             .ToList();
                if (series.Count < workingDays + 1) continue;

                // zkontroluj posledních N poklesů
                bool allDown = true;
                for (int i = series.Count - workingDays; i < series.Count; i++)
                {
                    if (series[i].Close >= series[i - 1].Close)
                    { allDown = false; break; }
                }
                if (allDown) result.Add(s);
            }
            return result;
        }

        public async Task<IReadOnlyList<string>> FilterDropsCountAsync(
            IEnumerable<string> symbols, DateTime to, int workingDays, int minDrops)
        {
            var result = new List<string>();
            var from = BusinessDayHelper.SubtractBusinessDays(to, workingDays);
            foreach (var s in symbols)
            {
                var series = (await _market.GetDailyPricesAsync(s, from, to))
                             .OrderByDescending(p => p.Date)
                             .Take(workingDays + 1)
                             .OrderBy(p => p.Date)
                             .ToList();
                if (series.Count < workingDays + 1) continue;

                int drops = 0;
                for (int i = 1; i < series.Count; i++)
                    if (series[i].Close < series[i - 1].Close) drops++;

                if (drops > minDrops) result.Add(s);
            }
            return result;
        }
    }
}