using MagazineParser.Interfaces;
using MagazineParser.Models;

namespace MagazineParser.Services;

public class ContentLineParser : IContentParser
{
    private readonly HashSet<string> _validCategories;

    public ContentLineParser(HashSet<string> validCategories)
    {
        _validCategories = validCategories;
    }
    public ContentLine? ParseContentLine(string line)
    {
        var parts = SplitRespectingEscapedCommas(line);
        if (parts.Count < 2)
            return null;

        var contentLine = new ContentLine();

        // Parse page numbers
        contentLine.Pages = ParsePageNumbers(parts[0], out bool hasError);
        contentLine.HasPageNumberError = hasError;
        if (contentLine.Pages.Count == 0)
            return null;

        // Parse other fields
        contentLine.Category = parts.Count > 1 ? parts[1] : "";

        // If category is missing, show error and stop processing
        if (string.IsNullOrWhiteSpace(contentLine.Category))
        {
            throw new Exception($"ERROR: Missing category in line: '{line}'");
        }

        // Normalize 'Contents' to 'Index'
        if (contentLine.Category.Equals("Contents", StringComparison.OrdinalIgnoreCase))
            contentLine.Category = "Index";

        contentLine.Title = parts.Count > 2 ? parts[2] : "";
        // Split model names by '|'
        if (parts.Count > 3)
        {
            contentLine.ModelNames = parts[3].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        // Split ages by '|', store as list of nullable ints
        List<int?> ages = new();
        if (parts.Count > 4)
        {
            var ageParts = parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var ap in ageParts)
            {
                if (int.TryParse(ap, out int a))
                    ages.Add(a);
                else
                    ages.Add(null);
            }
        }
        // Store first age for backward compatibility
        contentLine.Photographer = parts.Count > 5 ? parts[5] : "";
        // Split measurements by '|', store as list
        List<string> measurements = new();
        List<int?> bustSizes = new();
        List<int?> waistSizes = new();
        List<int?> hipSizes = new();
        List<string?> cupSizes = new();
        if (parts.Count > 6)
        {
            measurements = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            int modelCount = contentLine.ModelNames.Count;
            int measurementCount = measurements.Count;
            for (int i = 0; i < modelCount; i++)
            {
                string m = (i < measurementCount) ? measurements[i] : (measurementCount > 0 ? measurements[measurementCount - 1] : "");
                var sizes = m.Split('-');
                // Accept measurements with only bust/cup, or bust-waist-hip
                if (sizes.Length == 1 && !string.IsNullOrWhiteSpace(sizes[0]))
                {
                    // Only bust (may have cup size)
                    var bustPart = sizes[0].Trim();
                    string? cupSize = null;
                    if (bustPart.Length > 0 && char.IsLetter(bustPart[bustPart.Length - 1]))
                    {
                        if (bustPart.Length > 1 && char.IsLetter(bustPart[bustPart.Length - 2]) && bustPart[bustPart.Length - 1] == bustPart[bustPart.Length - 2])
                        {
                            cupSize = bustPart.Substring(bustPart.Length - 2, 2).ToUpper();
                            bustPart = bustPart.Substring(0, bustPart.Length - 2);
                        }
                        else
                        {
                            cupSize = bustPart[bustPart.Length - 1].ToString().ToUpper();
                            bustPart = bustPart.Substring(0, bustPart.Length - 1);
                        }
                    }
                    bustSizes.Add(int.TryParse(bustPart, out var bust) ? bust : null);
                    waistSizes.Add(null);
                    hipSizes.Add(null);
                    cupSizes.Add(cupSize);
                }
                else if (sizes.Length == 3)
                {
                    // Bust (may have cup size)
                    var bustPart = sizes[0].Trim();
                    string? cupSize = null;
                    if (bustPart.Length > 0 && char.IsLetter(bustPart[bustPart.Length - 1]))
                    {
                        if (bustPart.Length > 1 && char.IsLetter(bustPart[bustPart.Length - 2]) && bustPart[bustPart.Length - 1] == bustPart[bustPart.Length - 2])
                        {
                            cupSize = bustPart.Substring(bustPart.Length - 2, 2).ToUpper();
                            bustPart = bustPart.Substring(0, bustPart.Length - 2);
                        }
                        else
                        {
                            cupSize = bustPart[bustPart.Length - 1].ToString().ToUpper();
                            bustPart = bustPart.Substring(0, bustPart.Length - 1);
                        }
                    }
                    bustSizes.Add(int.TryParse(bustPart, out var bust) ? bust : null);
                    waistSizes.Add(int.TryParse(sizes[1].Trim(), out var waist) ? waist : null);
                    hipSizes.Add(int.TryParse(sizes[2].Trim(), out var hip) ? hip : null);
                    cupSizes.Add(cupSize);
                }
                else
                {
                    bustSizes.Add(null);
                    waistSizes.Add(null);
                    hipSizes.Add(null);
                    cupSizes.Add(null);
                }
            }
        }
        contentLine.ModelSize = parts.Count > 6 ? parts[6] : "";
        contentLine.Ages = ages;
        contentLine.Measurements = measurements;
        contentLine.BustSizes = bustSizes;
        contentLine.WaistSizes = waistSizes;
        contentLine.HipSizes = hipSizes;
        contentLine.CupSizes = cupSizes;

        // Special handling for Model and Cover categories
        if (contentLine.Category.Equals("Model", StringComparison.OrdinalIgnoreCase) ||
            contentLine.Category.Equals("Cover", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(contentLine.Title) && !string.IsNullOrWhiteSpace(contentLine.ModelName))
            {
                contentLine.Title = contentLine.ModelName;
                contentLine.ModelName = parts.Count > 4 && !int.TryParse(parts[4], out _) ? parts[4] : "";
                
                if (parts.Count > 5 && int.TryParse(parts[5], out int newAge))
                    contentLine.Age = newAge;
                    
                contentLine.Photographer = parts.Count > 6 ? parts[6] : "";
                contentLine.ModelSize = parts.Count > 7 ? parts[7] : "";
            }
        }

        // Parse model sizes
        ParseModelSizes(contentLine);

        // Validate all fields
        ValidateContentLine(contentLine);

        return contentLine;
    }

    public bool IsHeaderLine(string line, out string title, out int volume, out int number)
    {
        title = string.Empty;
        volume = 0;
        number = 0;

        var parts = SplitRespectingEscapedCommas(line);
        
        if (parts.Count >= 3 && int.TryParse(parts[1], out volume) && int.TryParse(parts[2], out number))
        {
            title = parts[0];
            return true;
        }

        return false;
    }

    private List<string> SplitRespectingEscapedCommas(string line)
    {
        var parts = new List<string>();
        var currentPart = new System.Text.StringBuilder();
        bool escaped = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (escaped)
            {
                currentPart.Append(c);
                escaped = false;
            }
            else if (c == '\\' && i + 1 < line.Length)
            {
                escaped = true;
            }
            else if (c == ',')
            {
                parts.Add(currentPart.ToString().Trim());
                currentPart.Clear();
            }
            else
            {
                currentPart.Append(c);
            }
        }

        // Add the last part
        parts.Add(currentPart.ToString().Trim());

        return parts;
    }

    private List<int> ParsePageNumbers(string pageStr, out bool hasError)
    {
        var pages = new List<int>();
        hasError = false;

        if (string.IsNullOrWhiteSpace(pageStr))
        {
            hasError = true;
            return pages;
        }

        // Split by '|' first (AND operator)
        var parts = pageStr.Split('|');
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            
            if (trimmed.Contains("-"))
            {
                // Handle range format (e.g., "5-10")
                var rangeParts = trimmed.Split('-');
                
                if (rangeParts.Length != 2 || 
                    string.IsNullOrWhiteSpace(rangeParts[0]) || 
                    string.IsNullOrWhiteSpace(rangeParts[1]))
                {
                    hasError = true;
                }
                else if (int.TryParse(rangeParts[0].Trim(), out int start) && 
                         int.TryParse(rangeParts[1].Trim(), out int end))
                {
                    if (end >= start)
                    {
                        for (int pg = start; pg <= end; pg++)
                            pages.Add(pg);
                    }
                    else
                    {
                        hasError = true;
                    }
                }
                else
                {
                    hasError = true;
                }
            }
            else if (int.TryParse(trimmed, out int page))
            {
                // Handle single page number
                pages.Add(page);
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                hasError = true;
            }
        }

        return pages;
    }

    private void ValidateContentLine(ContentLine contentLine)
    {
        // Validate category (required and must be valid)
        if (string.IsNullOrWhiteSpace(contentLine.Category))
        {
            contentLine.ValidationErrors.Add("Category is required");
        }
        else if (!_validCategories.Contains(contentLine.Category))
        {
            contentLine.ValidationErrors.Add($"Invalid category '{contentLine.Category}'. Valid categories: {string.Join(", ", _validCategories.OrderBy(c => c))}");
        }

        // Validate title (required for most categories)
        var categoriesWithoutTitles = new[] { "Index", "Cover", "Letters", "Travel", "Cartoons", "Editorial" };
        if (string.IsNullOrWhiteSpace(contentLine.Title) && 
            !categoriesWithoutTitles.Any(c => contentLine.Category.Equals(c, StringComparison.OrdinalIgnoreCase)))
        {
            contentLine.ValidationErrors.Add("Title is required for this category");
        }

        // Age validation is handled during parsing (must be valid int)

        // Validate model sizes (must be in correct format if provided, allow multiple sets separated by '|')
        if (!string.IsNullOrWhiteSpace(contentLine.ModelSize))
        {
            var measurementSets = contentLine.ModelSize.Split('|');
            foreach (var set in measurementSets)
            {
                var sizes = set.Trim().Split('-');
                if (sizes.Length == 1)
                {
                    // Only bust/cup provided, allow this
                    var bustPart = sizes[0].Trim();
                    if (bustPart.Length == 0)
                    {
                        contentLine.ValidationErrors.Add($"Model measurements '{set.Trim()}' must provide at least a bust size");
                        continue;
                    }
                    // Validate bust part (strip cup if present)
                    if (bustPart.Length > 0 && char.IsLetter(bustPart[bustPart.Length - 1]))
                    {
                        if (bustPart.Length > 1 && char.IsLetter(bustPart[bustPart.Length - 2]) && bustPart[bustPart.Length - 1] == bustPart[bustPart.Length - 2])
                        {
                            bustPart = bustPart.Substring(0, bustPart.Length - 2);
                        }
                        else
                        {
                            bustPart = bustPart.Substring(0, bustPart.Length - 1);
                        }
                    }
                    if (!int.TryParse(bustPart, out int bustSize) || bustSize < 10 || bustSize > 99)
                    {
                        contentLine.ValidationErrors.Add($"Invalid bust measurement '{sizes[0]}' in set '{set.Trim()}' (must be 10-99)");
                    }
                }
                else if (sizes.Length == 3)
                {
                    for (int i = 0; i < sizes.Length; i++)
                    {
                        var measurementPart = sizes[i].Trim();
                        // For bust size, strip trailing cup size letter(s) if present (C, DD, etc.)
                        if (i == 0 && measurementPart.Length > 0 && char.IsLetter(measurementPart[measurementPart.Length - 1]))
                        {
                            // Check for double letter (DD, FF, etc.)
                            if (measurementPart.Length > 1 && char.IsLetter(measurementPart[measurementPart.Length - 2]) && 
                                measurementPart[measurementPart.Length - 1] == measurementPart[measurementPart.Length - 2])
                            {
                                measurementPart = measurementPart.Substring(0, measurementPart.Length - 2);
                            }
                            else
                            {
                                measurementPart = measurementPart.Substring(0, measurementPart.Length - 1);
                            }
                        }
                        if (!int.TryParse(measurementPart, out int size) || size < 10 || size > 99)
                        {
                            string part = i == 0 ? "bust" : i == 1 ? "waist" : "hip";
                            contentLine.ValidationErrors.Add($"Invalid {part} measurement '{sizes[i]}' in set '{set.Trim()}' (must be 10-99)");
                        }
                    }
                }
                else
                {
                    contentLine.ValidationErrors.Add($"Model measurements '{set.Trim()}' must be in format: bust-waist-hip (e.g., 34-24-36 or 34C-24-36) or just bust/cup (e.g., 44DD)");
                }
            }
        }
    }

    private void ParseModelSizes(ContentLine contentLine)
    {
        if (string.IsNullOrWhiteSpace(contentLine.ModelSize))
            return;

        var sizes = contentLine.ModelSize.Split('-');
        if (sizes.Length >= 3)
        {
            // First part might have cup size appended (e.g., "34C", "34DD")
            var bustPart = sizes[0].Trim();
            var cupSize = "";
            
            // Extract cup size if present (one or two letters at end)
            if (bustPart.Length > 0 && char.IsLetter(bustPart[bustPart.Length - 1]))
            {
                // Check for double letter (DD, FF, etc.)
                if (bustPart.Length > 1 && char.IsLetter(bustPart[bustPart.Length - 2]) && 
                    bustPart[bustPart.Length - 1] == bustPart[bustPart.Length - 2])
                {
                    cupSize = bustPart.Substring(bustPart.Length - 2, 2).ToUpper();
                    bustPart = bustPart.Substring(0, bustPart.Length - 2);
                }
                else
                {
                    cupSize = bustPart[bustPart.Length - 1].ToString().ToUpper();
                    bustPart = bustPart.Substring(0, bustPart.Length - 1);
                }
            }
            
            if (int.TryParse(bustPart, out var bust))
            {
                contentLine.BustSize = bust;
                if (!string.IsNullOrEmpty(cupSize))
                    contentLine.CupSize = cupSize;
            }
            
            if (int.TryParse(sizes[1], out var waist))
                contentLine.WaistSize = waist;
            if (int.TryParse(sizes[2], out var hip))
                contentLine.HipSize = hip;
        }
    }
}
