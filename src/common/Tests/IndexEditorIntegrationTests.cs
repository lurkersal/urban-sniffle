using System;
using Xunit;
using IndexEditor.Shared;
using Common.Shared;
using System.IO;
using System.Collections.Generic;
using IndexEditor.Views;
using System.Linq;

namespace Common.Tests
{
    public class IndexEditorIntegrationTests
    {
        [Fact]
        public void TopBar_SaveAndOpen_RoundTrip()
        {
            // Arrange: create a temp folder
            var tmp = Path.Combine(Path.GetTempPath(), "index-editor-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                // Build sample articles in EditorState
                var a1 = new ArticleLine { Pages = new List<int> { 1, 2 }, Category = "Feature", Title = "A1" };
                var a2 = new ArticleLine { Pages = new List<int> { 5 }, Category = "Model", Title = "A2" };
                EditorState.Articles = new List<ArticleLine> { a1, a2 };
                EditorState.CurrentFolder = tmp;
                EditorState.CurrentMagazine = "MagTest";
                EditorState.CurrentVolume = "Vol1";
                EditorState.CurrentNumber = "No1";

                // Act: emulate TopBar save logic (we'll write using same format)
                var lines = new List<string>();
                foreach (var a in EditorState.Articles)
                {
                    var pagesText = a.PagesText;
                    string Escape(string s) => s?.Replace(",", "\\,") ?? string.Empty;
                    var modelNames = (a.ModelNames != null && a.ModelNames.Count > 0) ? string.Join('|', a.ModelNames) : string.Empty;
                    var ages = (a.Ages != null && a.Ages.Count > 0) ? string.Join('|', a.Ages.Select(v => v.HasValue ? v.Value.ToString() : string.Empty)) : string.Empty;
                    var photographers = (a.Photographers != null && a.Photographers.Count > 0) ? string.Join('|', a.Photographers) : string.Empty;
                    var authors = (a.Authors != null && a.Authors.Count > 0) ? string.Join('|', a.Authors) : string.Empty;
                    var measurements = (a.Measurements != null && a.Measurements.Count > 0) ? string.Join('|', a.Measurements) : string.Empty;
                    var parts = new List<string> { pagesText, Escape(a.Category), Escape(a.Title), Escape(modelNames), Escape(ages), Escape(photographers), Escape(authors), Escape(measurements) };
                    var line = string.Join(",", parts);
                    lines.Add(line);
                }
                var header = new List<string>();
                header.Add($"# Magazine: {EditorState.CurrentMagazine}");
                header.Add($"# Volume: {EditorState.CurrentVolume}");
                header.Add($"# Number: {EditorState.CurrentNumber}");
                var outLines = header.Concat(lines).ToArray();
                var indexPath = Path.Combine(tmp, "_index.txt");
                File.WriteAllLines(indexPath, outLines);

                // Clear EditorState and then emulate TopBar open
                EditorState.Articles = new List<ArticleLine>();
                // Read back
                var readLines = File.ReadAllLines(indexPath);
                var parsed = new List<ArticleLine>();
                foreach (var ln in readLines)
                {
                    if (string.IsNullOrWhiteSpace(ln) || ln.TrimStart().StartsWith("#")) continue;
                    var p = IndexFileParser.ParseArticleLine(ln);
                    if (p != null) parsed.Add(p);
                }

                // Assert: parsed articles match original by pages and title/category
                Assert.Equal(2, parsed.Count);
                Assert.Equal("A1", parsed[0].Title);
                Assert.Equal("Feature", parsed[0].Category);
                Assert.Contains(1, parsed[0].Pages);
                Assert.Equal("A2", parsed[1].Title);
                Assert.Equal("Model", parsed[1].Category);
                Assert.Contains(5, parsed[1].Pages);
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

        [Fact]
        public void Selection_Prevention_When_ActiveSegmentExists()
        {
            // Arrange: two articles
            var a1 = new ArticleLine { Pages = new List<int> { 1 }, Title = "T1" };
            var a2 = new ArticleLine { Pages = new List<int> { 5 }, Title = "T2" };
            EditorState.Articles = new List<ArticleLine> { a1, a2 };
            // Active segment on a1
            var seg = new Common.Shared.Segment(1);
            a1.Segments.Clear();
            a1.Segments.Add(seg);
            EditorState.ActiveArticle = a1;
            EditorState.ActiveSegment = seg;

            var vm = new EditorStateViewModel();
            // Initial selected article is a1
            vm.SelectedArticle = a1;
            // Act: try to set SelectedArticle to a different article
            vm.SelectedArticle = a2;
            // Assert: selection should not change away from a1
            Assert.True(object.ReferenceEquals(EditorState.ActiveArticle, a1));
            Assert.True(object.ReferenceEquals(vm.SelectedArticle, a1));
        }
    }
}
