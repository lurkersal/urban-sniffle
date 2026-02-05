namespace FindLinks.Services;

/// <summary>
/// Service for extracting text from images using OCR
/// </summary>
public interface IOcrService
{
    string ExtractText(string imagePath);
}
