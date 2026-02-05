namespace MagazineViewer.Models
{
    public class ArticleSearchFilters
    {
        public int? MagazineId { get; set; }
        public string? Category { get; set; }
        public int? Year { get; set; }
        public string? Keyword { get; set; }
    }
}
