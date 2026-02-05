using Xunit;
using FileRenamer;
using FileRenamer.Models;

namespace FileRenamer.Tests
{
    public class FileRenamerTests
    {
        [Fact]
        public void RenameOptions_Properties_AreSettable()
        {
            var options = new RenameOptions();
            options.DirectoryPath = "/tmp";
            options.StringToRemove = "test";
            Assert.Equal("/tmp", options.DirectoryPath);
            Assert.Equal("test", options.StringToRemove);
        }

        [Fact]
        public void Program_Main_DoesNotThrow()
        {
            // Arrange
            string[] args = new string[] { "/tmp", "test" };
            // Act & Assert
            var exception = Record.Exception(() => Program.Main(args));
            Assert.Null(exception);
        }
    }
}
