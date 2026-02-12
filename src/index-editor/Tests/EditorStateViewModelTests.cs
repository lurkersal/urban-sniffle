using System;
using System.IO;
using Xunit;
using IndexEditor.Views;
using Common.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Tests
{
    public class EditorStateViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        public EditorStateViewModelTests()
        {
            TestDIHelper.ResetState();
            _tempDir = Path.Combine(Path.GetTempPath(), "indexeditor_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Fact]
        public void FindFirstPageWithImage_PicksPlainFilename()
        {
            var vm = new EditorStateViewModel();
            var article = new ArticleLine();
            article.Pages = new System.Collections.Generic.List<int> { 1, 2 };
            File.WriteAllText(Path.Combine(_tempDir, "1.jpg"), "x");

            var found = vm.FindFirstPageWithImage(article, _tempDir);
            Assert.Equal(1, found);
        }

        [Fact]
        public void FindFirstPageWithImage_PicksPaddedFilename()
        {
            var vm = new EditorStateViewModel();
            var article = new ArticleLine();
            article.Pages = new System.Collections.Generic.List<int> { 1, 2 };
            File.WriteAllText(Path.Combine(_tempDir, "02.png"), "x");

            var found = vm.FindFirstPageWithImage(article, _tempDir);
            Assert.Equal(2, found);
        }

        [Fact]
        public void NavigateToArticle_FallsBackToMinPageWhenNoImage()
        {
            var vm = new EditorStateViewModel();
            var article = new ArticleLine();
            article.Pages = new System.Collections.Generic.List<int> { 10, 20 };
            IndexEditor.Shared.EditorState.CurrentFolder = _tempDir;

            vm.NavigateToArticle(article);
            Assert.Equal(10, IndexEditor.Shared.EditorState.CurrentPage);
        }
    }
}
