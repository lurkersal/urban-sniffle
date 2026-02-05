using Xunit;
using MagazineParser.Services;
using MagazineParser.Models;
using System.Collections.Generic;

namespace MagazineParser.Tests
{
    public class ContentLineParserTests
    {
        [Fact]
        public void ParseContentLine_ValidInput_ReturnsContentLine()
        {
            // Arrange
            var validCategories = new HashSet<string> { "Model", "Cover", "Index" };
            var parser = new ContentLineParser(validCategories);
            var input = "5, Model, Jane Doe";

            // Act
            var result = parser.ParseContentLine(input);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ContentLine>(result);
            Assert.Equal("Model", result.Category);
            Assert.Equal("Jane Doe", result.Title);
        }
    }
}
