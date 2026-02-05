using Xunit;
using MagazineParser.Services;
using MagazineParser.Models;
using System.Collections.Generic;
using NSubstitute;

namespace MagazineParser.Tests
{
    public class MagazineParsingServiceTests
    {
        [Fact]
        public void ParseFile_NoIndexFile_ReturnsZero()
        {
            // Arrange
            var repo = NSubstitute.Substitute.For<MagazineParser.Interfaces.IDatabaseRepository>();
            var parser = NSubstitute.Substitute.For<MagazineParser.Interfaces.IContentParser>();
            var ui = NSubstitute.Substitute.For<MagazineParser.Interfaces.IUserInteraction>();
            var service = new MagazineParsingService(repo, parser, ui);
            var fakePath = "nonexistent_index.txt";

            // Act
            var result = service.ParseFile(fakePath);

            // Assert
            Assert.Equal(0, result);
        }
    }
}
