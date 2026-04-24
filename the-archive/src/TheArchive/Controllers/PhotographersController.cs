using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;
using TheArchive.Models;

namespace TheArchive.Controllers;

public class PhotographersController : Controller
{
    private readonly ArchiveDatabase _db;

    public PhotographersController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /photographers - All photographers/contributors
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var photographers = await _db.GetContributorsAsync();
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Photographers", IsActive = true, IsLast = true }
        };
        
        return View(photographers);
    }

    /// <summary>
    /// GET /photographers/{id} - Single photographer detail
    /// </summary>
    [HttpGet("/photographers/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        var photographer = await _db.GetContributorAsync(id);
        
        if (photographer == null)
            return NotFound();
        
        var articles = await _db.GetArticlesByContributorAsync(photographer.ContributorId);
        var issues = await _db.GetIssuesByContributorAsync(photographer.ContributorId);
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Photographers", Url = "/photographers", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = photographer.Name, IsActive = true, IsLast = true }
        };
        
        ViewBag.Photographer = photographer;
        ViewBag.Articles = articles;
        ViewBag.Issues = issues;
        
        return View();
    }
}

