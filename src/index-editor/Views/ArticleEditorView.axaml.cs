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
        public ArticleEditorView()
        {
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
            var indexFilePath = System.IO.Path.Combine(_currentFolder, "_index.txt");
            if (!System.IO.File.Exists(indexFilePath)) return;
            var lines = System.IO.File.ReadAllLines(indexFilePath);
            var articles = new List<Common.Shared.ArticleLine>();
            foreach (var line in lines)
            {
                // Skip empty or comment lines (header metadata starts with '#') — do not attempt to parse these as article lines
                var raw = line?.Trim();
                if (string.IsNullOrEmpty(raw))
                    continue;
                if (raw.StartsWith("#"))
                    continue;
                try
                {
                    // Use the trimmed 'raw' string (non-null) for parsing to satisfy nullable analysis
                    var parsed = IndexEditor.Shared.IndexFileParser.ParseArticleLine(raw!);
                    if (parsed != null)
                    {
                        // ParseArticleLine returns a fully-populated Common.Shared.ArticleLine
                        articles.Add(parsed);
                    }
                }
                catch (FormatException fx)
                {
                    // Format error: show toast and open the index overlay for correction
                    try { IndexEditor.Shared.ToastService.Show("_index.txt format error: " + fx.Message); } catch { }
                    try
                    {
                        var wnd = this.VisualRoot as MainWindow ?? (this.VisualRoot as Window);
                        if (wnd != null)
                        {
                            var overlay = wnd.FindControl<Border>("IndexOverlay");
                            var tb = wnd.FindControl<TextBox>("IndexOverlayTextBox");
                            // Use the existing indexFilePath variable declared at the top of this method
                            if (overlay != null && tb != null)
                            {
                                var fileText = System.IO.File.Exists(indexFilePath) ? System.IO.File.ReadAllText(indexFilePath) : $"_index.txt not found in folder: {_currentFolder}";
                                tb.Text = fileText;
                                // Highlight the overlay with a red border/background to indicate parse error
                                try
                                {
                                    overlay.Background = Brushes.MistyRose;
                                    overlay.BorderBrush = Brushes.Red;
                                    overlay.BorderThickness = new Thickness(2);
                                }
                                catch { }

                                // Try to select the offending line in the TextBox so user can see it immediately
                                try
                                {
                                    // Use the trimmed 'raw' (non-null) when searching inside fileText
                                    var pos = fileText.IndexOf(raw ?? line ?? string.Empty, StringComparison.Ordinal);
                                    if (pos >= 0 && tb != null)
                                    {
                                        // Select the error substring (use safe string for length)
                                        try
                                        {
                                            var selectText = raw ?? line ?? string.Empty;
                                            tb.SelectionStart = pos;
                                            tb.SelectionEnd = pos + selectText.Length;
                                            tb.CaretIndex = tb.SelectionEnd;
                                            // give focus so selection is visible
                                            tb.Focus();
                                        }
                                        catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: selectText set", ex); }
                                    }
                                    else
                                    {
                                        // If line not found, just focus the textbox (if present)
                                        try { tb?.Focus(); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: focus tb fallback", ex); }
                                    }
                                }
                                catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: select offending line", ex); }

                                overlay.IsVisible = true;

                                // Clear highlight when user starts editing the overlay text
                                try
                                {
                                    // Subscribe to text changes to clear the red highlight (if textbox present)
                                    if (tb != null)
                                    {
                                        var disp = tb.GetObservable(TextBox.TextProperty).Subscribe(new LambdaObserver<string?>(_ =>
                                        {
                                            try
                                            {
                                                overlay.Background = Brushes.Transparent;
                                                overlay.BorderBrush = Brushes.Gray;
                                                overlay.BorderThickness = new Thickness(1);
                                            }
                                            catch { }
                                        }));
                                        // If the overlay is closed, dispose the subscription - hook into IsVisible property
                                        overlay.GetObservable(Border.IsVisibleProperty).Subscribe(new LambdaObserver<bool>(visible =>
                                        {
                                            if (!visible)
                                            {
                                                try { disp.Dispose(); } catch { }
                                            }
                                        }));
                                    }
                                }
                                catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: subscribe clear highlight", ex); }
                            }
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: show overlay on format error", ex); }

                    // Stop processing further lines
                    break;
                }
            }
            IndexEditor.Shared.EditorState.Articles = articles;

            // Update the view-model's observable collection so UI bindings (SelectedArticle, Articles) refresh.
            var vm = this.DataContext as EditorStateViewModel;
            if (vm != null)
            {
                try
                {
                    vm.Articles.Clear();
                    foreach (var a in articles)
                    {
                        try
                        {
                            if (a.Measurements == null || a.Measurements.Count == 0)
                                a.Measurements = new System.Collections.Generic.List<string> { string.Empty };
                            try { a.Validate(); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: a.Validate", ex); }
                            try { a.NotifyPropertyChanged(nameof(a.Measurements)); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: NotifyPropertyChanged Measurements", ex); }
                            try { a.NotifyPropertyChanged(nameof(a.Measurements0)); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: NotifyPropertyChanged Measurements0", ex); }
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: per-article setup", ex); }
                        vm.Articles.Add(a);
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: update VM articles", ex); }
                if (articles.Count > 0)
                {
                    try { vm.SelectedArticle = articles[0]; } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: set SelectedArticle", ex); }
                }
            }
            // Also maintain the global EditorState for other components
            if (articles.Count > 0)
                IndexEditor.Shared.EditorState.ActiveArticle = articles[0];
            IndexEditor.Shared.EditorState.NotifyStateChanged();
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
                    foreach (var a in articles)
                    {
                        try
                        {
                            if (a.Measurements == null || a.Measurements.Count == 0)
                                a.Measurements = new System.Collections.Generic.List<string> { string.Empty };
                            try { a.Validate(); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: a.Validate", ex); }
                            try { a.NotifyPropertyChanged(nameof(a.Measurements)); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: notify Measurements", ex); }
                            try { a.NotifyPropertyChanged(nameof(a.Measurements0)); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: notify Measurements0", ex); }
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: per-article", ex); }
                        try { vm.Articles.Add(a); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.RefreshFromEditorState: vm.Articles.Add", ex); }
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
