using Microsoft.AspNetCore.Mvc;
using MagazineViewer.Models;
using MagazineViewer.Services;

namespace MagazineViewer.Controllers;

public class IssuesController : Controller
{

    [HttpGet]
    public async Task<IActionResult> FindByVolNo(string magazineName, int volume, int number)
    {
        if (string.IsNullOrWhiteSpace(magazineName))
            return Json(new { found = false });
        var issue = await _db.FindIssueByVolNoAsync(magazineName, volume, number);
        if (issue != null)
            return Json(new { found = true, issueId = issue.IssueId });
        return Json(new { found = false });
    }
    private readonly MagazineDatabase _db;

    public IssuesController(MagazineDatabase db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? magazineId, string? category)
    {
        var issues = await _db.GetIssuesAsync(magazineId);
        var magazines = await _db.GetMagazinesAsync();
        
        return View((issues, magazines, magazineId, category));
    }

    public async Task<IActionResult> Details(int id)
    {
        var content = await _db.GetIssueContentAsync(id);
        var firstContent = content.FirstOrDefault();
        if (firstContent == null)
        {
            return NotFound();
        }
        
        // Get the cover image (first page)
        var coverImage = content.OrderBy(c => c.Page).FirstOrDefault()?.ImagePath;
        
        var issueInfo = new Issue
        {
            IssueId = id,
            MagazineId = firstContent.MagazineId,
            MagazineName = firstContent.MagazineName ?? "Unknown",
            Volume = firstContent.Volume,
            Number = firstContent.Number,
            Year = firstContent.Year,
            CoverImagePath = coverImage
        };
        
        return View((issueInfo, content));
    }

    public async Task<IActionResult> Article(int issueId, int articleId)
    {
        var allContent = await _db.GetIssueContentAsync(issueId);
        var firstContent = allContent.FirstOrDefault();
        if (firstContent == null)
        {
            return NotFound();
        }

        var issueInfo = new Issue
        {
            IssueId = issueId,
            MagazineId = firstContent.MagazineId,
            MagazineName = firstContent.MagazineName ?? "Unknown",
            Volume = firstContent.Volume,
            Number = firstContent.Number,
            Year = firstContent.Year
        };

        var articleContent = allContent.Where(c => c.ArticleId == articleId).ToList();
        if (!articleContent.Any())
        {
            return NotFound();
        }

        // Get linked issues for this article
        var linkedIssues = await _db.GetLinkedIssuesForArticleAsync(articleId);

        // DEBUG LOGGING TO FILE (project root)
        try
        {
            var logPath = System.IO.Path.Combine("..", "debug.log");
            using (var log = System.IO.File.AppendText(logPath))
            {
                log.WriteLine($"[DEBUG] ArticleId: {articleId}, LinkedIssues.Count: {(linkedIssues == null ? -1 : linkedIssues.Count())}");
                if (linkedIssues != null)
                {
                    foreach (var li in linkedIssues)
                    {
                        log.WriteLine($"[DEBUG] Linked Issue: Id={li.IssueId}, Vol={li.LinkedVolume}, No={li.LinkedNumber}");
                    }
                }
                log.Flush();
            }
        }
        catch { }

        return View((issueInfo, articleContent.AsEnumerable(), linkedIssues));
    }

    public async Task<IActionResult> EditIndex(int issueId)
    {
        var issueContent = await _db.GetIssueContentAsync(issueId);
        var firstContent = issueContent.FirstOrDefault();
        if (firstContent?.ImagePath == null)
        {
            return NotFound();
        }

        // If the image path is relative, prepend MAGAZINE_IMAGE_ROOT
        var imagePath = firstContent.ImagePath;
        var imageRoot = Environment.GetEnvironmentVariable("MAGAZINE_IMAGE_ROOT");
        string fullImagePath = imagePath;
        if (!Path.IsPathRooted(imagePath) && !string.IsNullOrEmpty(imageRoot))
        {
            fullImagePath = Path.Combine(imageRoot, imagePath);
        }

        // Extract directory from full image path
        var directory = Path.GetDirectoryName(fullImagePath);
        if (string.IsNullOrEmpty(directory))
        {
            return NotFound();
        }

        var indexPath = Path.Combine(directory, "_index.txt");
        if (!System.IO.File.Exists(indexPath))
        {
            return NotFound("_index.txt file not found");
        }

        // Read the file content and return it in a text editor view
        var fileContent = await System.IO.File.ReadAllTextAsync(indexPath);

        return View("EditIndex", (indexPath, fileContent, issueId, firstContent.ArticleId));
    }

    [HttpPost]
    public async Task<IActionResult> SaveIndex(string filePath, string content, int issueId, int articleId)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return NotFound("File not found");
        }
        
        try
        {
            await System.IO.File.WriteAllTextAsync(filePath, content);
            TempData["SuccessMessage"] = "_index.txt file saved successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error saving file: {ex.Message}";
        }
        
        return RedirectToAction("Article", new { issueId = issueId, articleId = articleId });
    }
}
