using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace STIN_Burza.Services
{
    public class BackgroundStockFetcher : BackgroundService
    {
        private readonly ILogger<BackgroundStockFetcher> _logger;

        public BackgroundStockFetcher(ILogger<BackgroundStockFetcher> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fetching stock data...");
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}
