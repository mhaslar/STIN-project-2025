using Microsoft.AspNetCore.Mvc;
using STIN_Burza.Models;
using STIN_Burza.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace STIN_Burza.Controllers
{
    [Route("api/burza")]
    [ApiController]
    public class BurzaController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly StockService _stockService;

        public BurzaController(HttpClient httpClient, StockService stockService)
        {
            _httpClient = httpClient;
            _stockService = stockService;
        }

        // Endpoint: `liststock`
        [HttpPost("liststock")]
        public async Task<IActionResult> PostListStock([FromBody] JsonElement payload)
        {
            // 1) Získání JSON payloadu jako string
            string jsonPayloadToSend = payload.ToString() ?? "{}";
            Console.WriteLine($"Volání externího API burzy. Payload: {jsonPayloadToSend}");
            System.Diagnostics.Debug.WriteLine($"Volání externího API burzy. Payload: {jsonPayloadToSend}");

            if (string.IsNullOrEmpty(jsonPayloadToSend))
            {
                System.Diagnostics.Debug.WriteLine("Prázdný payload.");
                return BadRequest("Tělo požadavku je prázdné.");
            }

            // 2) Příprava requestu na externí API
            string externalApiUrl = "https://novinky.zumepro.cz:8000/api/liststock";
            string userName = "burza";
            string password = "velmitajneheslo";

            var request = new HttpRequestMessage(HttpMethod.Post, externalApiUrl)
            {
                Headers =
                {
                    ExpectContinue = false,
                    Authorization = new AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"))
                    )
                }
            };

            // Explicitní Content-Type application/json (bez charset)
            var content = new StringContent(jsonPayloadToSend, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;

            // Přidat Accept: application/json
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine($"Odesílám na externí burzu: {request.Method} {request.RequestUri}");
            System.Diagnostics.Debug.WriteLine($"Payload: {jsonPayloadToSend}");

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Odpověď z burzy: {(int)response.StatusCode} {response.ReasonPhrase}: {responseContent}");
                System.Diagnostics.Debug.WriteLine($"Odpověď z burzy: {responseContent}");

                // === ZDE VRACÍME PLATNÝ JSON MÍSTO POUHÉHO ŘETĚZCE ===
                // Obalíme odpověď do objektu, aby klient mohl volat resp.json() bez chyby
                return Ok(new { status = responseContent });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"HttpRequestException: {ex.Message}");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    $"Chyba při komunikaci s externí službou burzy: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Neočekávaná chyba: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Neočekávaná chyba: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Interní chyba serveru: {ex.Message}");
            }
        }

        // Endpoint: `salestock`
        [HttpPost("salestock")]
        public IActionResult SellStock([FromBody] SellStockRequest request)
        {
            var result = _stockService.SellStock(request.Name);
            return Ok(result);
        }

        // Endpoint: `getrating`
        [HttpPost("getrating")]
        public async Task<IActionResult> GetRatingsFromZpravy([FromBody] StockRequest request)
        {
            var response = await _stockService.GetRatingsFromZpravyAsync(request);
            if (response == null)
                return StatusCode(500, "Nepodařilo se načíst data z externího API.");
            return Ok(response);
        }
    }
}
