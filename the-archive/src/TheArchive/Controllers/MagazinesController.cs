using Microsoft.AspNetCore.Mvc;
using TheArchive.Services;
using TheArchive.Models;

namespace TheArchive.Controllers;

public class MagazinesController : Controller
{
    private readonly ArchiveDatabase _db;

    public MagazinesController(ArchiveDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /magazines/{id} - Single magazine with all its issues
    /// </summary>
    [HttpGet("/magazines/{id}")]
    public async Task<IActionResult> Index(string id)
    {
        var magazine = await _db.GetMagazineAsync(id);
        
        if (magazine == null)
            return NotFound();
        
        var issues = await _db.GetIssuesByMagazineAsync(magazine.MagazineId);
        
        ViewBag.Breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", IsActive = false, IsLast = false },
            new BreadcrumbItem { Text = magazine.Name, IsActive = true, IsLast = true }
        };
        
        ViewBag.Magazine = magazine;
        ViewBag.Issues = issues;
        
        return View();
    }
}

