using Avalonia.Controls;
using IndexEditor.Shared;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using System.IO;

namespace IndexEditor.Views
{
    public partial class PageControllerView : UserControl
    {
        public int Page
        {
            get => EditorState.CurrentPage;
            set
            {
                EditorState.CurrentPage = value;
                var pageInput = this.FindControl<TextBox>("PageInput");
                if (pageInput != null)
                    pageInput.Text = EditorState.CurrentPage.ToString();
                // Do NOT update EditorState.ActiveSegment.End here; changing pages should not close the active segment.
                // Notify UI/state but do NOT auto-select an article when the current page changes.
                // Selection should only occur when the user explicitly presses the Sync button.
                EditorState.NotifyStateChanged();
            }
        }

        public PageControllerView()
        {
            InitializeComponent();
            var prevBtn = this.FindControl<Button>("PrevPageBtn");
            var nextBtn = this.FindControl<Button>("NextPageBtn");
            var newArticleBtn = this.FindControl<Button>("NewArticleBtn");
            var addSegmentBtn = this.FindControl<Button>("AddSegmentBtn");
            var endSegmentBtn = this.FindControl<Button>("EndSegmentBtn");
            var cancelSegmentBtn = this.FindControl<Button>("CancelSegmentBtn");
            var pageInput = this.FindControl<TextBox>("PageInput");
            var activeArticleTitle = this.FindControl<TextBlock>("ActiveArticleTitle");
            var activeSegmentText = this.FindControl<TextBlock>("ActiveSegmentText");
            var toastBorder = this.FindControl<Border>("ToastBorder");
            var toastText = this.FindControl<TextBlock>("ToastText");

            // Helper: show a small toast message that fades out using Avalonia.Animation
            void ShowToast(string message, int displayMs = 1200)
            {
                try
                {
                    if (toastText != null) toastText.Text = message;
                    if (toastBorder == null) return;
                    // Show immediately
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        toastBorder.IsVisible = true;
                        toastBorder.Opacity = 1.0;
                    });

                    // Hide after a delay (no fade)
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await System.Threading.Tasks.Task.Delay(displayMs).ConfigureAwait(false);
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                try { toastBorder.IsVisible = false; }
                                catch { }
                            });
                        }
                        catch { }
                    });
                }
                catch { }
            }

            // Subscribe to global toast requests; capture the delegate so we can unsubscribe
            Action<string> toastHandler = (msg) => { Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowToast(msg)); };
            IndexEditor.Shared.ToastService.ShowRequested += toastHandler;

            // Unsubscribe when this control unloads to prevent leaks
            this.DetachedFromVisualTree += (s, e) => { IndexEditor.Shared.ToastService.ShowRequested -= toastHandler; };

            if (prevBtn != null)
                prevBtn.Click += (s, e) =>
                {
                    var folder = EditorState.CurrentFolder;
                    if (string.IsNullOrWhiteSpace(folder)) { if (Page > 1) Page--; return; }
                    var found = FindNearestExistingPage(folder, EditorState.CurrentPage - 1, -1);
                    if (found.HasValue) Page = found.Value;
                };
            if (nextBtn != null)
                nextBtn.Click += (s, e) =>
                {
                    var folder = EditorState.CurrentFolder;
                    if (string.IsNullOrWhiteSpace(folder)) { Page++; return; }
                    var found = FindNearestExistingPage(folder, EditorState.CurrentPage + 1, 1);
                    if (found.HasValue) Page = found.Value;
                };

            // Page input commit on Enter
            if (pageInput != null)
            {
                pageInput.KeyDown += (s, e) =>
                {
                    if (e.Key == Avalonia.Input.Key.Enter)
                    {
                        if (int.TryParse(pageInput.Text, out var v) && v > 0)
                        {
                            Page = v;
                        }
                    }
                };
                // ensure UI reflects current page value
                pageInput.Text = EditorState.CurrentPage.ToString();
            }

            void UpdateUi()
            {
                // Buttons enablement
                // AddSegment: enabled only when an ActiveArticle exists and the current page is NOT already part of its Pages
                if (addSegmentBtn != null)
                {
                    addSegmentBtn.IsEnabled = EditorState.ActiveArticle != null &&
                                              (EditorState.ActiveArticle.Pages == null || !EditorState.ActiveArticle.Pages.Contains(EditorState.CurrentPage))
                                              && (EditorState.ActiveSegment == null || !EditorState.ActiveSegment.IsActive);
                }
                if (endSegmentBtn != null) endSegmentBtn.IsEnabled = EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive;
                if (cancelSegmentBtn != null) cancelSegmentBtn.IsEnabled = EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive;

                // Active article display: use DisplayTitle and CategoryDisplay to match Article card behaviour
                if (activeArticleTitle != null)
                    activeArticleTitle.Text = EditorState.ActiveArticle != null ? EditorState.ActiveArticle.DisplayTitle : "— none —";
                var activeCategoryBlock = this.FindControl<TextBlock>("ActiveArticleCategory");
                if (activeCategoryBlock != null)
                    activeCategoryBlock.Text = EditorState.ActiveArticle != null ? EditorState.ActiveArticle.CategoryDisplay : string.Empty;

                // Active segment display
                if (activeSegmentText != null)
                {
                    if (EditorState.ActiveSegment != null)
                    {
                        var seg = EditorState.ActiveSegment;
                        // When active (End == null) display 'start → currentPage' so the user sees live range preview
                        var segText = seg.End.HasValue ? $"{seg.Start} → {seg.End.Value}" : $"{seg.Start} → {EditorState.CurrentPage}";
                        activeSegmentText.Text = segText;
                    }
                    else
                    {
                        activeSegmentText.Text = "— none —";
                    }
                }

                // status area removed
            }

            // Subscribe to state changes to refresh UI
            EditorState.StateChanged += () => Dispatcher.UIThread.Post(() =>
            {
                if (pageInput != null) pageInput.Text = EditorState.CurrentPage.ToString();
                UpdateUi();
            });

            // Initial sync
            UpdateUi();

            if (newArticleBtn != null)
                newArticleBtn.Click += (s, e) =>
                {
                     try
                    {
                        // Do not auto-close an existing active segment; instead, block creating a new article while a segment is active
                        if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                        {
                            ShowToast("Finish or cancel the open segment first");
                            return;
                        }

                        // Create article with no populated fields except the first page
                        var article = new Common.Shared.ArticleLine();
                        article.Pages = new List<int> { EditorState.CurrentPage };

                        // Insert article into EditorState.Articles in sorted order by first page
                        if (EditorState.Articles == null)
                            EditorState.Articles = new List<Common.Shared.ArticleLine> { article };
                        else
                        {
                            int insertIndex = EditorState.Articles.FindIndex(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() > article.Pages.Min());
                            if (insertIndex == -1)
                                EditorState.Articles.Add(article);
                            else
                                EditorState.Articles.Insert(insertIndex, article);
                        }

                        // Make it the active article
                        EditorState.ActiveArticle = article;

                        // Create and attach a new active segment starting at the current page
                        var seg = new Common.Shared.Segment(EditorState.CurrentPage);
                        article.Segments.Add(seg);
                        EditorState.ActiveSegment = seg;

                        // Notify state change so view-models sync
                        EditorState.NotifyStateChanged();

                        // Perform selection on UI thread after view-model has synced its Articles collection
                        var vm = this.DataContext as EditorStateViewModel;
                        if (vm != null)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                try
                                {
                                    // Find the instance in vm.Articles that corresponds to our new article (by reference or by page match)
                                    var inList = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, article))
                                                ?? vm.Articles.FirstOrDefault(a => a.Pages != null && article.Pages != null && a.Pages.SequenceEqual(article.Pages));
                                    var toSelect = inList ?? article;
                                    if (vm.SelectArticleCommand.CanExecute(toSelect))
                                        vm.SelectArticleCommand.Execute(toSelect);
                                    // Also set SelectedArticle explicitly to ensure binding matches
                                    vm.SelectedArticle = toSelect;
                                    // Attempt to focus the list so the selection persists visually
                                    try
                                    {
                                        var main = this.VisualRoot as Window;
                                        var list = main?.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
                                        var lb = list?.FindControl<Avalonia.Controls.ListBox>("ArticlesListBox");
                                        lb?.Focus();
                                    }
                                    catch { }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[WARN] selecting new article after insert failed: {ex.Message}");
                                }
                            }, Avalonia.Threading.DispatcherPriority.Background);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Creating new article failed: {ex.Message}");
                    }
                };

            if (addSegmentBtn != null)
                addSegmentBtn.Click += (s, e) =>
                {
                    if (EditorState.ActiveArticle != null)
                    {
                        // If there's already an active segment, disallow creating another
                        if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                        {
                            // show toast instead of silent no-op
                            ShowToast("Finish or cancel the open segment first");
                             return;
                        }

                        // If the current page is already part of the article's pages, do not create a new segment
                        if (EditorState.ActiveArticle.Pages != null && EditorState.ActiveArticle.Pages.Contains(EditorState.CurrentPage))
                        {
                            ShowToast($"Page {EditorState.CurrentPage} already in article");
                            return;
                        }


                        // Create a new OPEN (active) segment starting at the current page
                        var seg = new Common.Shared.Segment(EditorState.CurrentPage);
                        EditorState.ActiveArticle.Segments.Add(seg);
                        EditorState.ActiveSegment = seg;

                        // Notify state change so view-models sync
                        EditorState.NotifyStateChanged();
                    }
                };

            // Cancel Segment: remove the active segment and clear state
            if (cancelSegmentBtn != null)
                cancelSegmentBtn.Click += (s, e) =>
                {
                    if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                    {
                        // Remove the active segment from its owning article if present
                        var art = EditorState.ActiveArticle;
                        if (art != null && art.Segments != null && art.Segments.Contains(EditorState.ActiveSegment))
                        {
                            art.Segments.Remove(EditorState.ActiveSegment);
                        }
                        // Clear global active segment
                        EditorState.ActiveSegment = null;
                        // Notify to update UI
                        EditorState.NotifyStateChanged();
                    }
                };

            // End Segment: when an active segment exists, update the article pages and UI
            if (endSegmentBtn != null)
                endSegmentBtn.Click += (s, e) =>
                {
                    if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                    {
                        var start = EditorState.ActiveSegment.Start;
                        var end = EditorState.CurrentPage;
                        if (end < start) (start, end) = (end, start);
                        // Add the page range [start..end] to the active article's Pages
                        var art = EditorState.ActiveArticle;
                        if (art != null)
                        {
                            // Build a new list so we can assign via the Pages setter (raises PropertyChanged)
                            var newPages = new List<int>(art.Pages ?? new List<int>());
                            for (int p = start; p <= end; p++)
                            {
                                if (!newPages.Contains(p)) newPages.Add(p);
                            }
                            newPages.Sort();
                            // Assign back to trigger ArticleLine.Pages setter and property notifications
                            art.Pages = newPages;

                            // Also try to sync the view-model's article instance (if present) so the editor card updates immediately
                            try
                            {
                                var vm = this.DataContext as EditorStateViewModel;
                                if (vm != null)
                                {
                                    var vmMatch = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, art))
                                                  ?? vm.Articles.FirstOrDefault(a => a.Pages != null && a.Pages.SequenceEqual(newPages) && (a.Title ?? string.Empty) == (art.Title ?? string.Empty));
                                    if (vmMatch != null && !object.ReferenceEquals(vmMatch, art))
                                    {
                                        vmMatch.Pages = new List<int>(newPages);
                                    }
                                }
                            }
                            catch { }
                        }
                        // Close the segment
                        EditorState.ActiveSegment.End = EditorState.CurrentPage;
                        EditorState.ActiveSegment = null;
                        EditorState.NotifyStateChanged();
                    }
                };

            // When state changes, try to load the page image for CurrentPage
            EditorState.StateChanged += () => Dispatcher.UIThread.Post(() => LoadCurrentPageImage());

            // Initial image load
            LoadCurrentPageImage();

            // Wire the Sync button to explicitly select an article that starts on the current page
            var syncBtn = this.FindControl<Button>("SyncToArticleBtn");
            if (syncBtn != null)
            {
                syncBtn.Click += (s, e) =>
                {
                    try
                    {
                        var vm = this.DataContext as EditorStateViewModel;
                        if (vm != null)
                        {
                            var match = EditorState.Articles.FirstOrDefault(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() == EditorState.CurrentPage);
                            if (match != null)
                            {
                                vm.NavigateToArticle(match);
                                if (vm.SelectArticleCommand.CanExecute(match))
                                    vm.SelectArticleCommand.Execute(match);
                                vm.SelectedArticle = match;
                            }
                        }
                    }
                    catch { }
                };
            }
        }

        private void LoadCurrentPageImage()
        {
            var img = this.FindControl<Image>("PageImage");
            var pageInput = this.FindControl<TextBox>("PageInput");
            var missing = this.FindControl<TextBlock>("ImageMissingText");
            if (img == null) return;
            img.Source = null;
            var folder = EditorState.CurrentFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                // No folder: show missing message
                if (missing != null) { missing.Text = "No folder opened"; missing.IsVisible = true; }
                if (pageInput != null) pageInput.Foreground = Brushes.Red;
                return;
            }
            var page = EditorState.CurrentPage;
            var path = FindImagePath(folder, page);
            if (path != null)
            {
                try
                {
                    var src = new Avalonia.Media.Imaging.Bitmap(path);
                    img.Source = src;
                    if (missing != null) missing.IsVisible = false;
                    if (pageInput != null) pageInput.Foreground = Brushes.Black;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load image '{path}': {ex.Message}");
                    if (missing != null) { missing.Text = $"Failed to load image: {Path.GetFileName(path)}"; missing.IsVisible = true; }
                    if (pageInput != null) pageInput.Foreground = Brushes.Red;
                }
            }
            else
            {
                // Image not found for this page
                if (missing != null) { missing.Text = $"Page {page} not found"; missing.IsVisible = true; }
                if (pageInput != null) pageInput.Foreground = Brushes.Red;
            }
        }

        private string? FindImagePath(string folder, int page)
        {
            var candidates = new List<string>
            {
                Path.Combine(folder, page.ToString() + ".jpg"),
                Path.Combine(folder, page.ToString() + ".png"),
                Path.Combine(folder, page.ToString("D2") + ".jpg"),
                Path.Combine(folder, page.ToString("D2") + ".png"),
                Path.Combine(folder, page.ToString("D3") + ".jpg"),
                Path.Combine(folder, page.ToString("D3") + ".png"),
                Path.Combine(folder, "page-" + page.ToString() + ".jpg"),
                Path.Combine(folder, "p" + page.ToString() + ".jpg")
            };
            foreach (var p in candidates)
                if (File.Exists(p)) return p;
            return null;
        }

        private int? FindNearestExistingPage(string folder, int startPage, int direction)
        {
            // direction: -1 backward, +1 forward
            if (direction == 0) return null;
            int attempts = 0;
            int page = startPage;
            // search up to 2000 pages to avoid infinite loops
            while (attempts < 2000 && page > 0)
            {
                var path = FindImagePath(folder, page);
                if (path != null) return page;
                page += direction;
                attempts++;
            }
            return null;
        }
    }
}
