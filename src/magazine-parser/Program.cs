using System;
using System.IO;
using MagazineParser.Interfaces;
using common.Shared.Repositories;
using common.Shared.Interfaces;
using common.Shared.Services;
using magazine_parser.Services;
using Npgsql;
using Common.Shared;

class Program
{
    static int Main(string[] args)
    {
        // Expected usage: magazine-parser [--no-insert] <directory>
        bool noInsert = false;
        string directory = string.Empty;

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: magazine-parser [--no-insert] <directory>");
            return 1;
        }

        if (args.Length == 1)
        {
            directory = args[0];
        }
        else if (args.Length == 2 && (args[0] == "--no-insert" || args[0] == "-n"))
        {
            noInsert = true;
            directory = args[1];
        }
        else
        {
            Console.WriteLine("Usage: magazine-parser [--no-insert] <directory>");
            return 1;
        }

        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"ERROR: Directory not found: {directory}");
            return 2;
        }

        // Phase 2: Prefer JSON format, fall back to TXT
        var jsonPath = Path.Combine(directory, "_index.json");
        var txtPath = Path.Combine(directory, "_index.txt");
        var autoIndexPath = Path.Combine(directory, "_auto_index.txt");
        
        string indexPath;
        if (File.Exists(jsonPath))
        {
            indexPath = jsonPath;
            Console.WriteLine($"Found _index.json - using JSON format");
        }
        else if (File.Exists(txtPath))
        {
            indexPath = txtPath;
            Console.WriteLine($"Found _index.txt - using legacy CSV format");
        }
        else if (File.Exists(autoIndexPath))
        {
            indexPath = autoIndexPath;
            Console.WriteLine($"Found _auto_index.txt - using legacy CSV format");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: No index file found in: {directory}");
            Console.WriteLine($"  Looked for: _index.json, _index.txt, _auto_index.txt");
            Console.ResetColor();
            return 3;
        }

        try
        {
            // Load connection string from secure configuration
            var connectionString = Common.Shared.Configuration.ConnectionStringProvider.GetConnectionString();
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
            
            var parsingService = new MagazineParsingService(repository, parser, userInteraction, noInsert);
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
