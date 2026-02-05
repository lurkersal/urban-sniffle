namespace MagazineViewer.Models;

public class Magazine
{
    public int MagazineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? LogoUrl { get; set; }
}
