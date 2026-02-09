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
                var desired = value;
                // If images are enabled, prefer an existing image page; if desired page has no image, find nearest existing one.
                try
                {
                    if (IndexEditor.Shared.EditorState.ShowImages)
                    {
                        var folder = EditorState.CurrentFolder;
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            var candidate = IndexEditor.Shared.ImageHelper.FindImagePath(folder, desired);
                            if (candidate == null)
                            {
                                var nearest = IndexEditor.Shared.ImageHelper.FindNearestExistingPageBothDirections(folder, desired);
                                if (nearest.HasValue)
                                    desired = nearest.Value;
                            }
                        }
                    }
                }
                catch { }

                EditorState.CurrentPage = desired;
                var pageInput = this.FindControl<TextBox>("PageInput");
                if (pageInput != null)
                    pageInput.Text = EditorState.CurrentPage.ToString();
                // Do NOT update EditorState.ActiveSegment.End here; changing pages should not close the active segment.
                // Notify UI/state but do NOT auto-select an article when the current page changes.
                // Selection should only occur when the user explicitly presses the Sync button.
                EditorState.NotifyStateChanged();
                // Also load the current page image immediately when Page is set
                try { LoadCurrentPageImage(); } catch { }
            }
        }

        public PageControllerView()
        {
            System.Console.WriteLine("[DEBUG] PageControllerView: constructor");
            InitializeComponent();

            var prevBtn = this.FindControl<Button>("PrevPageBtn");
            var nextBtn = this.FindControl<Button>("NextPageBtn");
            var pageInput = this.FindControl<TextBox>("PageInput");
            var toastBorder = this.FindControl<Border>("ToastBorder");
            var toastText = this.FindControl<TextBlock>("ToastText");

            // Helper: show a small toast message that fades out
            void ShowToast(string message, int displayMs = 1200)
            {
                try
                {
                    if (toastText != null) toastText.Text = message;
                    if (toastBorder == null) return;
                    Dispatcher.UIThread.Post(() => { toastBorder.IsVisible = true; toastBorder.Opacity = 1.0; });
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(displayMs).ConfigureAwait(false);
                        const int steps = 8; const int stepMs = 40;
                        for (int i = 0; i < steps; i++)
                        {
                            var t = i + 1;
                            Dispatcher.UIThread.Post(() => { try { toastBorder.Opacity = Math.Max(0.0, 1.0 - (double)t / steps); } catch { } });
                            await System.Threading.Tasks.Task.Delay(stepMs).ConfigureAwait(false);
                        }
                        Dispatcher.UIThread.Post(() => { try { toastBorder.IsVisible = false; toastBorder.Opacity = 1.0; } catch { } });
                    });
                }
                catch { }
            }

            Action<string> toastHandler = (msg) => { Dispatcher.UIThread.Post(() => ShowToast(msg)); };
            IndexEditor.Shared.ToastService.ShowRequested += toastHandler;
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

            if (pageInput != null)
            {
                pageInput.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter && int.TryParse(pageInput.Text, out var v) && v > 0) Page = v; };
                pageInput.Text = EditorState.CurrentPage.ToString();
            }

            void UpdateUi()
            {
                // Buttons enablement - no EndSegment button present
                // Active article/segment are displayed in the ArticleEditor now
            }

            // Subscribe to state changes to refresh UI
            EditorState.StateChanged += () => Dispatcher.UIThread.Post(() =>
            {
                if (pageInput != null) pageInput.Text = EditorState.CurrentPage.ToString();
                UpdateUi();
                try { LoadCurrentPageImage(); } catch { }
            });

            // Initial sync
            UpdateUi();
            // Load initial page image
            try { LoadCurrentPageImage(); } catch { }

            // Debug helper: if INDEXEDITOR_DEBUG_AUTOCREATE=1 is set, auto-trigger creating a new article
            try
            {
                var auto = Environment.GetEnvironmentVariable("INDEXEDITOR_DEBUG_AUTOCREATE");
                if (!string.IsNullOrEmpty(auto) && auto == "1")
                {
                    _ = System.Threading.Tasks.Task.Run(async () => { await System.Threading.Tasks.Task.Delay(600).ConfigureAwait(false); Dispatcher.UIThread.Post(() => { try { CreateNewArticle(); } catch { } }); });
                }
            }
            catch { }

            // EndSegment button removed; keep EndActiveSegment method available for programmatic use
        }

        // Ends the current active segment (if any) by setting its End to CurrentPage, updating the article pages,
        // syncing the view-model, clearing ActiveSegment and notifying the EditorState.
        public void EndActiveSegment()
        {
            try
            {
                if (EditorState.ActiveSegment == null || !EditorState.ActiveSegment.IsActive)
                    return;

                var start = EditorState.ActiveSegment.Start;
                var end = EditorState.CurrentPage;
                if (end < start) (start, end) = (end, start);

                var art = EditorState.ActiveArticle;
                if (art != null)
                {
                    var newPages = new List<int>(art.Pages ?? new List<int>());
                    for (int p = start; p <= end; p++) if (!newPages.Contains(p)) newPages.Add(p);
                    newPages.Sort();
                    art.Pages = newPages;

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

                // Close and clear the active segment
                EditorState.ActiveSegment.End = EditorState.CurrentPage;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            catch { }
        }


        private void LoadCurrentPageImage()
        {
            var img = this.FindControl<Image>("PageImage");
            var pageInput = this.FindControl<TextBox>("PageInput");
            var missing = this.FindControl<TextBlock>("ImageMissingText");
            if (img == null) return;
            img.Source = null;
            // Respect CLI flag to hide images
            if (!IndexEditor.Shared.EditorState.ShowImages)
            {
                if (missing != null) { missing.Text = "Images disabled (--no-images)"; missing.IsVisible = true; }
                if (pageInput != null) pageInput.Foreground = Brushes.Gray;
                return;
            }
            var folder = EditorState.CurrentFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                // No folder: show missing message
                if (missing != null) { missing.Text = "No folder opened"; missing.IsVisible = true; }
                if (pageInput != null) pageInput.Foreground = Brushes.Red;
                return;
            }
            var page = EditorState.CurrentPage;
            var path = IndexEditor.Shared.ImageHelper.FindImagePath(folder, page);
            if (path != null)
            {
                try
                {
                    var src = new Avalonia.Media.Imaging.Bitmap(path);
                    img.Source = src;
                    if (missing != null) missing.IsVisible = false;
                    if (pageInput != null) pageInput.Foreground = Brushes.Black;
                }
                catch
                {
                    // Failed to load image (suppressed)
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
                var path = IndexEditor.Shared.ImageHelper.FindImagePath(folder, page);
                if (path != null) return page;
                page += direction;
                attempts++;
            }
            return null;
        }

        // Find nearest existing page by searching both directions outward from the start page.
        private int? FindNearestExistingPageBothDirections(string folder, int startPage)
        {
            // quick check for exact match
            try
            {
                if (FindImagePath(folder, startPage) != null) return startPage;
            }
            catch { }
            int maxRadius = 2000;
            for (int r = 1; r <= maxRadius; r++)
            {
                var forward = startPage + r;
                if (forward > 0)
                {
                    try { if (IndexEditor.Shared.ImageHelper.FindImagePath(folder, forward) != null) return forward; } catch { }
                }
                var backward = startPage - r;
                if (backward > 0)
                {
                    try { if (IndexEditor.Shared.ImageHelper.FindImagePath(folder, backward) != null) return backward; } catch { }
                }
            }
            return null;
        }

        // Public API: create a new article (mirrors NewArticle button behavior)
        public void CreateNewArticle()
        {
            try
            {
                if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                {
                    IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first");
                    return;
                }

                // Create the article and insert into the shared EditorState
                var article = new Common.Shared.ArticleLine();
                article.Pages = new List<int> { EditorState.CurrentPage };
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

                EditorState.ActiveArticle = article;

                // Attach a single-page CLOSED segment for the current page and do NOT make it active.
                // First, guard against duplicate segments for the same page (defensive: CreateNewArticle may be invoked twice).
                bool alreadyHas = false;
                try
                {
                    if (article.Segments != null)
                    {
                        foreach (var s in article.Segments)
                        {
                            try
                            {
                                var start = s.Start;
                                var end = s.End ?? s.Start;
                                if (EditorState.CurrentPage >= start && EditorState.CurrentPage <= end)
                                {
                                    alreadyHas = true;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
                if (!alreadyHas)
                {
                    var seg = new Common.Shared.Segment(EditorState.CurrentPage);
                    // Close immediately (single-page) and mark as not-new so it behaves like an existing segment
                    seg.End = EditorState.CurrentPage;
                    seg.WasNew = false;
                    article.Segments.Add(seg);
                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: Added closed segment for page " + EditorState.CurrentPage);
                }
                else
                {
                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: Skipped adding duplicate segment for page " + EditorState.CurrentPage);
                }

                // Ensure the article pages include the page (article.Pages was already initialized to this page),
                // then notify so view-models and UI update.
                EditorState.NotifyStateChanged();

                // Try to select and focus the new article in the VM and editor
                try
                {
                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: scheduling selection+focus");
                    var vm = this.DataContext as EditorStateViewModel;
                    if (vm != null)
                    {
                        // Perform selection on UI thread after vm.Articles is updated
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                var inList = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, article))
                                            ?? vm.Articles.FirstOrDefault(a => a.Pages != null && article.Pages != null && a.Pages.SequenceEqual(article.Pages));
                                var toSelect = inList ?? article;
                                if (vm.SelectArticleCommand.CanExecute(toSelect))
                                    vm.SelectArticleCommand.Execute(toSelect);

                                vm.SelectedArticle = toSelect;

                                // Ensure the ListBox shows the selection
                                try
                                {
                                    var wnd = this.VisualRoot as Window;
                                    var articleList = wnd?.FindControl<IndexEditor.Views.ArticleList>("ArticleListControl");
                                    if (articleList != null)
                                    {
                                        try
                                        {
                                            var lb = articleList.FindControl<Avalonia.Controls.ListBox>("ArticlesListBox");
                                            if (lb != null)
                                                lb.SelectedItem = toSelect;
                                        }
                                        catch { }
                                    }
                                }
                                catch { }

                                // Re-notify after a short delay to help DataTemplate creation (ArticleEditor) react
                                try
                                {
                                    _ = System.Threading.Tasks.Task.Run(async () =>
                                    {
                                        await System.Threading.Tasks.Task.Delay(120).ConfigureAwait(false);
                                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                        {
                                            try { IndexEditor.Shared.EditorState.NotifyStateChanged(); System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: re-notified EditorState after selection"); } catch { }
                                        });
                                    });
                                }
                                catch { }

                                // Short retry loop that attempts to focus the editor controls when they become available
                                try
                                {
                                    var main = this.VisualRoot as Window;
                                    _ = System.Threading.Tasks.Task.Run(async () =>
                                    {
                                        const int attempts = 12;
                                        const int delayMs = 120;
                                        for (int i = 0; i < attempts; i++)
                                        {
                                            try
                                            {
                                                await System.Threading.Tasks.Task.Delay(delayMs).ConfigureAwait(false);
                                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                                {
                                                    try
                                                    {
                                                        // 1) Try to find TitleTextBox or CategoryComboBox within the EditorContentHost's Content
                                                        var host = main?.FindControl<ContentControl>("EditorContentHost");
                                                        if (host?.Content is Avalonia.Controls.Control hostContent)
                                                        {
                                                            try
                                                            {
                                                                var tb = hostContent.FindControl<TextBox>("TitleTextBox");
                                                                if (tb != null)
                                                                {
                                                                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focusing TitleTextBox inside host.Content");
                                                                    tb.Focus();
                                                                    return;
                                                                }
                                                                var cb = hostContent.FindControl<ComboBox>("CategoryComboBox");
                                                                if (cb != null)
                                                                {
                                                                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focusing CategoryComboBox inside host.Content");
                                                                    cb.Focus();
                                                                    return;
                                                                }
                                                            }
                                                            catch { }
                                                        }

                                                        // 2) Try to find ArticleEditor control on the Window and call its helpers
                                                        try
                                                        {
                                                            var ae = main?.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl")
                                                                     ?? main?.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                                                            if (ae != null)
                                                            {
                                                                System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: calling ae.FocusTitle()/FocusEditor()");
                                                                try { ae.FocusTitle(); } catch { }
                                                                try { ae.FocusEditor(); } catch { }
                                                                return;
                                                            }
                                                        }
                                                        catch { }

                                                        // 3) Directly search the Window for TitleTextBox or CategoryComboBox
                                                        try
                                                        {
                                                            var tbDirect = main?.FindControl<TextBox>("TitleTextBox");
                                                            if (tbDirect != null)
                                                            {
                                                                System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focusing TitleTextBox directly on Window");
                                                                try { tbDirect.Focus(); } catch { }
                                                                return;
                                                            }
                                                            var cbDirect = main?.FindControl<ComboBox>("CategoryComboBox");
                                                            if (cbDirect != null)
                                                            {
                                                                System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focusing CategoryComboBox directly on Window");
                                                                try { cbDirect.Focus(); } catch { }
                                                                return;
                                                            }
                                                        }
                                                        catch { }

                                                    }
                                                    catch { }
                                                });
                                            }
                                            catch { }
                                        }
                                        // If we exit the loop without focusing, log that attempt ended
                                        System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focus attempts completed");
                                    });
                                }
                                catch { }

                            }
                            catch { }
                        }, Avalonia.Threading.DispatcherPriority.Background);
                    }
                }
                catch { }

            }
            catch { }
        }

        // Public API to add an active segment at the current page. Returns true if a new active segment was created.
        public bool AddSegmentAtCurrentPage()
        {
            try
            {
                var art = EditorState.ActiveArticle;
                if (art == null)
                {
                    ToastService.Show("No active article selected");
                    try { Console.WriteLine("[DEBUG] AddSegmentAtCurrentPage: no active article"); } catch { }
                    return false;
                }

                // If an active segment exists, do not auto-close; block creation
                if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                {
                    ToastService.Show("Finish or cancel the open segment first");
                    try { Console.WriteLine("[DEBUG] AddSegmentAtCurrentPage: active segment already open"); } catch { }
                    return false;
                }

                // Do not allow adding if current page is already in the article's pages
                var page = EditorState.CurrentPage;
                if (art.Pages != null && art.Pages.Contains(page))
                {
                    ToastService.Show($"Page {page} already in article");
                    try { Console.WriteLine($"[DEBUG] AddSegmentAtCurrentPage: page {page} already in article"); } catch { }
                    return false;
                }

                // Create a new active segment starting at current page
                var seg = new Common.Shared.Segment(page);
                seg.WasNew = true;
                seg.End = null; // active open segment
                art.Segments.Add(seg);
                // Also ensure the Pages list contains the start page
                if (art.Pages == null) art.Pages = new List<int> { page };
                else if (!art.Pages.Contains(page)) { art.Pages.Add(page); art.Pages.Sort(); }

                EditorState.ActiveSegment = seg;
                EditorState.NotifyStateChanged();
                try { Console.WriteLine($"[DEBUG] AddSegmentAtCurrentPage: created new active segment start={page}"); } catch { }
                return true;
            }
            catch { return false; }
        }

        // Public helpers so external callers (e.g., MainWindow key handlers) can move to the previous/next existing page
        public void MoveLeft()
        {
            try
            {
                var folder = EditorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder) && IndexEditor.Shared.EditorState.ShowImages)
                {
                    var found = FindNearestExistingPage(folder, EditorState.CurrentPage - 1, -1);
                    if (found.HasValue) Page = found.Value;
                }
                else
                {
                    // No folder or images disabled: just decrement
                    Page = Math.Max(1, EditorState.CurrentPage - 1);
                }
            }
            catch { }
        }

        public void MoveRight()
        {
            try
            {
                var folder = EditorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder) && IndexEditor.Shared.EditorState.ShowImages)
                {
                    var found = FindNearestExistingPage(folder, EditorState.CurrentPage + 1, 1);
                    if (found.HasValue) Page = found.Value;
                }
                else
                {
                    // No folder or images disabled: just increment
                    Page = IndexEditor.Shared.EditorState.CurrentPage + 1;
                }
            }
            catch { }
        }
    }
}
