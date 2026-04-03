using System;
using System.Collections.Generic;
using Common.Shared;

namespace IndexEditor.Services;

/// <summary>
/// High-level service for coordinating file operations between IIndexFileService and EditorState.
/// Encapsulates folder loading, saving, and validation logic.
/// </summary>
public interface IFileOperationsService
{
    /// <summary>
    /// Loads articles and metadata from a folder and updates application state.
    /// </summary>
    /// <param name="folderPath">Path to the magazine folder</param>
    /// <returns>Discovered links from the index file, or empty dictionary</returns>
    Dictionary<int, List<MagazineLink>> LoadFolder(string folderPath);
    
    /// <summary>
    /// Saves the current articles and metadata to a folder.
    /// </summary>
    /// <param name="folderPath">Path to the magazine folder</param>
    /// <param name="articles">Articles to save</param>
    /// <param name="discoveredLinks">Links to save</param>
    void SaveFolder(string folderPath, List<ArticleLine> articles, Dictionary<int, List<MagazineLink>> discoveredLinks);
    
    /// <summary>
    /// Validates if an image exists in a folder for a given page number.
    /// </summary>
    /// <param name="folderPath">Path to the magazine folder</param>
    /// <param name="pageNumber">Page number to check</param>
    /// <returns>True if image exists</returns>
    bool ImageExists(string folderPath, int pageNumber);
    
    /// <summary>
    /// Finds the first existing image page in a folder within a range.
    /// </summary>
    /// <param name="folderPath">Path to the magazine folder</param>
    /// <param name="startPage">Start of range (inclusive)</param>
    /// <param name="endPage">End of range (inclusive)</param>
    /// <returns>First existing page number, or null if none found</returns>
    int? FindFirstImagePage(string folderPath, int startPage, int endPage);
}

