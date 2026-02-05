namespace FindLinks.Services;

using System.Collections.Generic;
using FindLinks.Models;

/// <summary>
/// Service for parsing magazine content from OCR text
/// </summary>
public interface IContentParser
{
    List<ContentEntry> ParseContentsPage(string ocrText);
}
