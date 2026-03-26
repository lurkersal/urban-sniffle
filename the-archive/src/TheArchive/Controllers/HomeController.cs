using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TheArchive.Models;
using TheArchive.Services;

namespace TheArchive.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ArchiveDatabase _db;
    private readonly ArchiveStatistics _stats;

    public HomeController(ILogger<HomeController> logger, ArchiveDatabase db, ArchiveStatistics stats)
    {
        _logger = logger;
        _db = db;
        _stats = stats;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var magazines = await _db.GetMagazinesAsync();
            var statistics = await _stats.GetStatisticsAsync();
            
            ViewBag.Magazines = magazines;
            ViewBag.MagazineCount = magazines.Count;
            ViewBag.Stats = statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            ViewBag.Status = $"Database Error: {ex.Message}";
            ViewBag.MagazineCount = 0;
            ViewBag.Magazines = new List<Magazine>();
            ViewBag.Stats = new ArchiveStats();
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
