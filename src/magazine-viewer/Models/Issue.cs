namespace MagazineViewer.Models;

public class Issue
{
    public int IssueId { get; set; }
    public int MagazineId { get; set; }
    public int Volume { get; set; }
    public int Number { get; set; }
    public int Year { get; set; }
    
    public string MagazineName { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public string DisplayName => $"{MagazineName} V{Volume} N{Number}";
}
