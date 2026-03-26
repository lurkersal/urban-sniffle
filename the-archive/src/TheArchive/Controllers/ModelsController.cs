using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;
using TheArchive.Models;

namespace TheArchive.Controllers;

public class ModelsController : Controller
{
    private readonly ArchiveDatabase _db;

    public ModelsController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /models - All models
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var models = await _db.GetModelsAsync();
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Models", IsActive = true, IsLast = true }
        };
        
        return View(models);
    }

    /// <summary>
    /// GET /models/{id} - Single model detail
    /// </summary>
    [HttpGet("/models/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        var model = await _db.GetModelAsync(id);
        
        if (model == null)
            return NotFound();
        
        var articles = await _db.GetArticlesByModelAsync(model.ModelId);
        var issues = await _db.GetIssuesByModelAsync(model.ModelId);
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Models", Url = "/models", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = model.Name, IsActive = true, IsLast = true }
        };
        
        ViewBag.Model = model;
        ViewBag.Articles = articles;
        ViewBag.Issues = issues;
        
        return View();
    }
}

