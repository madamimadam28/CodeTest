
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;

namespace URLShortenerImp
{
	class Program
	{
		// Character set: a-z, A-Z, 0-9
		private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		private const int Base = 62;
		private const string Domain = "http://short.rl/";
		private static readonly Dictionary<string, string> urlToShort = new();
		private static readonly Dictionary<string, string> shortToUrl = new();
		private static readonly string DataFile = "url_data.json";
		private static readonly object urlLock = new();
		private static long counter = 1; // Retained for legacy, but not used for key generation
		private static readonly int ShortKeyLength = 6; // 62^6 > 56B, enough for 1M unique URLs

		static void Main(string[] args)
		{
			LoadData();
			Console.WriteLine("URL Shortener Console App");
			while (true)
			{
				Console.WriteLine("\nChoose an option:");
				Console.WriteLine("1. Shorten a URL");
				Console.WriteLine("2. Retrieve original URL");
				Console.WriteLine("3. Exit");
				Console.Write("Enter choice: ");
				var choice = Console.ReadLine();
				switch (choice)
				{
					case "1":
						ShortenUrlFlow();
						break;
					case "2":
						RetrieveUrlFlow();
						break;
					case "3":
						SaveData();
						return;
					default:
						Console.WriteLine("Invalid choice. Try again.");
						break;
				}
			}
		}

		private static void ShortenUrlFlow()
		{
			Console.Write("Enter the original URL: ");
			var originalUrl = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(originalUrl))
			{
				Console.WriteLine("URL cannot be empty.");
				return;
			}

			string shortKey = string.Empty;
			lock (urlLock)
			{
				if (urlToShort.ContainsKey(originalUrl))
				{
					shortKey = urlToShort[originalUrl];
				}
				else
				{
					int maxAttempts = 10;
					int attempt = 0;
					do
					{
						shortKey = GenerateRandomShortKey(ShortKeyLength);
						attempt++;
					} while (shortToUrl.ContainsKey(shortKey) && attempt < maxAttempts);

					if (shortToUrl.ContainsKey(shortKey))
					{
						Console.WriteLine("Failed to generate a unique short URL. Please try again.");
						return;
					}

					urlToShort[originalUrl] = shortKey;
					shortToUrl[shortKey] = originalUrl;
					SaveData();
				}
			}
			Console.WriteLine($"Short URL: {Domain}{shortKey}");
				// Save mappings to disk
				private static void SaveData()
				{
					lock (urlLock)
					{
						var data = new UrlData
						{
							UrlToShort = urlToShort,
							ShortToUrl = shortToUrl
						};
						var json = JsonSerializer.Serialize(data);
						File.WriteAllText(DataFile, json);
					}
				}

				// Load mappings from disk
				private static void LoadData()
				{
					lock (urlLock)
					{
						if (File.Exists(DataFile))
						{
							var json = File.ReadAllText(DataFile);
							var data = JsonSerializer.Deserialize<UrlData>(json);
							if (data != null)
							{
								urlToShort.Clear();
								shortToUrl.Clear();
								foreach (var kv in data.UrlToShort)
									urlToShort[kv.Key] = kv.Value;
								foreach (var kv in data.ShortToUrl)
									shortToUrl[kv.Key] = kv.Value;
							}
						}
					}
				}

				// Helper class for serialization
				private class UrlData
				{
					public Dictionary<string, string> UrlToShort { get; set; } = new();
					public Dictionary<string, string> ShortToUrl { get; set; } = new();
				}
		}

		private static void RetrieveUrlFlow()
		{
			Console.Write("Enter the short URL: ");
			var input = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(input))
			{
				Console.WriteLine("Short URL cannot be empty.");
				return;
			}
			var shortKey = input.Replace(Domain, "");
			if (shortToUrl.TryGetValue(shortKey, out var originalUrl))
			{
				Console.WriteLine($"Original URL: {originalUrl}");
			}
			else
			{
				Console.WriteLine("Short URL not found.");
			}
		}

		// Base62 encoding (legacy, not used for random keys)
		private static string Encode(long num)
		{
			if (num == 0) return Alphabet[0].ToString();
			var s = string.Empty;
			while (num > 0)
			{
				s = Alphabet[(int)(num % Base)] + s;
				num /= Base;
			}
			return s;
		}

		// Generate a random short key of given length
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
	}
}
