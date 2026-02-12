using System;
using Xunit;
using IndexEditor.Views;
using System.IO;
using System.Linq;

namespace IndexEditor.Tests
{
    public class ArticleEditorMeasurementsIntegrationTests : IDisposable
    {
        private readonly string _tempDir;
        public ArticleEditorMeasurementsIntegrationTests()
        {
            TestDIHelper.ResetState();
            _tempDir = Path.Combine(Path.GetTempPath(), "idxedit_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Fact]
        public void ArticleEditor_LoadsMeasurements_FromIndexFile()
        {
            var indexPath = Path.Combine(_tempDir, "_index.txt");
            // 7-column format where last column is measurements
            var line = "80-85,Model,Louise,Louise Cohen,23,John Allum,35C-23-36";
            File.WriteAllText(indexPath, line + Environment.NewLine);

            var view = new ArticleEditorView();
            var vm = new EditorStateViewModel();
            view.DataContext = vm;
            // call SetCurrentFolder which loads _index.txt
            view.SetCurrentFolder(_tempDir);

            Assert.NotNull(vm.SelectedArticle);
            Assert.Equal("35C-23-36", vm.SelectedArticle.Measurements0);
        }
    }
}

