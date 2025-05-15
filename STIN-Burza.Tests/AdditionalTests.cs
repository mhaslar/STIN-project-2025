using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using STIN_Burza.Services;
using STIN_project_2025.Controllers;
using Xunit;

namespace STIN_Burza.Tests
{
    public class BackgroundStockFetcherTests
    {
        [Fact]
        public async Task ExecuteAsync_LogsFetchingMessage_AndStopsOnCancellation()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<BackgroundStockFetcher>>();
            var fetcher = new BackgroundStockFetcher(mockLogger.Object);
            var cts = new CancellationTokenSource();
            // Cancel after a short delay to allow one iteration
            _ = Task.Run(async () => { await Task.Delay(10); cts.Cancel(); });

            // Act
            await fetcher.StartAsync(cts.Token);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching stock data...")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.AtLeastOnce);
        }
    }

    public class ErrorControllerTests
    {
        [Fact]
        public void HandleErrorCode_404_ReturnsNotFoundView()
        {
            // Arrange
            var controller = new ErrorController();

            // Act
            var result = controller.HandleErrorCode(404) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("~/Pages/Error/NotFound.cshtml", result.ViewName);
        }

        [Fact]
        public void HandleErrorCode_Other_ReturnsGenericErrorView()
        {
            // Arrange
            var controller = new ErrorController();

            // Act
            var result = controller.HandleErrorCode(500) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("~/Pages/Error/GenericError.cshtml", result.ViewName);
        }
    }
}
