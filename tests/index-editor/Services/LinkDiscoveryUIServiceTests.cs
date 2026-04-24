using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using IndexEditor.Services;
using IndexEditor.Shared;
using Common.Shared;
using Xunit;

namespace IndexEditor.Tests.Services
{
    public class LinkDiscoveryUIServiceTests : AvaloniaTestBase
    {
        private class TestWindow : Window
        {
            public Dictionary<string, object> ControlsMap = new();

            public T? FindControl<T>(string name) where T : class
            {
                if (ControlsMap.TryGetValue(name, out var control))
                {
                    return control as T;
                }
                return null;
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
        public void ClearDiscoveredLinks_RemovesAllLinks()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            // Add some links
            service.DiscoveredLinks[1] = new List<MagazineLink>
            {
                new MagazineLink { Page = 1, Magazine = "Test", Volume = "1", Issue = "1" }
            };
            service.DiscoveredLinks[2] = new List<MagazineLink>
            {
                new MagazineLink { Page = 2, Magazine = "Test", Volume = "1", Issue = "2" }
            };

            Assert.Equal(2, service.DiscoveredLinks.Count);

            // Act
            service.ClearDiscoveredLinks();

            // Assert
            Assert.Empty(service.DiscoveredLinks);
        }

        [Fact]
        public void LoadLinksFromIndex_AddsLinksToDiscoveredLinks()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            var linksToLoad = new List<MagazineLink>
            {
                new MagazineLink { Page = 1, Magazine = "Magazine A", Volume = "1", Issue = "1" },
                new MagazineLink { Page = 1, Magazine = "Magazine B", Volume = "2", Issue = "3" },
                new MagazineLink { Page = 2, Magazine = "Magazine C", Volume = "3", Issue = "5" }
            };

            // Act
            service.LoadLinksFromIndex(linksToLoad);

            // Assert
            Assert.Equal(2, service.DiscoveredLinks.Count); // 2 unique pages
            Assert.Equal(2, service.DiscoveredLinks[1].Count); // Page 1 has 2 links
            Assert.Single(service.DiscoveredLinks[2]); // Page 2 has 1 link
        }

        [Fact]
        public void LoadLinksFromIndex_DeduplicatesLinks()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            var linksToLoad = new List<MagazineLink>
            {
                new MagazineLink { Page = 1, Magazine = "Test", Volume = "1", Issue = "1" },
                new MagazineLink { Page = 1, Magazine = "Test", Volume = "1", Issue = "1" }, // Duplicate
                new MagazineLink { Page = 1, Magazine = "Test", Volume = "1", Issue = "2" }
            };

            // Act
            service.LoadLinksFromIndex(linksToLoad);

            // Assert
            Assert.Single(service.DiscoveredLinks); // 1 page
            Assert.Equal(2, service.DiscoveredLinks[1].Count); // Only 2 unique links (duplicate removed)
        }

        [Fact]
        public void LoadLinksFromIndex_WithNullList_DoesNotThrow()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            // Act & Assert
            var exception = Record.Exception(() => service.LoadLinksFromIndex(null));
            Assert.Null(exception);
        }

        [Fact]
        public void LoadLinksFromIndex_WithEmptyList_DoesNothing()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            // Act
            service.LoadLinksFromIndex(new List<MagazineLink>());

            // Assert
            Assert.Empty(service.DiscoveredLinks);
        }

        [Fact]
        public void OnLinkDiscovered_AddsNewLinkToDiscoveredLinks()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            var eventArgs = new LinkDiscoveredEventArgs
            {
                Page = 5,
                Magazine = "Test Magazine",
                Volume = "10",
                Issue = "5"
            };

            // Act
            service.OnLinkDiscovered(null, eventArgs);

            // Assert
            Assert.Single(service.DiscoveredLinks);
            Assert.Contains(5, service.DiscoveredLinks.Keys);
            Assert.Single(service.DiscoveredLinks[5]);

            var link = service.DiscoveredLinks[5][0];
            Assert.Equal(5, link.Page);
            Assert.Equal("Test Magazine", link.Magazine);
            Assert.Equal("10", link.Volume);
            Assert.Equal("5", link.Issue);
        }

        [Fact]
        public void OnLinkDiscovered_SkipsDuplicateLink()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            var eventArgs = new LinkDiscoveredEventArgs
            {
                Page = 5,
                Magazine = "Test",
                Volume = "1",
                Issue = "1"
            };

            // Act
            service.OnLinkDiscovered(null, eventArgs);
            service.OnLinkDiscovered(null, eventArgs); // Add duplicate

            // Assert
            Assert.Single(service.DiscoveredLinks);
            Assert.Single(service.DiscoveredLinks[5]); // Only one link (duplicate ignored)
        }

        [Fact]
        public void OnLinkDiscoveryCompleted_MarksIndexAsModified_WhenLinksFound()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState { HasUnsavedChanges = false };
            var service = new LinkDiscoveryUIService(window, editorState);

            // Add a discovered link
            service.DiscoveredLinks[1] = new List<MagazineLink>
            {
                new MagazineLink { Page = 1, Magazine = "Test", Volume = "1", Issue = "1" }
            };

            // Act
            service.OnLinkDiscoveryCompleted(null, EventArgs.Empty);

            // Wait for dispatcher to process (in real scenario)
            System.Threading.Thread.Sleep(100);

            // Assert
            Assert.True(editorState.HasUnsavedChanges);
        }

        [Fact]
        public void DiscoveredLinks_ReturnsCorrectDictionary()
        {
            // Arrange
            var window = new TestWindow();
            var editorState = new TestEditorState();
            var service = new LinkDiscoveryUIService(window, editorState);

            var link1 = new MagazineLink { Page = 1, Magazine = "A", Volume = "1", Issue = "1" };
            var link2 = new MagazineLink { Page = 2, Magazine = "B", Volume = "2", Issue = "2" };

            // Act
            service.DiscoveredLinks[1] = new List<MagazineLink> { link1 };
            service.DiscoveredLinks[2] = new List<MagazineLink> { link2 };

            // Assert
            Assert.Equal(2, service.DiscoveredLinks.Count);
            Assert.Contains(1, service.DiscoveredLinks.Keys);
            Assert.Contains(2, service.DiscoveredLinks.Keys);
        }
    }
}


