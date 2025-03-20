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

        // 1. Z�sk�n� historick�ch dat + filtrov�n�
        [HttpPost("filter")]
        public async Task<IActionResult> GetFilteredStocks([FromBody] StockRequest request)
        {
            var stockResponse = await _stockService.GetFilteredStocksAsync(request);
            return Ok(stockResponse);
        }

        // 2. Odesl�n� po�adavku do modulu Zpr�vy
        [HttpPost("recommendation")]
        public async Task<IActionResult> SendToZpravyModule([FromBody] StockRequest request)
        {
            var response = await _stockService.SendToZpravyModuleAsync(request);
            return Ok(response);
        }
    }
}
