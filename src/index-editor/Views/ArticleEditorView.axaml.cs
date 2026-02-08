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
            };

            // Clicks are handled by the OnSegmentLozengePressed method wired in XAML.
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
            catch { }
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
            catch { }
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
                        try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch { }
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
            catch { }
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
                var parsed = IndexEditor.Shared.IndexContentLineParser.Parse(line);
                if (parsed != null)
                {
                    // Convert IndexContentLine to Common.Shared.ContentLine
                    var contentLine = new Common.Shared.ArticleLine
                    {
                        Pages = parsed.Pages,
                        Category = parsed.Category,
                        Title = parsed.Title,
                        ModelNames = parsed.ModelNames,
                        Photographers = parsed.Photographers,
                        Authors = parsed.Authors
                    };
                    articles.Add(contentLine);
                }
            }
            IndexEditor.Shared.EditorState.Articles = articles;
            if (articles.Count > 0)
                IndexEditor.Shared.EditorState.ActiveArticle = articles[0];
            IndexEditor.Shared.EditorState.NotifyStateChanged();
        }
    }
}
