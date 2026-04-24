using TheArchive.Models;

namespace TheArchive.ViewModels;

public class ModelDetailViewModel
{
    public Model Model { get; set; } = null!;
    public List<Article> Articles { get; set; } = new();
    public List<Issue> Issues { get; set; } = new();
    public string? ModelThumbnail { get; set; }
    
    // Computed property for initials
    public string Initials
    {
        get
        {
            if (string.IsNullOrEmpty(Model?.Name))
                return "?";
            
            var words = Model.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", words.Select(w => w.Length > 0 ? w[0].ToString() : ""));
        }
    }
}

