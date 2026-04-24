namespace TheArchive.Models;

/// <summary>
/// Represents a magazine title (e.g., "Club International", "Mayfair", "Men Only")
/// Maps to spec's "magazines" entity
/// </summary>
public class Magazine
{
    public int MagazineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    
    // Computed properties for the spec
    public string Id => ToSlug(Name); // URL-safe slug
    public string Tagline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? Founded { get; set; }
    public int IssueCount { get; set; }
    
    private static string ToSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "");
    }
}

