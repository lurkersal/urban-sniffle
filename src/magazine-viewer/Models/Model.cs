namespace MagazineViewer.Models;

public class Model
{
    // Number of unique articles this model has appeared in
    public int ArticleCount { get; set; }
    public int ModelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? YearOfBirth { get; set; }
    public int? BustSize { get; set; }
    public int? WaistSize { get; set; }
    public int? HipSize { get; set; }
    public string? CupSize { get; set; }
    
    public string Measurements => (BustSize.HasValue && WaistSize.HasValue && HipSize.HasValue) 
        ? $"{BustSize}{CupSize ?? ""}-{WaistSize}-{HipSize}" 
        : "";
}
