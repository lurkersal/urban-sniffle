using TheArchive.Models;

namespace TheArchive.ViewModels;

public class IssueDetailViewModel
{
    public Issue Issue { get; set; } = null!;
    public List<Article> Articles { get; set; } = new();
    public string? CoverImagePath { get; set; }
}

