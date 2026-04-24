using Avalonia.Controls;
using IndexEditor.Shared;
using Common.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Views
{
    public partial class ArticleEditorView : UserControl
    {
        private readonly Services.IIndexFileService? _indexFileService;
        private readonly Services.OverlayManager? _overlayManager;

        public ArticleEditorView(Services.IIndexFileService? indexFileService = null, Services.OverlayManager? overlayManager = null)
        {
            _indexFileService = indexFileService;
            _overlayManager = overlayManager;
            InitializeComponent();
            // Use the window's DataContext (shared EditorStateViewModel). Do not create a new VM here —
            // multiple instances caused duplicate category updates and selection sync issues.
            // DataContext will be inherited from the parent Window; if it's null at runtime the
            // DataContextChanged logic in other controls will set it.
            var editBtn = this.FindControl<Button>("EditSegmentBtn");
            var overlay = this.FindControl<Border>("SegmentEditorOverlay");
            // AddSegment button removed from PageController; Ctrl+A keyboard shortcut is handled at the window level.
            var titleBox = this.FindControl<TextBox>("TitleTextBox");

            if (editBtn != null && overlay != null)
            {
                editBtn.Click += (s, e) => overlay.IsVisible = true;
                overlay.PointerPressed += (s, e) =>
                {
                    // Hide overlay if background is clicked (not the popover)
                    if (e.Source == overlay)
                        overlay.IsVisible = false;
                };
            }

            // legacy AddSegmentBtn removed: no local click wiring needed

            // Listen for page changes to update UI (prototype only)
            EditorState.StateChanged += () =>
            {
                // Optionally update UI, e.g., refresh segment list, highlight active segment, etc.
                if (titleBox != null && EditorState.ActiveArticle != null)
                    titleBox.Text = EditorState.ActiveArticle.Title;
                // Also refresh the view-model from EditorState in case articles were loaded before DataContext was set
                RefreshFromEditorState();
            };

            // Ensure we refresh from initial EditorState in case articles were preloaded
            RefreshFromEditorState();
        }

        // Public helper: focus the editor's primary input (Title textbox)
        public void FocusEditor()
        {
            try
            {
                var tb = this.FindControl<TextBox>("TitleTextBox");
                if (tb != null) { tb.Focus(); return; }
                // fallback to category if title absent
                var cb = this.FindControl<ComboBox>("CategoryComboBox");
                if (cb != null) cb.Focus();
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.FocusEditor", ex); }
        }

        // Specifically focus the title textbox
        public void FocusTitle()
        {
            try
            {
                var tb = this.FindControl<TextBox>("TitleTextBox");
                if (tb != null) tb.Focus();
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.FocusTitle", ex); }
        }

        // Clicks are handled by the OnSegmentLozengePressed method wired in XAML.
        private void OnSegmentLozengePressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Border b && b.Tag is Common.Shared.Segment seg)
                {
                    // Find the article owning this segment
                    var owner = IndexEditor.Shared.EditorState.ActiveArticle;
                    // If the segment belongs to a different article, find it
                    if (owner == null || (owner.Segments != null && !owner.Segments.Contains(seg)))
                    {
                        owner = IndexEditor.Shared.EditorState.Articles.FirstOrDefault(a => a.Segments != null && a.Segments.Contains(seg));
                        if (owner != null)
                            IndexEditor.Shared.EditorState.ActiveArticle = owner;
                    }

                    // If there's another active segment on a different article, block
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                    if (activeSeg != null && activeSeg.IsActive && activeArticle != null && owner != null && !object.ReferenceEquals(activeArticle, owner))
                    {
                        try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.OnSegmentLozengePressed: ToastService.Show", ex); }
                        return;
                    }

                    // If reopening an existing closed segment, remember its end
                    if (seg.End.HasValue)
                    {
                        seg.OriginalEnd = seg.End;
                        seg.WasNew = false;
                        seg.End = null;
                    }

                    // Set active segment and jump page
                    IndexEditor.Shared.EditorState.ActiveSegment = seg;
                    IndexEditor.Shared.EditorState.CurrentPage = seg.Start;
                    IndexEditor.Shared.EditorState.NotifyStateChanged();
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.OnSegmentLozengePressed: outer", ex); }
        }

        private string? _currentFolder;

        public void SetCurrentFolder(string folderPath)
        {
            _currentFolder = folderPath;
            LoadArticlesFromIndexFile();
        }

        private void LoadArticlesFromIndexFile()
        {
            if (string.IsNullOrEmpty(_currentFolder)) return;

            List<Common.Shared.ArticleLine> articles;
            
            // Use IIndexFileService if available (preferred - eliminates duplication)
            if (_indexFileService != null)
            {
                try
                {
                    var (magazine, volume, number, year, loadedArticles, links) = _indexFileService.LoadFromFolder(_currentFolder);
                    articles = loadedArticles;
                    
                    // Update EditorState metadata
                    IndexEditor.Shared.EditorState.CurrentMagazine = magazine;
                    IndexEditor.Shared.EditorState.CurrentVolume = volume;
                    IndexEditor.Shared.EditorState.CurrentNumber = number;
                }
                catch (FormatException fx)
                {
                    // Format error: show toast and open the index overlay for correction
                    ShowFormatError(fx.Message);
                    return;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: service load failed", ex);
                    articles = new List<Common.Shared.ArticleLine>();
                }
            }
            else
            {
                // Legacy fallback path (maintained for backward compatibility)
                var indexFilePath = System.IO.Path.Combine(_currentFolder, "_index.txt");
                if (!System.IO.File.Exists(indexFilePath)) return;
                
                try
                {
                    articles = ParseLegacyIndexFile(indexFilePath);
                }
                catch (FormatException fx)
                {
                    ShowFormatError(fx.Message);
                    return;
                }
            }

            // Update EditorState and ViewModel with loaded articles
            UpdateEditorStateWithArticles(articles);
        }

        /// <summary>
        /// Shows format error to user via toast and overlay
        /// </summary>
        private void ShowFormatError(string errorMessage)
        {
            try { IndexEditor.Shared.ToastService.Show("Index file format error: " + errorMessage); } catch { }
            
            // Use OverlayManager service if available (preferred)
            if (_overlayManager != null)
            {
                try
                {
                    var indexFilePath = GetIndexFilePath();
                    var fileText = System.IO.File.Exists(indexFilePath) 
                        ? System.IO.File.ReadAllText(indexFilePath) 
                        : $"Index file not found in folder: {_currentFolder}";
                    
                    _overlayManager.ShowIndexOverlayError(errorMessage, fileText);
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.ShowFormatError: overlay manager", ex); }
            }
            else
            {
                // Legacy fallback: direct control manipulation
                ShowFormatErrorLegacy(errorMessage);
            }
        }

        /// <summary>
        /// Legacy error display method for backward compatibility
        /// </summary>
        private void ShowFormatErrorLegacy(string errorMessage)
        {
            try
            {
                var wnd = this.VisualRoot as Window;
                if (wnd == null) return;
                
                var indexFilePath = GetIndexFilePath();
                var fileText = System.IO.File.Exists(indexFilePath) 
                    ? System.IO.File.ReadAllText(indexFilePath) 
                    : $"Index file not found in folder: {_currentFolder}";
                
                var overlay = wnd.FindControl<Border>("IndexOverlay");
                var textBlock = wnd.FindControl<TextBlock>("IndexOverlayTextBlock");
                var errorBorder = wnd.FindControl<Border>("IndexOverlayErrorBorder");
                var errorLine = wnd.FindControl<TextBlock>("IndexOverlayErrorLine");
                
                if (overlay != null && textBlock != null)
                {
                    textBlock.Text = fileText;
                    overlay.IsVisible = true;
                    
                    if (errorBorder != null && errorLine != null)
                    {
                        errorLine.Text = errorMessage;
                        errorBorder.IsVisible = true;
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.ShowFormatErrorLegacy", ex); }
        }

        /// <summary>
        /// Gets the path to the index file (JSON preferred, TXT fallback)
        /// </summary>
        private string GetIndexFilePath()
        {
            var jsonPath = System.IO.Path.Combine(_currentFolder ?? string.Empty, "_index.json");
            return System.IO.File.Exists(jsonPath) 
                ? jsonPath 
                : System.IO.Path.Combine(_currentFolder ?? string.Empty, "_index.txt");
        }

        /// <summary>
        /// Parses legacy _index.txt file format
        /// </summary>
        private List<Common.Shared.ArticleLine> ParseLegacyIndexFile(string indexFilePath)
        {
            var lines = System.IO.File.ReadAllLines(indexFilePath);
            var articles = new List<Common.Shared.ArticleLine>();
            
            foreach (var line in lines)
            {
                var raw = line?.Trim();
                if (string.IsNullOrEmpty(raw) || raw.StartsWith("#"))
                    continue;
                    
                var parsed = IndexEditor.Shared.IndexFileParser.ParseArticleLine(raw!);
                if (parsed != null)
                    articles.Add(parsed);
            }
            
            return articles;
        }

        /// <summary>
        /// Updates EditorState and ViewModel with loaded articles
        /// </summary>
        private void UpdateEditorStateWithArticles(List<Common.Shared.ArticleLine> articles)
        {
            // Prepare articles (ensure measurements, validate, notify)
            foreach (var article in articles)
            {
                PrepareArticle(article);
            }

            // Update global EditorState
            IndexEditor.Shared.EditorState.Articles = articles;
            if (articles.Count > 0)
                IndexEditor.Shared.EditorState.ActiveArticle = articles[0];
            IndexEditor.Shared.EditorState.NotifyStateChanged();

            // Update ViewModel's observable collection
            var vm = this.DataContext as EditorStateViewModel;
            if (vm != null)
            {
                try
                {
                    vm.Articles.Clear();
                    foreach (var article in articles)
                    {
                        vm.Articles.Add(article);
                    }
                    
                    if (articles.Count > 0)
                    {
                        vm.SelectedArticle = articles[0];
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.UpdateEditorStateWithArticles: update VM", ex); }
            }
        }

        /// <summary>
        /// Prepares an article for display (ensures measurements, validates, notifies)
        /// </summary>
        private void PrepareArticle(Common.Shared.ArticleLine article)
        {
            try
            {
                if (article.Measurements == null || article.Measurements.Count == 0)
                    article.Measurements = new System.Collections.Generic.List<string> { string.Empty };
                    
                try { article.Validate(); } catch (Exception ex) { DebugLogger.LogException("PrepareArticle: Validate", ex); }
                try { article.NotifyPropertyChanged(nameof(article.Measurements)); } catch (Exception ex) { DebugLogger.LogException("PrepareArticle: Notify Measurements", ex); }
                try { article.NotifyPropertyChanged(nameof(article.Measurements0)); } catch (Exception ex) { DebugLogger.LogException("PrepareArticle: Notify Measurements0", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("PrepareArticle: outer", ex); }
        }

        // Helper: refresh the bound ViewModel (if present) from the static EditorState.Articles.
        private void RefreshFromEditorState()
        {
            try
            {
                var vm = this.DataContext as EditorStateViewModel;
                var articles = IndexEditor.Shared.EditorState.Articles ?? new List<Common.Shared.ArticleLine>();
                if (vm != null)
                {
                    try { vm.Articles.Clear(); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: clear vm.Articles", ex); }
                    
                    foreach (var article in articles)
                    {
                        PrepareArticle(article);
                        try { vm.Articles.Add(article); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: vm.Articles.Add", ex); }
                    }
                    
                    if (vm.SelectedArticle == null && vm.Articles.Count > 0)
                        vm.SelectedArticle = vm.Articles[0];
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: outer", ex); }
        }
    }

    // Simple IObserver<T> implementation used to adapt lambdas to IObserver for Subscribe calls
    internal class LambdaObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        public LambdaObserver(Action<T> onNext) => _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        public void OnNext(T value)
        {
            try { _onNext(value); } catch { }
        }
        public void OnError(Exception error) { /* no-op */ }
        public void OnCompleted() { /* no-op */ }
    }
}
