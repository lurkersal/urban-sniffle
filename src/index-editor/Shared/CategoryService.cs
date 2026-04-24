using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace IndexEditor.Shared
{
    /// <summary>
    /// Service to expose article categories as an ObservableCollection.
    /// Categories are now defined in the ArticleCategory enum (from schema_postgres.sql).
    /// No database connection required.
    /// </summary>
    public static class CategoryService
    {
        private static bool _initialized = false;
        public static ObservableCollection<string> Categories { get; } = new ObservableCollection<string>();

        public static Task InitializeAsync()
        {
            if (_initialized) return Task.CompletedTask;
            _initialized = true;

            try
            {
                // Load categories from enum
                var cats = ArticleCategoryHelper.GetAllCategories();
                Categories.Clear();
                foreach (var c in cats)
                {
                    Categories.Add(c);
                }

                DebugLogger.Log($"CategoryService: Loaded {Categories.Count} categories from ArticleCategory enum");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CategoryService init error", ex);
            }

            return Task.CompletedTask;
        }
    }
}
