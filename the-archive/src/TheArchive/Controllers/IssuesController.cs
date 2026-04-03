using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;
using TheArchive.Models;

namespace TheArchive.Controllers;

public class IssuesController : Controller
{
    private readonly ArchiveDatabase _db;

    public IssuesController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /issues - All issues across all magazines with optional filters
    /// </summary>
    public async Task<IActionResult> Index(
        int? yearFrom,
        int? yearTo,
        string? magazines)
    {
        // Parse magazine IDs from comma-separated string
        List<int>? magazineIds = null;
        if (!string.IsNullOrEmpty(magazines))
        {
            magazineIds = magazines.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var magId) ? magId : 0)
                .Where(id => id > 0)
                .ToList();
        }
        
        // Get filtered issues
        var issues = await _db.GetFilteredIssuesAsync(yearFrom, yearTo, magazineIds);
        
        // Get all magazines for filter dropdown
        var allMagazines = await _db.GetMagazinesAsync();
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "All Issues", IsActive = true, IsLast = true }
        };
        
        ViewBag.AllMagazines = allMagazines;
        ViewBag.YearFrom = yearFrom;
        ViewBag.YearTo = yearTo;
        ViewBag.SelectedMagazineIds = magazineIds ?? new List<int>();
        
        return View(issues);
    }

    /// <summary>
    /// GET /issues/{id} - Single issue detail
    /// </summary>
    [HttpGet("/issues/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var issue = await _db.GetIssueAsync(id);
        
        if (issue == null)
            return NotFound();
        
        var articles = await _db.GetArticlesByIssueAsync(id);
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = issue.MagazineName, Url = $"/magazines/{issue.MagazineSlug}", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = issue.DateLabel, IsActive = true, IsLast = true }
        };
        
        ViewBag.Issue = issue;
        ViewBag.Articles = articles;
        
        return View();
    }
}

