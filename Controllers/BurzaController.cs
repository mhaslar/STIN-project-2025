using Microsoft.AspNetCore.Mvc;
using STIN_Burza.Models;
using STIN_Burza.Services;
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

        // 1. Získání historických dat + filtrování
        [HttpPost("filter")]
        public async Task<IActionResult> GetFilteredStocks([FromBody] StockRequest request)
        {
            var stockResponse = await _stockService.GetFilteredStocksAsync(request);
            return Ok(stockResponse);
        }

        // 2. Odeslání požadavku do modulu Zprávy
        [HttpPost("recommendation")]
        public async Task<IActionResult> SendToZpravyModule([FromBody] StockRequest request)
        {
            var response = await _stockService.SendToZpravyModuleAsync(request);
            return Ok(response);
        }
    }
}
