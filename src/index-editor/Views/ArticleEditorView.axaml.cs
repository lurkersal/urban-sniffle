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
            DataContext = new EditorStateViewModel();
            var editBtn = this.FindControl<Button>("EditSegmentBtn");
            var overlay = this.FindControl<Border>("SegmentEditorOverlay");
            var addSegmentBtn = this.FindControl<Button>("AddSegmentBtn");
            var titleBox = this.FindControl<TextBox>("TitleTextBox");
            var categoryCombo = this.FindControl<ComboBox>("CategoryComboBox");

            // Load categories from DB
            _ = LoadCategoriesAsync(categoryCombo);

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

            if (addSegmentBtn != null)
            {
                addSegmentBtn.Click += (s, e) =>
                {
                    if (EditorState.ActiveArticle != null)
                    {
                        // No Segments property in ContentLine; optionally handle segment logic elsewhere
                        EditorState.NotifyStateChanged();
                    }
                };
            }

            // Listen for page changes to update UI (prototype only)
            EditorState.StateChanged += () =>
            {
                // Optionally update UI, e.g., refresh segment list, highlight active segment, etc.
                if (titleBox != null && EditorState.ActiveArticle != null)
                    titleBox.Text = EditorState.ActiveArticle.Title;
            };
        }

        private async Task LoadCategoriesAsync(ComboBox? combo)
        {
            if (combo == null) return;
            string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath)) return;
            var json = await File.ReadAllTextAsync(configPath);
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings)) return;
            if (!connStrings.TryGetProperty("MagazineDb", out var connStrElem)) return;
            string connStr = connStrElem.GetString() ?? "";
            var categories = await CategoryRepository.GetCategoriesAsync(connStr);
            combo.ItemsSource = categories;
            if (categories.Count > 0)
                combo.SelectedIndex = 0;
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
