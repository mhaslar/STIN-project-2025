using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using STIN_Burza.Models;

namespace STIN_Burza.Services.Filters
{
    public interface IStockFilterService
    {
        /// <summary>
        /// Vrátí jen ty symboly, které za poslední N pracovních dnů každý den klesaly.
        /// </summary>
        Task<IReadOnlyList<string>> FilterAlwaysDownAsync(
            IEnumerable<string> symbols, DateTime to, int workingDays);
        
        /// <summary>
        /// Vrátí jen ty symboly, které za posledních M pracovních dnů měly více než K poklesů.
        /// </summary>
        Task<IReadOnlyList<string>> FilterDropsCountAsync(
            IEnumerable<string> symbols, DateTime to, int workingDays, int minDrops);
    }
}