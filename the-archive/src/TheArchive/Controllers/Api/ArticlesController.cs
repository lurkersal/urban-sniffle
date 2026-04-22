using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;

namespace TheArchive.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly ArchiveDatabase _db;

    public ArticlesController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/articles - All articles (paginated). Supports ?category= and ?q= filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category = null, [FromQuery] string? q = null, [FromQuery] int page = 1, [FromQuery] int per_page = 50)
    {
        var articles = await _db.GetAllArticlesAsync(category, q, page, per_page);
        return Ok(articles);
    }

    /// <summary>
    /// GET /api/v1/articles/:id - Single article with full metadata
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var article = await _db.GetArticleAsync(id);
        
        if (article == null)
            return NotFound(new { error = "Article not found" });
        
        return Ok(article);
    }
}

