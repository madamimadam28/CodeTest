using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using URLShortenerImp;

namespace URLShortenerImp.Tests
{
    public class UrlShortenerServiceTests : IDisposable
    {
        private readonly string _testDataFile = Path.Combine(Path.GetTempPath(), $"test_url_data_{Guid.NewGuid()}.json");
        private readonly UrlShortenerService _service;

        public UrlShortenerServiceTests()
        {
            _service = new UrlShortenerService("http://short.rl/", _testDataFile);
        }

        public void Dispose()
        {
            // Clean up test data file
            if (File.Exists(_testDataFile))
                File.Delete(_testDataFile);
        }

        #region Basic Functionality Tests

        [Fact]
        public void ShortenUrl_WithValidUrl_ReturnsShortKey()
        {
            // Arrange
            var url = "https://www.example.com";

            // Act
            var result = _service.ShortenUrl(url);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(6, result.Length); // Default length is 6
        }

        [Fact]
        public void ShortenUrl_WithSameUrl_ReturnsSameShortKey()
        {
            // Arrange
            var url = "https://www.example.com";

            // Act
            var result1 = _service.ShortenUrl(url);
            var result2 = _service.ShortenUrl(url);

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void ShortenUrl_WithDifferentUrls_ReturnsDifferentShortKeys()
        {
            // Arrange
            var url1 = "https://www.example.com";
            var url2 = "https://www.google.com";

            // Act
            var result1 = _service.ShortenUrl(url1);
            var result2 = _service.ShortenUrl(url2);

            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void RetrieveUrl_WithValidShortKey_ReturnsOriginalUrl()
        {
            // Arrange
            var originalUrl = "https://www.example.com";
            var shortKey = _service.ShortenUrl(originalUrl);
            Assert.NotNull(shortKey);

            // Act
            var result = _service.RetrieveUrl(shortKey);

            // Assert
            Assert.Equal(originalUrl, result);
        }

        [Fact]
        public void RetrieveUrl_WithInvalidShortKey_ReturnsNull()
        {
            // Act
            var result = _service.RetrieveUrl("invalid");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFullShortUrl_ReturnsUrlWithDomain()
        {
            // Arrange
            var shortKey = "abc123";

            // Act
            var result = _service.GetFullShortUrl(shortKey);

            // Assert
            Assert.Equal("http://short.rl/abc123", result);
        }

        [Fact]
        public void ExtractShortKey_FromFullUrl_ReturnsShortKey()
        {
            // Arrange
            var fullUrl = "http://short.rl/abc123";

            // Act
            var result = _service.ExtractShortKey(fullUrl);

            // Assert
            Assert.Equal("abc123", result);
        }

        #endregion

        #region Edge Case Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ShortenUrl_WithInvalidInput_ReturnsNull(string? url)
        {
            // Act
            var result = _service.ShortenUrl(url ?? "");

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RetrieveUrl_WithInvalidInput_ReturnsNull(string? shortKey)
        {
            // Act
            var result = _service.RetrieveUrl(shortKey ?? "");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ShortenUrl_WithVeryLongUrl_ReturnsShortKey()
        {
            // Arrange
            var longUrl = "https://www.example.com/" + new string('a', 1000);

            // Act
            var result = _service.ShortenUrl(longUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result.Length);
        }

        [Fact]
        public void ShortenUrl_WithSpecialCharacters_ReturnsShortKey()
        {
            // Arrange
            var urlWithSpecialChars = "https://www.example.com/path?query=value&other=test#anchor";

            // Act
            var result = _service.ShortenUrl(urlWithSpecialChars);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result.Length);
        }

        #endregion

        #region Character Set Tests

        [Fact]
        public void ShortenUrl_GeneratesKeysWithValidCharacters()
        {
            // Arrange
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var urls = new[] { "https://url1.com", "https://url2.com", "https://url3.com" };

            // Act
            var shortKeys = new List<string>();
            foreach (var url in urls)
            {
                var key = _service.ShortenUrl(url);
                if (key != null)
                    shortKeys.Add(key);
            }

            // Assert
            foreach (var key in shortKeys)
            {
                foreach (var c in key)
                {
                    Assert.Contains(c, validChars);
                }
            }
        }

        #endregion

        #region Persistence Tests

        [Fact]
        public void Save_CreatesDataFile()
        {
            // Arrange
            _service.ShortenUrl("https://www.example.com");

            // Act
            _service.Save();

            // Assert
            Assert.True(File.Exists(_testDataFile));
        }

        [Fact]
        public void Load_RestoresDataFromFile()
        {
            // Arrange
            var originalUrl = "https://www.example.com";
            var shortKey = _service.ShortenUrl(originalUrl);
            Assert.NotNull(shortKey);
            _service.Save();

            // Create a new service instance and load
            var newService = new UrlShortenerService("http://short.rl/", _testDataFile);

            // Act
            newService.Load();
            var result = newService.RetrieveUrl(shortKey);

            // Assert
            Assert.Equal(originalUrl, result);
        }

        [Fact]
        public void Load_WithNonexistentFile_DoesNotThrow()
        {
            // Arrange
            var service = new UrlShortenerService("http://short.rl/", Path.Combine(Path.GetTempPath(), "nonexistent.json"));

            // Act & Assert (should not throw)
            service.Load();
        }

        [Fact]
        public void Save_And_Load_MultipleUrls()
        {
            // Arrange
            var urls = new[]
            {
                "https://www.example.com",
                "https://www.google.com",
                "https://www.github.com",
                "https://www.stackoverflow.com"
            };

            var shortKeys = new Dictionary<string, string>();
            foreach (var url in urls)
            {
                var key = _service.ShortenUrl(url);
                if (key != null)
                    shortKeys[url] = key;
            }

            _service.Save();

            // Create new service and load
            var newService = new UrlShortenerService("http://short.rl/", _testDataFile);
            newService.Load();

            // Act & Assert
            foreach (var kvp in shortKeys)
            {
                var retrievedUrl = newService.RetrieveUrl(kvp.Value);
                Assert.Equal(kvp.Key, retrievedUrl);
            }
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public void GetStatistics_WithNoUrls_ReturnsZero()
        {
            // Act
            var (totalUrls, uniqueKeys) = _service.GetStatistics();

            // Assert
            Assert.Equal(0, totalUrls);
            Assert.Equal(0, uniqueKeys);
        }

        [Fact]
        public void GetStatistics_WithMultipleUrls_ReturnsCorrectCounts()
        {
            // Arrange
            var urls = new[] { "https://url1.com", "https://url2.com", "https://url3.com" };
            foreach (var url in urls)
            {
                _service.ShortenUrl(url);
            }

            // Act
            var (totalUrls, uniqueKeys) = _service.GetStatistics();

            // Assert
            Assert.Equal(3, totalUrls);
            Assert.Equal(3, uniqueKeys);
        }

        [Fact]
        public void GetStatistics_WithDuplicateUrl_CountsCorrectly()
        {
            // Arrange
            var url = "https://example.com";
            _service.ShortenUrl(url);
            _service.ShortenUrl(url); // Same URL again

            // Act
            var (totalUrls, uniqueKeys) = _service.GetStatistics();

            // Assert
            Assert.Equal(1, totalUrls); // Only 1 unique URL
            Assert.Equal(1, uniqueKeys); // Only 1 unique key
        }

        #endregion

        #region Concurrency Tests

        [Fact]
        public void ShortenUrl_WithConcurrentRequests_NoCollisions()
        {
            // Arrange
            var urls = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                urls.Add($"https://example{i}.com");
            }

            var shortKeys = new HashSet<string>();
            var lockObj = new object();

            // Act
            Parallel.ForEach(urls, url =>
            {
                var key = _service.ShortenUrl(url);
                if (key != null)
                {
                    lock (lockObj)
                    {
                        shortKeys.Add(key);
                    }
                }
            });

            // Assert - all keys should be unique
            Assert.Equal(100, shortKeys.Count);
        }

        [Fact]
        public void ShortenUrl_WithConcurrentSameUrl_ReturnsSameKey()
        {
            // Arrange
            var url = "https://www.example.com";
            var results = new List<string?>();
            var lockObj = new object();

            // Act
            Parallel.For(0, 10, _ =>
            {
                var result = _service.ShortenUrl(url);
                lock (lockObj)
                {
                    results.Add(result);
                }
            });

            // Assert - all should have the same short key
            var firstKey = results[0];
            foreach (var result in results)
            {
                Assert.Equal(firstKey, result);
            }
        }

        #endregion

        #region Uniqueness Tests

        [Fact]
        public void ShortenUrl_GeneratesUniquKeysForLargeVolume()
        {
            // Arrange
            var urlCount = 1000;
            var shortKeys = new HashSet<string>();

            // Act
            for (int i = 0; i < urlCount; i++)
            {
                var key = _service.ShortenUrl($"https://example{i}.com");
                if (key != null)
                    shortKeys.Add(key);
            }

            // Assert
            Assert.Equal(urlCount, shortKeys.Count);
        }

        #endregion
    }
}
