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

namespace IndexEditor.Views
{
    public class EditorStateViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
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

            // Populate categories from articles (unique, sorted) as a fallback
            var cats = new HashSet<string>(Articles.Select(a => a.Category).Where(c => !string.IsNullOrWhiteSpace(c)));
            foreach (var c in cats.OrderBy(s => s))
                Categories.Add(c);

            // Asynchronously try to load categories from DB (do not block UI thread)
            IsLoadingCategories = true;
            Task.Run(async () =>
            {
                try
                {
                    var dbCats = await LoadCategoriesFromDatabaseAsync();
                    if (dbCats != null && dbCats.Count > 0)
                    {
                        // Update the observable collection on the UI thread
                        Dispatcher.UIThread.Post(() => UpdateCategories(dbCats.OrderBy(s => s).ToList()));
                    }
                }
                catch (Exception ex)
                {
                    // Failed to load categories from DB (suppressed)
                }
                finally
                {
                    Dispatcher.UIThread.Post(() => IsLoadingCategories = false);
                }
            });

            // Debug: Print type of every item
            // Skip debug printing of article types
             // Listen for changes
             EditorState.StateChanged += SyncArticles;

             // Debug: Print formatted text for each article
            // Skip debug formatted output for cards
         }

        private async Task<List<string>?> LoadCategoriesFromDatabaseAsync()
        {
            try
            {
                // Read appsettings.json from the app folder
                var appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                if (!File.Exists(appsettingsPath))
                    return null;
                using var fs = File.OpenRead(appsettingsPath);
                using var doc = await JsonDocument.ParseAsync(fs);
                if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connSection))
                    return null;
                if (!connSection.TryGetProperty("MagazineDb", out var connStringElem))
                    return null;
                var connString = connStringElem.GetString();
                if (string.IsNullOrWhiteSpace(connString))
                    return null;

                var cats = await IndexEditor.Shared.CategoryRepository.GetCategoriesAsync(connString);
                if (cats != null && cats.Count > 0)
                {
                    return cats;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                // Failed to load categories from DB (suppressed)
                return null;
            }
        }

        private void UpdateCategories(List<string> newCats)
        {
            // Ensure selected category is preserved
            var selectedCat = SelectedArticle?.Category;
            // UpdateCategories called
            if (!string.IsNullOrWhiteSpace(selectedCat) && !newCats.Contains(selectedCat))
            {
                newCats.Add(selectedCat);
                // Added selected category to preserve selection
            }

            // Add any new categories, but do NOT remove existing ones. Removing can change ComboBox indices
            // and cause SelectedItem to shift and write back incorrect values into Article.Category.
            var union = new HashSet<string>(Categories);
            foreach (var c in newCats)
                union.Add(c);
            // Preserve insertion order by sorting the union; we only append missing items to the existing collection
            var sorted = union.OrderBy(s => s).ToList();
            foreach (var c in sorted)
            {
                if (!Categories.Contains(c))
                {
                    // Adding category to Categories (union)
                    Categories.Add(c);
                }
            }
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

            // Recompute categories from current articles and update the category list
            var cats = new HashSet<string>(Articles.Select(a => a.Category).Where(c => !string.IsNullOrWhiteSpace(c)));
            UpdateCategories(cats.OrderBy(s => s).ToList());
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

            // Update categories set based on new ordering without re-syncing all article subscriptions
            var cats = new HashSet<string>(EditorState.Articles.Select(a => a.Category).Where(c => !string.IsNullOrWhiteSpace(c)));
            UpdateCategories(cats.OrderBy(s => s).ToList());
            // ReorderArticlesByPage completed
            _suppressCategorySet = false;
        }
    }
}
