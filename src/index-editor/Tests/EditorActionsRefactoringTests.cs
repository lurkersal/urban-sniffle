using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using IndexEditor.Shared;
using Common.Shared;

namespace IndexEditor.Tests
{
    /// <summary>
    /// Unit tests for refactored EditorActions.cs
    /// Tests verify code smell fixes:
    /// 1. Separation of Concerns (Event-driven architecture)
    /// 2. Extracted helper methods are working correctly
    /// 3. Event subscriptions work as expected
    /// </summary>
    public class EditorActionsRefactoringTests : IDisposable
    {
        public EditorActionsRefactoringTests()
        {
            // Reset EditorState before each test
            EditorState.Articles = new List<ArticleLine>();
            EditorState.ActiveArticle = null;
            EditorState.ActiveSegment = null;
            EditorState.CurrentPage = 1;
        }

        public void Dispose()
        {
            // Clean up EditorState after each test
            EditorState.Articles = new List<ArticleLine>();
            EditorState.ActiveArticle = null;
            EditorState.ActiveSegment = null;
        }

        #region Fix 1: Separation of Concerns - Event-Driven Architecture

        [Fact]
        public void CreateNewArticle_Should_RaiseArticleCreatedEvent()
        {
            // Arrange
            bool eventRaised = false;
            ArticleLine? createdArticle = null;

            EditorActions.ArticleCreated += (article) =>
            {
                eventRaised = true;
                createdArticle = article;
            };

            try
            {
                // Act
                EditorActions.CreateNewArticle();

                // Assert
                Assert.True(eventRaised, "ArticleCreated event should have been raised");
                Assert.NotNull(createdArticle);
            }
            finally
            {
                // Cleanup: Unsubscribe
                EditorActions.ArticleCreated -= (article) => { };
            }
        }

        [Fact]
        public void CreateNewArticle_Should_CreateArticleWithCurrentPage()
        {
            // Arrange
            EditorState.CurrentPage = 42;
            ArticleLine? createdArticle = null;

            EditorActions.ArticleCreated += (article) => createdArticle = article;

            try
            {
                // Act
                EditorActions.CreateNewArticle();

                // Assert
                Assert.NotNull(createdArticle);
                Assert.NotNull(createdArticle.Pages);
                Assert.Single(createdArticle.Pages);
                Assert.Equal(42, createdArticle.Pages[0]);
            }
            finally
            {
                EditorActions.ArticleCreated -= (article) => { };
            }
        }

        [Fact]
        public void CreateNewArticle_Should_SetAsActiveArticle()
        {
            // Arrange
            ArticleLine? createdArticle = null;
            EditorActions.ArticleCreated += (article) => createdArticle = article;

            try
            {
                // Act
                EditorActions.CreateNewArticle();

                // Assert
                Assert.NotNull(EditorState.ActiveArticle);
                Assert.Same(createdArticle, EditorState.ActiveArticle);
            }
            finally
            {
                EditorActions.ArticleCreated -= (article) => { };
            }
        }

        [Fact]
        public void CreateNewArticle_Should_AddClosedSegment()
        {
            // Arrange
            EditorState.CurrentPage = 10;
            ArticleLine? createdArticle = null;
            EditorActions.ArticleCreated += (article) => createdArticle = article;

            try
            {
                // Act
                EditorActions.CreateNewArticle();

                // Assert
                Assert.NotNull(createdArticle);
                Assert.NotNull(createdArticle.Segments);
                Assert.Single(createdArticle.Segments);
                
                var segment = createdArticle.Segments[0];
                Assert.Equal(10, segment.Start);
                Assert.Equal(10, segment.End);
                Assert.False(segment.WasNew);
            }
            finally
            {
                EditorActions.ArticleCreated -= (article) => { };
            }
        }

        [Fact]
        public void CreateNewArticle_Should_NotCreate_WhenActiveSegmentExists()
        {
            // Arrange
            EditorState.ActiveSegment = new Segment(1) { End = null }; // Active segment
            bool eventRaised = false;
            EditorActions.ArticleCreated += (article) => eventRaised = true;

            try
            {
                // Act
                EditorActions.CreateNewArticle();

                // Assert
                Assert.False(eventRaised, "ArticleCreated should not be raised when active segment exists");
            }
            finally
            {
                EditorActions.ArticleCreated -= (article) => { };
            }
        }

        [Fact]
        public void CreateNewArticle_Should_InsertInCorrectPosition()
        {
            // Arrange
            EditorState.Articles = new List<ArticleLine>
            {
                new ArticleLine { Pages = new List<int> { 1, 2 } },
                new ArticleLine { Pages = new List<int> { 5, 6 } },
                new ArticleLine { Pages = new List<int> { 10, 11 } }
            };
            EditorState.CurrentPage = 7;

            // Act
            EditorActions.CreateNewArticle();

            // Assert
            Assert.Equal(4, EditorState.Articles.Count);
            Assert.Equal(7, EditorState.Articles[2].Pages[0]); // Should be inserted at index 2
        }

        #endregion

        #region Fix 3: Long Method Refactoring - AddSegmentAtCurrentPage

        [Fact]
        public void AddSegmentAtCurrentPage_Should_ReturnFalse_WhenNoActiveArticle()
        {
            // Arrange
            EditorState.ActiveArticle = null;

            // Act
            var result = EditorActions.AddSegmentAtCurrentPage();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddSegmentAtCurrentPage_Should_ReturnFalse_WhenActiveSegmentExists()
        {
            // Arrange
            var article = new ArticleLine { Pages = new List<int> { 1 } };
            EditorState.ActiveArticle = article;
            EditorState.ActiveSegment = new Segment(1) { End = null }; // Active segment

            // Act
            var result = EditorActions.AddSegmentAtCurrentPage();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddSegmentAtCurrentPage_Should_CreateNewSegment_WhenPageNotInArticle()
        {
            // Arrange
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 1 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment>
                {
                    new Segment(1) { End = 1 }
                }
            };
            EditorState.ActiveArticle = article;
            EditorState.CurrentPage = 5; // New page

            // Act
            var result = EditorActions.AddSegmentAtCurrentPage();

            // Assert
            Assert.True(result);
            Assert.Equal(2, article.Segments.Count);
            Assert.NotNull(EditorState.ActiveSegment);
            Assert.Equal(5, EditorState.ActiveSegment.Start);
            Assert.Null(EditorState.ActiveSegment.End); // Active segment has no End
            Assert.True(EditorState.ActiveSegment.WasNew);
        }

        [Fact]
        public void AddSegmentAtCurrentPage_Should_ActivateExistingSegment_WhenPageInArticle()
        {
            // Arrange
            var existingSegment = new Segment(5) { End = 7 }; // Closed segment
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 5, 6, 7 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment> { existingSegment }
            };
            EditorState.ActiveArticle = article;
            EditorState.CurrentPage = 6; // Page within existing segment

            // Act
            var result = EditorActions.AddSegmentAtCurrentPage();

            // Assert
            Assert.True(result);
            Assert.Same(existingSegment, EditorState.ActiveSegment);
            Assert.Null(existingSegment.End); // Now active (End is null)
            Assert.Equal(7, existingSegment.OriginalEnd); // Original end saved
        }

        [Fact]
        public void AddSegmentAtCurrentPage_Should_AddPageToArticle()
        {
            // Arrange
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 1, 2, 3 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment>()
            };
            EditorState.ActiveArticle = article;
            EditorState.CurrentPage = 5;

            // Act
            EditorActions.AddSegmentAtCurrentPage();

            // Assert
            Assert.Contains(5, article.Pages);
            Assert.Equal(new List<int> { 1, 2, 3, 5 }, article.Pages); // Sorted
        }

        #endregion

        #region CancelActiveSegment Tests

        [Fact]
        public void CancelActiveSegment_Should_RemoveNewSegment()
        {
            // Arrange
            var segment = new Segment(5) { End = null, WasNew = true };
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 5 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment> { segment }
            };
            EditorState.ActiveArticle = article;
            EditorState.ActiveSegment = segment;

            // Act
            EditorActions.CancelActiveSegment();

            // Assert
            Assert.Empty(article.Segments);
            Assert.Null(EditorState.ActiveSegment);
        }

        [Fact]
        public void CancelActiveSegment_Should_RestoreOriginalEnd()
        {
            // Arrange
            var segment = new Segment(5) { End = null, OriginalEnd = 7, WasNew = false };
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 5, 6, 7 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment> { segment }
            };
            EditorState.ActiveArticle = article;
            EditorState.ActiveSegment = segment;

            // Act
            EditorActions.CancelActiveSegment();

            // Assert
            Assert.Single(article.Segments);
            Assert.Equal(7, segment.End); // Restored
            Assert.Null(segment.OriginalEnd); // Cleared
            Assert.Null(EditorState.ActiveSegment);
        }

        [Fact]
        public void CancelActiveSegment_Should_CloseSegment_WhenNoOriginalEnd()
        {
            // Arrange
            var segment = new Segment(5) { End = null, WasNew = false };
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 5 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment> { segment }
            };
            EditorState.ActiveArticle = article;
            EditorState.ActiveSegment = segment;

            // Act
            EditorActions.CancelActiveSegment();

            // Assert
            Assert.Equal(5, segment.End); // Set to Start (closed)
            Assert.Null(EditorState.ActiveSegment);
        }

        [Fact]
        public void CancelActiveSegment_Should_DoNothing_WhenNoActiveSegment()
        {
            // Arrange
            EditorState.ActiveSegment = null;

            // Act
            EditorActions.CancelActiveSegment();

            // Assert - Should not throw
            Assert.Null(EditorState.ActiveSegment);
        }

        #endregion

        #region EndActiveSegment Tests

        [Fact]
        public void EndActiveSegment_Should_SetEndToCurrentPage()
        {
            // Arrange
            var segment = new Segment(5) { End = null };
            EditorState.ActiveSegment = segment;
            EditorState.CurrentPage = 10;

            // Act
            EditorActions.EndActiveSegment();

            // Assert
            Assert.Equal(10, segment.End);
            Assert.Null(EditorState.ActiveSegment);
        }

        [Fact]
        public void EndActiveSegment_Should_AddAllPagesInRange()
        {
            // Arrange
            var segment = new Segment(5) { End = null };
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 5 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment> { segment }
            };
            EditorState.ActiveArticle = article;
            EditorState.ActiveSegment = segment;
            EditorState.CurrentPage = 8;

            // Act
            EditorActions.EndActiveSegment();

            // Assert
            Assert.Equal(new List<int> { 5, 6, 7, 8 }, article.Pages);
        }

        [Fact]
        public void EndActiveSegment_Should_HandleReverseRange()
        {
            // Arrange
            var segment = new Segment(10) { End = null };
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 10 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment> { segment }
            };
            EditorState.ActiveArticle = article;
            EditorState.ActiveSegment = segment;
            EditorState.CurrentPage = 7; // Earlier page

            // Act
            EditorActions.EndActiveSegment();

            // Assert
            Assert.Equal(new List<int> { 7, 8, 9, 10 }, article.Pages);
        }

        [Fact]
        public void EndActiveSegment_Should_ClearCurrentPreviewEnd()
        {
            // Arrange
            var segment = new Segment(5) { End = null, CurrentPreviewEnd = 10 };
            EditorState.ActiveSegment = segment;
            EditorState.CurrentPage = 10;

            // Act
            EditorActions.EndActiveSegment();

            // Assert
            Assert.Null(segment.CurrentPreviewEnd);
        }

        #endregion

        #region FocusArticleTitle Tests

        [Fact]
        public void FocusArticleTitle_Should_NotThrow()
        {
            // Act & Assert - Should not throw
            EditorActions.FocusArticleTitle();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void CompleteWorkflow_CreateArticle_AddSegment_EndSegment()
        {
            // Arrange
            EditorState.CurrentPage = 10;
            ArticleLine? createdArticle = null;
            EditorActions.ArticleCreated += (article) => createdArticle = article;

            try
            {
                // Act 1: Create article
                EditorActions.CreateNewArticle();
                Assert.NotNull(createdArticle);

                // Act 2: Add a new segment
                EditorState.CurrentPage = 15;
                var addResult = EditorActions.AddSegmentAtCurrentPage();
                Assert.True(addResult);

                // Act 3: End the segment
                EditorState.CurrentPage = 20;
                EditorActions.EndActiveSegment();

                // Assert final state
                Assert.Equal(2, createdArticle.Segments.Count);
                Assert.Equal(new List<int> { 10, 15, 16, 17, 18, 19, 20 }, createdArticle.Pages);
                Assert.Null(EditorState.ActiveSegment);
            }
            finally
            {
                EditorActions.ArticleCreated -= (article) => { };
            }
        }

        [Fact]
        public void CompleteWorkflow_CreateArticle_AddSegment_Cancel()
        {
            // Arrange
            EditorState.CurrentPage = 10;
            ArticleLine? createdArticle = null;
            EditorActions.ArticleCreated += (article) => createdArticle = article;

            try
            {
                // Act 1: Create article
                EditorActions.CreateNewArticle();
                Assert.NotNull(createdArticle);

                // Act 2: Add a new segment
                EditorState.CurrentPage = 15;
                EditorActions.AddSegmentAtCurrentPage();

                // Act 3: Cancel the segment
                EditorActions.CancelActiveSegment();

                // Assert final state
                Assert.Single(createdArticle.Segments); // Only original segment
                Assert.Equal(new List<int> { 10, 15 }, createdArticle.Pages); // Page 15 stays (correct behavior)
                Assert.Null(EditorState.ActiveSegment);
            }
            finally
            {
                EditorActions.ArticleCreated -= (article) => { };
            }
        }

        [Fact]
        public void MultipleEventSubscribers_Should_AllReceiveEvent()
        {
            // Arrange
            int callCount = 0;
            ArticleLine? article1 = null;
            ArticleLine? article2 = null;
            ArticleLine? article3 = null;

            void handler1(ArticleLine a) { callCount++; article1 = a; }
            void handler2(ArticleLine a) { callCount++; article2 = a; }
            void handler3(ArticleLine a) { callCount++; article3 = a; }

            EditorActions.ArticleCreated += handler1;
            EditorActions.ArticleCreated += handler2;
            EditorActions.ArticleCreated += handler3;

            try
            {
                // Act
                EditorActions.CreateNewArticle();

                // Assert
                Assert.Equal(3, callCount);
                Assert.NotNull(article1);
                Assert.Same(article1, article2);
                Assert.Same(article2, article3);
            }
            finally
            {
                EditorActions.ArticleCreated -= handler1;
                EditorActions.ArticleCreated -= handler2;
                EditorActions.ArticleCreated -= handler3;
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void CreateNewArticle_Should_InitializeEmptyArticleList()
        {
            // Arrange
            EditorState.Articles = null!;

            // Act
            EditorActions.CreateNewArticle();

            // Assert
            Assert.NotNull(EditorState.Articles);
            Assert.Single(EditorState.Articles);
        }

        [Fact]
        public void AddSegmentAtCurrentPage_Should_HandleNullPages()
        {
            // Arrange
            var article = new ArticleLine 
            { 
                Pages = null,
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment>()
            };
            EditorState.ActiveArticle = article;
            EditorState.CurrentPage = 5;

            // Act
            var result = EditorActions.AddSegmentAtCurrentPage();

            // Assert
            Assert.True(result);
            Assert.NotNull(article.Pages);
            Assert.Single(article.Pages);
            Assert.Equal(5, article.Pages[0]);
        }

        [Fact]
        public void EndActiveSegment_Should_HandleNullArticle()
        {
            // Arrange
            var segment = new Segment(5) { End = null };
            EditorState.ActiveSegment = segment;
            EditorState.ActiveArticle = null;
            EditorState.CurrentPage = 10;

            // Act
            EditorActions.EndActiveSegment();

            // Assert - Should not throw
            Assert.Equal(10, segment.End);
            Assert.Null(EditorState.ActiveSegment);
        }

        [Fact]
        public void AddSegmentAtCurrentPage_Should_NotDuplicatePages()
        {
            // Arrange
            var article = new ArticleLine 
            { 
                Pages = new List<int> { 1, 2, 3, 5 },
                Segments = new System.Collections.ObjectModel.ObservableCollection<Segment>()
            };
            EditorState.ActiveArticle = article;
            EditorState.CurrentPage = 5; // Page already exists

            // Store original count
            int originalPageCount = article.Pages.Count;

            // Act
            EditorActions.AddSegmentAtCurrentPage();

            // Assert
            Assert.Equal(originalPageCount, article.Pages.Count); // No duplicate
            Assert.Equal(1, article.Pages.Count(p => p == 5)); // Only one instance of page 5
        }

        #endregion
    }
}

