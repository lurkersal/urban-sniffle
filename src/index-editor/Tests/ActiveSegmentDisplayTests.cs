using System;
using Xunit;
using IndexEditor.Views;
using IndexEditor.Shared;
using Common.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Tests
{
    public class ActiveSegmentDisplayTests
    {
        public ActiveSegmentDisplayTests() 
        { 
            TestDIHelper.ResetState();
        }
        public void Dispose() { }

        [Fact]
        public void ActiveSegmentDisplay_UpdatesThroughLifecycle()
        {
            // Reset shared state
            EditorState.Articles = new System.Collections.Generic.List<ArticleLine>();
            EditorState.ActiveArticle = null;
            EditorState.ActiveSegment = null;
            EditorState.CurrentPage = 1;

            var art = new ArticleLine { Title = "T", Category = "Feature" };
            art.Pages = new System.Collections.Generic.List<int> { 1 };
            EditorState.Articles.Add(art);

            var vm = new EditorStateViewModel();
            // Ensure vm sees the article
            vm.Articles.Clear(); vm.Articles.Add(art);
            vm.SelectedArticle = art;

            // No active segment
            Assert.Equal("— none —", vm.ActiveSegmentDisplay);

            // Create active segment
            var seg = new Segment(3);
            seg.WasNew = true;
            seg.End = null;
            art.Segments.Add(seg);
            EditorState.ActiveSegment = seg;
            EditorState.ActiveArticle = art;
            EditorState.CurrentPage = 4;
            EditorState.NotifyStateChanged();

            // After activation, display should update
            Assert.Contains("3", vm.ActiveSegmentDisplay);
            Assert.Contains("→", vm.ActiveSegmentDisplay);

            // Change current page -> live preview should update
            EditorState.CurrentPage = 6;
            EditorState.NotifyStateChanged();
            Assert.Contains("6", vm.ActiveSegmentDisplay);

            // End segment
            seg.End = 8;
            EditorState.ActiveSegment = null;
            EditorState.NotifyStateChanged();
            Assert.Contains("8", vm.ActiveSegmentDisplay);
        }
    }
}

