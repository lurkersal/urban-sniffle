using TheArchive.Models;

namespace TheArchive.ViewModels;

public class ModelIndexViewModel
{
    public Model Model { get; set; } = null!;
    public string? ThumbnailPath { get; set; }
    
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

