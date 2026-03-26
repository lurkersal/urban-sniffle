namespace TheArchive.Models;

/// <summary>
/// Represents a magazine issue (e.g., "Club International Vol 12 No 4, 1983")
/// Maps to spec's "issues" entity
/// </summary>
public class Issue
{
    public int IssueId { get; set; }
    public int MagazineId { get; set; }
    public int Volume { get; set; }
    public int Number { get; set; }
    public int Year { get; set; }
    public bool LinkScanPerformed { get; set; }
    
    // Related data
    public string MagazineName { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public int ArticleCount { get; set; }
    public int PageCount { get; set; }
    
    // Computed properties for the spec
    public string Id => $"{ToSlug(MagazineName)}-{Year:D2}{GetMonthCode()}"; // e.g., "ci-83apr"
    public string MagazineSlug => ToSlug(MagazineName);
    public string VolumeDisplay => $"Vol. {Volume}";
    public string IssueNumber => $"Issue {Number}";
    public string DateLabel => $"{GetMonthName()} {Year}";
    public DateTime DateSort => new DateTime(Year, GetEstimatedMonth(), 1);
    
    private static string ToSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "");
    }
    
    private int GetEstimatedMonth()
    {
        // Estimate month based on issue number (assuming 12 issues per year)
        return Math.Min(12, Math.Max(1, Number));
    }
    
    private string GetMonthCode()
    {
        var month = GetEstimatedMonth();
        string[] codes = { "", "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
        return month >= 1 && month <= 12 ? codes[month] : "jan";
    }
    
    private string GetMonthName()
    {
        var month = GetEstimatedMonth();
        return new DateTime(2000, month, 1).ToString("MMMM");
    }
}

