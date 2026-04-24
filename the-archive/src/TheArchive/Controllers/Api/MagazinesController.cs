using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;

namespace TheArchive.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
public class MagazinesController : ControllerBase
{
    private readonly ArchiveDatabase _db;

    public MagazinesController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/magazines - List all magazines with issue count
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q = null)
    {
        var magazines = await _db.GetMagazinesAsync();
        
        if (!string.IsNullOrWhiteSpace(q))
        {
            magazines = magazines
                .Where(m => m.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        return Ok(magazines);
    }

    /// <summary>
    /// GET /api/v1/magazines/:id - Single magazine with metadata
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var magazine = await _db.GetMagazineAsync(id);
        
        if (magazine == null)
            return NotFound(new { error = "Magazine not found" });
        
        return Ok(magazine);
    }

    /// <summary>
    /// GET /api/v1/magazines/:id/issues - All issues for a magazine, newest first
    /// </summary>
    [HttpGet("{id}/issues")]
    public async Task<IActionResult> GetIssues(string id)
    {
        var magazine = await _db.GetMagazineAsync(id);
        
        if (magazine == null)
            return NotFound(new { error = "Magazine not found" });
        
        var issues = await _db.GetIssuesByMagazineAsync(magazine.MagazineId);
        
        return Ok(issues);
    }
}

