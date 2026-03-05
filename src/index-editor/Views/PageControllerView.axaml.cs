using Avalonia.Controls;
using IndexEditor.Shared;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using System.IO;
using Avalonia;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Views
{
    public partial class PageControllerView : UserControl
    {
        private IndexEditor.Shared.IEditorState _editorState;
        private IPageControllerBridge? _assignedBridge;
        
        // Injected services (replacing local implementations)
        private readonly Services.IPageNavigationService _pageNavigationService;
        private readonly Services.IImageLoadingService _imageLoadingService;
        private readonly Services.ILinkManagementService _linkManagementService;

        public void SetBridge(IPageControllerBridge bridge)
        {
            _assignedBridge = bridge;
        }

        /// <summary>
        /// Inject EditorState after XAML construction (for dependency injection)
        /// </summary>
        public void SetEditorState(IndexEditor.Shared.IEditorState editorState)
        {
            _editorState = editorState;
        }

        /// <summary>
        /// Scans the current folder and builds a list of available page numbers from image files.
        /// Delegates to PageNavigationService.
        /// </summary>
        private void ScanAvailablePages()
        {
            var folder = _editorState.CurrentFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return;
            }

            _pageNavigationService.ScanAvailablePages(folder);
            UpdateNavigationButtons();
        }

        /// <summary>
        /// Updates the enabled/disabled state of prev/next buttons based on current position in available pages.
        /// </summary>
        private void UpdateNavigationButtons()
        {
            try
            {
                var prevBtn = this.FindControl<Button>("PrevPageBtn");
                var nextBtn = this.FindControl<Button>("NextPageBtn");

                if (prevBtn == null || nextBtn == null) return;

                var currentPage = _editorState.CurrentPage;
                var navigationState = _pageNavigationService.GetNavigationState(currentPage);
                
                prevBtn.IsEnabled = navigationState.CanGoBack;
                nextBtn.IsEnabled = navigationState.CanGoForward;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageControllerView.UpdateNavigationButtons", ex);
            }
        }

        /// <summary>
        /// Updates the current article information display at the top of the page controller.
        /// Shows article cards for all articles that contain the current page (stacked if multiple).
        /// Prioritizes showing the currently selected article if it exists.
        /// </summary>
        private void UpdateCurrentArticleDisplay()
        {
            try
            {
                var articleCardsContainer = this.FindControl<StackPanel>("ArticleCardsContainer");
                if (articleCardsContainer == null) return;

                // Clear existing article cards
                articleCardsContainer.Children.Clear();

                // Get the currently selected article from the view model
                var vm = this.DataContext as EditorStateViewModel;
                var selectedArticle = vm?.SelectedArticle;

                // Find all articles that contain the current page
                var currentPage = _editorState.CurrentPage;
                var articles = _editorState.Articles
                    .Where(a => a.Pages != null && a.Pages.Contains(currentPage))
                    .ToList();

                // Always show articles that are on the current page
                // If the selected article is on the current page, show it first
                if (selectedArticle != null && selectedArticle.Pages != null && selectedArticle.Pages.Contains(currentPage))
                {
                    var card = CreateArticleCard(selectedArticle);
                    if (card != null)
                    {
                        articleCardsContainer.Children.Add(card);
                    }
                    
                    // Show other articles on this page (excluding the selected one)
                    foreach (var article in articles.Where(a => !object.ReferenceEquals(a, selectedArticle)))
                    {
                        card = CreateArticleCard(article);
                        if (card != null)
                        {
                            articleCardsContainer.Children.Add(card);
                        }
                    }
                }
                else if (articles.Count > 0)
                {
                    // No selected article or it's not on this page, show all articles on the current page
                    foreach (var article in articles)
                    {
                        var card = CreateArticleCard(article);
                        if (card != null)
                        {
                            articleCardsContainer.Children.Add(card);
                        }
                    }
                }
                // If no articles found on the current page, leave container empty
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageControllerView.UpdateCurrentArticleDisplay", ex);
            }
        }

        /// <summary>
        /// Creates a visual card for an article with colored bar, title, details, and category.
        /// </summary>
        private Border CreateArticleCard(Common.Shared.ArticleLine article)
        {
            try
            {
                // Create the card border
                var cardBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                // Create grid with 3 columns: color bar | content | category
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(GridLength.Auto),    // Color bar
                        new ColumnDefinition(GridLength.Star),    // Content
                        new ColumnDefinition(GridLength.Auto)     // Category label
                    }
                };

                // Create color bar
                var colorBarOuter = new Border
                {
                    Width = 16,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(colorBarOuter, 0);

                // Get category color
                var converter = new ArticleCategoryToColorConverter();
                var colorBrush = converter.Convert(article.Category, typeof(SolidColorBrush), null, 
                    System.Globalization.CultureInfo.InvariantCulture) as SolidColorBrush;

                var colorBarInner = new Border
                {
                    Background = colorBrush ?? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    Opacity = 0.6,
                    CornerRadius = new CornerRadius(1)
                };
                colorBarOuter.Child = colorBarInner;

                // Create content stack (title and details)
                var contentStack = new StackPanel
                {
                    Spacing = 2
                };
                Grid.SetColumn(contentStack, 1);

                // Title
                var titleText = new TextBlock
                {
                    Text = !string.IsNullOrWhiteSpace(article.Title) ? article.Title : article.Category,
                    FontWeight = FontWeight.Bold,
                    FontSize = 16
                };
                contentStack.Children.Add(titleText);

                // Details - show different fields based on category
                var details = new List<string>();
                var cat = (article.Category ?? string.Empty).ToLowerInvariant();
                
                // Show model name and age only for Model, Cover, Group, Wives, and Interview categories
                if (cat == "model" || cat == "cover" || cat == "group" || cat == "wives" || cat == "interview")
                {
                    if (!string.IsNullOrWhiteSpace(article.ModelName0))
                    {
                        details.Add(article.ModelName0);
                    }
                    if (!string.IsNullOrWhiteSpace(article.Age0))
                    {
                        details.Add(article.Age0);
                    }
                }
                
                // Show contributor for all categories (as photographer, author, illustrator, etc.)
                if (!string.IsNullOrWhiteSpace(article.Contributor0))
                {
                    details.Add(article.Contributor0);
                }

                if (details.Count > 0)
                {
                    var detailsText = new TextBlock
                    {
                        Text = string.Join(" • ", details),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66))
                    };
                    contentStack.Children.Add(detailsText);
                }

                // Category label
                var categoryText = new TextBlock
                {
                    Text = article.Category,
                    FontWeight = FontWeight.Bold,
                    FontSize = 13,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(categoryText, 2);

                // Add all elements to grid
                grid.Children.Add(colorBarOuter);
                grid.Children.Add(contentStack);
                grid.Children.Add(categoryText);

                cardBorder.Child = grid;
                return cardBorder;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageControllerView.CreateArticleCard", ex);
                return null;
            }
        }

        public int Page
        {
            get => _editorState.CurrentPage;
            set
            {
                var desired = value;
                var pageInput = this.FindControl<TextBox>("PageInput");
                
                // Ensure we have scanned available pages for the current folder
                var folder = _editorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder) && folder != _lastScannedFolder)
                {
                    ScanAvailablePages();
                }
                
                // If images are enabled, only allow navigation to pages that exist in our scanned list
                try
                {
                    if (_editorState.ShowImages)
                    {
                        if (!string.IsNullOrWhiteSpace(folder) && _availablePages.Count > 0)
                        {
                            // Check if the desired page is in our available pages list
                            if (!_availablePages.Contains(desired))
                            {
                                // Find nearest available page
                                var nearest = _availablePages
                                    .OrderBy(p => Math.Abs(p - desired))
                                    .FirstOrDefault();

                                if (nearest > 0)
                                {
                                    desired = nearest;
                                    // Notify user that we jumped to nearest page
                                    if (nearest != value)
                                    {
                                        try 
                                        { 
                                            IndexEditor.Shared.ToastService.Show($"Page {value} not found, showing nearest page {nearest}"); 
                                        } 
                                        catch (Exception toastEx) 
                                        { 
                                            DebugLogger.LogException("PageControllerView.Page: toast notification", toastEx); 
                                        }
                                    }
                                }
                                else
                                {
                                    // No pages available
                                    DebugLogger.Log($"PageControllerView.Page: No available pages in folder");
                                    try 
                                    { 
                                        IndexEditor.Shared.ToastService.Show($"Page {value} not found"); 
                                    } 
                                    catch (Exception toastEx) 
                                    { 
                                        DebugLogger.LogException("PageControllerView.Page: toast notification (no pages)", toastEx); 
                                    }
                                    
                                    // Reset the textbox to show the actual current page
                                    if (pageInput != null)
                                    {
                                        pageInput.Text = _editorState.CurrentPage.ToString();
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("PageControllerView.Page: image lookup", ex); }

                _editorState.CurrentPage = desired;
                if (pageInput != null)
                    pageInput.Text = _editorState.CurrentPage.ToString();
                    
                // Update button states
                UpdateNavigationButtons();
                
                // Update current article display
                UpdateCurrentArticleDisplay();
                    
                // If an active segment exists, update its preview end so UI displays Start → CurrentPage
                try
                {
                    var seg = _editorState.ActiveSegment;
                    if (seg != null && seg.IsActive)
                    {
                        seg.CurrentPreviewEnd = _editorState.CurrentPage;
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("PageControllerView.Page: update preview end", ex); }
                // Do NOT update _editorState.ActiveSegment.End here; changing pages should not close the active segment.
                // Notify UI/state but do NOT auto-select an article when the current page changes.
                // Selection should only occur when the user explicitly presses the Sync button.
                _editorState.NotifyStateChanged();
                // Also load the current page image immediately when Page is set
                try { LoadCurrentPageImage(); } catch (Exception ex) { DebugLogger.LogException("PageControllerView.Page: LoadCurrentPageImage", ex); }
                
                // Update link indicator for new page
                UpdateLinkIndicator();
            }
        }

        /// <summary>
        /// Parameterless constructor for XAML instantiation
        /// </summary>
        public PageControllerView() : this(null, null, null, null) { }

        /// <summary>
        /// Constructor with optional dependency injection
        /// </summary>
        public PageControllerView(
            IndexEditor.Shared.IEditorState? editorState,
            Services.IPageNavigationService? pageNavigationService = null,
            Services.IImageLoadingService? imageLoadingService = null,
            Services.ILinkManagementService? linkManagementService = null)
        {
            _editorState = editorState ?? new IndexEditor.Shared.EditorStateService();
            _pageNavigationService = pageNavigationService ?? new Services.PageNavigationService();
            _imageLoadingService = imageLoadingService ?? new Services.ImageLoadingService();
            _linkManagementService = linkManagementService ?? new Services.LinkManagementService();
            
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
                        try
                        {
                            await System.Threading.Tasks.Task.Delay(displayMs).ConfigureAwait(false);
                            const int steps = 8; const int stepMs = 40;
                            for (int i = 0; i < steps; i++)
                            {
                                var t = i + 1;
                                Dispatcher.UIThread.Post(() => { try { toastBorder.Opacity = Math.Max(0.0, 1.0 - (double)t / steps); } catch (Exception ex) { DebugLogger.LogException("PageControllerView.ShowToast: opacity", ex); } });
                                await System.Threading.Tasks.Task.Delay(stepMs).ConfigureAwait(false);
                            }
                            Dispatcher.UIThread.Post(() => { try { toastBorder.IsVisible = false; toastBorder.Opacity = 1.0; } catch (Exception ex) { DebugLogger.LogException("PageControllerView.ShowToast: hide", ex); } });
                        }
                        catch (Exception ex) { DebugLogger.LogException("PageControllerView.ShowToast: background task", ex); }
                    });
                }
                catch (Exception ex) { DebugLogger.LogException("PageControllerView.ShowToast: outer", ex); }
            }

            Action<string> toastHandler = (msg) => { Dispatcher.UIThread.Post(() => ShowToast(msg)); };
            IndexEditor.Shared.ToastService.ShowRequested += toastHandler;
            this.DetachedFromVisualTree += (s, e) => { IndexEditor.Shared.ToastService.ShowRequested -= toastHandler; };

            if (prevBtn != null)
                prevBtn.Click += (s, e) =>
                {
                    try
                    {
                        var currentPage = _editorState.CurrentPage;
                        var prevPage = _pageNavigationService.GetPreviousPage(currentPage);
                        
                        if (prevPage.HasValue)
                        {
                            Page = prevPage.Value;
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("PageControllerView.PrevBtn.Click", ex); }
                };
            if (nextBtn != null)
                nextBtn.Click += (s, e) =>
                {
                    try
                    {
                        var currentPage = _editorState.CurrentPage;
                        var nextPage = _pageNavigationService.GetNextPage(currentPage);
                        
                        if (nextPage.HasValue)
                        {
                            Page = nextPage.Value;
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("PageControllerView.NextBtn.Click", ex); }
                };

            if (pageInput != null)
            {
                pageInput.KeyDown += (s, ke) => { if (ke.Key == Avalonia.Input.Key.Enter && int.TryParse(pageInput.Text, out var v) && v > 0) Page = v; };
                pageInput.Text = _editorState.CurrentPage.ToString();
            }

            void UpdateUi()
            {
                // Buttons enablement - no EndSegment button present
                // Active article/segment are displayed in the ArticleEditor now
            }

            // Subscribe to state changes to refresh UI
            _editorState.StateChanged += () => Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Always rescan available pages to pick up newly added files
                    // This ensures that if image files are added while the editor is open,
                    // they become available for navigation
                    ScanAvailablePages();
                    
                    if (pageInput != null) pageInput.Text = _editorState.CurrentPage.ToString();
                    UpdateUi();
                    UpdateNavigationButtons();
                    UpdateCurrentArticleDisplay();
                    LoadCurrentPageImage();
                }
                catch (Exception ex) { DebugLogger.LogException("PageControllerView.StateChanged handler", ex); }
            });

            // Initial sync
            UpdateUi();
            // Scan available pages on initialization
            ScanAvailablePages();
            // Load initial page image
            try { LoadCurrentPageImage(); } catch (Exception ex) { DebugLogger.LogException("PageControllerView ctor: LoadCurrentPageImage", ex); }

            // Debug helper: if INDEXEDITOR_DEBUG_AUTOCREATE=1 is set, auto-trigger creating a new article
            try
            {
                var auto = Environment.GetEnvironmentVariable("INDEXEDITOR_DEBUG_AUTOCREATE");
                if (!string.IsNullOrEmpty(auto) && auto == "1")
                {
                    // Use a local async void helper to avoid ambiguous Task.Run overload resolution
                    async void AutoCreateAsync()
                    {
                        await System.Threading.Tasks.Task.Delay(600).ConfigureAwait(false);
                        Dispatcher.UIThread.Post(() => { try { CreateNewArticle(); } catch (Exception ex) { DebugLogger.LogException("PageControllerView.DebugAutoCreate", ex); } });
                    }
                    AutoCreateAsync();
                }
            }
            catch (Exception ex) { DebugLogger.LogException("PageControllerView.DebugAutoCreate outer", ex); }

            // EndSegment button removed; keep EndActiveSegment method available for programmatic use
        }

        // Ends the current active segment (if any) by setting its End to CurrentPage, updating the article pages,
        // syncing the view-model, clearing ActiveSegment and notifying the _editorState.
        public void EndActiveSegment()
        {
            try
            {
                DebugLogger.Log("EndActiveSegment: invoked");
                Console.WriteLine("[DEBUG] EndActiveSegment: invoked");
                if (_editorState.ActiveSegment == null || !_editorState.ActiveSegment.IsActive)
                {
                    DebugLogger.Log("EndActiveSegment: no active segment to end");
                    return;
                }

                var start = _editorState.ActiveSegment.Start;
                var end = _editorState.CurrentPage;
                if (end < start) (start, end) = (end, start);
                DebugLogger.Log($"EndActiveSegment: start={start} end={end}");
                Console.WriteLine($"[DEBUG] EndActiveSegment: start={start} end={end}");

                var art = _editorState.ActiveArticle;
                if (art != null)
                {
                    var newPages = new List<int>(art.Pages ?? new List<int>());
                    for (int p = start; p <= end; p++) if (!newPages.Contains(p)) newPages.Add(p);
                    newPages.Sort();
                    art.Pages = newPages;
                    DebugLogger.Log($"EndActiveSegment: article updated with {newPages.Count} pages (first={newPages.FirstOrDefault()})");
                    Console.WriteLine($"[DEBUG] EndActiveSegment: article updated with {newPages.Count} pages (first={newPages.FirstOrDefault()})");

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
                                DebugLogger.Log("EndActiveSegment: VM article pages updated");
                                Console.WriteLine("[DEBUG] EndActiveSegment: VM article pages updated");
                            }
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("EndActiveSegment: updating VM", ex); }
                }

                // Close and clear the active segment
                if (_editorState.ActiveSegment != null)
                {
                    _editorState.ActiveSegment.End = _editorState.CurrentPage;
                    _editorState.ActiveSegment.CurrentPreviewEnd = null;
                    DebugLogger.Log($"EndActiveSegment: set ActiveSegment.End={_editorState.CurrentPage}");
                    Console.WriteLine($"[DEBUG] EndActiveSegment: set ActiveSegment.End={_editorState.CurrentPage}");
                }
                
                // Validate segments for missing pages
                if (art != null)
                {
                    try
                    {
                        var folder = _editorState.CurrentFolder;
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            art.ValidateSegments(folder, (f, p) => IndexEditor.Shared.ImageHelper.ImageExists(f, p));
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("EndActiveSegment: validate segments", ex); }
                }
                
                _editorState.ActiveSegment = null;
                _editorState.NotifyStateChanged();
                DebugLogger.Log("EndActiveSegment: completed and cleared ActiveSegment");
                Console.WriteLine("[DEBUG] EndActiveSegment: completed and cleared ActiveSegment");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("EndActiveSegment", ex);
            }
        }


        private void LoadCurrentPageImage()
        {
            var img = this.FindControl<Image>("PageImage");
            var pageInput = this.FindControl<TextBox>("PageInput");
            var missing = this.FindControl<TextBlock>("ImageMissingText");
            if (img == null) return;
            
            img.Source = null;
            
            // Respect CLI flag to hide images
            if (!_editorState.ShowImages)
            {
                if (missing != null) { missing.Text = "Images disabled (--no-images)"; missing.IsVisible = true; }
                if (pageInput != null) pageInput.Foreground = Brushes.Gray;
                return;
            }
            
            var folder = _editorState.CurrentFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                // No folder: show missing message
                if (missing != null) { missing.Text = "No folder opened"; missing.IsVisible = true; }
                if (pageInput != null) pageInput.Foreground = Brushes.Red;
                return;
            }
            
            var page = _editorState.CurrentPage;
            var loadResult = _imageLoadingService.LoadPageImage(page);
            
            if (loadResult.Success && loadResult.Bitmap != null)
            {
                img.Source = loadResult.Bitmap;
                
                // Set high quality interpolation mode for better image rendering
                RenderOptions.SetBitmapInterpolationMode(img, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
                
                if (missing != null) missing.IsVisible = false;
                if (pageInput != null) pageInput.Foreground = Brushes.Black;
            }
            else
            {
                // Image not found or failed to load
                if (missing != null) 
                { 
                    missing.Text = loadResult.ErrorMessage ?? $"Page {page} not found"; 
                    missing.IsVisible = true; 
                }
                if (pageInput != null) pageInput.Foreground = Brushes.Red;
            }
        }


        // Public API: create a new article (mirrors NewArticle button behavior)
        public void CreateNewArticle()
        {
            try
            {
                if (_editorState.ActiveSegment != null && _editorState.ActiveSegment.IsActive)
                {
                    IndexEditor.Shared.ToastService.Show("End or cancel the active segment before adding a new article");
                    return;
                }

                // Create the article and insert into the shared EditorState
                var article = new Common.Shared.ArticleLine();
                article.Pages = new List<int> { _editorState.CurrentPage };
                if (_editorState.Articles == null)
                    _editorState.Articles = new List<Common.Shared.ArticleLine> { article };
                else
                {
                    int insertIndex = _editorState.Articles.FindIndex(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() > article.Pages.Min());
                    if (insertIndex == -1)
                        _editorState.Articles.Add(article);
                    else
                        _editorState.Articles.Insert(insertIndex, article);
                }

                _editorState.ActiveArticle = article;

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
                                if (_editorState.CurrentPage >= start && _editorState.CurrentPage <= end)
                                {
                                    alreadyHas = true;
                                    break;
                                }
                            }
                            catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: checking existing segments", ex); }
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: outer segment check", ex); }
                if (!alreadyHas)
                {
                    var seg = new Common.Shared.Segment(_editorState.CurrentPage);
                    // Close immediately (single-page) and mark as not-new so it behaves like an existing segment
                    seg.End = _editorState.CurrentPage;
                    seg.WasNew = false;
                    if (article.Segments == null) article.Segments = new System.Collections.ObjectModel.ObservableCollection<Common.Shared.Segment>();
                    article.Segments.Add(seg);
                    DebugLogger.Log($"PageController.CreateNewArticle: Added closed segment for page {_editorState.CurrentPage}");
                }
                else
                {
                    DebugLogger.Log($"PageController.CreateNewArticle: Skipped adding duplicate segment for page {_editorState.CurrentPage}");
                }

                // Ensure the article pages include the page (article.Pages was already initialized to this page),
                // then notify so view-models and UI update.
                _editorState.NotifyStateChanged();
                try { _editorState.RequestArticleEditorFocus(); DebugLogger.Log("PageController.CreateNewArticle: requested ArticleEditor focus"); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: RequestArticleEditorFocus", ex); }

                // Notify user of success
                try { IndexEditor.Shared.ToastService.Show("New article created"); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: toast", ex); }

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
                                        catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: set lb.SelectedItem", ex); }
                                    }
                                }
                                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: ArticleList selection", ex); }

                                // Re-notify after a short delay to help DataTemplate creation (ArticleEditor) react
                                try
                                {
                                    async void ReNotifyAsync()
                                    {
                                        try
                                        {
                                            await System.Threading.Tasks.Task.Delay(120).ConfigureAwait(false);
                                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                            {
                                                try { _editorState.NotifyStateChanged(); System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: re-notified EditorState after selection"); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: re-notify", ex); }
                                            });
                                        }
                                        catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: re-notify background", ex); }
                                    }
                                    ReNotifyAsync();
                                }
                                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: re-notify scheduling", ex); }

                                // Also request ArticleEditor focus explicitly so any ArticleEditor instance can react
                                try { _editorState.RequestArticleEditorFocus(); Console.WriteLine("[DEBUG] PageController.CreateNewArticle: RequestArticleEditorFocus called after scheduling"); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: RequestArticleEditorFocus after schedule", ex); }

                                // Forceful focus: directly find the ArticleEditor control on the window and repeatedly call its focus helpers
                                try
                                {
                                    var mainWindow = this.VisualRoot as Window;
                                    if (mainWindow != null)
                                    {
                                        var ae = mainWindow.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl")
                                                 ?? mainWindow.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                                        if (ae != null)
                                        {
                                            System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: performing forced focus retries on ArticleEditor instance");
                                            // Run short retry attempts on a background thread to avoid blocking the UI thread
                                            async void ForcedFocusLoopAsync()
                                            {
                                                try
                                                {
                                                    const int attempts = 10;
                                                    const int delayMs = 80;
                                                    for (int i = 0; i < attempts; i++)
                                                    {
                                                        try
                                                        {
                                                            await System.Threading.Tasks.Task.Delay(delayMs).ConfigureAwait(false);
                                                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                                            {
                                                                try
                                                                {
                                                                    System.Console.WriteLine($"[DEBUG] PageController.CreateNewArticle: forced focus attempt {i}");
                                                                    try { ae.FocusTitle(); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: ae.FocusTitle", ex); }
                                                                    try { ae.FocusEditor(); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: ae.FocusEditor", ex); }
                                                                }
                                                                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: forced focus post", ex); }
                                                            });
                                                        }
                                                        catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: forced focus delay", ex); }
                                                    }
                                                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: forced focus retry loop finished");
                                                }
                                                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: forced focus outer", ex); }
                                            }
                                            ForcedFocusLoopAsync();
                                        }
                                        else
                                        {
                                            System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: ArticleEditor instance not found on Window for forced focus");
                                        }
                                    }
                                }
                                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: forced focus outer", ex); }
                            
                                // Short retry loop that attempts to focus the editor controls when they become available
                                 try
                                 {
                                    async void FocusRetryAsync()
                                    {
                                        try
                                        {
                                            var main = this.VisualRoot as Window;
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
                                                            var host = main?.FindControl<ContentControl>("EditorContentHost") ?? main?.FindControl<ContentControl>("EditorContent");
                                                            if (host != null)
                                                            {
                                                                System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: found host ContentControl: " + host.Name);
                                                            }
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
                                                                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: focus inside host", ex); }
                                                            }

                                                            // 2) Try to find ArticleEditor control on the Window and call its helpers
                                                            try
                                                            {
                                                                var ae = main?.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl")
                                                                         ?? main?.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                                                                if (ae != null)
                                                                {
                                                                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: calling ae.FocusTitle()/FocusEditor()");
                                                                    try { ae.FocusTitle(); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: ae.FocusTitle 2", ex); }
                                                                    try { ae.FocusEditor(); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: ae.FocusEditor 2", ex); }
                                                                    return;
                                                                }
                                                            }
                                                            catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: find ae", ex); }

                                                            // 3) Directly search the Window for TitleTextBox or CategoryComboBox
                                                            try
                                                            {
                                                                var tbDirect = main?.FindControl<TextBox>("TitleTextBox");
                                                                if (tbDirect != null)
                                                                {
                                                                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focusing TitleTextBox directly on Window");
                                                                    try { tbDirect.Focus(); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: tbDirect.Focus", ex); }
                                                                    return;
                                                                }
                                                                var cbDirect = main?.FindControl<ComboBox>("CategoryComboBox");
                                                                if (cbDirect != null)
                                                                {
                                                                    System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focusing CategoryComboBox directly on Window");
                                                                    try { cbDirect.Focus(); } catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: cbDirect.Focus", ex); }
                                                                    return;
                                                                }
                                                            }
                                                            catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: direct search", ex); }

                                                        }
                                                        catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: retry UI post", ex); }
                                                    });
                                                }
                                                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: retry delay", ex); }
                                            }
                                            // If we exit the loop without focusing, log that attempt ended
                                            System.Console.WriteLine("[DEBUG] PageController.CreateNewArticle: focus attempts completed");
                                        }
                                        catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: retry loop outer", ex); }
                                    }
                                    FocusRetryAsync();
                                 }
                                 catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: short retry loop", ex); }

                            }
                            catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: scheduling selection+focus", ex); }
                        }, Avalonia.Threading.DispatcherPriority.Background);
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("CreateNewArticle: outer", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("PageController.CreateNewArticle: outermost", ex); }
        }

        // Public API to add an active segment at the current page. Returns true if a new active segment was created.
        public bool AddSegmentAtCurrentPage()
        {
            try
            {
                return IndexEditor.Shared.EditorActions.AddSegmentAtCurrentPage();
            }
            catch (Exception ex) { DebugLogger.LogException("PageControllerView.AddSegmentAtCurrentPage: outer", ex); return false; }
        }

        // Public helpers so external callers (e.g., MainWindow key handlers) can move to the previous/next existing page
        public void MoveLeft()
        {
            try
            {
                var folder = _editorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder) && _editorState.ShowImages)
                {
                    // Ensure pages are scanned
                    if (folder != _lastScannedFolder)
                    {
                        ScanAvailablePages();
                    }

                    if (_availablePages.Count > 0)
                    {
                        var currentPage = _editorState.CurrentPage;
                        var currentIndex = _availablePages.IndexOf(currentPage);

                        if (currentIndex > 0)
                        {
                            // Go to previous page in the list
                            Page = _availablePages[currentIndex - 1];
                        }
                        else if (currentIndex < 0)
                        {
                            // Current page not in list, find closest page before it
                            var prevPage = _availablePages
                                .Where(p => p < currentPage)
                                .OrderByDescending(p => p)
                                .FirstOrDefault();
                            if (prevPage > 0)
                            {
                                Page = prevPage;
                            }
                        }
                    }
                }
                else
                {
                    // No folder or images disabled: just decrement
                    Page = Math.Max(1, _editorState.CurrentPage - 1);
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MoveLeft", ex); }
        }

        public void MoveRight()
        {
            try
            {
                var folder = _editorState.CurrentFolder;
                if (!string.IsNullOrWhiteSpace(folder) && _editorState.ShowImages)
                {
                    // Ensure pages are scanned
                    if (folder != _lastScannedFolder)
                    {
                        ScanAvailablePages();
                    }

                    if (_availablePages.Count > 0)
                    {
                        var currentPage = _editorState.CurrentPage;
                        var currentIndex = _availablePages.IndexOf(currentPage);

                        if (currentIndex >= 0 && currentIndex < _availablePages.Count - 1)
                        {
                            // Go to next page in the list
                            Page = _availablePages[currentIndex + 1];
                        }
                        else if (currentIndex < 0)
                        {
                            // Current page not in list, find closest page after it
                            var nextPage = _availablePages
                                .Where(p => p > currentPage)
                                .OrderBy(p => p)
                                .FirstOrDefault();
                            if (nextPage > 0)
                            {
                                Page = nextPage;
                            }
                        }
                    }
                }
                else
                {
                    // No folder or images disabled: just increment
                    Page = _editorState.CurrentPage + 1;
                }
            }
            catch (Exception ex) { DebugLogger.LogException("MoveRight", ex); }
        }

        // Handle double-click on the page image to show fullscreen view
        private void OnImageDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                DebugLogger.Log("PageControllerView.OnImageDoubleTapped: opening fullscreen image viewer");
                
                // Find the MainWindow
                var mainWindow = this.VisualRoot as MainWindow;
                if (mainWindow == null)
                {
                    DebugLogger.Log("PageControllerView.OnImageDoubleTapped: MainWindow not found");
                    return;
                }

                // Get the current image source
                var img = this.FindControl<Image>("PageImage");
                if (img?.Source == null)
                {
                    DebugLogger.Log("PageControllerView.OnImageDoubleTapped: No image to display");
                    IndexEditor.Shared.ToastService.Show("No image to display in fullscreen");
                    return;
                }

                // Call MainWindow method to show fullscreen overlay
                var showMethod = typeof(MainWindow).GetMethod("ShowFullscreenImage", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
                if (showMethod != null)
                {
                    showMethod.Invoke(mainWindow, new object[] { img.Source, _editorState.CurrentPage });
                    DebugLogger.Log($"PageControllerView.OnImageDoubleTapped: Invoked ShowFullscreenImage for page {_editorState.CurrentPage}");
                }
                else
                {
                    DebugLogger.Log("PageControllerView.OnImageDoubleTapped: ShowFullscreenImage method not found");
                }
            }
            catch (Exception ex) 
            { 
                DebugLogger.LogException("PageControllerView.OnImageDoubleTapped", ex); 
            }
        }

        // Bridge will call the public methods defined on this view (AddSegmentAtCurrentPage, CreateNewArticle, EndActiveSegment, MoveLeft, MoveRight).

        private Dictionary<int, List<Common.Shared.MagazineLink>> _pageLinks = new();

        /// <summary>
        /// Update the link indicator for discovered links
        /// </summary>
        public void UpdateDiscoveredLinks(Dictionary<int, List<Common.Shared.MagazineLink>> discoveredLinks)
        {
            _pageLinks = discoveredLinks ?? new Dictionary<int, List<Common.Shared.MagazineLink>>();
            UpdateLinkIndicator();
        }

        /// <summary>
        /// Update the link indicator visibility and count based on current page
        /// </summary>
        private void UpdateLinkIndicator()
        {
            try
            {
                var linkIndicator = this.FindControl<StackPanel>("LinkIndicator");
                var linkCount = this.FindControl<TextBlock>("LinkCount");
                var tooltipContent = this.FindControl<StackPanel>("LinkTooltipContent");
                
                if (linkIndicator == null) return;

                var currentPage = _editorState.CurrentPage;
                if (_pageLinks.TryGetValue(currentPage, out var links) && links.Count > 0)
                {
                    linkIndicator.IsVisible = true;
                    if (linkCount != null)
                    {
                        linkCount.Text = links.Count == 1 ? "1 link" : $"{links.Count} links";
                    }
                    
                    // Populate tooltip with link details
                    if (tooltipContent != null)
                    {
                        // Clear existing content except the header
                        while (tooltipContent.Children.Count > 1)
                        {
                            tooltipContent.Children.RemoveAt(1);
                        }
                        
                        // Add each link to the tooltip
                        foreach (var link in links)
                        {
                            var linkText = new TextBlock
                            {
                                Text = $"• {link.Magazine} Vol.{link.Volume} No.{link.Issue}",
                                FontSize = 11,
                                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33))
                            };
                            tooltipContent.Children.Add(linkText);
                        }
                    }
                }
                else
                {
                    linkIndicator.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageControllerView.UpdateLinkIndicator", ex);
            }
        }
    }
}
