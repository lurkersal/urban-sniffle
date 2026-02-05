namespace FindLinks.Models;

/// <summary>
/// Represents magazine issue information
/// </summary>
public record MagazineIssueInfo
{
    public required string Magazine { get; init; }
    public required int Volume { get; init; }
    public required int Number { get; init; }
    public int Year { get; init; }
}
