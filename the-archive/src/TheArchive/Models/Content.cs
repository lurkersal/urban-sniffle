namespace TheArchive.Models;

/// <summary>
/// Represents a single page of content within an article
/// </summary>
public class Content
{
    public int ContentId { get; set; }
    public int ArticleId { get; set; }
    public int IssueId { get; set; }
    public int Page { get; set; }
    public string? ImagePath { get; set; }
}

