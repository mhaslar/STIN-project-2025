using Microsoft.AspNetCore.Mvc;
using STIN_Burza.Models;
using STIN_Burza.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STIN_Burza.Controllers
{
    [Route("api/burza")]
    [ApiController]
    public class BurzaController : ControllerBase
    {
        private readonly StockService _stockService;

        public BurzaController(StockService stockService)
        {
            _stockService = stockService;
        }

        // Endpoint: `liststock`
        [HttpGet("liststock")]
        public async Task<IActionResult> ListStock(
    [FromQuery] DateTime dateFrom,
    [FromQuery] DateTime dateTo,
    [FromQuery] bool filter3Down = true,
    [FromQuery] int? minDropsIn5 = null)
        {
            var favs = await _db.GetFavoritesAsync();
            List<string> symbols = new(favs);

            if (filter3Down)
                symbols = (await _filter.FilterAlwaysDownAsync(symbols, dateTo, 3)).ToList();
            if (minDropsIn5.HasValue)
                symbols = (await _filter.FilterDropsCountAsync(symbols, dateTo, 5, minDropsIn5.Value)).ToList();

            return Ok(symbols);
        }

        [HttpPost("trigger")]
        public IActionResult TriggerNow()
        {
            _fetcher.TriggerManualRun();
            return Accepted();
        }

        //  Endpoint: `salestock`
        [HttpPost("salestock")]
        public IActionResult SellStock([FromBody] SellStockRequest request)
        {
            var result = _stockService.SellStock(request.Name);
            return Ok(result);
        }

        // Endpoint: `getrating` (napojení na vzdálené zprávy API)
        [HttpPost("getrating")]
        public async Task<IActionResult> GetRatingsFromZpravy([FromBody] StockRequest request)
        {
            var response = await _stockService.GetRatingsFromZpravyAsync(request.DateFrom, request.DateTo);
            if (response == null)
                return StatusCode(500, "Nepodařilo se načíst data z externího API.");
            return Ok(response);
        }
    }
}
