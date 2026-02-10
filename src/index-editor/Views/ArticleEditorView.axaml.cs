using Avalonia.Controls;
using IndexEditor.Shared;
using Common.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
                System.Console.WriteLine("[DEBUG] ArticleEditorView.FocusEditor: called");
                var tb = this.FindControl<TextBox>("TitleTextBox");
                System.Console.WriteLine(tb == null ? "[DEBUG] ArticleEditorView.FocusEditor: TitleTextBox not found" : "[DEBUG] ArticleEditorView.FocusEditor: TitleTextBox found");
                if (tb != null) { tb.Focus(); return; }
                // fallback to category if title absent
                var cb = this.FindControl<ComboBox>("CategoryComboBox");
                System.Console.WriteLine(cb == null ? "[DEBUG] ArticleEditorView.FocusEditor: CategoryComboBox not found" : "[DEBUG] ArticleEditorView.FocusEditor: CategoryComboBox found");
                if (cb != null) cb.Focus();
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.FocusEditor", ex); }
        }

        // Specifically focus the title textbox
        public void FocusTitle()
        {
            System.Console.WriteLine("[DEBUG] ArticleEditorView.FocusTitle: called");
            try
            {
                var tb = this.FindControl<TextBox>("TitleTextBox");
                System.Console.WriteLine(tb == null ? "[DEBUG] ArticleEditorView.FocusTitle: TitleTextBox not found" : "[DEBUG] ArticleEditorView.FocusTitle: TitleTextBox found");
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
            var indexPath = System.IO.Path.Combine(_currentFolder, "_index.txt");
            if (!System.IO.File.Exists(indexPath)) return;
            var lines = System.IO.File.ReadAllLines(indexPath);
            var articles = new List<Common.Shared.ArticleLine>();
            foreach (var line in lines)
            {
                var parsed = IndexEditor.Shared.IndexFileParser.ParseArticleLine(line);
                if (parsed != null)
                {
                    // ParseArticleLine returns a fully-populated Common.Shared.ArticleLine
                    articles.Add(parsed);
                }
            }
            IndexEditor.Shared.EditorState.Articles = articles;
            // Debug: append first article measurements to a temp log so tests can inspect it when console is unavailable
            try
            {
                if (articles.Count > 0)
                {
                    var m = string.Join(";", articles[0].Measurements ?? new List<string>());
                    var log = $"Loaded folder '{_currentFolder ?? "?"}' first article measurements: {m}\n";
                    try { System.IO.File.AppendAllText("/tmp/index_editor_measurements_debug.txt", log); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: append measurements debug", ex); }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: outer append", ex); }
            // Update the view-model's observable collection so UI bindings (SelectedArticle, Articles) refresh.
            var vm = this.DataContext as EditorStateViewModel;
            if (vm != null)
            {
                try
                {
                    vm.Articles.Clear();
                    foreach (var a in articles)
                    {
                        // Ensure Measurements list exists and normalize the first value via Validate
                        try
                        {
                            if (a.Measurements == null || a.Measurements.Count == 0)
                                a.Measurements = new System.Collections.Generic.List<string> { string.Empty };
                            // Trigger normalization/validation so Measurements0 becomes canonical
                            try { a.Validate(); } catch (Exception ex) { DebugLogger.LogException("ArticleEditorView.LoadArticlesFromIndexFile: a.Validate", ex); }
                            // Raise PropertyChanged for Measurements and Measurements0 so UI bindings see the values
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
}
