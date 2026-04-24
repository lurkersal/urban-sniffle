using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;
using TheArchive.Models;
using TheArchive.ViewModels;

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
        var thumbnails = await _db.GetModelThumbnailsAsync();
        
        var viewModels = models.Select(m => new ModelIndexViewModel
        {
            Model = m,
            ThumbnailPath = thumbnails.TryGetValue(m.ModelId, out var path) ? path : null
        }).ToList();
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Models", IsActive = true, IsLast = true }
        };
        
        return View(viewModels);
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
        
        // Get the first image of the first article for the model thumbnail
        string? modelThumbnail = null;
        if (articles.Count > 0)
        {
            var firstArticle = articles.OrderBy(a => a.IssueId).ThenBy(a => a.PageStart).First();
            var articlePages = await _db.GetArticlePageImagesAsync(firstArticle.ArticleId);
            if (articlePages.Count > 0)
            {
                modelThumbnail = articlePages[0].ImagePath;
            }
        }
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = "Models", Url = "/models", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = model.Name, IsActive = true, IsLast = true }
        };
        
        var viewModel = new ModelDetailViewModel
        {
            Model = model,
            Articles = articles,
            Issues = issues,
            ModelThumbnail = modelThumbnail
        };
        
        return View(viewModel);
    }
}

