using System;
using Xunit;
using Common.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Tests
{
    public class EscapeCancelsActiveSegmentTests
    {
        public EscapeCancelsActiveSegmentTests() 
        { 
            TestDIHelper.ResetState();
        }
        public void Dispose() { }

        [Fact]
        public void Escape_RemovesNewActiveSegment()
        {
            // Reset shared EditorState
            IndexEditor.Shared.EditorState.Articles = new System.Collections.Generic.List<ArticleLine>();
            IndexEditor.Shared.EditorState.ActiveArticle = null;
            IndexEditor.Shared.EditorState.ActiveSegment = null;

            // Setup article and active new segment
            var a = new ArticleLine { Title = "T1", Category = "Feature" };
            a.Pages = new System.Collections.Generic.List<int> { 5 };
            IndexEditor.Shared.EditorState.Articles.Add(a);
            IndexEditor.Shared.EditorState.ActiveArticle = a;

            var seg = new Segment(7);
            seg.WasNew = true;
            a.Segments.Add(seg);
            IndexEditor.Shared.EditorState.ActiveSegment = seg;

            // Simulate Escape cancellation using shared helper
            IndexEditor.Shared.EditorActions.CancelActiveSegment();

            // Assert segment removed
            Assert.DoesNotContain(a.Segments, x => object.ReferenceEquals(x, seg));
            Assert.Null(IndexEditor.Shared.EditorState.ActiveSegment);
        }

        [Fact]
        public void Escape_RestoresOriginalEndOnReopen()
        {
            IndexEditor.Shared.EditorState.Articles = new System.Collections.Generic.List<ArticleLine>();
            IndexEditor.Shared.EditorState.ActiveArticle = null;
            IndexEditor.Shared.EditorState.ActiveSegment = null;

            var a = new ArticleLine { Title = "T1", Category = "Feature" };
            a.Pages = new System.Collections.Generic.List<int> { 5 };
            var closed = new Segment(10, 12);
            a.Segments.Add(closed);
            IndexEditor.Shared.EditorState.Articles.Add(a);
            IndexEditor.Shared.EditorState.ActiveArticle = a;

            // Reopen closed segment
            closed.OriginalEnd = closed.End;
            closed.End = null;
            closed.WasNew = false;
            IndexEditor.Shared.EditorState.ActiveSegment = closed;

            // Simulate Escape cancellation using shared helper
            IndexEditor.Shared.EditorActions.CancelActiveSegment();

            // Assert original end restored
            Assert.Equal(12, closed.End);
            Assert.Null(closed.OriginalEnd);
            Assert.Null(IndexEditor.Shared.EditorState.ActiveSegment);
        }

        [Fact]
        public void Escape_ClosesActiveSegmentToSinglePage_WhenNoOriginalEnd()
        {
            IndexEditor.Shared.EditorState.Articles = new System.Collections.Generic.List<ArticleLine>();
            IndexEditor.Shared.EditorState.ActiveArticle = null;
            IndexEditor.Shared.EditorState.ActiveSegment = null;

            var a = new ArticleLine { Title = "T1", Category = "Feature" };
            a.Pages = new System.Collections.Generic.List<int> { 5 };
            IndexEditor.Shared.EditorState.Articles.Add(a);
            IndexEditor.Shared.EditorState.ActiveArticle = a;

            var seg = new Segment(15);
            seg.WasNew = false;
            // Note: seg.OriginalEnd is null and seg.End is null (active)
            a.Segments.Add(seg);
            IndexEditor.Shared.EditorState.ActiveSegment = seg;

            // Simulate Escape cancellation using shared helper
            IndexEditor.Shared.EditorActions.CancelActiveSegment();

            // Assert closed to single page
            Assert.Equal(15, seg.End);
            Assert.Null(IndexEditor.Shared.EditorState.ActiveSegment);
        }
    }
}
