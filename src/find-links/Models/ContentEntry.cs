namespace FindLinks.Models;

/// <summary>
/// Represents a content entry from a magazine contents page
/// </summary>
public record ContentEntry
{
    public required string Title { get; init; }
    public required string Page { get; set; }
    public required string Category { get; init; }
    public string Photographer { get; init; } = "";
    public string Age { get; set; } = "";
    public string Measurements { get; set; } = "";
    public string ModelName { get; set; } = "";
}
