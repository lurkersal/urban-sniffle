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
        var articles = await _db.GetAllArticlesAsync(category, searchQuery: null, page, perPage: 100);
        
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
    /// GET /articles/{id} - Redirects to issue page with article auto-opened
    /// </summary>
    [HttpGet("/articles/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var article = await _db.GetArticleAsync(id);
        
        if (article == null)
            return NotFound();
        
        // Redirect to issue page with article auto-open parameter
        return Redirect($"/issues/{article.IssueId}?openArticle={id}");
    }
}

