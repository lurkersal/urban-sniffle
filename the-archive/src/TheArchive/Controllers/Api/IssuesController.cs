using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;

namespace TheArchive.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
public class IssuesController : ControllerBase
{
    private readonly ArchiveDatabase _db;

    public IssuesController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/issues - All issues across all magazines, sorted by date desc
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int per_page = 50)
    {
        var issues = await _db.GetAllIssuesAsync(page, per_page);
        return Ok(issues);
    }

    /// <summary>
    /// GET /api/v1/issues/:id - Single issue with metadata
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var issue = await _db.GetIssueAsync(id);
        
        if (issue == null)
            return NotFound(new { error = "Issue not found" });
        
        return Ok(issue);
    }

    /// <summary>
    /// GET /api/v1/issues/:id/articles - All articles in an issue, sorted by page_start asc
    /// </summary>
    [HttpGet("{id:int}/articles")]
    public async Task<IActionResult> GetArticles(int id, [FromQuery] string? category = null)
    {
        var issue = await _db.GetIssueAsync(id);
        
        if (issue == null)
            return NotFound(new { error = "Issue not found" });
        
        var articles = await _db.GetArticlesByIssueAsync(id, category);
        
        return Ok(articles);
    }

    /// <summary>
    /// GET /api/v1/issues/find - Find an issue by magazine, volume, and number
    /// </summary>
    [HttpGet("find")]
    public async Task<IActionResult> FindIssue([FromQuery] string magazine, [FromQuery] string volume, [FromQuery] string number)
    {
        if (string.IsNullOrWhiteSpace(magazine) || string.IsNullOrWhiteSpace(volume) || string.IsNullOrWhiteSpace(number))
        {
            return BadRequest(new { error = "Magazine, volume, and number are required" });
        }

        var issue = await _db.FindIssueByMagazineVolNoAsync(magazine, volume, number);
        
        if (issue == null)
        {
            return NotFound(new { error = $"Issue not found: {magazine} Vol.{volume} #{number}" });
        }
        
        return Ok(issue);
    }
}
