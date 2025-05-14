// Services/ZpravyApiClient.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using STIN_Burza.Models;

public class ZpravyApiClient
{
    private readonly HttpClient _http;

    public ZpravyApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// První i druhá výměna: pošle seznam akcií (ratings null/sell null)
    /// a vrátí seznam s naplněným ratingem.
    /// </summary>
    public async Task<StockResponse> GetRatingAsync(StockRequest request)
    {
        var resp = await _http.PostAsJsonAsync("getRating", request);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StockResponse>();
    }

    /// <summary>
    /// Třetí výměna: pošle seznam akcií s flagem sell=true/false,
    /// externí API nemusí vracet žádná data – postačí EnsureSuccess.
    /// </summary>
    public async Task SendRecommendationsAsync(StockRequest recommendations)
    {
        var resp = await _http.PostAsJsonAsync("getRating", recommendations);
        resp.EnsureSuccessStatusCode();
    }
}