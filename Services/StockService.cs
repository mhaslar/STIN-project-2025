using Microsoft.Extensions.Caching.Memory;
using STIN_Burza.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace STIN_Burza.Services
{
	public class StockService
	{
		private readonly HttpClient _httpClient;
		private readonly IMemoryCache _cache;
		private readonly string _apiKey = "YOUR_API_KEY";

		public StockService(HttpClient httpClient, IMemoryCache cache)
		{
			_httpClient = httpClient;
			_cache = cache;
		}

		// 1. Získání dat a aplikace filtru
		public async Task<StockResponse> GetFilteredStocksAsync(StockRequest request)
		{
			var response = new StockResponse
			{
				Timestamp = DateTime.UtcNow,
				DateFrom = request.DateFrom,
				DateTo = request.DateTo,
				Stocks = new List<StockDataEntry>()
			};

			foreach (var stock in request.Stocks)
			{
				var stockData = await GetStockDataAsync(stock.Name);
				if (stockData == null) continue;

				bool declineLast3Days = CheckDeclineLast3Days(stockData);
				bool moreThanTwoDeclines = CheckMoreThanTwoDeclinesLast5Days(stockData);

				response.Stocks.Add(new StockDataEntry
				{
					Name = stock.Name,
					Sell = declineLast3Days || moreThanTwoDeclines
				});
			}

			return response;
		}

		// 2. Odeslání doporuèení do modulu Zprávy
		public async Task<StockResponse> SendToZpravyModuleAsync(StockRequest request)
		{
			var response = await _httpClient.PostAsJsonAsync("http://zpravy-module/api/zpravy/analyze", request);
			return await response.Content.ReadFromJsonAsync<StockResponse>();
		}

		// Získání historických dat z Alpha Vantage
		private async Task<StockData> GetStockDataAsync(string symbol)
		{
			string cacheKey = $"stock:{symbol}";
			if (_cache.TryGetValue(cacheKey, out StockData cachedData))
				return cachedData;

			var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_apiKey}";
			var response = await _httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode) return null;

			var json = await response.Content.ReadAsStringAsync();
			var stockData = JsonSerializer.Deserialize<StockData>(json);

			_cache.Set(cacheKey, stockData, TimeSpan.FromMinutes(5));
			return stockData;
		}

		// Kontrola poklesu za poslední 3 dny
		private bool CheckDeclineLast3Days(StockData stockData)
		{
			var dates = new List<string>(stockData.DailyTimeSeries.Keys);
			dates.Sort();
			if (dates.Count < 3) return false;

			for (int i = dates.Count - 3; i < dates.Count; i++)
			{
				if (float.Parse(stockData.DailyTimeSeries[dates[i]]["4. close"]) >=
					float.Parse(stockData.DailyTimeSeries[dates[i - 1]]["4. close"]))
				{
					return false;
				}
			}
			return true;
		}

		// Kontrola více než dvou poklesù za posledních 5 dní
		private bool CheckMoreThanTwoDeclinesLast5Days(StockData stockData)
		{
			var dates = new List<string>(stockData.DailyTimeSeries.Keys);
			dates.Sort();
			if (dates.Count < 5) return false;

			int declineCount = 0;
			for (int i = dates.Count - 5; i < dates.Count; i++)
			{
				if (float.Parse(stockData.DailyTimeSeries[dates[i]]["4. close"]) <
					float.Parse(stockData.DailyTimeSeries[dates[i - 1]]["4. close"]))
				{
					declineCount++;
				}
			}
			return declineCount > 2;
		}
	}
}
