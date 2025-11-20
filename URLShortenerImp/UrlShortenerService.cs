using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;

namespace URLShortenerImp
{
    /// <summary>
    /// Service to manage URL shortening with persistence and thread safety.
    /// </summary>
    public class UrlShortenerService
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int Base = 62;
        private const int ShortKeyLength = 6; // 62^6 > 56B, enough for 1M unique URLs
        private const int MaxCollisionAttempts = 10;

        private readonly string _domain;
        private readonly string _dataFile;
        private readonly Dictionary<string, string> _urlToShort = new();
        private readonly Dictionary<string, string> _shortToUrl = new();
        private readonly object _lockObject = new();

        public UrlShortenerService(string domain = "http://short.rl/", string dataFile = "url_data.json")
        {
            _domain = domain;
            _dataFile = dataFile;
        }

        /// <summary>
        /// Load data from disk into memory.
        /// </summary>
        public void Load()
        {
            lock (_lockObject)
            {
                if (!File.Exists(_dataFile))
                    return;

                try
                {
                    var json = File.ReadAllText(_dataFile);
                    var data = JsonSerializer.Deserialize<UrlMappingData>(json);
                    
                    if (data != null)
                    {
                        _urlToShort.Clear();
                        _shortToUrl.Clear();
                        
                        foreach (var kv in data.UrlToShort)
                            _urlToShort[kv.Key] = kv.Value;
                        
                        foreach (var kv in data.ShortToUrl)
                            _shortToUrl[kv.Key] = kv.Value;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading data: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Save data from memory to disk.
        /// </summary>
        public void Save()
        {
            lock (_lockObject)
            {
                try
                {
                    var data = new UrlMappingData
                    {
                        UrlToShort = new Dictionary<string, string>(_urlToShort),
                        ShortToUrl = new Dictionary<string, string>(_shortToUrl)
                    };
                    
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_dataFile, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving data: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Shorten a URL. Returns the short key if successful, null otherwise.
        /// </summary>
        public string? ShortenUrl(string originalUrl)
        {
            if (string.IsNullOrWhiteSpace(originalUrl))
                return null;

            lock (_lockObject)
            {
                // Check if URL already exists
                if (_urlToShort.TryGetValue(originalUrl, out var existingShortKey))
                    return existingShortKey;

                // Generate a unique short key
                string? shortKey = GenerateUniqueShortKey();
                if (shortKey == null)
                    return null;

                // Store the mapping
                _urlToShort[originalUrl] = shortKey;
                _shortToUrl[shortKey] = originalUrl;
                
                Save();
                return shortKey;
            }
        }

        /// <summary>
        /// Get the original URL from a short key.
        /// </summary>
        public string? RetrieveUrl(string shortKey)
        {
            if (string.IsNullOrWhiteSpace(shortKey))
                return null;

            lock (_lockObject)
            {
                _shortToUrl.TryGetValue(shortKey, out var originalUrl);
                return originalUrl;
            }
        }

        /// <summary>
        /// Get the full short URL including domain.
        /// </summary>
        public string GetFullShortUrl(string shortKey)
        {
            return $"{_domain}{shortKey}";
        }

        /// <summary>
        /// Get the short key from a full short URL.
        /// </summary>
        public string ExtractShortKey(string fullUrl)
        {
            return fullUrl.Replace(_domain, "").Trim();
        }

        /// <summary>
        /// Get statistics about the stored URLs.
        /// </summary>
        public (int totalUrls, int uniqueShortKeys) GetStatistics()
        {
            lock (_lockObject)
            {
                return (_urlToShort.Count, _shortToUrl.Count);
            }
        }

        /// <summary>
        /// Generate a unique short key with collision detection.
        /// </summary>
        private string? GenerateUniqueShortKey()
        {
            for (int attempt = 0; attempt < MaxCollisionAttempts; attempt++)
            {
                string shortKey = GenerateRandomShortKey(ShortKeyLength);
                if (!_shortToUrl.ContainsKey(shortKey))
                    return shortKey;
            }
            return null; // Failed to generate unique key
        }

        /// <summary>
        /// Generate a random short key of specified length using Base62 encoding.
        /// </summary>
        private static string GenerateRandomShortKey(int length)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = Alphabet[bytes[i] % Base];
            }

            return new string(chars);
        }

        /// <summary>
        /// Helper class for JSON serialization of URL mappings.
        /// </summary>
        private class UrlMappingData
        {
            public Dictionary<string, string> UrlToShort { get; set; } = new();
            public Dictionary<string, string> ShortToUrl { get; set; } = new();
        }
    }
}
