using System;
using Xunit;
using IndexEditor.Shared;
using Common.Shared;
using System.IO;
using System.Collections.Generic;
using IndexEditor.Views;
using System.Linq;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace Common.Tests
{
    public class IndexEditorIntegrationTests
    {
        public IndexEditorIntegrationTests()
        {
            TestDIHelper.ResetState();
        }
        
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
                    var contributors = (a.Contributors != null && a.Contributors.Count > 0) ? string.Join('|', a.Contributors) : string.Empty;
                    var measurements = (a.Measurements != null && a.Measurements.Count > 0) ? string.Join('|', a.Measurements) : string.Empty;
                    // Use canonical 7-field format: pages,category,title,modelNames,ages,contributors,measurements
                    var parts = new List<string> { pagesText, Escape(a.Category), Escape(a.Title), Escape(modelNames), Escape(ages), Escape(contributors), Escape(measurements) };
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
            // This test verifies that the EditorState properly tracks active segments
            // The actual UI behavior for preventing selection is handled by the ViewModel
            
            // Arrange: article with active segment
            var a1 = new ArticleLine { Pages = new List<int> { 1 }, Title = "T1" };
            var seg = new Common.Shared.Segment(1);
            seg.End = null; // Active segment (no End)
            a1.Segments.Clear();
            a1.Segments.Add(seg);
            
            EditorState.ActiveArticle = a1;
            EditorState.ActiveSegment = seg;
            
            // Assert: EditorState correctly tracks the active segment
            Assert.NotNull(EditorState.ActiveSegment);
            Assert.True(EditorState.ActiveSegment.IsActive);
            Assert.Equal(a1, EditorState.ActiveArticle);
        }
    }
}
