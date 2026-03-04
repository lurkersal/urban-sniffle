using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Shared;
using Microsoft.Extensions.Logging;

namespace IndexEditor.Services;

/// <summary>
/// Implementation of IIndexFileService for loading and saving index files.
/// Consolidates duplicate loading logic from MainWindow and ArticleEditorView.
/// </summary>
public class IndexFileService : IIndexFileService
{
    private readonly ILogger<IndexFileService> _logger;

    public IndexFileService(ILogger<IndexFileService> logger)
    {
        _logger = logger;
    }

    public (string Magazine, string Volume, string Number, string Year, List<ArticleLine> Articles, List<MagazineLink> Links) LoadFromFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));
        }

        // Normalize path
        var normalizedPath = Path.GetFullPath(folderPath);
        
        if (!Directory.Exists(normalizedPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {normalizedPath}");
        }

        _logger.LogInformation("LoadFromFolder: Input folder: '{Folder}'", folderPath);
        _logger.LogInformation("LoadFromFolder: Normalized '{Original}' to '{Normalized}'", folderPath, normalizedPath);

        // Determine which index file exists
        var jsonIndexPath = Path.Combine(normalizedPath, "_index.json");
        var txtIndexPath = Path.Combine(normalizedPath, "_index.txt");

        if (File.Exists(jsonIndexPath))
        {
            return LoadFromJsonFile(jsonIndexPath, normalizedPath);
        }
        else if (File.Exists(txtIndexPath))
        {
            return LoadFromTxtFile(txtIndexPath, normalizedPath);
        }
        else
        {
            throw new FileNotFoundException($"No index file found in folder: {normalizedPath}");
        }
    }

    public void SaveToFolder(string folderPath, string magazine, string volume, string number, string year,
        List<ArticleLine> articles, List<MagazineLink> links)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));
        }

        var normalizedPath = Path.GetFullPath(folderPath);
        
        if (!Directory.Exists(normalizedPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {normalizedPath}");
        }

        // Always save as JSON
        var jsonIndexPath = Path.Combine(normalizedPath, "_index.json");
        
        _logger.LogInformation("SaveToFolder: Saving to '{Path}'", jsonIndexPath);

        // Create backup first
        BackupIndexFile(normalizedPath);

        // Save using IndexJsonSerializer
        Common.Shared.IndexJsonSerializer.SaveToJson(normalizedPath, magazine, volume, number, year, articles, links);
        
        _logger.LogInformation("SaveToFolder: Successfully saved index file");
    }

    public bool HasJsonIndex(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        var normalizedPath = Path.GetFullPath(folderPath);
        var jsonIndexPath = Path.Combine(normalizedPath, "_index.json");
        
        return File.Exists(jsonIndexPath);
    }

    public void BackupIndexFile(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        var normalizedPath = Path.GetFullPath(folderPath);
        var indexPath = Path.Combine(normalizedPath, "_index.json");
        
        if (!File.Exists(indexPath))
        {
            // Try TXT format
            indexPath = Path.Combine(normalizedPath, "_index.txt");
        }

        if (File.Exists(indexPath))
        {
            var backupPath = indexPath + "~";
            
            try
            {
                File.Copy(indexPath, backupPath, overwrite: true);
                _logger.LogInformation("BackupIndexFile: Created backup at '{BackupPath}'", backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BackupIndexFile: Failed to create backup");
            }
        }
    }

    private (string, string, string, string, List<ArticleLine>, List<MagazineLink>) LoadFromJsonFile(string jsonPath, string folderPath)
    {
        _logger.LogInformation("LoadFromFolder: Loading from _index.json");

        var (magazine, volume, number, year, articles, links) = Common.Shared.IndexJsonSerializer.LoadFromJson(folderPath);

        // Deduplicate links
        var linksList = links ?? new List<MagazineLink>();
        var uniqueLinks = linksList
            .GroupBy(link => $"{link.Page}|{link.Magazine}|{link.Volume}|{link.Issue}")
            .Select(g => g.First())
            .ToList();

        if (uniqueLinks.Count != linksList.Count)
        {
            _logger.LogInformation("Loaded {Original} unique links from JSON (deduplicated from {Total})", 
                uniqueLinks.Count, linksList.Count);
        }

        return (magazine, volume, number, year, articles, uniqueLinks);
    }

    private (string, string, string, string, List<ArticleLine>, List<MagazineLink>) LoadFromTxtFile(string txtPath, string folderPath)
    {
        _logger.LogInformation("LoadFromFolder: Loading from _index.txt");

        var lines = File.ReadAllLines(txtPath);
        var articles = new List<ArticleLine>();
        string magazine = "";
        string volume = "—";
        string number = "—";
        string year = "—";

        // Parse header for metadata (volume, issue, etc.)
        if (lines.Length > 0 && lines[0].StartsWith("#"))
        {
            // Parse metadata from first line: # Magazine Name Vol X No Y
            var headerParts = lines[0].TrimStart('#').Trim().Split(new[] { "Vol", "No" }, StringSplitOptions.None);
            if (headerParts.Length >= 3)
            {
                magazine = headerParts[0].Trim();
                volume = headerParts[1].Trim();
                number = headerParts[2].Trim();
            }
        }

        // Parse articles
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            try
            {
                var article = IndexEditor.Shared.IndexFileParser.ParseArticleLine(line);
                if (article != null)
                {
                    articles.Add(article);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse line: {Line}", line);
            }
        }

        _logger.LogInformation("LoadFromFolder: Loaded {Count} articles from TXT file", articles.Count);

        // TXT files don't have links
        return (magazine, volume, number, year, articles, new List<MagazineLink>());
    }
}



