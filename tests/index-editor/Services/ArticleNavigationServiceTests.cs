using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using IndexEditor.Services;
using IndexEditor.Shared;
using IndexEditor.Views;
using Xunit;
using Moq;
using Common.Shared;

namespace IndexEditor.Tests.Services
{
    public class ArticleNavigationServiceTests
    {
        private class TestWindow : Window
        {
            public EditorStateViewModel? TestViewModel { get; set; }

            public TestWindow()
            {
                DataContext = TestViewModel;
            }
        }

        private class TestEditorState : IEditorState
        {
            public string? CurrentFolder { get; set; } = "";
            public string? CurrentMagazine { get; set; } = "";
            public string? CurrentVolume { get; set; } = "";
            public string? CurrentNumber { get; set; } = "";
            public string? CurrentYear { get; set; } = "";
            public int CurrentPage { get; set; }
            public List<ArticleLine> Articles { get; set; } = new();
            public ArticleLine? ActiveArticle { get; set; }
            public Segment? ActiveSegment { get; set; }
            public bool HasUnsavedChanges { get; set; }
            public bool IsArticleEditorFocused { get; set; }
            public bool ShowImages { get; set; } = true;
            public int ArticleEditorFocusRequest { get; private set; }
            
            public event Action? StateChanged;

            public void NotifyStateChanged()
            {
                StateChanged?.Invoke();
            }

            public void RequestArticleEditorFocus()
            {
                ArticleEditorFocusRequest++;
            }
        }

        [Fact]
        public void NavigateToPreviousArticle_WithMultipleArticles_SelectsPreviousArticle()
        {
            // Arrange
            var editorState = new TestEditorState
            {
                CurrentFolder = "/test",
                CurrentPage = 10,
                IsArticleEditorFocused = false
            };

            var articles = new List<ArticleLine>
            {
                new ArticleLine { Title = "Article 1", Pages = new List<int> { 1, 2, 3 } },
                new ArticleLine { Title = "Article 2", Pages = new List<int> { 4, 5, 6 } },
                new ArticleLine { Title = "Article 3", Pages = new List<int> { 7, 8, 9 } }
            };

            var viewModel = new EditorStateViewModel();
            foreach (var article in articles)
            {
                viewModel.Articles.Add(article);
            }
            viewModel.SelectedArticle = articles[1]; // Start at Article 2

            var window = new TestWindow { TestViewModel = viewModel };
            window.DataContext = viewModel;

            var service = new ArticleNavigationService(window, editorState);

            // Act
            var result = service.NavigateToPreviousArticle();

            // Assert
            Assert.True(result);
            Assert.Equal(articles[0], viewModel.SelectedArticle);
        }

        [Fact]
        public void NavigateToPreviousArticle_AtFirstArticle_StaysAtFirst()
        {
            // Arrange
            var editorState = new TestEditorState
            {
                CurrentFolder = "/test",
                CurrentPage = 1,
                IsArticleEditorFocused = false
            };

            var articles = new List<ArticleLine>
            {
                new ArticleLine { Title = "Article 1", Pages = new List<int> { 1, 2, 3 } },
                new ArticleLine { Title = "Article 2", Pages = new List<int> { 4, 5, 6 } }
            };

            var viewModel = new EditorStateViewModel();
            foreach (var article in articles)
            {
                viewModel.Articles.Add(article);
            }
            viewModel.SelectedArticle = articles[0]; // Start at first article

            var window = new TestWindow { TestViewModel = viewModel };
            window.DataContext = viewModel;

            var service = new ArticleNavigationService(window, editorState);

            // Act
            var result = service.NavigateToPreviousArticle();

            // Assert
            Assert.True(result);
            Assert.Equal(articles[0], viewModel.SelectedArticle); // Still at first
        }

        [Fact]
        public void NavigateToNextArticle_WithMultipleArticles_SelectsNextArticle()
        {
            // Arrange
            var editorState = new TestEditorState
            {
                CurrentFolder = "/test",
                CurrentPage = 5,
                IsArticleEditorFocused = false
            };

            var articles = new List<ArticleLine>
            {
                new ArticleLine { Title = "Article 1", Pages = new List<int> { 1, 2, 3 } },
                new ArticleLine { Title = "Article 2", Pages = new List<int> { 4, 5, 6 } },
                new ArticleLine { Title = "Article 3", Pages = new List<int> { 7, 8, 9 } }
            };

            var viewModel = new EditorStateViewModel();
            foreach (var article in articles)
            {
                viewModel.Articles.Add(article);
            }
            viewModel.SelectedArticle = articles[1]; // Start at Article 2

            var window = new TestWindow { TestViewModel = viewModel };
            window.DataContext = viewModel;

            var service = new ArticleNavigationService(window, editorState);

            // Act
            var result = service.NavigateToNextArticle();

            // Assert
            Assert.True(result);
            Assert.Equal(articles[2], viewModel.SelectedArticle);
        }

        [Fact]
        public void NavigateToNextArticle_AtLastArticle_StaysAtLast()
        {
            // Arrange
            var editorState = new TestEditorState
            {
                CurrentFolder = "/test",
                CurrentPage = 6,
                IsArticleEditorFocused = false
            };

            var articles = new List<ArticleLine>
            {
                new ArticleLine { Title = "Article 1", Pages = new List<int> { 1, 2, 3 } },
                new ArticleLine { Title = "Article 2", Pages = new List<int> { 4, 5, 6 } }
            };

            var viewModel = new EditorStateViewModel();
            foreach (var article in articles)
            {
                viewModel.Articles.Add(article);
            }
            viewModel.SelectedArticle = articles[1]; // Start at last article

            var window = new TestWindow { TestViewModel = viewModel };
            window.DataContext = viewModel;

            var service = new ArticleNavigationService(window, editorState);

            // Act
            var result = service.NavigateToNextArticle();

            // Assert
            Assert.True(result);
            Assert.Equal(articles[1], viewModel.SelectedArticle); // Still at last
        }

        [Fact]
        public void NavigateToPreviousArticle_WhenEditorFocused_ReturnsFalse()
        {
            // Arrange
            var editorState = new TestEditorState
            {
                CurrentFolder = "/test",
                CurrentPage = 5,
                IsArticleEditorFocused = true // Editor has focus
            };

            var window = new TestWindow();
            var service = new ArticleNavigationService(window, editorState);

            // Act
            var result = service.NavigateToPreviousArticle();

            // Assert
            Assert.False(result); // Should not navigate when editor is focused
        }

        [Fact]
        public void NavigateToNextArticle_WhenEditorFocused_ReturnsFalse()
        {
            // Arrange
            var editorState = new TestEditorState
            {
                CurrentFolder = "/test",
                CurrentPage = 5,
                IsArticleEditorFocused = true // Editor has focus
            };

            var window = new TestWindow();
            var service = new ArticleNavigationService(window, editorState);

            // Act
            var result = service.NavigateToNextArticle();

            // Assert
            Assert.False(result); // Should not navigate when editor is focused
        }

        [Fact]
        public void NavigateToPreviousArticle_WithNoArticles_ReturnsTrue()
        {
            // Arrange
            var editorState = new TestEditorState
            {
                CurrentFolder = "/test",
                CurrentPage = 1,
                IsArticleEditorFocused = false,
                Articles = new List<ArticleLine>() // No articles
            };

            var viewModel = new EditorStateViewModel();
            var window = new TestWindow { TestViewModel = viewModel };
            window.DataContext = viewModel;

            var service = new ArticleNavigationService(window, editorState);

            // Act
            var result = service.NavigateToPreviousArticle();

            // Assert
            Assert.True(result); // Returns true even with no articles (graceful handling)
        }
    }
}


