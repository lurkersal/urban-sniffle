using System;
using Xunit;
using IndexEditor.Shared;
using Common.Shared;
using System.Linq;

namespace Common.Tests
{
    public class IndexFileParserTests
    {
        [Fact]
        public void ParseArticleLine_WithAuthors_RoundTrip()
        {
            // build a CSV-like line matching TopBar writer format: pages,category,title,modelNames,ages,photographers,authors,measurements
            // Use canonical 7-field format: pages,category,title,models,ages,contributors,measurements
            var line = "1-2|4,Feature,My Title,ModelA|ModelB,23|,John Doe|Jane Smith|Author One|Author Two,34-24-34";
            var parsed = IndexFileParser.ParseArticleLine(line);
            Assert.NotNull(parsed);
            Assert.Equal("Feature", parsed.Category);
            Assert.Equal("My Title", parsed.Title);
            Assert.Contains(1, parsed.Pages);
            Assert.Contains(2, parsed.Pages);
            Assert.Contains(4, parsed.Pages);
            Assert.Contains("John Doe", parsed.Contributors);
            Assert.Contains("Author One", parsed.Contributors);
        }

        [Fact]
        public void ParsePageNumbers_HandlesRangesAndSingles()
        {
            var pages = IndexFileParser.ParsePageNumbers("1|3-5|7", out bool err);
            Assert.False(err);
            Assert.Equal(new int[] {1,3,4,5,7}, pages.ToArray());
        }

        [Fact]
        public void ParsePageNumbers_Invalid_ReturnsError()
        {
            var pages = IndexFileParser.ParsePageNumbers("abc|2-", out bool err);
            Assert.True(err);
            Assert.Empty(pages);
        }
    }
}
