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
using System.Text.Json.Serialization;

namespace STIN_Burza.Controllers
{
    [Route("api/burza")]
    [ApiController]
    public class BurzaController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly StockService _stockService;
        private static int _threshold;

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

        [HttpPost("getRating")]
        public async Task<IActionResult> GetRatingsFromZpravy([FromBody] JsonElement request)
        {
            // 1) Deserializace payloadu
            var json = request.GetRawText();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            GetRatingRequest ratingReq;
            try
            {
                ratingReq = JsonSerializer.Deserialize<GetRatingRequest>(json, options)
                            ?? throw new JsonException("Nedefinovaný objekt.");
            }
            catch (JsonException ex)
            {
                return BadRequest($"Chybný JSON formát: {ex.Message}");
            }

            // 2) Pro každý stock nastavíme sell = true, pokud rating < 0, jinak false
            foreach (var s in ratingReq.Stocks)
            {
                if (s.Rating.HasValue && s.Rating.Value > _threshold)
                {
                    s.Sell = 1;
                }
                else
                {
                    s.Sell = 0;
                }
            }

            // 2) Připravíme nový payload se správným formátem timestampů a nulovým ratingem
            var sellPayload = new
            {
                timestamp = ratingReq.Timestamp
                             .ToUniversalTime()
                             .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                date_from = ratingReq.DateFrom
                             .ToUniversalTime()
                             .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                date_to = ratingReq.DateTo
                             .ToUniversalTime()
                             .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                stocks = ratingReq.Stocks.Select(s => new
                {
                    name = s.Name,
                    rating = (decimal?)null,
                    sell = s.Sell
                }).ToList()
            };

            // 3) Serializace čistého payloadu
            var resultJson = JsonSerializer.Serialize(sellPayload);
            // pro debug:
            Console.WriteLine("Odesílám na salestock JSON: " + resultJson);

            // 4) Zbytek zůstává stejný
            var externalUrl = "https://novinky.zumepro.cz:8000/api/salestock";  // DVOJITÉ "l"
            var userName = "burza";
            var password = "velmitajneheslo";
            var httpReq = new HttpRequestMessage(HttpMethod.Post, externalUrl)
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

            var content = new StringContent(resultJson, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpReq.Content = content;
            httpReq.Headers.Accept.Clear();
            httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await _httpClient.SendAsync(httpReq);
                var respContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok("Ok");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, respContent);
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                                   $"Chyba při volání externího API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                                   $"Neočekávaná chyba: {ex.Message}");
            }
        }

        [HttpPost("setThreshold")]
        public IActionResult SetThreshold([FromBody] JsonElement request)
        {
            if (!request.TryGetProperty("threshold", out JsonElement thEl))
                return BadRequest("Missing 'threshold' in body.");

            if (!thEl.TryGetInt32(out int threshold))
                return BadRequest("'threshold' must be an integer.");

            _threshold = threshold;
            return Ok();
        }

        [HttpGet("getThreshold")]
        public IActionResult GetThreshold()
        {
            return Ok(new { threshold = _threshold });
        }
    }

    public class StockRating
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; set; }

        [JsonPropertyName("sell")]
        public int? Sell { get; set; }
    }

    public class GetRatingRequest
    {
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("date_from")]
        public DateTimeOffset DateFrom { get; set; }

        [JsonPropertyName("date_to")]
        public DateTimeOffset DateTo { get; set; }

        [JsonPropertyName("stocks")]
        public List<StockRating> Stocks { get; set; }
    }
}