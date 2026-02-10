using System;
using Xunit;
using IndexEditor.Shared;
using Common.Shared;

namespace IndexEditor.Tests
{
    public class ContributorsParsingTests
    {
        [Fact]
        public void ParseCanonical7Field_PopulatesContributorsAndProxies()
        {
            // canonical 7-field: pages,category,title,modelNames,ages,contributors,measurements
            var line = "1-2,Feature,My Title,ModelA,23,John Doe,34-24-34";
            var parsed = IndexFileParser.ParseArticleLine(line);
            Assert.NotNull(parsed);
            Assert.Equal("Feature", parsed.Category);
            Assert.Equal("My Title", parsed.Title);
            Assert.Contains(1, parsed.Pages);
            Assert.Contains(2, parsed.Pages);
            // unified contributors list
            Assert.NotNull(parsed.Contributors);
            Assert.Contains("John Doe", parsed.Contributors);
            // proxies should reflect the contributor value
            Assert.Equal("John Doe", parsed.Contributor0);
            Assert.Equal("John Doe", parsed.Author0);
            Assert.Equal("John Doe", parsed.Photographer0);
        }

        [Fact]
        public void Legacy8Field_ThrowsFormatException()
        {
            // legacy 8-field line (photographers + authors) must be treated as error
            var legacy = "1-2|4,Feature,My Title,ModelA|ModelB,23|,John Doe|Jane Smith,Author One|Author Two,34-24-34";
            Assert.Throws<FormatException>(() => IndexFileParser.ParseArticleLine(legacy));
        }
    }
}

