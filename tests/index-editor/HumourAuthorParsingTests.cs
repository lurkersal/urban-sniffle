using System;
using Xunit;
using IndexEditor.Shared;

namespace IndexEditor.Tests
{
    public class HumourAuthorParsingTests
    {
        [Fact]
        public void HumourLine_PopulatesAuthor0_FromPhotographerIfAuthorsMissing()
        {
            // Arrange: a line where the photographers column contains the author for humour
            var line = "52|55|57, Humour, Horray for Henrietta,,, Nigel Buxton";

            // Act
            var parsed = IndexFileParser.ParseArticleLine(line);

            // Assert
            Assert.NotNull(parsed);
            Assert.Equal("Humour", parsed.Category);
            Assert.Equal("Horray for Henrietta", parsed.Title);
            Assert.NotNull(parsed.Contributors);
            Assert.True(parsed.Contributors.Count >= 1, "Contributors list should contain at least one entry");
            Assert.Equal("Nigel Buxton", parsed.Contributor0);
        }
    }
}
