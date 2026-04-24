using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;
using TheArchive.Models;
using System.Text.Json;

namespace TheArchive.Controllers;

/// <summary>
/// Controller for viewing and editing _index.json files for issues
/// </summary>
public class IndexEditorController : Controller
{
    private readonly ArchiveDatabase _db;

    public IndexEditorController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /index-editor/{issueId} - Get the _index.json content for an issue
    /// </summary>
    [HttpGet("/index-editor/{issueId}")]
    public async Task<IActionResult> GetIndexJson(int issueId)
    {
        try
        {
            var indexPath = await GetIndexPathForIssue(issueId);
            if (indexPath == null)
            {
                return NotFound(new { error = "Could not determine issue folder path" });
            }

            var jsonPath = Path.Combine(indexPath, "_index.json");
            if (!System.IO.File.Exists(jsonPath))
            {
                return NotFound(new { error = $"_index.json not found at {jsonPath}" });
            }

            var content = await System.IO.File.ReadAllTextAsync(jsonPath);
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /index-editor/{issueId} - Save the _index.json content for an issue
    /// </summary>
    [HttpPost("/index-editor/{issueId}")]
    public async Task<IActionResult> SaveIndexJson(int issueId, [FromBody] SaveIndexRequest request)
    {
        try
        {
            var indexPath = await GetIndexPathForIssue(issueId);
            if (indexPath == null)
            {
                return NotFound(new { error = "Could not determine issue folder path" });
            }

            var jsonPath = Path.Combine(indexPath, "_index.json");
            
            // Validate JSON before saving
            try
            {
                JsonDocument.Parse(request.Content);
            }
            catch (JsonException ex)
            {
                return BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
            }

            // Create backup
            if (System.IO.File.Exists(jsonPath))
            {
                var backupPath = jsonPath + ".bak";
                System.IO.File.Copy(jsonPath, backupPath, overwrite: true);
            }

            // Save the file
            await System.IO.File.WriteAllTextAsync(jsonPath, request.Content);

            return Ok(new { success = true, message = "Index saved successfully", path = jsonPath });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /index-editor/{issueId}/links - Get page links from _index.json for an issue
    /// </summary>
    [HttpGet("/index-editor/{issueId}/links")]
    public async Task<IActionResult> GetPageLinks(int issueId)
    {
        try
        {
            var indexPath = await GetIndexPathForIssue(issueId);
            if (indexPath == null)
            {
                return NotFound(new { error = "Could not determine issue folder path" });
            }

            var jsonPath = Path.Combine(indexPath, "_index.json");
            if (!System.IO.File.Exists(jsonPath))
            {
                return Ok(new { links = new List<object>() }); // No index file, no links
            }

            var content = await System.IO.File.ReadAllTextAsync(jsonPath);
            var indexData = JsonSerializer.Deserialize<IndexFileJson>(content, IndexJsonSerializer.JsonOptions);

            if (indexData?.Links == null || indexData.Links.Count == 0)
            {
                return Ok(new { links = new List<object>() }); // No links in index
            }

            // Return links with page and target issue info
            var links = indexData.Links.Select(link => new
            {
                page = link.Page,
                magazine = link.Magazine,
                volume = link.Volume,
                issue = link.Issue
            }).ToList();

            return Ok(new { links });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Helper method to determine the folder path for an issue based on its first image
    /// </summary>
    private async Task<string?> GetIndexPathForIssue(int issueId)
    {
        // Get the first content record to extract the directory
        var content = await _db.GetIssueContentAsync(issueId);
        var firstContent = content.FirstOrDefault();
        
        if (firstContent?.ImagePath == null)
        {
            return null;
        }

        // Get the full path
        var imagePath = firstContent.ImagePath;
        var imageRoot = Environment.GetEnvironmentVariable("MAGAZINE_IMAGE_ROOT");
        
        if (string.IsNullOrEmpty(imageRoot))
        {
            throw new InvalidOperationException("MAGAZINE_IMAGE_ROOT environment variable is not set");
        }

        string fullImagePath;
        if (Path.IsPathRooted(imagePath))
        {
            fullImagePath = imagePath;
        }
        else
        {
            fullImagePath = Path.Combine(imageRoot, imagePath);
        }

        // Extract directory from full image path
        var directory = Path.GetDirectoryName(fullImagePath);
        return directory;
    }
}

public class SaveIndexRequest
{
    public string Content { get; set; } = string.Empty;
}

