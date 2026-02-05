using MagazineParser.Interfaces;

namespace MagazineParser.Services;

public class ConsoleUserInteraction : IUserInteraction
{
    public void DisplayMessage(string message)
    {
        // Auto-detect message type based on content
        if (message.StartsWith("ERROR:") || message.Contains("✗") || message.Contains("Failed"))
        {
            DisplayError(message);
        }
        else if (message.Contains("✓") || message.Contains("created") || message.Contains("Inserted") || message.Contains("success"))
        {
            DisplaySuccess(message);
        }
        else if (message.Contains("Choose") || message.Contains("Enter") || message.EndsWith("?"))
        {
            DisplayPrompt(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    private void DisplayError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void DisplaySuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void DisplayPrompt(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public string? PromptUser(string prompt)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(prompt);
        Console.ResetColor();
        return Console.ReadLine()?.Trim();
    }

    public bool ConfirmAction(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
        var response = Console.ReadLine()?.Trim().ToLowerInvariant();
        return response == "y" || response == "yes";
    }

    public string? GetCorrectedLine(string originalLine)
    {
        Console.WriteLine($"\nOriginal line:\n{originalLine}");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nEnter the corrected line (or press Enter to skip):");
        Console.ResetColor();
        return Console.ReadLine();
    }

    public int ChooseCategoryAction(string categoryName)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Choose an option:");
        Console.WriteLine($"1. Create new category '{categoryName}' in database");
        Console.WriteLine($"2. Correct the line");
        Console.WriteLine($"3. Skip this line");
        Console.Write("Enter choice (1/2/3): ");
        Console.ResetColor();
        var choice = Console.ReadLine()?.Trim();
        
        return choice switch
        {
            "1" => 1,
            "2" => 2,
            "3" => 3,
            _ => 3
        };
    }

    public void DisplayInsertSummary(string category, string? title, string? modelName, int? age, string? measurements, string? photographer, List<int> pages, int imageCount)
    {
        Console.WriteLine("\n=== Insert Summary ===");
        Console.WriteLine($"Category:     {category}");
        
        if (!string.IsNullOrEmpty(title))
            Console.WriteLine($"Title:        {title}");
        
        if (!string.IsNullOrEmpty(modelName))
        {
            Console.Write($"Model:        {modelName}");
            if (age.HasValue)
                Console.Write($", {age}");
            if (!string.IsNullOrEmpty(measurements))
                Console.Write($", {measurements}");
            Console.WriteLine();
        }
        
        if (!string.IsNullOrEmpty(photographer))
            Console.WriteLine($"Photographer: {photographer}");
        
        if (pages.Count == 1)
            Console.WriteLine($"Page:         {pages[0]}");
        else if (pages.Count > 1)
            Console.WriteLine($"Pages:        {string.Join(", ", pages)} ({pages.Count} pages)");
        
        Console.WriteLine($"Images:       {imageCount} of {pages.Count} found");
        Console.WriteLine("=====================");
    }

    public bool ConfirmInsert()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("\nProceed with insert? (y/n): ");
        Console.ResetColor();
        var response = Console.ReadLine()?.Trim().ToLowerInvariant();
        return response == "y" || response == "yes";
    }

    public int ChooseIssueResolution(string categoryName, int affectedCount)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\nMissing category '{categoryName}' affects {affectedCount} item(s).");
        Console.WriteLine("Choose an option:");
        Console.WriteLine($"1. Create category '{categoryName}' in database");
        Console.WriteLine($"2. Skip - items with this category will fail");
        Console.Write("Enter choice (1/2): ");
        Console.ResetColor();
        var choice = Console.ReadLine()?.Trim();
        
        return choice switch
        {
            "1" => 1,
            "2" => 2,
            _ => 2
        };
    }
}
