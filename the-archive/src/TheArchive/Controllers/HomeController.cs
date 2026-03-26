using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TheArchive.Models;
using TheArchive.Services;

namespace TheArchive.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ArchiveDatabase _db;

    public HomeController(ILogger<HomeController> logger, ArchiveDatabase db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var magazines = await _db.GetMagazinesAsync();
            ViewBag.MagazineCount = magazines.Count;
            ViewBag.Magazines = magazines;
            ViewBag.Status = "Database Connected!";
        }
        catch (Exception ex)
        {
            ViewBag.Status = $"Database Error: {ex.Message}";
            ViewBag.MagazineCount = 0;
            ViewBag.Magazines = new List<Magazine>();
        }
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
