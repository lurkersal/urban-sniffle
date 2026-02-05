namespace MagazineParser.Models;

public class ContentLine
{
    public List<int> Pages { get; set; } = new();
    public bool HasPageNumberError { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> ModelNames { get; set; } = new();
    public int? Age { get; set; }
    public List<int?> Ages { get; set; } = new();
    public List<string> Photographers { get; set; } = new();
    public List<string> Authors { get; set; } = new();
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
        get => Photographers.Count > 0 ? string.Join(" | ", Photographers) : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) Photographers = SplitNames(value); }
    }
    public string Author
    {
        get => Authors.Count > 0 ? string.Join(" | ", Authors) : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) Authors = SplitNames(value); }
    }
    public string Illustrator
    {
        get => Illustrators.Count > 0 ? string.Join(" | ", Illustrators) : string.Empty;
        set { if (!string.IsNullOrWhiteSpace(value)) Illustrators = SplitNames(value); }
    }

    private static List<string> SplitNames(string value)
    {
        return value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
