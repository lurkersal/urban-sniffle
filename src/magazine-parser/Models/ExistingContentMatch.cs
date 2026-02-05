namespace MagazineParser.Models;

public class ExistingContentMatch
{
    public bool Matches { get; set; }
    public string DbRepresentation { get; set; } = string.Empty;
    public List<int> ContentIds { get; set; } = new();
}
