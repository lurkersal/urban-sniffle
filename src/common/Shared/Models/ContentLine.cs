namespace common.Shared.Models;

public class ContentLine
{
    public List<int> Pages { get; set; } = new();
    public bool HasPageNumberError { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> ModelNames { get; set; } = new();
    public int? Age { get; set; }
    public List<int?> Ages { get; set; } = new();
    public List<string> Contributors { get; set; } = new();
    public List<string> Illustrators { get; set; } = new();
    public string ModelSize { get; set; } = string.Empty;
    public List<string> Measurements { get; set; } = new();
    public int? BustSize { get; set; }
    public int? WaistSize { get; set; }
    public int? HipSize { get; set; }
    public string? CupSize { get; set; }
    public List<int?> BustSizes { get; set; } = new();
    public List<int?> WaistSizes { get; set; } = new();
    public List<int?> HipSizes { get; set; } = new();
    public List<string?> CupSizes { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
    public bool HasValidationError => ValidationErrors.Count > 0;
    public bool WasAutoInserted { get; set; }

    // Helper properties for backward compatibility during parsing
    public string ModelName
    {
        get => ModelNames.Count > 0 ? ModelNames[0] : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) ModelNames = SplitNames(value); }
    }
    public string Photographer
    {
        // For backward compatibility, reflect the first contributor as photographer
        get => Contributors.Count > 0 ? string.Join(" | ", Contributors) : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) Contributors = SplitNames(value); }
    }
    public string Author
    {
        // For backward compatibility, reflect the first contributor as author
        get => Contributors.Count > 0 ? string.Join(" | ", Contributors) : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) Contributors = SplitNames(value); }
    }
    public string Illustrator
    {
        get => Illustrators.Count > 0 ? string.Join(" | ", Illustrators) : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) Illustrators = SplitNames(value); }
    }
    public string Contributor
    {
        get => Contributors.Count > 0 ? string.Join(" | ", Contributors) : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) Contributors = SplitNames(value); }
    }

    private static List<string> SplitNames(string value)
    {
        return value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
