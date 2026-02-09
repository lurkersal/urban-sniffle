using System;
using Xunit;
using IndexEditor.Shared;

namespace IndexEditor.Tests
{
    public class FolderMetadataParserTests
    {
        [Theory]
        // Valid strict formats
        [InlineData("Mayfair 17-03, 1982", "Mayfair", "17", "03")]
        [InlineData("Magazine 10-03, 1950", "Magazine", "10", "03")]
        
        // Invalid formats (should return original folder name and placeholders)
        [InlineData("Mayfair 17-3, 1982", "Mayfair 17-3, 1982", "—", "—")]
        [InlineData("Mayfair Volume 17-03", "Mayfair Volume 17-03", "—", "—")]
        [InlineData("Mayfair Vol17No3", "Mayfair Vol17No3", "—", "—")]
        [InlineData("MagazineName 10-3", "MagazineName 10-3", "—", "—")]
        [InlineData("Magazine-Name 10-3", "Magazine-Name 10-3", "—", "—")]
        [InlineData("MagazineName_V10_N3", "MagazineName_V10_N3", "—", "—")]
        [InlineData("MagazineName v10 n3", "MagazineName v10 n3", "—", "—")]
        [InlineData("MagazineName 1950", "MagazineName 1950", "—", "—")]
        [InlineData("Magazine 17 03", "Magazine 17 03", "—", "—")]
        [InlineData("Book-Vol.10-No.3", "Book-Vol.10-No.3", "—", "—")]
        public void ParseFolderMetadata_ParsesExpected(string input, string expMag, string expVol, string expNum)
        {
            var (mag, vol, num) = FolderMetadataParser.ParseFolderMetadata(input);
            Assert.Equal(expMag, mag);
            Assert.Equal(expVol, vol);
            Assert.Equal(expNum, num);
        }
    }
}
