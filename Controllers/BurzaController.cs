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

        // ✅ 1️⃣ Endpoint: `liststock`
        [HttpGet("liststock")]
        public IActionResult ListStocks()
        {
            var stocks = _stockService.GetAllStocks();
            return Ok(stocks);
        }

        // ✅ 2️⃣ Endpoint: `salestock`
        [HttpPost("salestock")]
        public IActionResult SellStock([FromBody] SellStockRequest request)
        {
            var result = _stockService.SellStock(request.Name);
            return Ok(result);
        }
    }
}
