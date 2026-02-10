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
                try { candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")); } catch (Exception ex) { DebugLogger.LogException("CategoryService.Initialize: cwd candidate", ex); }
                try { candidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "appsettings.json")); } catch (Exception ex) { DebugLogger.LogException("CategoryService.Initialize: base dir candidate", ex); }
                try
                {
                    var asmFolder = Path.GetDirectoryName(typeof(CategoryService).Assembly.Location);
                    if (!string.IsNullOrWhiteSpace(asmFolder)) candidates.Add(Path.Combine(asmFolder, "appsettings.json"));
                }
                catch (Exception ex) { DebugLogger.LogException("CategoryService.Initialize: asm folder candidate", ex); }

                string? foundPath = null;
                foreach (var cand in candidates.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
                {
                    try { if (File.Exists(cand)) { foundPath = cand; break; } } catch (Exception ex) { DebugLogger.LogException("CategoryService.Initialize: file exists check", ex); }
                }

                try { File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"CategoryService.Initialize: candidates={string.Join(";", candidates)} found={foundPath}\n"); } catch (Exception ex) { DebugLogger.LogException("CategoryService.Initialize: write debug file", ex); }

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
                    try { File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"CategoryService: DB returned {cats?.Count ?? 0} categories\n"); } catch (Exception ex) { DebugLogger.LogException("CategoryService.Initialize: write db debug", ex); }
                    if (cats != null && cats.Count > 0)
                    {
                        // Replace collection contents on caller thread; callers should marshal to UI thread.
                        Categories.Clear();
                        foreach (var c in cats.OrderBy(s => s)) Categories.Add(c);
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("CategoryService DB error", ex);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CategoryService init error", ex);
            }
        }
    }
}
