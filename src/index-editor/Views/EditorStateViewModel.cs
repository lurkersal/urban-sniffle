using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Common.Shared;
using IndexEditor.Shared;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Windows.Input;
using System.Collections.Specialized;

namespace IndexEditor.Views
{
    public class EditorStateViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        // Whether categories have been populated from DB. When true, ignore non-DB updates.
        // Make static so multiple VM instances don't overwrite each other's DB result.
        private static bool _categoriesLoadedFromDb = false;
        public SelectArticleCommand SelectArticleCommand { get; }
        
        // Public helper: find the first page number for the given article that has an image file in the provided folder.
        // Returns the page number if found, otherwise null.
        public int? FindFirstPageWithImage(Common.Shared.ArticleLine article, string? folder)
        {
            if (article == null || article.Pages == null || article.Pages.Count == 0)
                return null;
            if (string.IsNullOrWhiteSpace(folder))
                return null;

            foreach (var p in article.Pages.OrderBy(x => x))
            {
                var candidates = new[] {
                    Path.Combine(folder, p.ToString() + ".jpg"),
                    Path.Combine(folder, p.ToString() + ".png"),
                    Path.Combine(folder, p.ToString("D2") + ".jpg"),
                    Path.Combine(folder, p.ToString("D2") + ".png"),
                    Path.Combine(folder, p.ToString("D3") + ".jpg"),
                    Path.Combine(folder, p.ToString("D3") + ".png"),
                    Path.Combine(folder, "page-" + p.ToString() + ".jpg"),
                    Path.Combine(folder, "p" + p.ToString() + ".jpg") };
                foreach (var c in candidates)
                {
                    try { if (File.Exists(c)) return p; } catch { }
                }
            }
            return null;
        }

        // Public action: navigate to the first page with an image for the given article (falls back to min page when no image found).
        // This updates the shared EditorState.CurrentPage and triggers a state notification so the PageController will load the image.
        public void NavigateToArticle(Common.Shared.ArticleLine article)
        {
            if (article == null) return;
            int? pick = null;
            try
            {
                pick = FindFirstPageWithImage(article, IndexEditor.Shared.EditorState.CurrentFolder);
            }
            catch { }

            if (pick.HasValue)
                IndexEditor.Shared.EditorState.CurrentPage = pick.Value;
            else if (article.Pages != null && article.Pages.Count > 0)
                IndexEditor.Shared.EditorState.CurrentPage = article.Pages.Min();
            // Notify so UI updates and image gets reloaded
            IndexEditor.Shared.EditorState.NotifyStateChanged();
        }

        private ArticleLine? _selectedArticle;
        public ArticleLine? SelectedArticle
        {
            get => _selectedArticle;
            set
            {
                // Normalize the incoming article to an instance from our Articles collection if possible
                ArticleLine? incoming = value;
                if (incoming != null)
                {
                    var mapped = Articles.FirstOrDefault(a => object.ReferenceEquals(a, incoming))
                                 ?? Articles.FirstOrDefault(a => a.Pages != null && incoming.Pages != null && a.Pages.SequenceEqual(incoming.Pages) && (a.Title ?? string.Empty) == (incoming.Title ?? string.Empty));
                    if (mapped != null) incoming = mapped;
                }

                // If there's an active open segment, prevent changing selection to a DIFFERENT article.
                var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                if (activeSeg != null && activeSeg.IsActive && incoming != null)
                {
                    if (activeArticle != null && !object.ReferenceEquals(activeArticle, incoming))
                    {
                        // Inform user and do not change selection while a segment is open
                        IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first");
                        // Push a property changed so UI bindings revert to the existing selected article
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));
                        return;
                    }
                }

                // If the incoming value is null but we already have a selected article that still
                // exists in our Articles collection, ignore the transient null to avoid losing the editor view.
                if (incoming == null && _selectedArticle != null && Articles.Contains(_selectedArticle))
                {
                    // ignore transient clear
                    return;
                }

                if (_selectedArticle != incoming)
                {
                    _selectedArticle = incoming;
                    // Update IsSelected flags on all articles so UI bindings reflect selection
                    try
                    {
                        foreach (var a in Articles)
                        {
                            try { a.IsSelected = object.ReferenceEquals(a, _selectedArticle); } catch { }
                        }
                    }
                    catch { }
                     // Ensure the global EditorState reflects the current selected article so
                     // other views (PageController, etc.) can read the active article details.
                     try
                     {
                         IndexEditor.Shared.EditorState.ActiveArticle = _selectedArticle;
                         IndexEditor.Shared.EditorState.NotifyStateChanged();
                     }
                     catch { }
                     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));
                     // Notify SelectedCategory so the editor ComboBox updates to the new article's category
                     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCategory)));
                     // Also notify CurrentShownArticle which may change when SelectedArticle changes
                     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShownArticle)));
                     // Run validation after UI bindings have a chance to populate the editor fields.
                     // Schedule validation at Background priority so two-way bindings and initial control
                     // population complete first; this avoids a false-negative when the editor first shows an article.
                     Dispatcher.UIThread.Post(() => _selectedArticle?.Validate(), Avalonia.Threading.DispatcherPriority.Background);
                     // Also schedule a second validation after a short delay to handle any remaining
                     // asynchronous population that may occur after initial layout/binding.
                     Task.Run(async () =>
                     {
                         await Task.Delay(150).ConfigureAwait(false);
                         Dispatcher.UIThread.Post(() => _selectedArticle?.Validate(), Avalonia.Threading.DispatcherPriority.Background);
                     });
                 }
             }
         }

         private bool _suppressCategorySet = false;
         public string? SelectedCategory
         {
             get => SelectedArticle?.Category;
             set
             {
                 if (_suppressCategorySet) return;
                 if (SelectedArticle == null) return;
                 if (string.IsNullOrWhiteSpace(value))
                 {
                     // Ignore transient clears from the UI (ItemsSource changes) to avoid wiping the model
                     return;
                 }
                 var newVal = value!;
                 if (SelectedArticle.Category != newVal)
                 {
                     // Only accept category values that come from the DB-backed Categories list
                     if (!Categories.Contains(newVal))
                     {
                         try { IndexEditor.Shared.ToastService.Show("Category must be chosen from the predefined list"); } catch { }
                         // Re-notify so the UI reverts selection
                         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCategory)));
                         return;
                     }
                     SelectedArticle.Category = newVal;
                     // Forward notify so bindings update
                     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCategory)));
                 }
             }
         }

         public ObservableCollection<Common.Shared.ArticleLine> Articles { get; } = new();
         public ObservableCollection<string> Categories { get; } = new();

         private bool _isLoadingCategories = false;
         public bool IsLoadingCategories
         {
             get => _isLoadingCategories;
             private set
             {
                 if (_isLoadingCategories != value)
                 {
                     _isLoadingCategories = value;
                     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoadingCategories)));
                 }
             }
         }

         public EditorStateViewModel()
         {
             SelectArticleCommand = new SelectArticleCommand(this);
             // Initialize from static EditorState
             foreach (var article in EditorState.Articles)
                 Articles.Add(article);

             // IMPORTANT: Categories must come only from the database. Do not populate from Articles.
             // Leave Categories empty until DB load completes. The ComboBox will show DB-provided values only.

            // Initialize the shared CategoryService once and mirror its collection into our VM's Categories
            _ = Task.Run(async () =>
            {
                try
                {
                    await IndexEditor.Shared.CategoryService.InitializeAsync();
                    // Mirror the service collection to our VM on UI thread
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Categories.Clear();
                        foreach (var c in IndexEditor.Shared.CategoryService.Categories) Categories.Add(c);
                        // Mark that categories came from DB
                        try { _categoriesLoadedFromDb = IndexEditor.Shared.CategoryService.Categories.Count > 0; } catch { }
                        // Subscribe to future changes so we mirror updates
                        try
                        {
                            IndexEditor.Shared.CategoryService.Categories.CollectionChanged += (s, e) =>
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    Categories.Clear();
                                    foreach (var cc in IndexEditor.Shared.CategoryService.Categories) Categories.Add(cc);
                                });
                            };
                        }
                        catch { }
                     });
                 }
                 catch { }
             });

             // Asynchronously try to load categories from DB (do not block UI thread)
             IsLoadingCategories = true;
             Task.Run(async () =>
             {
                 try
                 {
                     // The CategoryService already loads categories; we just mirror from it above. Keep IsLoadingCategories for compatibility.
                     await IndexEditor.Shared.CategoryService.InitializeAsync();
                     Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                     {
                         Categories.Clear();
                         foreach (var c in IndexEditor.Shared.CategoryService.Categories) Categories.Add(c);
                     });
                 }
                 catch { }
                 finally
                 {
                     Dispatcher.UIThread.Post(() => IsLoadingCategories = false);
                 }
             });

             // Debug: Print type of every item
             // Skip debug printing of article types
              // Listen for changes
              EditorState.StateChanged += SyncArticles;
              // Also raise SelectedArticle when the global EditorState changes (e.g., CurrentPage) so bindings like SelectedArticle.ActiveSegment re-evaluate
              EditorState.StateChanged += OnEditorStateChanged;
          }

        // Returns the article that should be shown for active-segment display: prefer the selected article, otherwise the global active article
        public ArticleLine? CurrentShownArticle => SelectedArticle ?? IndexEditor.Shared.EditorState.ActiveArticle;

        // A computed display string for the active segment (mirrors ActiveSegmentToTextConverter logic), convenient for binding
        public string ActiveSegmentDisplay
        {
            get
            {
                try
                {
                    var art = CurrentShownArticle;
                    var seg = art?.ActiveSegment;
                    // If there's no active segment, fall back to LastModifiedSegment only when it's relevant
                    if (seg == null)
                    {
                        var lm = art?.LastModifiedSegment;
                        if (lm != null && (lm.IsActive || lm.WasNew || lm.OriginalEnd.HasValue))
                            seg = lm;
                    }
                     if (seg == null) return "— none —";
                     if (seg.IsActive)
                     {
                         var current = IndexEditor.Shared.EditorState.CurrentPage;
                         return $"{seg.Start} → {current}";
                     }
                     return seg.Display ?? "— none —";
                 }
                 catch { return "— none —"; }
             }
         }

         private async Task<List<string>?> LoadCategoriesFromDatabaseAsync()
         {
            try
            {
                // Try multiple likely locations for appsettings.json to avoid issues when working directory differs from app folder.
                var candidates = new List<string>();
                try { candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")); } catch { }
                try { candidates.Add(Path.Combine(AppContext.BaseDirectory ?? string.Empty, "appsettings.json")); } catch { }
                // Also try the application's assembly folder as a fallback
                try
                {
                    var asmFolder = Path.GetDirectoryName(typeof(EditorStateViewModel).Assembly.Location);
                    if (!string.IsNullOrWhiteSpace(asmFolder)) candidates.Add(Path.Combine(asmFolder, "appsettings.json"));
                }
                catch { }

                string? foundPath = null;
                foreach (var cand in candidates.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
                {
                    try
                    {
                        if (File.Exists(cand))
                        {
                            foundPath = cand;
                            break;
                        }
                    }
                    catch { }
                }

                try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"LoadCategories: candidates={string.Join(";", candidates)} found={foundPath}\n"); } catch { }

                if (string.IsNullOrWhiteSpace(foundPath))
                    return null;

                using var fs = File.OpenRead(foundPath);
                using var doc = await JsonDocument.ParseAsync(fs);
                if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connSection))
                    return null;
                if (!connSection.TryGetProperty("MagazineDb", out var connStringElem))
                    return null;
                var connString = connStringElem.GetString();
                if (string.IsNullOrWhiteSpace(connString))
                    return null;

                try
                {
                    var cats = await IndexEditor.Shared.CategoryRepository.GetCategoriesAsync(connString);
                    try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"DB returned {cats?.Count ?? 0} categories\n"); } catch { }
                    if (cats != null && cats.Count > 0)
                    {
                        return cats;
                    }
                }
                catch (Exception ex)
                {
                    try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"DB error: {ex}\n"); } catch { }
                }
                return null;
             }
             catch { return null; }
         }

        private void UpdateCategories(List<string> newCats, bool fromDatabase = false)
         {
             // Ensure selected category is preserved
             var selectedCat = SelectedArticle?.Category;
            // If categories are already loaded from DB, ignore any non-DB updates
            if (!fromDatabase && _categoriesLoadedFromDb)
            {
                try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", "UpdateCategories: skipped non-DB update because DB list already loaded\n"); } catch { }
                return;
            }

            // If this update comes from the database, prefer showing DB categories exactly (preserve selectedCategory if missing)
            if (fromDatabase && newCats != null && newCats.Count > 0)
            {
                var sorted = newCats.OrderBy(s => s).ToList();
                if (!string.IsNullOrWhiteSpace(selectedCat) && !sorted.Contains(selectedCat))
                    sorted.Add(selectedCat);

                // If we already have DB-loaded categories, avoid downgrading to a smaller set.
                if (_categoriesLoadedFromDb)
                {
                    var currentSet = new HashSet<string>(Categories);
                    var newSet = new HashSet<string>(sorted);
                    if (newSet.SetEquals(currentSet))
                    {
                        try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", "UpdateCategories: DB update identical to current set - ignored\n"); } catch { }
                        return;
                    }
                    // Accept only if new set is a superset or strictly larger (new categories added)
                    if (newSet.IsSupersetOf(currentSet) && newSet.Count >= currentSet.Count)
                    {
                        Categories.Clear();
                        foreach (var c in sorted)
                            Categories.Add(c);
                        try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"Updated Categories (DB superset applied): {string.Join(",", sorted)}\n"); } catch { }
                        return;
                    }
                    else
                    {
                        try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"UpdateCategories: DB update skipped (would shrink/replace smaller set): {string.Join(",", sorted)}\n"); } catch { }
                        return;
                    }
                }

                // First DB load: accept unconditionally
                Categories.Clear();
                foreach (var c in sorted)
                    Categories.Add(c);
                _categoriesLoadedFromDb = true;
                try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"Updated Categories (DB preferred first load): {string.Join(",", sorted)}\n"); } catch { }
                return;
            }

            // Fallback: merge categories discovered from articles (existing behavior)
            if (!string.IsNullOrWhiteSpace(selectedCat) && !newCats.Contains(selectedCat))
            {
                newCats.Add(selectedCat);
            }
            var union = new HashSet<string>(Categories);
            foreach (var c in newCats)
                union.Add(c);
            var merged = union.OrderBy(s => s).ToList();
            foreach (var c in merged)
            {
                if (!Categories.Contains(c))
                    Categories.Add(c);
            }
            try { System.IO.File.AppendAllText("/tmp/index_editor_categories_debug.txt", $"Updated Categories (merged): {string.Join(",", merged)}\n"); } catch { }
         }

        private void SyncArticles()
        {
            // During initial sync we suppress category writes that may arise from control rebinds
            _suppressCategorySet = true;
            // Perform an in-place minimal-diff update of the ObservableCollection to avoid
            // recreating item controls. This preserves control instances and their bindings
            // so TwoWay bindings (like Category) don't accidentally write into the wrong model.
            var desired = EditorState.Articles ?? new List<Common.Shared.ArticleLine>();

            // Remove items not present in desired
            for (int i = Articles.Count - 1; i >= 0; i--)
            {
                var existing = Articles[i];
                if (!desired.Contains(existing))
                {
                    Articles.RemoveAt(i);
                }
            }

            // Insert/move items to match desired order
            for (int targetIndex = 0; targetIndex < desired.Count; targetIndex++)
            {
                var article = desired[targetIndex];
                var currentIndex = Articles.IndexOf(article);
                if (currentIndex == -1)
                {
                    Articles.Insert(targetIndex, article);
                }
                else if (currentIndex != targetIndex)
                {
                    Articles.Move(currentIndex, targetIndex);
                }

                // Ensure we are subscribed to property changes once
                article.PropertyChanged -= OnArticlePropertyChanged;
                article.PropertyChanged += OnArticlePropertyChanged;
            }

            // Categories are exclusively DB-sourced; do not recompute or update Categories from articles here.
            // SyncArticles completed
            _suppressCategorySet = false;
        }

        private void OnArticlePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ArticleLine article)
            {
                // OnArticlePropertyChanged
                // If pages or category changed, we may need to reorder
                if (e.PropertyName == nameof(ArticleLine.Pages) || e.PropertyName == nameof(ArticleLine.PagesText) || e.PropertyName == nameof(ArticleLine.Category))
                 {
                     ReorderArticlesByPage();
                 }
                // If the article's active segment or last-modified segment changed, notify ActiveSegmentDisplay
                if (e.PropertyName == nameof(ArticleLine.ActiveSegment) || e.PropertyName == nameof(ArticleLine.LastModifiedSegment))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveSegmentDisplay)));
                }
             }
         }

        private void ReorderArticlesByPage()
        {
            // Suppress category writes while we reorder/move items to avoid transient writes
            _suppressCategorySet = true;
            // Compute ordered list (articles with no pages end up after those with pages)
            var ordered = EditorState.Articles
                .OrderBy(a => (a.Pages != null && a.Pages.Count > 0) ? a.Pages.Min() : int.MaxValue)
                .ThenBy(a => a.Title)
                .ToList();

            // Update backing list
            EditorState.Articles = ordered;

            // Reorder the ObservableCollection in-place to avoid recreating item controls
            for (int targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
            {
                var article = ordered[targetIndex];
                var currentIndex = Articles.IndexOf(article);
                if (currentIndex == -1)
                {
                    // If the Articles collection doesn't contain the article (shouldn't happen), insert it
                    Articles.Insert(targetIndex, article);
                }
                else if (currentIndex != targetIndex)
                {
                    // Move the item to the target index
                    Articles.Move(currentIndex, targetIndex);
                }
            }

            // Categories are exclusively DB-sourced; do not recompute or update Categories from articles here.
            // ReorderArticlesByPage completed
            _suppressCategorySet = false;
        }

        private void OnEditorStateChanged()
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle))); } catch { }
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShownArticle))); } catch { }
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveSegmentDisplay))); } catch { }
                });
            }
            catch { }
        }
    }
}
