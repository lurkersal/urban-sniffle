namespace FindLinks.Services;

using System.Text.RegularExpressions;
using FindLinks.Models;
using System.Collections.Generic;

/// <summary>
/// Parser for magazine contents page OCR text
/// </summary>
public class MagazineContentParser : IContentParser
{
    public List<ContentEntry> ParseContentsPage(string ocrText)
    {
        var entries = new List<ContentEntry>();
        var lines = ocrText.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
            // Simple regex: Title ... Page
            var match = Regex.Match(trimmedLine, @"^(.*?)(\.{2,}|\s+)(\d+)$");
            if (match.Success)
            {
                entries.Add(new ContentEntry
                {
                    Title = match.Groups[1].Value.Trim(),
                    Page = match.Groups[3].Value.Trim(),
                    Category = "Unknown"
                });
            }
        }
        return entries;
    }
}
