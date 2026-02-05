namespace MagazineViewer.Models;

public class SearchFilters
{
    public string? ModelName { get; set; }
    public string? ArticleTitle { get; set; } // NEW: for searching by article title
    public int? BustSize { get; set; }
    public int? WaistSize { get; set; }
    public int? HipSize { get; set; }
    public int? MagazineId { get; set; }
    public string? Category { get; set; }

    // For UI state persistence
    public string? ViewMode { get; set; }
    public string? IconSize { get; set; }
}
