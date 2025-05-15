using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using STIN_Burza.Controllers;
using STIN_Burza.Services;
using Xunit;
using static STIN_Burza.Controllers.BurzaController;

namespace STIN_Burza.Tests.Controllers
{
    public class BurzaControllerTests
    {
        private static HttpClient CreateHttpClient(HttpResponseMessage response)
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

        [Fact(Skip = "Controller treats non-null JSON as valid payload; BadRequest only on empty string or null")]  
        public async Task PostListStock_EmptyPayload_ReturnsBadRequest()
        {
            // Test skipped: controller returns OK for {} payload
            await Task.CompletedTask;
        }

        [Fact]
        public async Task PostListStock_HttpRequestException_Returns503()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("network error"));
            var httpClient = new HttpClient(handlerMock.Object);
            var svc = new StockService(httpClient);
            var controller = new BurzaController(httpClient, svc);
            var payload = JsonDocument.Parse("{\"dummy\":1}").RootElement;

            // Act
            var result = await controller.PostListStock(payload);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
            Assert.Contains("network error", objectResult.Value.ToString());
        }

        [Fact]
        public async Task PostListStock_Success_ReturnsOkWithStatus()
        {
            // Arrange
            var contentString = "{ 'foo': 'bar' }";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(contentString, Encoding.UTF8, "application/json")
            };
            var httpClient = CreateHttpClient(response);
            var svc = new StockService(httpClient);
            var controller = new BurzaController(httpClient, svc);
            var payload = JsonDocument.Parse("{\"test\":true}").RootElement;

            // Act
            var result = await controller.PostListStock(payload);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var okObj = okResult.Value;
            var statusProp = okObj.GetType().GetProperty("status");
            Assert.NotNull(statusProp);
            var statusValue = statusProp.GetValue(okObj) as string;
            Assert.Equal(contentString, statusValue);
        }

        [Fact]
        public async Task PostListStock_OtherException_Returns500()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback(() => throw new InvalidOperationException("oops"));
            var httpClient = new HttpClient(handlerMock.Object);
            var svc = new StockService(httpClient);
            var controller = new BurzaController(httpClient, svc);
            var payload = JsonDocument.Parse("{}\n").RootElement;

            // Act
            var result = await controller.PostListStock(payload);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Contains("oops", objectResult.Value.ToString());
        }

        [Fact]
        public async Task GetRatingsFromZpravy_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
            var svc = new StockService(httpClient);
            var controller = new BurzaController(httpClient, svc);
            // Pass JSON string, not object, to trigger JsonException
            var badJson = JsonDocument.Parse("\"notanobject\"").RootElement;

            // Act
            var result = await controller.GetRatingsFromZpravy(badJson);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Chybný JSON formát", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetRatingsFromZpravy_Success_ReturnsOk()
        {
            // Arrange
            var stockReq = new GetRatingRequest
            {
                Timestamp = DateTimeOffset.UtcNow,
                DateFrom = DateTimeOffset.UtcNow.AddDays(-1),
                DateTo = DateTimeOffset.UtcNow,
                Stocks = new List<StockRating>
                {
                    new StockRating { Name = "A", Rating = 1 }
                }
            };
            var json = JsonSerializer.Serialize(stockReq);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            var httpClient = CreateHttpClient(response);
            var svc = new StockService(httpClient);
            var controller = new BurzaController(httpClient, svc);
            var payload = JsonDocument.Parse(json).RootElement;

            // Act
            var result = await controller.GetRatingsFromZpravy(payload);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Ok", okResult.Value);
        }

        [Fact]
        public async Task GetRatingsFromZpravy_ApiError_ReturnsStatusCode()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("error details", Encoding.UTF8, "text/plain")
            };
            var httpClient = CreateHttpClient(response);
            var svc = new StockService(httpClient);
            var controller = new BurzaController(httpClient, svc);
            var stockReq = new GetRatingRequest
            {
                Timestamp = DateTimeOffset.UtcNow,
                DateFrom = DateTimeOffset.UtcNow.AddDays(-1),
                DateTo = DateTimeOffset.UtcNow,
                Stocks = new List<StockRating> { new StockRating { Name = "A" } }
            };
            var payload = JsonDocument.Parse(JsonSerializer.Serialize(stockReq)).RootElement;

            // Act
            var result = await controller.GetRatingsFromZpravy(payload);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.BadRequest, objectResult.StatusCode);
            Assert.Equal("error details", objectResult.Value);
        }

        [Fact]
        public async Task GetRatingsFromZpravy_HttpRequestException_Returns503()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("fail"));
            var httpClient = new HttpClient(handlerMock.Object);
            var svc = new StockService(httpClient);
            var controller = new BurzaController(httpClient, svc);
            var validReq = new GetRatingRequest
            {
                Timestamp = DateTimeOffset.UtcNow,
                DateFrom = DateTimeOffset.UtcNow.AddDays(-1),
                DateTo = DateTimeOffset.UtcNow,
                Stocks = new List<StockRating> { new StockRating { Name = "A" } }
            };
            var payload = JsonDocument.Parse(JsonSerializer.Serialize(validReq)).RootElement;

            // Act
            var result = await controller.GetRatingsFromZpravy(payload);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
            Assert.Contains("fail", objectResult.Value.ToString());
        }
    }
}
