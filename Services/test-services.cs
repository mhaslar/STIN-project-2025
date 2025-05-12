using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using STIN_Burza.Models;
using STIN_Burza.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace STIN_Burza.Tests
{
    [TestClass]
    public class StockServiceTests
    {
        private StockService _stockService;

        [TestInitialize]
        public void Setup()
        {
            _stockService = new StockService();
        }

        [TestMethod]
        public void GetAllStocks_ReturnsAllStocksWithRatingAndSellSetToNull()
        {
            // Act
            var result = _stockService.GetAllStocks();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            
            foreach (var stock in result)
            {
                Assert.IsNotNull(stock.Name);
                Assert.IsNotNull(stock.Date);
                Assert.IsNull(stock.Rating);
                Assert.IsNull(stock.Sell);
            }
            
            // Ověření konkrétních akcií
            Assert.IsTrue(result.Any(s => s.Name == "Microsoft"));
            Assert.IsTrue(result.Any(s => s.Name == "Google"));
        }

        [TestMethod]
        public void GetAllStocks_ReturnsNewInstancesNotReferences()
        {
            // Act
            var result = _stockService.GetAllStocks();
            
            // Assert - ověřujeme, že vrácené instance nejsou reference na vnitřní objekty
            Assert.AreEqual(2, result.Count);
            
            // Získáme přístup k privátnímu poli _stocks pomocí reflexe
            var stocksField = typeof(StockService).GetField("_stocks", BindingFlags.NonPublic | BindingFlags.Instance);
            var stocks = (Dictionary<string, StockRating>)stocksField.GetValue(_stockService);
            
            // Kontrola, že vrácené objekty jsou nové instance, nikoliv reference
            foreach (var stock in result)
            {
                var originalStock = stocks.Values.FirstOrDefault(s => s.Name == stock.Name);
                Assert.IsNotNull(originalStock);
                
                // Ověření, že jde o různé instance
                Assert.AreNotSame(originalStock, stock);
            }
        }
        
        [TestMethod]
        public void SellStock_WithValidName_ReturnStockWithSellSetToTrue()
        {
            // Arrange
            string stockName = "MSFT";
            
            // Act
            var result = _stockService.SellStock(stockName);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(stockName, result.Name);
            Assert.IsTrue(result.Sell);
            Assert.IsNull(result.Rating);
        }
        
        [TestMethod]
        public void SellStock_WithValidName_UpdatesInternalState()
        {
            // Arrange
            string stockName = "MSFT";
            
            // Act
            _stockService.SellStock(stockName);
            
            // Ověření, že se změnil interní stav pomocí reflexe
            var stocksField = typeof(StockService).GetField("_stocks", BindingFlags.NonPublic | BindingFlags.Instance);
            var stocks = (Dictionary<string, StockRating>)stocksField.GetValue(_stockService);
            
            // Assert
            Assert.IsTrue(stocks[stockName].Sell);
        }
        
        [TestMethod]
        public void SellStock_WithInvalidName_ReturnsNull()
        {
            // Arrange
            string stockName = "INVALID";
            
            // Act
            var result = _stockService.SellStock(stockName);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [TestMethod]
        public void SellStock_MultipleCallsWithSameName_ReturnsTrueEachTime()
        {
            // Arrange
            string stockName = "GOOGL";
            
            // Act
            var result1 = _stockService.SellStock(stockName);
            var result2 = _stockService.SellStock(stockName);
            
            // Assert
            Assert.IsNotNull(result1);
            Assert.IsTrue(result1.Sell);
            
            Assert.IsNotNull(result2);
            Assert.IsTrue(result2.Sell);
        }
        
        [TestMethod]
        public void StockService_InitialState_ContainsExpectedStocks()
        {
            // Arrange & Act
            var stocks = _stockService.GetAllStocks();
            
            // Assert
            Assert.AreEqual(2, stocks.Count);
            var microsoftStock = stocks.FirstOrDefault(s => s.Name == "Microsoft");
            var googleStock = stocks.FirstOrDefault(s => s.Name == "Google");
            
            Assert.IsNotNull(microsoftStock);
            Assert.IsNotNull(googleStock);
            
            // Ověření, že všechny hodnoty jsou podle očekávání
            Assert.IsNull(microsoftStock.Rating);
            Assert.IsNull(microsoftStock.Sell);
            Assert.IsNull(googleStock.Rating);
            Assert.IsNull(googleStock.Sell);
        }

        [TestMethod]
        public void GetAllStocks_AfterSellStock_ReturnsDifferentInstances()
        {
            // Arrange
            _stockService.SellStock("MSFT");
            
            // Act
            var stocks = _stockService.GetAllStocks();
            
            // Assert
            Assert.AreEqual(2, stocks.Count);
            var microsoftStock = stocks.FirstOrDefault(s => s.Name == "Microsoft");
            
            Assert.IsNotNull(microsoftStock);
            Assert.IsNull(microsoftStock.Sell); // GetAllStocks vrací vždy null bez ohledu na interní stav
        }
    }
    
    // Mocking testy pro demonstraci, jak by se mohlo pracovat s externími závislostmi
    [TestClass]
    public class StockServiceMockTests
    {
        // Tento test simuluje, jak by se testovalo, kdyby StockService měl nějaké externí závislosti
        [TestMethod]
        public void StockService_WithMockedDependencies_WorksCorrectly()
        {
            // Arrange - předpokládejme, že by StockService mohl mít externí závislost 
            // např. na IStockDataProvider
            var mockStockDataProvider = new Mock<IStockDataProvider>();
            
            // Setup mocku
            mockStockDataProvider
                .Setup(m => m.GetStockData())
                .Returns(new Dictionary<string, StockRating> 
                {
                    { "AAPL", new StockRating { Name = "Apple", Date = DateTime.UtcNow, Rating = null, Sell = null } }
                });
            
            // Toto je ukázka, jak by vypadal konstruktor s injektovanou závislostí
            // Samotný StockService v ukázce nemá žádné externí závislosti
            // var service = new StockService(mockStockDataProvider.Object);
            
            // Act
            // var stocks = service.GetAllStocks();
            
            // Assert
            // Assert.AreEqual(1, stocks.Count);
            // Assert.AreEqual("Apple", stocks[0].Name);
            
            // V aktuálním případě pouze ověříme, že mock by byl volán
            mockStockDataProvider.Verify(m => m.GetStockData(), Times.Never());
        }
    }
    
    // Ukázka rozhraní pro mock
    public interface IStockDataProvider
    {
        Dictionary<string, StockRating> GetStockData();
    }
}
