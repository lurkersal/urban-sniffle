using System.Collections.Generic;
namespace MagazineViewer.Models
{
    public class ArticleSearchViewModel
    {
        public IEnumerable<Magazine> Magazines { get; set; } = new List<Magazine>();
        public IEnumerable<string> Categories { get; set; } = new List<string>();
        public IEnumerable<int> Years { get; set; } = new List<int>();
        public IEnumerable<ArticleResult> Articles { get; set; } = new List<ArticleResult>();
        public int? SelectedMagazineId { get; set; }
        public string? SelectedCategory { get; set; }
        public int? SelectedYear { get; set; }
        public string? Keyword { get; set; }
    }
    public class ArticleResult
    {
        public int ArticleId { get; set; }
        public int IssueId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MagazineName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Page { get; set; }
    }
}
