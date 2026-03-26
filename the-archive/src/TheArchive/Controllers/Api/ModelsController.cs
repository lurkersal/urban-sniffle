using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;

namespace TheArchive.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly ArchiveDatabase _db;

    public ModelsController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/models - All models with appearance count
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q = null)
    {
        var models = await _db.GetModelsAsync();
        
        if (!string.IsNullOrWhiteSpace(q))
        {
            models = models
                .Where(m => m.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        return Ok(models);
    }

    /// <summary>
    /// GET /api/v1/models/:id - Single model profile
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var model = await _db.GetModelAsync(id);
        
        if (model == null)
            return NotFound(new { error = "Model not found" });
        
        return Ok(model);
    }

    /// <summary>
    /// GET /api/v1/models/:id/articles - All articles featuring this model, with issue and magazine data embedded
    /// </summary>
    [HttpGet("{id}/articles")]
    public async Task<IActionResult> GetArticles(string id)
    {
        var model = await _db.GetModelAsync(id);
        
        if (model == null)
            return NotFound(new { error = "Model not found" });
        
        var articles = await _db.GetArticlesByModelAsync(model.ModelId);
        
        return Ok(articles);
    }

    /// <summary>
    /// GET /api/v1/models/:id/issues - All issues this model appears in
    /// </summary>
    [HttpGet("{id}/issues")]
    public async Task<IActionResult> GetIssues(string id)
    {
        var model = await _db.GetModelAsync(id);
        
        if (model == null)
            return NotFound(new { error = "Model not found" });
        
        var issues = await _db.GetIssuesByModelAsync(model.ModelId);
        
        return Ok(issues);
    }
}

