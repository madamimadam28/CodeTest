using System;

namespace URLShortenerImp
{
	class Program
	{
		private static readonly UrlShortenerService _service = new();

		static void Main(string[] args)
		{
			_service.Load();
			Console.WriteLine("========================================");
			Console.WriteLine("   URL Shortener Console Application");
			Console.WriteLine("========================================\n");

			while (true)
			{
				DisplayMenu();
				var choice = Console.ReadLine()?.Trim();

				switch (choice)
				{
					case "1":
						ShortenUrlFlow();
						break;
					case "2":
						RetrieveUrlFlow();
						break;
					case "3":
						ShowStatistics();
						break;
					case "4":
						Exit();
						return;
					default:
						Console.WriteLine("❌ Invalid choice. Please try again.\n");
						break;
				}
			}
		}

		private static void DisplayMenu()
		{
			Console.WriteLine("\nChoose an option:");
			Console.WriteLine("  1. Shorten a URL");
			Console.WriteLine("  2. Retrieve original URL");
			Console.WriteLine("  3. View statistics");
			Console.WriteLine("  4. Exit");
			Console.Write("\nEnter choice: ");
		}

		private static void ShortenUrlFlow()
		{
			Console.Write("\nEnter the original URL: ");
			var originalUrl = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(originalUrl))
			{
				Console.WriteLine("❌ URL cannot be empty.\n");
				return;
			}

			var shortKey = _service.ShortenUrl(originalUrl);
			if (shortKey != null)
			{
				var fullUrl = _service.GetFullShortUrl(shortKey);
				Console.WriteLine($"✅ Short URL: {fullUrl}\n");
			}
			else
			{
				Console.WriteLine("❌ Failed to generate a unique short URL. Please try again.\n");
			}
		}

		private static void RetrieveUrlFlow()
		{
			Console.Write("\nEnter the short URL or short key: ");
			var input = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(input))
			{
				Console.WriteLine("❌ Short URL cannot be empty.\n");
				return;
			}

			var shortKey = _service.ExtractShortKey(input);
			var originalUrl = _service.RetrieveUrl(shortKey);

			if (originalUrl != null)
			{
				Console.WriteLine($"✅ Original URL: {originalUrl}\n");
			}
			else
			{
				Console.WriteLine("❌ Short URL not found.\n");
			}
		}

		private static void ShowStatistics()
		{
			var (totalUrls, uniqueKeys) = _service.GetStatistics();
			Console.WriteLine($"\n📊 Statistics:");
			Console.WriteLine($"   Total unique original URLs: {totalUrls}");
			Console.WriteLine($"   Total short keys generated: {uniqueKeys}\n");
		}

		private static void Exit()
		{
			_service.Save();
			Console.WriteLine("\n✅ Data saved. Goodbye!\n");
		}
	}
}
