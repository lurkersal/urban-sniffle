namespace TheArchive.Models;

/// <summary>
/// Represents a photographer or other contributor
/// </summary>
public class Contributor
{
    public int ContributorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AppearanceCount { get; set; }
    
    // Computed property for URL slug
    public string Slug => Name.ToLower().Replace(" ", "-").Replace(".", "");
}

