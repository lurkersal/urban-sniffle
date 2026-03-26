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
    /// GET /issues - All issues across all magazines
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var issues = await _db.GetAllIssuesAsync(page: 1, perPage: 100);
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "All Issues", IsActive = true, IsLast = true }
        };
        
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

