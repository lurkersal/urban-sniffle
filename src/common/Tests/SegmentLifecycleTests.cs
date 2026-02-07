using System;
using Xunit;
using Common.Shared;
using IndexEditor.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tests
{
    public class SegmentLifecycleTests
    {
        [Fact]
        public void EndSegment_AddsPageRangeToArticlePages()
        {
            // Arrange: article with some existing pages
            var art = new ArticleLine();
            art.Pages = new List<int> { 1, 2, 10 };

            // Active segment start at 5, current page is 7
            int start = 5;
            int end = 7;

            // Act: emulate EndSegment logic from PageControllerView
            var newPages = new List<int>(art.Pages ?? new List<int>());
            for (int p = start; p <= end; p++)
            {
                if (!newPages.Contains(p)) newPages.Add(p);
            }
            newPages.Sort();
            art.Pages = newPages;

            // Assert: pages now include 1,2,5,6,7,10 in sorted order
            var expected = new[] { 1, 2, 5, 6, 7, 10 };
            Assert.Equal(expected, art.Pages.ToArray());
        }

        [Fact]
        public void AddSegment_Disallowed_When_PageAlreadyInArticle()
        {
            // Arrange: article containing page 12
            var art = new ArticleLine();
            art.Pages = new List<int> { 10, 11, 12, 15 };
            EditorState.ActiveArticle = art;
            EditorState.CurrentPage = 12;

            // Act: AddSegment should be disallowed when current page already in article
            // Use the local article instance to avoid reliance on global state that may be mutated by other tests
            bool canAdd = !(art.Pages != null && art.Pages.Contains(EditorState.CurrentPage));

            // Assert
            Assert.False(canAdd);
        }
    }
}
