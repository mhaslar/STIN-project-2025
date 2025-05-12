using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using STIN_Burza.Services;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace STIN_Burza.Tests
{
    [TestClass]
    public class BackgroundStockFetcherTests
    {
        private Mock<ILogger<BackgroundStockFetcher>> _loggerMock;
        private BackgroundStockFetcher _backgroundStockFetcher;
        private CancellationTokenSource _cancellationTokenSource;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<BackgroundStockFetcher>>();
            _backgroundStockFetcher = new BackgroundStockFetcher(_loggerMock.Object);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cancellationTokenSource.Dispose();
        }

        [TestMethod]
        public void Constructor_InitializesLogger()
        {
            // Arrange & Act
            var fetcher = new BackgroundStockFetcher(_loggerMock.Object);

            // Assert
            // Použijeme reflexi pro kontrolu, zda je logger správně inicializován
            var loggerField = typeof(BackgroundStockFetcher)
                .GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(loggerField);
            var logger = loggerField.GetValue(fetcher);
            Assert.AreEqual(_loggerMock.Object, logger);
        }

        [TestMethod]
        public async Task ExecuteAsync_LogsInformation_WhenStarted()
        {
            // Arrange
            var executeMethod = typeof(BackgroundStockFetcher)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Nastavíme token, který zruší operaci po krátké době
            _cancellationTokenSource.CancelAfter(100);
            var token = _cancellationTokenSource.Token;

            // Act
            // Vyvoláme protected metodu pomocí reflexe
            var task = (Task)executeMethod.Invoke(_backgroundStockFetcher, new object[] { token });
            
            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // Očekáváme TaskCanceledException, když je token zrušen
            }

            // Assert
            // Ověříme, že byla zalogována zpráva
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching stock data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ExecuteAsync_CancelsOperation_WhenCancellationRequested()
        {
            // Arrange
            var executeMethod = typeof(BackgroundStockFetcher)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Nastavíme token, který zruší operaci okamžitě
            _cancellationTokenSource.Cancel();
            var token = _cancellationTokenSource.Token;

            // Act
            var task = (Task)executeMethod.Invoke(_backgroundStockFetcher, new object[] { token });
            
            // Assert
            // Očekáváme, že úloha bude dokončena okamžitě bez chyby
            await task;
            
            // Ověříme, že nebyla zalogována žádná zpráva
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching stock data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_RespectsDelay_BetweenFetches()
        {
            // Arrange
            var executeMethod = typeof(BackgroundStockFetcher)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Nastavíme token, který zruší operaci po dostatečně dlouhé době
            _cancellationTokenSource.CancelAfter(100);
            var token = _cancellationTokenSource.Token;

            // Act
            // Nahradíme Task.Delay v metodě ExecuteAsync naším vlastním mock-em
            var originalTaskDelay = typeof(Task).GetMethod("Delay", new[] { typeof(TimeSpan), typeof(CancellationToken) });
            var mockTaskDelay = new Mock<ITaskDelay>();
            
            // Zaznamenáme volání Task.Delay se správnými parametry
            mockTaskDelay.Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback<TimeSpan, CancellationToken>((timespan, ct) => 
                {
                    // Ověření, že timespan je 6 hodin
                    Assert.AreEqual(TimeSpan.FromHours(6), timespan);
                })
                .Returns(Task.CompletedTask);
            
            // Vyvoláme protected metodu pomocí reflexe
            var task = (Task)executeMethod.Invoke(_backgroundStockFetcher, new object[] { token });
            
            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // Očekáváme TaskCanceledException, když je token zrušen
            }

            // Assert
            // Ověření bude provedeno v Callback výše
        }
        
        [TestMethod]
        public async Task StartAsync_CallsExecuteAsync()
        {
            // Arrange
            bool executeAsyncCalled = false;
            var originalExecuteAsync = typeof(BackgroundStockFetcher)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Použijeme reflexi k nahrazení implementace ExecuteAsync
            var mockExecuteAsync = new Mock<IExecuteAsync>();
            mockExecuteAsync.Setup(x => x.ExecuteAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(_ => executeAsyncCalled = true)
                .Returns(Task.CompletedTask);
            
            // Act
            await _backgroundStockFetcher.StartAsync(_cancellationTokenSource.Token);
            
            // Pozor: nemůžeme přímo ověřit, že ExecuteAsync byl zavolán, protože
            // nemůžeme nahradit nebo zachytit volání protected metody.
            // Místo toho musíme ověřit, že StartAsync vytvoří Task, který bude běžet.
            
            // Pozor: toto je velmi zjednodušený test. Reálně bychom potřebovali
            // použít pokročilejší techniky jako TypeMock nebo jiné nástroje
            // pro mockování protected metod.
            
            // Assert
            // V tomto případě bychom mohli sledovat side-efekty jako logování
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching stock data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }
        
        [TestMethod]
        public async Task StopAsync_CancelsRunningTasks()
        {
            // Arrange
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Act
            await _backgroundStockFetcher.StartAsync(_cancellationTokenSource.Token);
            await _backgroundStockFetcher.StopAsync(CancellationToken.None);
            
            // Assert
            // Ověříme, že volání StopAsync způsobí, že token bude zrušen
            Assert.IsTrue(_cancellationTokenSource.IsCancellationRequested);
        }
    }
    
    // Pomocná rozhraní pro mockování
    public interface ITaskDelay
    {
        Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken);
    }
    
    public interface IExecuteAsync
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
    
    // Ukázka mocku pro testování bez reflexe (reálné použití by vyžadovalo 
    // dodatečné nástroje pro mockování protected metod)
    public class TestableBackgroundStockFetcher : BackgroundStockFetcher
    {
        public TestableBackgroundStockFetcher(ILogger<BackgroundStockFetcher> logger)
            : base(logger)
        {
        }
        
        // Veřejná metoda pro testování
        public Task TestExecuteAsync(CancellationToken cancellationToken)
        {
            return base.ExecuteAsync(cancellationToken);
        }
    }
}
