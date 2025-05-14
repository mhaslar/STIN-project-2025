using Microsoft.AspNetCore.Mvc;
using STIN_Burza.Models;
using STIN_Burza.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
            // Získání JSON payloadu jako string.
            // Pokud je 'payload' typu JsonElement (System.Text.Json) nebo JObject (Newtonsoft.Json)
            // z deserializace [FromBody], pak payload.ToString() by měl vrátit JSON řetězec.
            // Pokud je 'payload' jiný typ C# objektu, payload.ToString() nemusí vrátit validní JSON
            // a bylo by potřeba explicitní serializace (např. System.Text.Json.JsonSerializer.Serialize(payload)).
            // Vycházíme z předpokladu, že payload.ToString() poskytne očekávaný JSON, jak naznačoval původní kód.
            string jsonPayloadToSend = payload.ToString() ?? "{}";

            Console.WriteLine($"Volání externího API burzy. Přijatý payload (po ToString()): {jsonPayloadToSend} (BurzaController.cs)");
            System.Diagnostics.Debug.WriteLine($"Volání externího API burzy. Přijatý payload (po ToString()): {jsonPayloadToSend} (BurzaController.cs)");

            if (string.IsNullOrEmpty(jsonPayloadToSend))
            {
                System.Diagnostics.Debug.WriteLine("Payload pro externí API je prázdný nebo null po ToString(). (BurzaController.cs)");
                return BadRequest("Tělo požadavku (payload) je prázdné nebo se nepodařilo ho převést na řetězec.");
            }

            // Cílová URL adresa pro externí API (z curl příkazu)
            string externalApiUrl = "https://novinky.zumepro.cz:8000/api/liststock";

            // Přihlašovací údaje pro Basic Auth
            string userName = "burza";
            // Toto heslo by mělo být ideálně uloženo bezpečně, např. v konfiguraci (appsettings.json) a načítáno přes IConfiguration
            string password = "velmitajneheslo";

            // Vytvoření HttpRequestMessage
            var request = new HttpRequestMessage(HttpMethod.Post, externalApiUrl);

            // Zakázání hlavičky "Expect: 100-continue"
            // Některé servery mohou mít problém se správným zpracováním této hlavičky
            // a může to vést k problémům s rozpoznáním Content-Type.
            request.Headers.ExpectContinue = false;

            // Nastavení obsahu požadavku (JSON payload)
            request.Content = new StringContent(jsonPayloadToSend, Encoding.UTF8, "application/json");

            // Vytvoření a přidání Authorization hlavičky pro Basic Auth
            string credentials = $"{userName}:{password}";
            byte[] credentialsBytes = Encoding.UTF8.GetBytes(credentials);
            string base64Credentials = Convert.ToBase64String(credentialsBytes);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

            Console.WriteLine($"Odesílám požadavek na externí burzu: Method={request.Method}, Uri={request.RequestUri}, ExpectContinue={request.Headers.ExpectContinue} (BurzaController.cs)");
            System.Diagnostics.Debug.WriteLine($"Odesílám požadavek na externí burzu: Method={request.Method}, Uri={request.RequestUri}, ExpectContinue={request.Headers.ExpectContinue} (BurzaController.cs)");
            Console.WriteLine($"JSON Payload odesílaný na externí burzu: {jsonPayloadToSend} (BurzaController.cs)");
            System.Diagnostics.Debug.WriteLine($"JSON Payload odesílaný na externí burzu: {jsonPayloadToSend}");

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Odpověď z externí burzy: StatusCode={(int)response.StatusCode} ({response.ReasonPhrase}), Content='{responseContent}' (BurzaController.cs)");
                System.Diagnostics.Debug.WriteLine($"Odpověď z externí burzy: StatusCode={(int)response.StatusCode} ({response.ReasonPhrase}), Content='{responseContent}' (BurzaController.cs)");

                return new ContentResult
                {
                    Content = responseContent,
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", // Zachováme ContentType z odpovědi
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException při volání externí burzy: {ex.Message}{(ex.InnerException != null ? " | InnerException: " + ex.InnerException.Message : "")} (BurzaController.cs)");
                System.Diagnostics.Debug.WriteLine($"HttpRequestException při volání externí burzy: {ex.Message}{(ex.InnerException != null ? " | InnerException: " + ex.InnerException.Message : "")} (BurzaController.cs)");
                // V produkčním prostředí by zde mělo být robustnější logování.
                return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Chyba při komunikaci s externí službou burzy: {ex.Message}");
            }
            catch (Exception ex) // Zachycení jakýchkoliv dalších neočekávaných chyb
            {
                Console.WriteLine($"Neočekávaná chyba při volání externí burzy: {ex.Message} (BurzaController.cs)");
                System.Diagnostics.Debug.WriteLine($"Neočekávaná chyba při volání externí burzy: {ex.Message} (BurzaController.cs)");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Interní chyba serveru při zpracování požadavku na burzu: {ex.Message}");
            }
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
            var response = await _stockService.GetRatingsFromZpravyAsync(request);
            if (response == null)
                return StatusCode(500, "Nepodařilo se načíst data z externího API.");
            return Ok(response);
        }
    }
}
