namespace MagazineViewer.Models;

public class ModelWithArticleTypes : Model
{
    public int CoverCount { get; set; }
    public int ModelArticleCount { get; set; }
    public string? FirstArticleImagePath { get; set; }

    // Stores all magazine names this model has appeared in
    public HashSet<string> MagazineNames { get; set; } = new HashSet<string>();
}
