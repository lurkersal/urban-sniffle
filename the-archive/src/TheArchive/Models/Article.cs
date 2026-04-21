namespace TheArchive.Models;

/// <summary>
/// Represents an article/feature within a magazine issue
/// Maps to spec's "articles" entity
/// </summary>
public class Article
{
    public int ArticleId { get; set; }
    public int CategoryId { get; set; }
    public string? Title { get; set; }
    
    // Related data from joins
    public int IssueId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int PageStart { get; set; }
    public string? Author { get; set; }
    public string? Photographer { get; set; }
    public int? ModelId { get; set; }
    public string? ModelName { get; set; }
    public string? FirstImagePath { get; set; }
    
    // Issue date fields (from Issue table joins)
    public int Year { get; set; }
    public int Volume { get; set; }
    public int Number { get; set; }
    public string MagazineName { get; set; } = string.Empty;
    
    // Computed properties for the spec
    public string Id => $"art-{ArticleId}";
    public string Category => MapCategoryToSpec(CategoryName);
    
    /// <summary>
    /// Maps existing database categories to spec categories
    /// </summary>
    private static string MapCategoryToSpec(string dbCategory)
    {
        return dbCategory.ToLowerInvariant() switch
        {
            "feature" => "feature",
            "model" => "model",
            "pinup" => "model",
            "motoring" => "motoring",
            "letters" => "letters",
            "interview" => "interview",
            "editorial" => "editorial",
            "travel" => "travel",
            "review" => "technology",
            "fiction" => "culture",
            "humour" => "culture",
            "cartoons" => "culture",
            "fashion" => "fashion",
            "health" => "health",
            _ => "feature"
        };
    }
}

