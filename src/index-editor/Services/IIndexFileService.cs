using System.Collections.Generic;
using Common.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for loading and saving index files (_index.txt or _index.json).
/// Centralizes file I/O operations to eliminate duplication.
/// </summary>
public interface IIndexFileService
{
    /// <summary>
    /// Loads articles, metadata, and links from a magazine folder.
    /// Automatically detects JSON or TXT format.
    /// </summary>
    /// <param name="folderPath">Path to the magazine folder</param>
    /// <returns>Tuple of (magazine, volume, number, year, articles, links)</returns>
    (string Magazine, string Volume, string Number, string Year, List<ArticleLine> Articles, List<MagazineLink> Links) LoadFromFolder(string folderPath);
    
    /// <summary>
    /// Saves articles, metadata, and links to a magazine folder.
    /// Uses JSON format by default.
    /// </summary>
    /// <param name="folderPath">Path to the magazine folder</param>
    /// <param name="magazine">Magazine name</param>
    /// <param name="volume">Volume number</param>
    /// <param name="number">Issue number</param>
    /// <param name="year">Year</param>
    /// <param name="articles">List of articles</param>
    /// <param name="links">List of magazine links</param>
    void SaveToFolder(string folderPath, string magazine, string volume, string number, string year, 
        List<ArticleLine> articles, List<MagazineLink> links);
    
    /// <summary>
    /// Determines if the folder contains a JSON index file.
    /// </summary>
    bool HasJsonIndex(string folderPath);
    
    /// <summary>
    /// Creates a backup of the index file before saving.
    /// </summary>
    void BackupIndexFile(string folderPath);
}


