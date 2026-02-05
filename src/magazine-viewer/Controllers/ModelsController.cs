using Microsoft.AspNetCore.Mvc;
using MagazineViewer.Models;
using MagazineViewer.Services;

namespace MagazineViewer.Controllers;

public class ModelsController : Controller
{
    private readonly MagazineDatabase _db;

    public ModelsController(MagazineDatabase db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(SearchFilters filters)
    {
        // Ensure ViewMode and IconSize are preserved in ViewData for Razor/JS
        if (!string.IsNullOrEmpty(Request.Query["ViewMode"]))
            filters.ViewMode = Request.Query["ViewMode"];
        if (!string.IsNullOrEmpty(Request.Query["IconSize"]))
            filters.IconSize = Request.Query["IconSize"];

        var models = (await _db.SearchModelsWithArticleTypesAsync(filters)).ToList();
        var magazines = await _db.GetMagazinesAsync();
        var categories = await _db.GetCategoriesAsync();

        // Populate MagazineNames for each model
        foreach (var model in models)
        {
            var content = await _db.GetModelContentAsync(model.ModelId);
            var magNames = content.Select(c => c.MagazineName).Where(n => !string.IsNullOrEmpty(n)).Distinct();
            model.MagazineNames = new HashSet<string>(magNames);
        }

        return View(((IEnumerable<ModelWithArticleTypes>)models, filters, magazines, categories));
    }

    public async Task<IActionResult> Details(int? id, string? name)
    {
        Model? model = null;
        
        if (id.HasValue)
        {
            model = await _db.GetModelAsync(id.Value);
        }
        else if (!string.IsNullOrEmpty(name))
        {
            model = await _db.GetModelByNameAsync(name);
        }
        
        if (model == null)
        {
            return NotFound();
        }
        
        var content = await _db.GetModelContentAsync(model.ModelId);
        var issueIds = content.Select(c => c.IssueId).Distinct();
        var coverPaths = await _db.GetIssueCoverPathsAsync(issueIds);
        
        return View((model, content, coverPaths));
    }
}
