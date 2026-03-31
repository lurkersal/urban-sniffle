using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;
using TheArchive.Models;

namespace TheArchive.Controllers;

public class ArticlesController : Controller
{
    private readonly ArchiveDatabase _db;

    public ArticlesController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /articles - All articles with optional category filter
    /// </summary>
    public async Task<IActionResult> Index(string? category, int page = 1)
    {
        var articles = await _db.GetAllArticlesAsync(category, page, perPage: 100);
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Articles", IsActive = true, IsLast = true }
        };
        
        ViewBag.Category = category;
        ViewBag.Page = page;
        
        return View(articles);
    }

    /// <summary>
    /// GET /articles/{id} - Single article detail
    /// </summary>
    [HttpGet("/articles/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var article = await _db.GetArticleAsync(id);
        
        if (article == null)
            return NotFound();
        
        var issue = await _db.GetIssueAsync(article.IssueId);
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Articles", Url = "/articles", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = article.Title ?? "Article", IsActive = true, IsLast = true }
        };
        
        ViewBag.Article = article;
        ViewBag.Issue = issue;
        
        return View();
    }
}

