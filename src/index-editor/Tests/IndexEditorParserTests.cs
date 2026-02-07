using System;
using Xunit;
using IndexEditor.Shared;
using Common.Shared;
using System.Linq;

namespace IndexEditor.Tests
{
    public class IndexEditorParserTests
    {
        [Fact]
        public void ParseAndRoundTripAuthorsAndFields()
        {
            var line = "5|7,Humour,Funny,|,|,John Photographer,Author A|Author B,";
            var parsed = IndexFileParser.ParseArticleLine(line);
            Assert.NotNull(parsed);
            Assert.Equal("Humour", parsed.Category);
            Assert.Equal("Funny", parsed.Title);
            Assert.Contains(5, parsed.Pages);
            Assert.Contains(7, parsed.Pages);
            Assert.Contains("Author A", parsed.Authors);
        }
    }
}
