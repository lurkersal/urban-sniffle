using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;

namespace TheArchive.Controllers.Api;

[ApiController]
[Route("api/v1/pages")]
public class PagesController : ControllerBase
{
    private readonly ArchiveDatabase _db;

    public PagesController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/pages/issue/{issueId} - Get all page images for an issue
    /// </summary>
    [HttpGet("issue/{issueId}")]
    public async Task<IActionResult> GetIssuePages(int issueId)
    {
        var pages = await _db.GetPageImagePathsAsync(issueId);
        var maxPage = await _db.GetMaxPageAsync(issueId);
        
        return Ok(new
        {
            issueId,
            maxPage,
            pages
        });
    }

    /// <summary>
    /// GET /api/v1/pages/article/{articleId} - Get all page images for an article
    /// </summary>
    [HttpGet("article/{articleId}")]
    public async Task<IActionResult> GetArticlePages(int articleId)
    {
        var pages = await _db.GetArticlePageImagesAsync(articleId);
        
        return Ok(new
        {
            articleId,
            pages = pages.Select(p => new { page = p.Page, imagePath = p.ImagePath }).ToList()
        });
    }
}

