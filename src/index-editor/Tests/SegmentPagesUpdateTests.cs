using System;
using Xunit;
using Common.Shared;
using IndexEditor.Shared;
using System.Collections.Generic;
using System.Linq;

namespace IndexEditor.Tests
{
    public class SegmentPagesUpdateTests
    {
        [Fact]
        public void EndingSegment_AddsPagesToArticle()
        {
            var art = new ArticleLine { Title = "Test" };
            art.Pages = new List<int> { 1, 2 };
            var seg = new Segment(3);
            art.Segments.Add(seg);
            EditorState.ActiveArticle = art;
            EditorState.ActiveSegment = seg;
            EditorState.CurrentPage = 5;

            // End the segment logic (simulate End button handler)
            var start = EditorState.ActiveSegment.Start;
            var end = EditorState.CurrentPage;
            if (end < start) (start, end) = (end, start);
            var newPages = new List<int>(art.Pages ?? new List<int>());
            for (int p = start; p <= end; p++) if (!newPages.Contains(p)) newPages.Add(p);
            newPages.Sort();
            art.Pages = newPages;

            // Close segment
            EditorState.ActiveSegment.End = EditorState.CurrentPage;
            EditorState.ActiveSegment = null;

            Assert.Contains(3, art.Pages);
            Assert.Contains(4, art.Pages);
            Assert.Contains(5, art.Pages);
            Assert.True(art.Pages.SequenceEqual(new List<int>{1,2,3,4,5}));
        }
    }
}
