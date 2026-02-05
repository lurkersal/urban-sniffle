namespace FindLinks.Services;

/// <summary>
/// Service for file system operations related to magazine images
/// </summary>
public class MagazineFileSystemService : IFileSystemService
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
    private static readonly string[] ContentsPageNumbers = { "3", "03", "003", "5", "05", "005", "6", "06", "006", "7", "07", "007" };

    public string? FindContentsPage(string magazineDirectory)
    {
        foreach (var pageNum in ContentsPageNumbers)
        {
            foreach (var ext in ImageExtensions)
            {
                var path = Path.Combine(magazineDirectory, pageNum + ext);
                if (File.Exists(path))
                    return path;
            }
        }
        return null;
    }

    public string? FindImageForPage(string magazineDirectory, int pageNumber)
    {
        foreach (var ext in ImageExtensions)
        {
            var imagePath = Path.Combine(magazineDirectory, $"{pageNumber}{ext}");
            if (File.Exists(imagePath))
                return imagePath;
        }
        return null;
    }
}
