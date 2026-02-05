using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MagazineViewer.Models;
using MagazineViewer.Services;

namespace MagazineViewer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MagazineDatabase _db;

    public HomeController(ILogger<HomeController> logger, MagazineDatabase db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var magazines = await _db.GetMagazinesAsync();
        var categories = await _db.GetCategoriesAsync();
        return View((magazines, categories));
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
