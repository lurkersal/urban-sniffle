using Avalonia.Controls;
using IndexEditor.Shared;
using System;

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
                var pageText = this.FindControl<TextBlock>("PageText");
                if (pageText != null)
                    pageText.Text = $"Page {EditorState.CurrentPage}";
                // Update active segment end live
                if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                    EditorState.ActiveSegment.End = EditorState.CurrentPage;
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

            if (prevBtn != null)
                prevBtn.Click += (s, e) => { if (Page > 1) Page--; };
            if (nextBtn != null)
                nextBtn.Click += (s, e) => { Page++; };

            if (newArticleBtn != null)
                newArticleBtn.Click += (s, e) =>
                {
                    // End active segment
                    if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                        EditorState.ActiveSegment.End = EditorState.CurrentPage;
                    // Create article
                    var article = new Common.Shared.ArticleLine { Title = "", Category = "Letters" };
                    EditorState.Articles.Add(article);
                    EditorState.ActiveArticle = article;
                    // No Segments property in ContentLine; optionally handle segment logic elsewhere
                    EditorState.NotifyStateChanged();
                };

            if (addSegmentBtn != null)
                addSegmentBtn.Click += (s, e) =>
                {
                    if (EditorState.ActiveArticle != null)
                    {
                        // No Segments property in ContentLine; optionally handle segment logic elsewhere
                        EditorState.NotifyStateChanged();
                    }
                };

            if (endSegmentBtn != null)
                endSegmentBtn.Click += (s, e) =>
                {
                    if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                    {
                        EditorState.ActiveSegment.End = EditorState.CurrentPage;
                        EditorState.ActiveSegment = null;
                        EditorState.NotifyStateChanged();
                    }
                };
        }
    }
}
