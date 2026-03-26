namespace TheArchive.Models;

/// <summary>
/// Represents a model featured in magazine articles
/// Maps to spec's "models" entity
/// </summary>
public class Model
{
    public int ModelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? YearOfBirth { get; set; }
    public int? BustSize { get; set; }
    public int? WaistSize { get; set; }
    public int? HipSize { get; set; }
    public string? CupSize { get; set; }
    
    // Computed/aggregate data
    public int AppearanceCount { get; set; }
    public List<string> IssueAppearances { get; set; } = new();
    
    // Computed properties for the spec
    public string Id => ToSlug(Name);
    public string? RealName { get; set; } // Not in existing schema, will be null
    public int? Age { get; set; } // Article-specific, from ContentModel table
    public string? Height { get; set; } // Not in existing schema
    public string Measurements => GetMeasurements();
    public string? Hair { get; set; } // Not in existing schema
    public string? Eyes { get; set; } // Not in existing schema
    
    private string GetMeasurements()
    {
        if (BustSize.HasValue && WaistSize.HasValue && HipSize.HasValue)
        {
            var bust = CupSize != null ? $"{BustSize}{CupSize}" : BustSize.ToString();
            return $"{bust}-{WaistSize}-{HipSize}";
        }
        return string.Empty;
    }
    
    private static string ToSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace(".", "");
    }
}

