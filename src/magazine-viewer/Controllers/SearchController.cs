using Microsoft.AspNetCore.Mvc;
using MagazineViewer.Models;
using MagazineViewer.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MagazineViewer.Controllers
{
    public class SearchController : Controller
    {
        private readonly MagazineDatabase _db;
        public SearchController(MagazineDatabase db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int? magazineId, string? category, int? year, string? keyword)
        {
            var magazines = await _db.GetMagazinesAsync();
            var categories = await _db.GetCategoriesAsync();
            var years = await _db.GetYearsAsync();
            IEnumerable<ArticleResult> articles = Enumerable.Empty<ArticleResult>();
            bool anyCriteria = magazineId.HasValue || !string.IsNullOrEmpty(category) || year.HasValue || !string.IsNullOrEmpty(keyword);
            if (anyCriteria)
            {
                articles = await _db.SearchArticlesAsync(magazineId, category, year, keyword);
            }
            var model = new ArticleSearchViewModel
            {
                Magazines = magazines,
                Categories = categories,
                Years = years,
                Articles = articles,
                SelectedMagazineId = magazineId,
                SelectedCategory = category,
                SelectedYear = year,
                Keyword = keyword
            };
            return View(model);
        }
    }
}
