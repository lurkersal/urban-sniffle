namespace MagazineViewer.Models;

public class LinkedIssueView
{
    // From issuelink
    public int LinkedMagazineId { get; set; }
    public int LinkedVolume { get; set; }
    public int LinkedNumber { get; set; }
    public int Page { get; set; } // The page in the current article this link is associated with
    // From issues (may be null if not found)
    public int? IssueId { get; set; }
    public string? MagazineName { get; set; }
    public int? Year { get; set; }
    public string? CoverImagePath { get; set; }
}
