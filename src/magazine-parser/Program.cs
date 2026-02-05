using System;
using System.IO;
using MagazineParser.Interfaces;
using MagazineParser.Repositories;
using MagazineParser.Services;
using Npgsql;

class Program
{
    static int Main(string[] args)
    {
        // Expected usage: magazine-parser <directory>
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: magazine-parser <directory>");
            return 1;
        }

        var directory = args[0];

        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"ERROR: Directory not found: {directory}");
            return 2;
        }

        var indexPath = Path.Combine(directory, "_index.txt");
        
        // If _index.txt doesn't exist, try _auto_index.txt
        if (!File.Exists(indexPath))
        {
            var autoIndexPath = Path.Combine(directory, "_auto_index.txt");
            if (File.Exists(autoIndexPath))
            {
                indexPath = autoIndexPath;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Neither _index.txt nor _auto_index.txt found in: {directory}");
                Console.ResetColor();
                return 3;
            }
        }

        try
        {
            var connectionString = "Host=localhost;Username=postgres;Password=Barnowl1;Database=magazines";
            
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            // Dependency injection using SOLID principles
            IDatabaseRepository repository = new PostgresRepository(connection);
            
            // Load valid categories from database
            var categories = repository.GetAllCategories();
            // Add "Contents" as an alias for "Index"
            categories.Add("Contents");
            var validCategories = new HashSet<string>(categories, StringComparer.OrdinalIgnoreCase);
            
            IContentParser parser = new ContentLineParser(validCategories);
            IUserInteraction userInteraction = new ConsoleUserInteraction();
            
            var parsingService = new MagazineParsingService(repository, parser, userInteraction);
            parsingService.ParseFile(indexPath);
            
            connection.Close();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }
}

