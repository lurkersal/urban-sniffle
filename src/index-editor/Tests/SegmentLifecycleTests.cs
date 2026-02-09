using System;
using Xunit;
using IndexEditor.Views;
using Common.Shared;

namespace IndexEditor.Tests
{
    public class SegmentLifecycleTests : IDisposable
    {
        public SegmentLifecycleTests() { }
        public void Dispose() { }

        [Fact]
        public void StartSegment_PreventsSelection_ThenAllowsAfterEnd()
        {
            // Reset shared EditorState to avoid interference from other tests
            IndexEditor.Shared.EditorState.ActiveArticle = null;
            IndexEditor.Shared.EditorState.ActiveSegment = null;
            IndexEditor.Shared.EditorState.Articles = new System.Collections.Generic.List<ArticleLine>();

            // Setup two articles
            var a1 = new ArticleLine { Title = "A1" };
            a1.Pages = new System.Collections.Generic.List<int> { 1 };
            var a2 = new ArticleLine { Title = "A2" };
            a2.Pages = new System.Collections.Generic.List<int> { 10 };

            // Seed shared state BEFORE creating the ViewModel to avoid background sync races
            IndexEditor.Shared.EditorState.Articles = new System.Collections.Generic.List<ArticleLine> { a1, a2 };

            // Create VM and ensure it copies the shared articles
            var vm = new EditorStateViewModel();

            // Ensure VM collection mirrors shared state
            vm.Articles.Clear();
            vm.Articles.Add(a1);
            vm.Articles.Add(a2);

            // Select first article
            vm.SelectedArticle = a1;
            Assert.Same(a1, vm.SelectedArticle);

            // Start a segment on a1
            IndexEditor.Shared.EditorState.ActiveArticle = a1;
            var seg = new Common.Shared.Segment(1);
            a1.Segments.Add(seg);
            IndexEditor.Shared.EditorState.ActiveSegment = seg;

            // Attempt to select a2 via command
            vm.SelectArticleCommand.Execute(a2);
            // Selection should remain a1
            Assert.Same(a1, vm.SelectedArticle);

            // Attempt to set SelectedArticle programmatically
            vm.SelectedArticle = a2;
            Assert.Same(a1, vm.SelectedArticle);

            // End the segment
            IndexEditor.Shared.EditorState.ActiveSegment.End = 2;
            IndexEditor.Shared.EditorState.ActiveSegment = null;

            // Now selection should be allowed
            vm.SelectArticleCommand.Execute(a2);
            Assert.Same(a2, vm.SelectedArticle);
        }
    }
}
