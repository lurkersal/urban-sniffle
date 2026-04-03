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
        private Services.IPageNavigationService _pageNavigationService;
        private Services.IImageLoadingService _imageLoadingService;
        private Services.ILinkManagementService _linkManagementService;
        private Services.IArticleCardRenderer _articleCardRenderer;
        private Services.IArticleDisplayCoordinator _articleDisplayCoordinator;
        private Services.IArticleFocusManager _articleFocusManager;
        private Services.ISegmentManagementService _segmentManagementService;
        private Services.IPageNavigationCoordinator _pageNavigationCoordinator;
        
        // Store the StateChanged handler so we can unsubscribe/resubscribe when EditorState changes
        private Action? _stateChangedHandler;

        public void SetBridge(IPageControllerBridge bridge)
        {
            _assignedBridge = bridge;
        }

        /// <summary>
        /// Inject EditorState after XAML construction (for dependency injection)
        /// </summary>
        public void SetEditorState(IndexEditor.Shared.IEditorState editorState)
        {
            // Unsubscribe from old EditorState if we have a handler
            if (_stateChangedHandler != null && _editorState != null)
            {
                _editorState.StateChanged -= _stateChangedHandler;
            }
            
            _editorState = editorState;
            var folder = _editorState?.CurrentFolder ?? "(null)";
            System.Console.WriteLine($"[DEBUG] PageControllerView.SetEditorState: CurrentFolder = '{folder}'");
            DebugLogger.Log($"PageControllerView.SetEditorState: CurrentFolder = '{folder}'");
            
            // Re-subscribe to the new EditorState
            if (_stateChangedHandler != null && _editorState != null)
            {
                _editorState.StateChanged += _stateChangedHandler;
                System.Console.WriteLine("[DEBUG] PageControllerView.SetEditorState: Re-subscribed to StateChanged");
                DebugLogger.Log("PageControllerView.SetEditorState: Re-subscribed to StateChanged");
                
                // Trigger an immediate update to load the current state
                try
                {
                    _stateChangedHandler.Invoke();
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("PageControllerView.SetEditorState: initial state update", ex);
                }
            }
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
                
                prevBtn.IsEnabled = _pageNavigationService.HasPreviousPage(currentPage);
                nextBtn.IsEnabled = _pageNavigationService.HasNextPage(currentPage);
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

                // Get the currently selected article from the view model
                var vm = this.DataContext as EditorStateViewModel;
                var selectedArticle = vm?.SelectedArticle;

                // Delegate to the display coordinator
                _articleDisplayCoordinator.UpdateArticleDisplay(articleCardsContainer, selectedArticle);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PageControllerView.UpdateCurrentArticleDisplay", ex);
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
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    // The service tracks _lastScannedFolder internally
                    _pageNavigationService.ScanAvailablePages(folder);
                }
                
                // If images are enabled, only allow navigation to pages that exist in our scanned list
                try
                {
                    if (_editorState.ShowImages)
                    {
                        var availablePages = _pageNavigationService.AvailablePages;
                        if (!string.IsNullOrWhiteSpace(folder) && availablePages.Count > 0)
                        {
                            // Check if the desired page is in our available pages list
                            if (!availablePages.Contains(desired))
                            {
                                // Find nearest available page
                                var nearest = availablePages
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
        /// Parameterless constructor for XAML instantiation.
        /// Services should be injected via SetServices() after construction for proper DI.
        /// </summary>
        public PageControllerView()
        {
            // Default initialization for XAML - services can be injected later via SetServices
            _editorState = new IndexEditor.Shared.EditorStateService();
            _pageNavigationService = new Services.PageNavigationService();
            _imageLoadingService = new Services.ImageLoadingService();
            _linkManagementService = new Services.LinkManagementService();
            _articleCardRenderer = new Services.ArticleCardRenderer();
            _articleDisplayCoordinator = new Services.ArticleDisplayCoordinator(_editorState, _articleCardRenderer);
            _articleFocusManager = new Services.ArticleFocusManager(_editorState);
            _segmentManagementService = new Services.SegmentManagementService(_editorState);
            _pageNavigationCoordinator = new Services.PageNavigationCoordinator(_editorState, _pageNavigationService);
            
            System.Console.WriteLine("[DEBUG] PageControllerView: parameterless constructor (XAML)");
            InitializeComponent();
            InitializeUI();
        }

        /// <summary>
        /// Constructor with full dependency injection (for unit testing or programmatic creation)
        /// </summary>
        public PageControllerView(
            IndexEditor.Shared.IEditorState editorState,
            Services.IPageNavigationService pageNavigationService,
            Services.IImageLoadingService imageLoadingService,
            Services.ILinkManagementService linkManagementService,
            Services.IArticleCardRenderer articleCardRenderer,
            Services.IArticleDisplayCoordinator articleDisplayCoordinator,
            Services.IArticleFocusManager articleFocusManager,
            Services.ISegmentManagementService segmentManagementService,
            Services.IPageNavigationCoordinator pageNavigationCoordinator)
        {
            _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            _pageNavigationService = pageNavigationService ?? throw new ArgumentNullException(nameof(pageNavigationService));
            _imageLoadingService = imageLoadingService ?? throw new ArgumentNullException(nameof(imageLoadingService));
            _linkManagementService = linkManagementService ?? throw new ArgumentNullException(nameof(linkManagementService));
            _articleCardRenderer = articleCardRenderer ?? throw new ArgumentNullException(nameof(articleCardRenderer));
            _articleDisplayCoordinator = articleDisplayCoordinator ?? throw new ArgumentNullException(nameof(articleDisplayCoordinator));
            _articleFocusManager = articleFocusManager ?? throw new ArgumentNullException(nameof(articleFocusManager));
            _segmentManagementService = segmentManagementService ?? throw new ArgumentNullException(nameof(segmentManagementService));
            _pageNavigationCoordinator = pageNavigationCoordinator ?? throw new ArgumentNullException(nameof(pageNavigationCoordinator));
            
            System.Console.WriteLine("[DEBUG] PageControllerView: full DI constructor");
            InitializeComponent();
            InitializeUI();
        }
        
        /// <summary>
        /// Inject services after XAML construction (called from MainWindow)
        /// </summary>
        public void SetServices(
            Services.IPageNavigationService pageNavigationService,
            Services.IImageLoadingService imageLoadingService,
            Services.ILinkManagementService linkManagementService,
            Services.IArticleCardRenderer articleCardRenderer,
            Services.IArticleDisplayCoordinator articleDisplayCoordinator,
            Services.IArticleFocusManager articleFocusManager,
            Services.ISegmentManagementService segmentManagementService,
            Services.IPageNavigationCoordinator pageNavigationCoordinator)
        {
            _pageNavigationService = pageNavigationService ?? throw new ArgumentNullException(nameof(pageNavigationService));
            _imageLoadingService = imageLoadingService ?? throw new ArgumentNullException(nameof(imageLoadingService));
            _linkManagementService = linkManagementService ?? throw new ArgumentNullException(nameof(linkManagementService));
            _articleCardRenderer = articleCardRenderer ?? throw new ArgumentNullException(nameof(articleCardRenderer));
            _articleDisplayCoordinator = articleDisplayCoordinator ?? throw new ArgumentNullException(nameof(articleDisplayCoordinator));
            _articleFocusManager = articleFocusManager ?? throw new ArgumentNullException(nameof(articleFocusManager));
            _segmentManagementService = segmentManagementService ?? throw new ArgumentNullException(nameof(segmentManagementService));
            _pageNavigationCoordinator = pageNavigationCoordinator ?? throw new ArgumentNullException(nameof(pageNavigationCoordinator));
            
            System.Console.WriteLine("[DEBUG] PageControllerView: services injected via SetServices");
        }
        
        /// <summary>
        /// Initialize UI controls and event handlers (extracted from constructor)
        /// </summary>
        private void InitializeUI()
        {
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

            // Create and store the StateChanged handler so it can be unsubscribed/resubscribed if EditorState changes
            _stateChangedHandler = () => Dispatcher.UIThread.Post(() =>
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
            
            // Subscribe to state changes to refresh UI
            _editorState.StateChanged += _stateChangedHandler;

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
        // Ends the current active segment (if any) by setting its End to CurrentPage, updating the article pages,
        // syncing the view-model, clearing ActiveSegment and notifying the _editorState.
        public void EndActiveSegment()
        {
            _segmentManagementService.EndActiveSegment(this.DataContext);
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
            System.Console.WriteLine($"[DEBUG] LoadCurrentPageImage: folder = '{folder ?? "(null)"}'");
            DebugLogger.Log($"LoadCurrentPageImage: folder = '{folder ?? "(null)"}'");
            
            if (string.IsNullOrWhiteSpace(folder))
            {
                // No folder: show missing message
                if (missing != null) { missing.Text = "No folder opened"; missing.IsVisible = true; }
                if (pageInput != null) pageInput.Foreground = Brushes.Red;
                return;
            }
            
            var page = _editorState.CurrentPage;
            var bitmap = _imageLoadingService.LoadPageImage(folder, page);
            
            if (bitmap != null)
            {
                img.Source = bitmap;
                
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
                    missing.Text = $"Page {page} not found"; 
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
                
                // Refresh UI bindings to ensure all fields are properly initialized and
                // any stale values from the previously selected article are cleared
                article.RefreshUIBindings();
                
                if (_editorState.Articles == null)
                {
                    _editorState.Articles = new List<Common.Shared.ArticleLine> { article };
                }
                else
                {
                    int insertIndex = _editorState.Articles.FindIndex(a => 
                        a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() > article.Pages.Min());
                    if (insertIndex == -1)
                        _editorState.Articles.Add(article);
                    else
                        _editorState.Articles.Insert(insertIndex, article);
                }

                _editorState.ActiveArticle = article;

                // Attach a single-page CLOSED segment for the current page
                AddInitialSegmentToArticle(article);

                // Notify so view-models and UI update
                _editorState.NotifyStateChanged();
                
                try 
                { 
                    _editorState.RequestArticleEditorFocus(); 
                    DebugLogger.Log("PageController.CreateNewArticle: requested ArticleEditor focus"); 
                } 
                catch (Exception ex) 
                { 
                    DebugLogger.LogException("CreateNewArticle: RequestArticleEditorFocus", ex); 
                }

                // Notify user of success
                try 
                { 
                    IndexEditor.Shared.ToastService.Show("New article created"); 
                } 
                catch (Exception ex) 
                { 
                    DebugLogger.LogException("CreateNewArticle: toast", ex); 
                }

                // Delegate focus management to the ArticleFocusManager service
                _articleFocusManager.SetupFocusForNewArticle(article, this.VisualRoot!, this.DataContext);
            }
            catch (Exception ex) 
            { 
                DebugLogger.LogException("PageController.CreateNewArticle: outermost", ex); 
            }
        }

        /// <summary>
        /// Adds an initial closed segment to a newly created article
        /// </summary>
        private void AddInitialSegmentToArticle(Common.Shared.ArticleLine article)
        {
            try
            {
                // Guard against duplicate segments for the same page
                bool alreadyHas = false;
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
                        catch (Exception ex) 
                        { 
                            DebugLogger.LogException("AddInitialSegment: checking existing segments", ex); 
                        }
                    }
                }

                if (!alreadyHas)
                {
                    var seg = new Common.Shared.Segment(_editorState.CurrentPage);
                    // Close immediately (single-page) and mark as not-new
                    seg.End = _editorState.CurrentPage;
                    seg.WasNew = false;
                    
                    if (article.Segments == null) 
                        article.Segments = new System.Collections.ObjectModel.ObservableCollection<Common.Shared.Segment>();
                    
                    article.Segments.Add(seg);
                    DebugLogger.Log($"PageController.AddInitialSegment: Added closed segment for page {_editorState.CurrentPage}");
                }
                else
                {
                    DebugLogger.Log($"PageController.AddInitialSegment: Skipped adding duplicate segment for page {_editorState.CurrentPage}");
                }
            }
            catch (Exception ex) 
            { 
                DebugLogger.LogException("AddInitialSegmentToArticle", ex); 
            }
        }

        // Public API to add an active segment at the current page. Returns true if a new active segment was created.
        public bool AddSegmentAtCurrentPage()
        {
            return _segmentManagementService.AddSegmentAtCurrentPage();
        }

        // Public helpers so external callers (e.g., MainWindow key handlers) can move to the previous/next existing page
        public void MoveLeft()
        {
            var newPage = _pageNavigationCoordinator.MoveToPreviousPage();
            if (newPage.HasValue)
            {
                Page = newPage.Value;
            }
        }

        public void MoveRight()
        {
            var newPage = _pageNavigationCoordinator.MoveToNextPage();
            if (newPage.HasValue)
            {
                Page = newPage.Value;
            }
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
