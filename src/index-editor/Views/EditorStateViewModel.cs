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

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers
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
                    try
                    {
                        if (File.Exists(c)) return p;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("EditorStateViewModel.FindFirstPageWithImage: File.Exists", ex);
                    }
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
            catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.NavigateToArticle: FindFirstPageWithImage", ex); }

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
                try { DebugLogger.Log($"==> SelectedArticle SETTER CALLED: incoming='{value?.Title}', current='{_selectedArticle?.Title}', stacktrace={Environment.StackTrace}"); } catch { }
                
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
                        try { DebugLogger.Log($"==> SelectedArticle SETTER: BLOCKED - active segment prevents selection change"); } catch { }
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
                    try { DebugLogger.Log($"==> SelectedArticle SETTER: IGNORING transient null (current article still in collection)"); } catch { }
                    // ignore transient clear
                    return;
                }

                if (_selectedArticle != incoming)
                {
                    try { DebugLogger.Log($"SelectedArticle changing. incoming.Title='{incoming?.Title}', Category='{incoming?.Category}', Contributor0='{incoming?.Contributor0}'"); } catch {}
                     _selectedArticle = incoming;
                      // Update IsSelected flags on all articles so UI bindings reflect selection
                      // Suppress HasUnsavedChanges since this is just a UI state change
                      try
                      {
                          _suppressHasUnsavedChanges = true;
                          foreach (var a in Articles)
                          {
                              try { a.IsSelected = object.ReferenceEquals(a, _selectedArticle); } catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.SelectedArticle: set IsSelected", ex); }
                          }
                      }
                      catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.SelectedArticle: updating IsSelected flags", ex); }
                      finally
                      {
                          _suppressHasUnsavedChanges = false;
                      }
                      // Ensure the global EditorState reflects the current selected article so
                      // other views (PageController, etc.) can read the active article details.
                      try
                      {
                          IndexEditor.Shared.EditorState.ActiveArticle = _selectedArticle;
                          IndexEditor.Shared.EditorState.NotifyStateChanged();
                      }
                      catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.SelectedArticle: set active article/notify", ex); }
                      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));
                    try { 
                        var pagesStr = _selectedArticle?.Pages != null ? string.Join(",", _selectedArticle.Pages) : "(null)";
                        var pagesTextStr = _selectedArticle?.PagesText ?? "(null)";
                        DebugLogger.Log($"SelectedArticle set. current.Title='{_selectedArticle?.Title}', Category='{_selectedArticle?.Category}', Contributor0='{_selectedArticle?.Contributor0}', Pages=[{pagesStr}], PagesText='{pagesTextStr}'"); 
                    } catch {}
                    
                    // Note: Babepedia check removed from automatic selection - now triggered manually
                    
                      // Force the ArticleLine to notify all UI-bound properties changed so TextBox bindings refresh
                      // Suppress HasUnsavedChanges during this refresh since no actual data is changing
                      try
                      {
                          _suppressHasUnsavedChanges = true;
                          _selectedArticle?.RefreshUIBindings();
                      }
                      catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.SelectedArticle: RefreshUIBindings", ex); }
                      finally
                      {
                          _suppressHasUnsavedChanges = false;
                      }
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
         private bool _suppressHasUnsavedChanges = false;
         private bool _isReordering = false;
         public string? SelectedCategory
         {
             get
             {
                 var category = SelectedArticle?.Category;
                 // Debug: Log when category is accessed to verify binding is working
                 if (category != null)
                 {
                     try { DebugLogger.Log($"SelectedCategory GET: '{category}' for article '{SelectedArticle?.Title}'"); } catch { }
                 }
                 return category;
             }
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
                         try { IndexEditor.Shared.ToastService.Show("Category must be chosen from the predefined list"); } catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.SelectedCategory: ToastService.Show", ex); }
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

          /// <summary>
          /// Notify that SelectedCategory changed. Used to refresh bindings after programmatic setup.
          /// </summary>
          public void NotifySelectedCategoryChanged()
          {
              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCategory)));
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
             
             // CRITICAL: Initialize categories FIRST, before loading articles
             // Categories load from enum (instant), so do it synchronously to avoid race conditions
             // This ensures ComboBox ItemsSource is populated before articles with categories are loaded
             try
             {
                 // Initialize CategoryService synchronously if not already done
                 // Since it loads from enum, this is instant (no I/O, no async needed)
                 if (IndexEditor.Shared.CategoryService.Categories.Count == 0)
                 {
                     // Call InitializeAsync synchronously - it's instant for enum-based loading
                     IndexEditor.Shared.CategoryService.InitializeAsync().Wait();
                 }
                 
                 // Mirror categories to our VM's collection
                 Categories.Clear();
                 foreach (var c in IndexEditor.Shared.CategoryService.Categories)
                     Categories.Add(c);
                 
                 _categoriesLoadedFromDb = IndexEditor.Shared.CategoryService.Categories.Count > 0;
                 
                 // Explicitly ensure the loading indicator is off (should already be false, but ensure binding updates)
                 IsLoadingCategories = false;
                 
                 DebugLogger.Log($"EditorStateViewModel: Categories initialized synchronously, count={Categories.Count}, IsLoadingCategories={IsLoadingCategories}");
             }
             catch (Exception ex)
             {
                 DebugLogger.LogException("EditorStateViewModel: category initialization", ex);
                 IsLoadingCategories = false;  // Ensure it's false even on error
             }

             // Now load articles - categories are guaranteed to be available
             foreach (var article in EditorState.Articles ?? new System.Collections.Generic.List<Common.Shared.ArticleLine>())
                 Articles.Add(article);

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
                 catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.ActiveSegmentDisplay: compute", ex); return "— none —"; }
             }
         }

         private async Task<List<string>?> LoadCategoriesFromDatabaseAsync()
         {
            // Categories are now loaded from ArticleCategory enum, not from database.
            // This method is kept for compatibility but now returns enum-based categories.
            try
            {
                var cats = ArticleCategoryHelper.GetAllCategories();
                DebugLogger.Log($"LoadCategories: Loaded {cats.Count} categories from ArticleCategory enum");
                return cats;
            }
            catch (Exception ex) 
            { 
                DebugLogger.LogException("EditorStateViewModel.LoadCategories: outer", ex); 
                return null; 
            }
         }

        private void UpdateCategories(List<string> newCats, bool fromDatabase = false)
         {
            if (newCats == null) newCats = new List<string>();
            
            // Ensure selected category is preserved
            var selectedCat = SelectedArticle?.Category;
            
            // Categories now come from the ArticleCategory enum
            // We merge discovered categories with enum categories to preserve any custom ones
            var sorted = newCats.OrderBy(s => s).ToList();
            if (!string.IsNullOrWhiteSpace(selectedCat) && !sorted.Contains(selectedCat))
                sorted.Add(selectedCat);

            // Update the collection
            Categories.Clear();
            foreach (var c in sorted)
                Categories.Add(c);
            
            try { DebugLogger.Log($"Updated Categories: {string.Join(",", sorted)}"); } catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.UpdateCategories: append", ex); }
         }

        private void SyncArticles()
        {
            // During initial sync we suppress category writes that may arise from control rebinds
            _suppressCategorySet = true;
            // Perform an in-place minimal-diff update of the ObservableCollection to avoid
            // recreating item controls. This preserves control instances and their bindings
            // so TwoWay bindings (like Category) don't accidentally write into the wrong model.
            var desired = EditorState.Articles ?? new System.Collections.Generic.List<Common.Shared.ArticleLine>();

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
                // Mark that we have unsaved changes only for properties that represent actual user data changes
                // Exclude UI-only properties like ActiveSegment, LastModifiedSegment, WasAutoHighlighted, FormattedCardText, IsSelected, etc.
                var dataProperties = new[] 
                { 
                    nameof(ArticleLine.Pages), 
                    nameof(ArticleLine.PagesText), 
                    nameof(ArticleLine.Category), 
                    nameof(ArticleLine.Title),
                    nameof(ArticleLine.ModelNames),
                    nameof(ArticleLine.Age),
                    nameof(ArticleLine.Ages),
                    nameof(ArticleLine.Contributors),
                    nameof(ArticleLine.Illustrators),
                    nameof(ArticleLine.ModelSize),
                    nameof(ArticleLine.Measurements),
                    nameof(ArticleLine.BustSize),
                    nameof(ArticleLine.WaistSize),
                    nameof(ArticleLine.HipSize),
                    nameof(ArticleLine.CupSize),
                    nameof(ArticleLine.BustSizes),
                    nameof(ArticleLine.WaistSizes),
                    nameof(ArticleLine.HipSizes),
                    nameof(ArticleLine.CupSizes),
                    nameof(ArticleLine.Notes)
                };
                
                if (dataProperties.Contains(e.PropertyName))
                {
                    // Only set HasUnsavedChanges if we're not in the middle of refreshing UI bindings
                    if (!_suppressHasUnsavedChanges)
                    {
                        try { IndexEditor.Shared.EditorState.HasUnsavedChanges = true; } catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.OnArticlePropertyChanged: set HasUnsavedChanges", ex); }
                    }
                }
                
                // OnArticlePropertyChanged
                // If pages or category changed, we may need to reorder
                if (e.PropertyName == nameof(ArticleLine.Pages) || e.PropertyName == nameof(ArticleLine.PagesText) || e.PropertyName == nameof(ArticleLine.Category))
                 {
                     // Skip if already reordering to prevent recursive calls
                     if (!_isReordering)
                     {
                         ReorderArticlesByPage();
                     }
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
            // Prevent recursive calls during reordering
            if (_isReordering)
            {
                try { DebugLogger.Log("ReorderArticlesByPage: Skipping - already reordering"); } catch { }
                return;
            }
            
            _isReordering = true;
            try
            {
                // Preserve the current selection so it doesn't get lost during reordering
                var currentSelection = _selectedArticle;
                try { DebugLogger.Log($"ReorderArticlesByPage: Saving selection: '{currentSelection?.Title}' (Category: {currentSelection?.Category})"); } catch { }
                
                // Suppress category writes while we reorder/move items to avoid transient writes
                _suppressCategorySet = true;
            // Compute ordered list (articles with no pages end up after those with pages)
            var ordered = (EditorState.Articles ?? new System.Collections.Generic.List<Common.Shared.ArticleLine>())
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

            // Restore the selection after reordering to prevent article from disappearing
            // This must happen AFTER the ObservableCollection is updated so the Contains check in the setter works
            if (currentSelection != null && ordered.Contains(currentSelection))
            {
                try { DebugLogger.Log($"ReorderArticlesByPage: currentSelection found in ordered list, attempting restore"); } catch { }
                // Find the article in the new ordered list (it might be the same reference or a matching one)
                var restored = ordered.FirstOrDefault(a => object.ReferenceEquals(a, currentSelection))
                            ?? ordered.FirstOrDefault(a => a.Pages != null && currentSelection.Pages != null && 
                                                          a.Pages.SequenceEqual(currentSelection.Pages) && 
                                                          (a.Title ?? string.Empty) == (currentSelection.Title ?? string.Empty));
                if (restored != null)
                {
                    try { DebugLogger.Log($"ReorderArticlesByPage: Restoring selection to '{restored.Title}' (same ref: {object.ReferenceEquals(currentSelection, restored)})"); } catch { }
                    // Directly set the backing field to avoid the setter's guard logic
                    // which might reject the restoration
                    var wasSelected = _selectedArticle;
                    _selectedArticle = restored;
                    
                    // Only raise PropertyChanged if the selection actually changed
                    if (!object.ReferenceEquals(wasSelected, restored))
                    {
                        try
                        {
                            // Update IsSelected flags on all articles
                            foreach (var a in Articles)
                            {
                                try { a.IsSelected = object.ReferenceEquals(a, _selectedArticle); } 
                                catch (Exception ex) { DebugLogger.LogException("ReorderArticlesByPage: set IsSelected", ex); }
                            }
                            
                            // Update global state
                            IndexEditor.Shared.EditorState.ActiveArticle = _selectedArticle;
                            
                            // Notify bindings
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle)));
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCategory)));
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShownArticle)));
                        }
                        catch (Exception ex) { DebugLogger.LogException("ReorderArticlesByPage: restore selection notifications", ex); }
                    }
                    else
                    {
                        try { DebugLogger.Log($"ReorderArticlesByPage: Selection unchanged (same reference), no notification needed"); } catch { }
                    }
                }
                else
                {
                    try { DebugLogger.Log($"ReorderArticlesByPage: WARNING - Could not find restored article in ordered list!"); } catch { }
                }
            }
            else
            {
                try { DebugLogger.Log($"ReorderArticlesByPage: WARNING - currentSelection is null or not in ordered list (currentSelection null: {currentSelection == null})"); } catch { }
            }

            // Categories are exclusively DB-sourced; do not recompute or update Categories from articles here.
            // ReorderArticlesByPage completed
            _suppressCategorySet = false;
            }
            finally
            {
                _isReordering = false;
            }
        }

        private void OnEditorStateChanged()
        {
            try
            {
                try { DebugLogger.Log($"==> OnEditorStateChanged CALLED: SelectedArticle is currently '{_selectedArticle?.Title}'"); } catch { }
                Dispatcher.UIThread.Post(() =>
                {
                    // DO NOT raise PropertyChanged for SelectedArticle here!
                    // Raising it when the value hasn't actually changed confuses the binding
                    // system and causes the article to disappear from the editor.
                    // Only the SelectedArticle setter should raise this event.
                    
                    // try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedArticle))); } catch { }
                    
                    // These are fine - they may actually change when EditorState changes
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShownArticle))); } catch { }
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveSegmentDisplay))); } catch { }
                    
                    try { DebugLogger.Log($"==> OnEditorStateChanged COMPLETED: SelectedArticle is still '{_selectedArticle?.Title}'"); } catch { }
                });
            }
            catch (Exception ex) { DebugLogger.LogException("EditorStateViewModel.OnEditorStateChanged", ex); }
        }

        /// <summary>
        /// Manually check babepedia.com for the currently selected article
        /// </summary>
        public void CheckBabepediaForSelectedArticle()
        {
            if (_selectedArticle != null)
            {
                CheckBabepediaAsync(_selectedArticle);
            }
            else
            {
                IndexEditor.Shared.ToastService.Show("No article selected");
            }
        }

        /// <summary>
        /// Check babepedia.com for a specific article's model
        /// </summary>
        public async void CheckBabepediaAsync(Common.Shared.ArticleLine article)
        {
            try
            {
                DebugLogger.Log($"CheckBabepediaAsync: Called for article '{article.Title}' (Category: {article.Category})");
                
                // Only check Model and Cover categories
                if (article.Category != "Model" && article.Category != "Cover")
                {
                    DebugLogger.Log($"CheckBabepediaAsync: Skipping - not a Model or Cover article");
                    return;
                }

                DebugLogger.Log($"CheckBabepediaAsync: Starting babepedia check for Model/Cover article");
                var (exists, modelName, url) = await IndexEditor.Services.BabepediaService.CheckArticleModelAsync(article);
                DebugLogger.Log($"CheckBabepediaAsync: Result - exists={exists}, modelName='{modelName}', url='{url}'");

                if (exists)
                {
                    // Log to debug
                    DebugLogger.Log($"BABEPEDIA: Model '{modelName}' found at {url}");
                    
                    // Show debug popup on UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            // Find the main window
                            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                                ? desktop.MainWindow
                                : null;

                            if (mainWindow != null)
                            {
                                BabepediaDebugDialog.ShowDialog(mainWindow, modelName, url);
                            }
                            else
                            {
                                // Fallback to toast if we can't find the main window
                                IndexEditor.Shared.ToastService.Show($"✓ Babepedia: {modelName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("CheckBabepediaAsync: Show babepedia dialog", ex);
                            IndexEditor.Shared.ToastService.Show($"✓ Babepedia: {modelName}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CheckBabepediaAsync", ex);
            }
        }
    }
}


