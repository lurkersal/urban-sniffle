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
                // Update active segment end live
                if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                    EditorState.ActiveSegment.End = EditorState.CurrentPage;
                EditorState.NotifyStateChanged();

                // If this page is the first page of an article, select that article and open it in the editor
                try
                {
                    var vm = this.DataContext as EditorStateViewModel;
                    if (vm != null)
                    {
                        var match = EditorState.Articles.FirstOrDefault(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() == EditorState.CurrentPage);
                        if (match != null)
                        {
                            // Use the command so selection behavior is consistent
                            if (vm.SelectArticleCommand.CanExecute(match))
                                vm.SelectArticleCommand.Execute(match);
                        }
                    }
                }
                catch { /* swallow any exception to avoid breaking page changes */ }
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
            var pageInput = this.FindControl<TextBox>("PageInput");
            var activeArticleTitle = this.FindControl<TextBlock>("ActiveArticleTitle");
            var activeSegmentText = this.FindControl<TextBlock>("ActiveSegmentText");
            var statusText = this.FindControl<TextBlock>("StatusText");

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
                if (addSegmentBtn != null) addSegmentBtn.IsEnabled = EditorState.ActiveArticle != null;
                if (endSegmentBtn != null) endSegmentBtn.IsEnabled = EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive;

                // Active article display
                if (activeArticleTitle != null)
                    activeArticleTitle.Text = EditorState.ActiveArticle != null && !string.IsNullOrWhiteSpace(EditorState.ActiveArticle.Title) ? EditorState.ActiveArticle.Title : "— none —";

                // Active segment display
                if (activeSegmentText != null)
                {
                    if (EditorState.ActiveSegment != null)
                    {
                        var seg = EditorState.ActiveSegment;
                        var segText = seg.End.HasValue ? $"{seg.Start} – {seg.End.Value}" : $"{seg.Start} – (extending)";
                        activeSegmentText.Text = segText;
                    }
                    else
                    {
                        activeSegmentText.Text = "— none —";
                    }
                }

                // Status text: simple messages
                if (statusText != null)
                {
                    if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                    {
                        statusText.Text = $"● Segment extending (page {EditorState.CurrentPage} → {EditorState.ActiveSegment.End?.ToString() ?? "…"})";
                    }
                    else
                    {
                        statusText.Text = string.Empty;
                    }
                }
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
                    // End any currently active segment
                    if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                        EditorState.ActiveSegment.End = EditorState.CurrentPage;

                    // Create article and add the current page as its first page
                    var article = new Common.Shared.ArticleLine { Title = "", Category = "Letters" };
                    article.Pages = new List<int> { EditorState.CurrentPage };
                    EditorState.Articles.Add(article);
                    EditorState.ActiveArticle = article;

                    // Create and attach a new active segment starting at the current page
                    var seg = new Common.Shared.Segment(EditorState.CurrentPage);
                    article.Segments.Add(seg);
                    EditorState.ActiveSegment = seg;

                    EditorState.NotifyStateChanged();
                };

            if (addSegmentBtn != null)
                addSegmentBtn.Click += (s, e) =>
                {
                    if (EditorState.ActiveArticle != null)
                    {
                        // Append the current page to the article's Pages list if not present
                        if (EditorState.ActiveArticle.Pages == null) EditorState.ActiveArticle.Pages = new List<int>();
                        if (!EditorState.ActiveArticle.Pages.Contains(EditorState.CurrentPage))
                        {
                            EditorState.ActiveArticle.Pages.Add(EditorState.CurrentPage);
                            EditorState.ActiveArticle.Pages.Sort();
                        }
                        // Create a short-lived segment representing this single page (optional)
                        var seg = new Common.Shared.Segment(EditorState.CurrentPage, EditorState.CurrentPage);
                        EditorState.ActiveArticle.Segments.Add(seg);
                        // Do not mark as ActiveSegment since it's closed; if you want it active, set ActiveSegment = seg;
                        EditorState.NotifyStateChanged();
                    }
                };

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
                            if (art.Pages == null) art.Pages = new List<int>();
                            for (int p = start; p <= end; p++)
                            {
                                if (!art.Pages.Contains(p)) art.Pages.Add(p);
                            }
                            art.Pages.Sort();
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
