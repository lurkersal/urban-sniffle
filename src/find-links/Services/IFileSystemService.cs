namespace FindLinks.Services;

/// <summary>
/// Service for file system operations related to magazine images
/// </summary>
public interface IFileSystemService
{
    string? FindContentsPage(string magazineDirectory);
    string? FindImageForPage(string magazineDirectory, int pageNumber);
}
