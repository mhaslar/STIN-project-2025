using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using STIN_Burza.Models;
using STIN_Burza.Services;
using Xunit;

namespace STIN_Burza.Tests.Services
{
    public class StockServiceTests
    {
        private HttpClient CreateHttpClient(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response)
               .Verifiable();

            return new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://fake-api/")
            };
        }

        [Fact]
        public void GetAllStocks_ReturnsDefaultStocks()
        {
            // Arrange
            var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new StockService(httpClient);

            // Act
            var all = service.GetAllStocks();

            // Assert
            Assert.Collection(all,
                item =>
                {
                    Assert.Equal("MSFT", item.Name);
                    Assert.Null(item.Rating);
                    Assert.Null(item.Sell);
                },
                item =>
                {
                    Assert.Equal("GOOGL", item.Name);
                    Assert.Null(item.Rating);
                    Assert.Null(item.Sell);
                }
            );
        }

        [Fact]
        public void SellStock_Existing_ReturnsStockWithSellTrue()
        {
            // Arrange
            var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new StockService(httpClient);

            // Act
            var result = service.SellStock("MSFT");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MSFT", result.Name);
            Assert.True(result.Sell);
            Assert.Null(result.Rating);
        }

        [Fact]
        public void SellStock_NonExisting_ReturnsNull()
        {
            // Arrange
            var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new StockService(httpClient);

            // Act
            var result = service.SellStock("UNKNOWN");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRatingsFromZpravyAsync_Success_ReturnsResponse()
        {
            // Arrange
            var sampleResponse = new StockResponse
            {
                Timestamp = DateTime.UtcNow,
                DateFrom = DateTime.UtcNow.AddDays(-1),
                DateTo = DateTime.UtcNow,
                Stocks = new List<StockDataEntry>
                {
                    new StockDataEntry { Name = "A", Rating = 1, Sell = true }
                }
            };
            var json = JsonSerializer.Serialize(sampleResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var httpClient = CreateHttpClient(httpResponse);
            var service = new StockService(httpClient);
            var request = new StockRequest
            {
                Timestamp = DateTimeOffset.UtcNow.DateTime,
                DateFrom = DateTimeOffset.UtcNow.AddDays(-1).DateTime,
                DateTo = DateTimeOffset.UtcNow.DateTime,
                Stocks = new List<StockDataEntry>
                {
                    new StockDataEntry { Name = "A" }
                }
            };

            // Act
            var result = await service.GetRatingsFromZpravyAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sampleResponse.Stocks.Count, result.Stocks.Count);
            Assert.Equal(sampleResponse.Stocks[0].Name, result.Stocks[0].Name);
        }

        [Fact]
        public async Task GetRatingsFromZpravyAsync_Failure_ReturnsNull()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var httpClient = CreateHttpClient(httpResponse);
            var service = new StockService(httpClient);
            var request = new StockRequest();

            // Act
            var result = await service.GetRatingsFromZpravyAsync(request);

            // Assert
            Assert.Null(result);
        }
    }
}
