using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IndexEditor.Shared
{
    // Singleton-like service to load categories from DB once and expose them as an ObservableCollection.
    public static class CategoryService
    {
        private static bool _initialized = false;
        public static ObservableCollection<string> Categories { get; } = new ObservableCollection<string>();

        public static async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                var candidates = new List<string>();
                try { candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")); } catch { }
                try { candidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "appsettings.json")); } catch { }
                try
                {
                    var asmFolder = Path.GetDirectoryName(typeof(CategoryService).Assembly.Location);
                    if (!string.IsNullOrWhiteSpace(asmFolder)) candidates.Add(Path.Combine(asmFolder, "appsettings.json"));
                }
                catch { }

                string? foundPath = null;
                foreach (var cand in candidates.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
                {
                    try { if (File.Exists(cand)) { foundPath = cand; break; } } catch { }
                }

                try { File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"CategoryService.Initialize: candidates={string.Join(";", candidates)} found={foundPath}\n"); } catch { }

                if (string.IsNullOrWhiteSpace(foundPath)) return;

                using var fs = File.OpenRead(foundPath);
                using var doc = await JsonDocument.ParseAsync(fs);
                if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connSection)) return;
                if (!connSection.TryGetProperty("MagazineDb", out var connStringElem)) return;
                var connString = connStringElem.GetString();
                if (string.IsNullOrWhiteSpace(connString)) return;

                try
                {
                    var cats = await IndexEditor.Shared.CategoryRepository.GetCategoriesAsync(connString);
                    try { File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"CategoryService: DB returned {cats?.Count ?? 0} categories\n"); } catch { }
                    if (cats != null && cats.Count > 0)
                    {
                        // Replace collection contents on caller thread; callers should marshal to UI thread.
                        Categories.Clear();
                        foreach (var c in cats.OrderBy(s => s)) Categories.Add(c);
                    }
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"CategoryService DB error: {ex}\n"); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"CategoryService init error: {ex}\n"); } catch { }
            }
        }
    }
}

