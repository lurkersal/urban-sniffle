using SixLabors.ImageSharp;

namespace MagazineViewer.Models;

public class MagazineContent
{
    public int ContentId { get; set; }
    public int IssueId { get; set; }
    public int Page { get; set; }
    public int ArticleId { get; set; }
    public string? ImagePath { get; set; }
    
    // Extended properties
    public int MagazineId { get; set; }
    public string MagazineName { get; set; } = string.Empty;
    public int Volume { get; set; }
    public int Number { get; set; }
    public int Year { get; set; }
    public string IssueName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Photographer { get; set; }
    public string? Illustrator { get; set; }
    public string? ModelName { get; set; }
    public string? ModelNames { get; set; }
    public int? ModelAge { get; set; }
    public string? ModelMeasurements { get; set; }

    public static int CalculatePageCount(IEnumerable<MagazineContent> contents)
    {
        return contents.Count();
    }
}
