using common.Shared.Interfaces;
using MagazineParser.Interfaces;
using common.Shared.Repositories;
using common.Shared.Models;
using MagazineParser.Models;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;

namespace magazine_parser.Services;

public class MagazineParsingService
{
    private readonly IDatabaseRepository _repository;
    private readonly IContentParser _parser;
    private readonly IUserInteraction _userInteraction;
    private string _magazineDirectory = string.Empty;

    public MagazineParsingService(
        IDatabaseRepository repository,
        IContentParser parser,
        IUserInteraction userInteraction)
    {
        _repository = repository;
        _parser = parser;
        _userInteraction = userInteraction;
    }

    public int ParseFile(string indexPath)
    {
        if (!File.Exists(indexPath))
        {
            _userInteraction.DisplayMessage($"No _index.txt file found at {indexPath}");
            return 0;
        }

        var directory = Path.GetDirectoryName(indexPath) ?? string.Empty;
        _magazineDirectory = Path.GetFullPath(directory);
        _userInteraction.DisplayMessage($"Reading {indexPath}...\n");
        
        // Extract year from end of folder name (e.g., "1975", "1975-01", "Mayfair 17-03, 1982")
        var folderName = Path.GetFileName(_magazineDirectory);
        var yearMatch = Regex.Match(folderName, @"(19\d{2}|20\d{2})\s*$");
        
        if (!yearMatch.Success || !int.TryParse(yearMatch.Groups[1].Value, out int year))
        {
            _userInteraction.DisplayMessage($"ERROR: Could not extract year from end of folder name '{folderName}'");
            _userInteraction.DisplayMessage("Folder name must end with a 4-digit year (e.g., '1975', '1975-01', or 'Magazine 17-03, 1982')");
            return 0;
        }
        
        _userInteraction.DisplayMessage($"Extracted year from folder name: {year}\n");

        var allLines = File.ReadAllLines(indexPath).ToList();
        
        // Parse header to get issue info
        string magazineTitle = "";
        int volume = 0;
        int number = 0;
        
        for (int i = 0; i < allLines.Count; i++)
        {
            var line = allLines[i];
            if (ShouldSkipLine(line))
                continue;

            if (_parser.IsHeaderLine(line, out magazineTitle, out volume, out number))
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(magazineTitle))
        {
            _userInteraction.DisplayMessage("Could not find magazine header in file");
            return 0;
        }


        var magazineId = _repository.GetMagazineId(magazineTitle);
        if (magazineId == 0)
        {
            _userInteraction.DisplayMessage($"Magazine '{magazineTitle}' not found in database");
            return 0;
        }

        // Check if issue already exists before proceeding
        var existingIssueId = _repository.GetExistingIssueId(magazineId, volume, number);
        if (existingIssueId.HasValue)
        {
            _userInteraction.DisplayMessage($"WARNING: Issue already exists: {magazineTitle} V{volume} N{number} (IssueId: {existingIssueId.Value})\nAborting parser execution.");
            return 0;
        }

        // Parse all content lines first
        var contentLines = new List<common.Shared.Models.ContentLine>();
        var lineNumbers = new List<int>();
        var autoInsertedCount = 0;
        
        // First pass: Check for page number errors and validation errors, allow corrections
        for (int index = 0; index < allLines.Count; index++)
        {
            var line = allLines[index];
            if (ShouldSkipLine(line) || _parser.IsHeaderLine(line, out _, out _, out _))
                continue;

            var contentLine = _parser.ParseContentLine(line);
            if (contentLine != null)
            {
                // Check if category was auto-inserted
                if (contentLine.WasAutoInserted)
                {
                    autoInsertedCount++;
                    var parts = line.Split(',').Select(p => p.Trim()).ToList();
                    parts.Insert(1, "Model");
                    var correctedLine = string.Join(", ", parts);
                    allLines[index] = correctedLine;
                    _userInteraction.DisplayMessage($"✓ Auto-inserted 'Model' category in line {index + 1}");
                }
                
                if (contentLine.HasPageNumberError || contentLine.HasValidationError)
                {
                    if (contentLine.HasPageNumberError)
                    {
                        _userInteraction.DisplayMessage($"\nERROR: Invalid page number format in line {index + 1}:");
                        _userInteraction.DisplayMessage($"{line}");
                        _userInteraction.DisplayMessage("\nPage numbers should be in format: 5 or 5-10 or 5|7|9 or 5-10|15|20-25");
                    }
                    
                    if (contentLine.HasValidationError)
                    {
                        _userInteraction.DisplayMessage($"\nERROR: Validation failed in line {index + 1}:");
                        _userInteraction.DisplayMessage($"{line}");
                        foreach (var error in contentLine.ValidationErrors)
                        {
                            _userInteraction.DisplayMessage($"  - {error}");
                        }
                    }
                    
                    var correctedLine = _userInteraction.GetCorrectedLine(line);
                    if (!string.IsNullOrWhiteSpace(correctedLine))
                    {
                        // Save the correction back to the file
                        allLines[index] = correctedLine;
                        File.WriteAllLines(indexPath, allLines);
                        _userInteraction.DisplayMessage("✓ Line corrected and saved to file");
                        
                        // Re-parse the corrected line
                        contentLine = _parser.ParseContentLine(correctedLine);
                        if (contentLine == null || contentLine.HasPageNumberError || contentLine.HasValidationError)
                        {
                            _userInteraction.DisplayMessage("ERROR: Corrected line still has errors. Skipping.");
                        }
                    }
                    else
                    {
                        _userInteraction.DisplayMessage("Skipping line.");
                    }
                }
            }
        }
        
        // Save auto-inserted categories to file
        if (autoInsertedCount > 0)
        {
            File.WriteAllLines(indexPath, allLines);
            _userInteraction.DisplayMessage($"\n✓ Saved {autoInsertedCount} auto-inserted categories to file");
        }
        
        // Second pass: Parse all valid content lines
        foreach (var (line, index) in allLines.Select((l, i) => (l, i)))
        {
            if (ShouldSkipLine(line) || _parser.IsHeaderLine(line, out _, out _, out _))
                continue;

            var contentLine = _parser.ParseContentLine(line);
            if (contentLine != null)
            {
                contentLines.Add(contentLine);
                lineNumbers.Add(index + 1);
            }
        }

        if (contentLines.Count == 0)
        {
            _userInteraction.DisplayMessage("No content lines found in file");
            return 0;
        }

        // Display summary and resolve issues
        DisplayMagazineSummary(magazineTitle, volume, number, contentLines);
        
        // Check for and resolve missing categories
        var missingCategories = GetMissingCategories(contentLines);
        bool hasIssues = missingCategories.Any();
        
        if (hasIssues)
        {
            _userInteraction.DisplayMessage("\n--- Resolving Issues ---");
            foreach (var (category, count) in missingCategories)
            {
                var choice = _userInteraction.ChooseIssueResolution(category, count);
                if (choice == 1)
                {
                    _repository.CreateCategory(category);
                    _userInteraction.DisplayMessage($"✓ Category '{category}' created");
                }
                else
                {
                    _userInteraction.DisplayMessage($"⊘ Skipped - {count} items will fail");
                }
            }
        }

        // Check for validation errors
        bool hasValidationErrors = contentLines.Any(c => c.HasValidationError);
        
        // Request confirmation only if there are issues or errors
        if (hasIssues || hasValidationErrors)
        {
            if (!_userInteraction.ConfirmAction("\nCommit all content to database? (y/n)"))
            {
                _userInteraction.DisplayMessage("Import cancelled");
                return 0;
            }
        }
        else
        {
            _userInteraction.DisplayMessage("\n✓ No errors found - proceeding with import");
        }

        // ...existing code...
        // Issue existence is now checked above; insert new issue here
        int issueId = _repository.InsertIssue(magazineId, volume, number, year);
        _userInteraction.DisplayMessage($"\nIssue created: {magazineTitle} V{volume} N{number} Year: {year} (IssueId: {issueId})");

        // Insert all content
        int successCount = 0;
        for (int i = 0; i < contentLines.Count; i++)
        {
            var contentLine = contentLines[i];
            var lineNum = lineNumbers[i];
            
            _userInteraction.DisplayMessage($"\nProcessing line {lineNum}: {allLines[lineNumbers[i] - 1]}");
            
            var result = InsertContentLineWithoutConfirmation(issueId, contentLine);
            if (result.Success)
            {
                var pageInfo = contentLine.Pages.Count > 1 
                    ? $"pages {contentLine.Pages.Min()}-{contentLine.Pages.Max()}" 
                    : $"page {contentLine.Pages[0]}";
                _userInteraction.DisplayMessage($"  ✓ Inserted {contentLine.Category} ({pageInfo})");
                successCount++;
            }
            else
            {
                _userInteraction.DisplayMessage($"  ✗ Failed: {result.ErrorMessage}");
            }
        }

        _userInteraction.DisplayMessage($"\nTotal content entries inserted: {successCount} of {contentLines.Count}");
        return successCount;
    }

    private bool ShouldSkipLine(string line)
    {
        return string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#");
    }

    private int ProcessHeaderLine(string title, int volume, int number, int year)
    {
        var magazineId = _repository.GetMagazineId(title);
        var existingIssueId = _repository.GetExistingIssueId(magazineId, volume, number);

        if (existingIssueId.HasValue)
        {
            _userInteraction.DisplayMessage($"Issue already exists: {title} V{volume} N{number} (IssueId: {existingIssueId}). Quit parsing or continue? (q=quit / c=continue)");
            var response = _userInteraction.PromptUser("")?.ToLowerInvariant();
            
            if (response == "q" || response == "quit")
            {
                _userInteraction.DisplayMessage("Parsing aborted by user.");
                return 0;
            }

            _userInteraction.DisplayMessage($"Continuing parse using existing IssueId: {existingIssueId}");
            return existingIssueId.Value;
        }

        var newIssueId = _repository.InsertIssue(magazineId, volume, number, year);
        _userInteraction.DisplayMessage($"Issue created: {title} V{volume} N{number} Year: {year} (IssueId: {newIssueId})");
        return newIssueId;
    }

    private (bool success, bool modified) ProcessContentLine(string line, int issueId, int lineNumber, List<string> allLines, int lineIndex)
    {
        var contentLine = _parser.ParseContentLine(line);
        if (contentLine == null)
            return (false, false);

        // Show the line being parsed
        _userInteraction.DisplayMessage($"\n--- Line {lineNumber}: {line}");

        var existingContent = _repository.GetExistingContent(issueId, contentLine);

        (bool success, bool modified) result;
        
        if (existingContent != null)
        {
            result = HandleExistingContent(line, existingContent, lineNumber);
        }
        else
        {
            result = HandleNewContent(line, issueId, contentLine, lineNumber, allLines, lineIndex);
        }

        // Prompt to continue after each line (for testing)
        _userInteraction.DisplayMessage("\nPress Enter to continue...");
        _userInteraction.PromptUser("");

        return result;
    }

    private (bool success, bool modified) HandleExistingContent(string line, common.Shared.Models.ExistingContentMatch existingContent, int lineNumber)
    {
        if (existingContent.Matches)
        {
            _userInteraction.DisplayMessage($"    ✓ Already exists in database (no changes)");
            return (false, false);
        }

        _userInteraction.DisplayMessage($"\n    ⚠ Differs from database:");
        _userInteraction.DisplayMessage($"    File:     {line}");
        _userInteraction.DisplayMessage($"    Database: {existingContent.DbRepresentation}");
        _userInteraction.DisplayMessage("    Update database with file content? (y/n/s=skip all)");
        
        var response = _userInteraction.PromptUser("")?.ToLowerInvariant();

        if (response == "s" || response == "skip")
        {
            _userInteraction.DisplayMessage("    Skipping remaining comparisons.");
            return (false, false);
        }

        if (response == "y" || response == "yes")
        {
            _repository.DeleteContent(existingContent.ContentIds);
            var result = InsertContentLine(0, _parser.ParseContentLine(line)!);
            
            if (result.Success)
            {
                _userInteraction.DisplayMessage($"    ✓ Database updated successfully");
                return (true, false);
            }
            
            _userInteraction.DisplayMessage($"    ✗ Failed to update - {result.ErrorMessage}");
        }
        else
        {
            _userInteraction.DisplayMessage($"    ⊘ Keeping database version");
        }

        return (false, false);
    }

    private (bool success, bool modified) HandleNewContent(string line, int issueId, common.Shared.Models.ContentLine contentLine, int lineNumber, List<string> allLines, int lineIndex)
    {
        var insertResult = InsertContentLine(issueId, contentLine);

        if (insertResult.Success)
        {
            var pageInfo = contentLine.Pages.Count > 1
                ? $"pages {contentLine.Pages.Min()}-{contentLine.Pages.Max()}"
                : $"page {contentLine.Pages[0]}";
            _userInteraction.DisplayMessage($"    ✓ Inserted {contentLine.Category} ({pageInfo})");
            return (true, false);
        }

        // User cancelled the insert
        if (insertResult.ErrorMessage == "Insert cancelled by user")
        {
            _userInteraction.DisplayMessage($"    ⊘ Insert cancelled");
            return (false, false);
        }

        _userInteraction.DisplayMessage($"    ✗ Problem: {insertResult.ErrorMessage}");

        if (insertResult.ErrorMessage.Contains("not found in database"))
        {
            return HandleCategoryError(line, issueId, contentLine, insertResult.ErrorMessage, allLines, lineIndex, lineNumber);
        }

        return HandleGeneralError(line, issueId, allLines, lineIndex, lineNumber);
    }

    private (bool success, bool modified) HandleCategoryError(string line, int issueId, common.Shared.Models.ContentLine contentLine, string errorMessage, List<string> allLines, int lineIndex, int lineNumber)
    {
        var categoryMatch = Regex.Match(errorMessage, @"'([^']+)'");
        if (!categoryMatch.Success)
            return (false, false);

        var categoryName = categoryMatch.Groups[1].Value;
        var choice = _userInteraction.ChooseCategoryAction(categoryName);

        switch (choice)
        {
            case 1: // Create category
                _repository.CreateCategory(categoryName);
                _userInteraction.DisplayMessage($"    ✓ Category '{categoryName}' created successfully");
                var retryResult = InsertContentLine(issueId, contentLine);
                
                if (retryResult.Success)
                {
                    _userInteraction.DisplayMessage($"    ✓ Inserted successfully");
                    return (true, false);
                }
                
                _userInteraction.DisplayMessage($"    ✗ Still has issues - {retryResult.ErrorMessage}. Skipping.");
                return (false, false);

            case 2: // Correct line
                var correctedLine = _userInteraction.GetCorrectedLine(line);
                if (!string.IsNullOrWhiteSpace(correctedLine))
                {
                    allLines[lineIndex] = correctedLine;
                    var correctedContentLine = _parser.ParseContentLine(correctedLine);
                    
                    if (correctedContentLine != null)
                    {
                        var result = InsertContentLine(issueId, correctedContentLine);
                        if (result.Success)
                        {
                            _userInteraction.DisplayMessage($"    ✓ Corrected and inserted successfully");
                            return (true, true);
                        }
                        
                        _userInteraction.DisplayMessage($"    ✗ Corrected line still has issues - {result.ErrorMessage}. Skipping.");
                    }
                }
                return (false, true);

            default: // Skip
                _userInteraction.DisplayMessage($"    ⊘ Skipped");
                return (false, false);
        }
    }
    private (bool success, bool modified) HandleGeneralError(string line, int issueId, List<string> allLines, int lineIndex, int lineNumber)
    {
        if (_userInteraction.ConfirmAction("Would you like to correct this line? (y/n)"))
        {
            var correctedLine = _userInteraction.GetCorrectedLine(line);
            if (!string.IsNullOrWhiteSpace(correctedLine))
            {
                allLines[lineIndex] = correctedLine;
                var correctedContentLine = _parser.ParseContentLine(correctedLine);
                
                if (correctedContentLine != null)
                {
                    var result = InsertContentLine(issueId, correctedContentLine);
                    if (result.Success)
                    {
                        _userInteraction.DisplayMessage($"    ✓ Corrected and inserted successfully");
                        return (true, true);
                    }
                    
                    _userInteraction.DisplayMessage($"    ✗ Corrected line still has issues - {result.ErrorMessage}. Skipping.");
                }
            }
            
            return (false, true);
        }

        _userInteraction.DisplayMessage($"    ⊘ Skipped");
        return (false, false);
    }

    private InsertResult InsertContentLine(int issueId, common.Shared.Models.ContentLine contentLine)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentLine.Category))
                return new InsertResult { Success = false, ErrorMessage = "Category field is missing or empty" };

            var categoryId = _repository.GetOrCreateCategoryId(contentLine.Category);
            if (categoryId == 0)
                return new InsertResult { Success = false, ErrorMessage = $"Category '{contentLine.Category}' not found in database" };

            // Find images first to display count in summary
            var imagesFound = 0;
            foreach (var page in contentLine.Pages)
            {
                var imagePath = FindImageForPage(page);
                if (!string.IsNullOrEmpty(imagePath))
                    imagesFound++;
            }

            // Display summary and request confirmation
            _userInteraction.DisplayInsertSummary(
                contentLine.Category,
                contentLine.Title,
                contentLine.ModelName,
                contentLine.Age,
                contentLine.ModelSize,
                contentLine.Photographer,
                contentLine.Pages,
                imagesFound);

            if (!_userInteraction.ConfirmInsert())
            {
                return new InsertResult { Success = false, ErrorMessage = "Insert cancelled by user" };
            }

            // Insert article with title only
            var articleId = _repository.InsertArticle(
                categoryId,
                contentLine.Title);

            // Link article to all models if present, associating age and measurements by order
            var ages = new List<int?>();
            var measurements = new List<string?>();
            // Parse ages and measurements by splitting on '|', matching ModelNames order
            if (!string.IsNullOrWhiteSpace(contentLine.Age?.ToString()))
            {
                ages = contentLine.Age.ToString()!.Split('|').Select(s => int.TryParse(s.Trim(), out var a) ? (int?)a : null).ToList();
            }
            if (!string.IsNullOrWhiteSpace(contentLine.ModelSize))
            {
                measurements = contentLine.ModelSize.Split('|').Select(s => string.IsNullOrWhiteSpace(s) ? null : s.Trim()).ToList();
            }

            for (int i = 0; i < contentLine.ModelNames.Count; i++)
            {
                var modelName = contentLine.ModelNames[i];
                int? bust = (i < contentLine.BustSizes.Count) ? contentLine.BustSizes[i] : null;
                int? waist = (i < contentLine.WaistSizes.Count) ? contentLine.WaistSizes[i] : null;
                int? hip = (i < contentLine.HipSizes.Count) ? contentLine.HipSizes[i] : null;
                string? cup = (i < contentLine.CupSizes.Count) ? contentLine.CupSizes[i] : null;
                var mid = _repository.GetOrCreateModelId(
                    modelName,
                    bust,
                    waist,
                    hip,
                    cup);
                if (mid.HasValue)
                {
                    int? age = (i < ages.Count) ? ages[i] : null;
                    string? measurement = (i < measurements.Count) ? measurements[i] : null;
                    _repository.LinkArticleToModel(articleId, mid.Value, age, measurement);
                }
            }

            // Insert one content entry per page
            var imageRoot = Environment.GetEnvironmentVariable("MAGAZINE_IMAGE_ROOT");
            if (string.IsNullOrEmpty(imageRoot))
            {
                _userInteraction.DisplayMessage("ERROR: MAGAZINE_IMAGE_ROOT environment variable is not set.\nPlease set it to the root of your magazine image mount, e.g.:\n  export MAGAZINE_IMAGE_ROOT=\"/mnt/your-mount\"");
                return new InsertResult { Success = false, ErrorMessage = "MAGAZINE_IMAGE_ROOT not set" };
            }
            foreach (var page in contentLine.Pages)
            {
                var imagePath = FindImageForPage(page);
                string? relativePath = null;
                if (!string.IsNullOrEmpty(imagePath))
                {
                    if (!string.IsNullOrEmpty(imageRoot) && imagePath.StartsWith(imageRoot))
                        relativePath = imagePath.Substring(imageRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    else
                        relativePath = imagePath;
                }
                else
                {
                    _userInteraction.DisplayMessage($"    ⚠ Warning: Image file for page {page} not found");
                }
                var contentId = _repository.InsertContent(issueId, page, articleId, relativePath);
                // Link contributors to content
                foreach (var photographer in contentLine.Photographers)
                {
                    var contributorId = _repository.GetOrCreateContributorId(photographer, "Photographer");
                    _repository.LinkContentToContributor(contentId, contributorId);
                }
                
                foreach (var author in contentLine.Authors)
                {
                    var contributorId = _repository.GetOrCreateContributorId(author, "Author");
                    _repository.LinkContentToContributor(contentId, contributorId);
                }
                
                foreach (var illustrator in contentLine.Illustrators)
                {
                    var contributorId = _repository.GetOrCreateContributorId(illustrator, "Illustrator");
                    _repository.LinkContentToContributor(contentId, contributorId);
                }
            }

            return new InsertResult { Success = true };
        }
        catch (Exception ex)
        {
            return new InsertResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private string? FindImageForPage(int pageNumber)
    {
        if (string.IsNullOrEmpty(_magazineDirectory))
            return null;

        // Common image extensions
        string[] extensions = { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp" };
        
        // Try different page number formats: "001", "01", "1"
        string[] formats = { 
            pageNumber.ToString("D3"),  // 001
            pageNumber.ToString("D2"),  // 01
            pageNumber.ToString()        // 1
        };

        foreach (var format in formats)
        {
            foreach (var ext in extensions)
            {
                var filePath = Path.Combine(_magazineDirectory, format + ext);
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
        }

        // If this is an odd page and no direct image found, try preceding even page
        if (pageNumber % 2 == 1)
        {
            var precedingPage = pageNumber - 1;
            string[] precedingFormats = { 
                precedingPage.ToString("D3"),
                precedingPage.ToString("D2"),
                precedingPage.ToString()
            };

            foreach (var format in precedingFormats)
            {
                foreach (var ext in extensions)
                {
                    var filePath = Path.Combine(_magazineDirectory, format + ext);
                    if (File.Exists(filePath))
                    {
                        return filePath;
                    }
                }
            }
        }

        return null;
    }

    private List<(string category, int count)> GetMissingCategories(List<ContentLine> contentLines)
    {
        var missingCategories = new List<(string, int)>();
        var categoryCounts = new Dictionary<string, int>();

        foreach (var contentLine in contentLines)
        {
            if (!string.IsNullOrWhiteSpace(contentLine.Category))
            {
                var categoryId = _repository.GetOrCreateCategoryId(contentLine.Category);
                if (categoryId == 0)
                {
                    if (categoryCounts.ContainsKey(contentLine.Category))
                        categoryCounts[contentLine.Category]++;
                    else
                        categoryCounts[contentLine.Category] = 1;
                }
            }
        }

        foreach (var kvp in categoryCounts)
        {
            missingCategories.Add((kvp.Key, kvp.Value));
        }

        return missingCategories;
    }

    private void DisplayMagazineSummary(string magazineTitle, int volume, int number, List<ContentLine> contentLines)
    {
        _userInteraction.DisplayMessage("\n" + new string('=', 70));
        _userInteraction.DisplayMessage($"MAGAZINE IMPORT SUMMARY");
        _userInteraction.DisplayMessage(new string('=', 70));
        _userInteraction.DisplayMessage($"\nMagazine: {magazineTitle} V{volume} N{number}");
        _userInteraction.DisplayMessage($"Total Items: {contentLines.Count}");
        
        // Check for potential issues
        var missingCategories = new List<string>();
        foreach (var contentLine in contentLines)
        {
            if (!string.IsNullOrWhiteSpace(contentLine.Category))
            {
                var categoryId = _repository.GetOrCreateCategoryId(contentLine.Category);
                if (categoryId == 0 && !missingCategories.Contains(contentLine.Category))
                {
                    missingCategories.Add(contentLine.Category);
                }
            }
        }

        if (missingCategories.Any())
        {
            _userInteraction.DisplayMessage("\n⚠ WARNING: The following issues will prevent some content from being inserted:");
            _userInteraction.DisplayMessage("\n--- Missing Categories ---");
            foreach (var cat in missingCategories)
            {
                var affectedLines = contentLines
                    .Where(c => c.Category.Equals(cat, StringComparison.OrdinalIgnoreCase))
                    .Count();
                _userInteraction.DisplayMessage($"  • '{cat}' - {affectedLines} items will fail");
            }
        }
        
        // Group by category
        var categoryCounts = contentLines.GroupBy(c => c.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);
        
        _userInteraction.DisplayMessage("\n--- Content by Category ---");
        foreach (var cat in categoryCounts)
        {
            var categoryId = _repository.GetOrCreateCategoryId(cat.Category);
            var status = categoryId == 0 ? " ✗ MISSING" : "";
            _userInteraction.DisplayMessage($"  {cat.Category,-15} : {cat.Count} items{status}");
        }
        
        // Models
        var models = contentLines
            .Where(c => !string.IsNullOrEmpty(c.ModelName))
            .Select(c => new { 
                Name = c.ModelName, 
                Age = c.Age, 
                Measurements = c.ModelSize 
            })
            .Distinct()
            .ToList();
        
        if (models.Any())
        {
            _userInteraction.DisplayMessage($"\n--- Models ({models.Count}) ---");
            foreach (var model in models)
            {
                var info = model.Name;
                if (model.Age.HasValue)
                    info += $", {model.Age}";
                if (!string.IsNullOrEmpty(model.Measurements))
                    info += $", {model.Measurements}";
                _userInteraction.DisplayMessage($"  • {info}");
            }
        }
        
        // Articles (non-model content with titles)
        var articles = contentLines
            .Where(c => !string.IsNullOrEmpty(c.Title) && string.IsNullOrEmpty(c.ModelName))
            .Select(c => new { 
                Category = c.Category, 
                Title = c.Title,
                Photographer = c.Photographer
            })
            .ToList();
        
        if (articles.Any())
        {
            _userInteraction.DisplayMessage($"\n--- Articles ({articles.Count}) ---");
            foreach (var article in articles.Take(10))
            {
                var info = $"{article.Category}: {article.Title}";
                if (!string.IsNullOrEmpty(article.Photographer))
                    info += $" (Photo: {article.Photographer})";
                _userInteraction.DisplayMessage($"  • {info}");
            }
            if (articles.Count > 10)
                _userInteraction.DisplayMessage($"  ... and {articles.Count - 10} more");
        }
        
        // Count images
        var totalPages = contentLines.Sum(c => c.Pages.Count);
        var imagesFound = 0;
        foreach (var contentLine in contentLines)
        {
            foreach (var page in contentLine.Pages)
            {
                if (!string.IsNullOrEmpty(FindImageForPage(page)))
                    imagesFound++;
            }
        }
        
        _userInteraction.DisplayMessage($"\n--- Images ---");
        _userInteraction.DisplayMessage($"  Total pages: {totalPages}");
        _userInteraction.DisplayMessage($"  Images found: {imagesFound}");
        _userInteraction.DisplayMessage($"  Missing: {totalPages - imagesFound}");
        
        _userInteraction.DisplayMessage("\n" + new string('=', 70));
    }

    private InsertResult InsertContentLineWithoutConfirmation(int issueId, ContentLine contentLine)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentLine.Category))
                return new InsertResult { Success = false, ErrorMessage = "Category field is missing or empty" };

            var categoryId = _repository.GetOrCreateCategoryId(contentLine.Category);
            if (categoryId == 0)
                return new InsertResult { Success = false, ErrorMessage = $"Category '{contentLine.Category}' not found in database" };

            // Insert article with title only
            var articleId = _repository.InsertArticle(
                categoryId,
                contentLine.Title);

            // Link article to all models if present
            foreach (var modelName in contentLine.ModelNames)
            {
                var mid = _repository.GetOrCreateModelId(
                    modelName,
                    contentLine.BustSize,
                    contentLine.WaistSize,
                    contentLine.HipSize,
                    contentLine.CupSize);
                if (mid.HasValue)
                    _repository.LinkArticleToModel(articleId, mid.Value, contentLine.Age, contentLine.ModelSize);
            }

            // Insert one content entry per page
            var imageRoot = Environment.GetEnvironmentVariable("MAGAZINE_IMAGE_ROOT");
            if (string.IsNullOrEmpty(imageRoot))
            {
                _userInteraction.DisplayMessage("ERROR: MAGAZINE_IMAGE_ROOT environment variable is not set.\nPlease set it to the root of your magazine image mount, e.g.:\n  export MAGAZINE_IMAGE_ROOT=\"/mnt/your-mount\"");
                return new InsertResult { Success = false, ErrorMessage = "MAGAZINE_IMAGE_ROOT not set" };
            }
            foreach (var page in contentLine.Pages)
            {
                var imagePath = FindImageForPage(page);
                string? relativePath = null;
                if (!string.IsNullOrEmpty(imagePath))
                {
                    if (!string.IsNullOrEmpty(imageRoot) && imagePath.StartsWith(imageRoot))
                        relativePath = imagePath.Substring(imageRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    else
                        relativePath = imagePath;
                }
                else
                {
                    _userInteraction.DisplayMessage($"    ⚠ Warning: Image file for page {page} not found");
                }
                var contentId = _repository.InsertContent(issueId, page, articleId, relativePath);
                // Link contributors to content using canonical Contributors list
                var contributors = contentLine.Contributors ?? new List<string>();
                foreach (var contrib in contributors)
                {
                    var role = (contentLine.Category.Equals("Model", StringComparison.OrdinalIgnoreCase) || contentLine.Category.Equals("Cover", StringComparison.OrdinalIgnoreCase))
                        ? "Photographer"
                        : "Author";
                    var contributorId = _repository.GetOrCreateContributorId(contrib, role);
                    _repository.LinkContentToContributor(contentId, contributorId);
                }
                
                foreach (var illustrator in contentLine.Illustrators)
                {
                    var contributorId = _repository.GetOrCreateContributorId(illustrator, "Illustrator");
                    _repository.LinkContentToContributor(contentId, contributorId);
                }
            }

            return new InsertResult { Success = true };
        }
        catch (Exception ex)
        {
            return new InsertResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
