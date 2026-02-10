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
            // Legacy 8-field lines are no longer supported and should raise a FormatException
            var line = "5|7,Humour,Funny,|,|,John Photographer,Author A|Author B,";
            Assert.Throws<FormatException>(() => IndexFileParser.ParseArticleLine(line));
        }
    }
}
