using System;
using Xunit;
using IndexEditor.Shared;
using IndexEditor.Views;

namespace IndexEditor.Tests
{
    public class ParserAndViewModelMeasurementsTests
    {
        [Fact]
        public void Parser_Populates_Measurements_And_ViewModel_Sees_It()
        {
            var line = "80-85,Model,Louise,Louise Cohen,23,John Allum,35C-23-36";
            var parsed = IndexFileParser.ParseArticleLine(line);
            Assert.NotNull(parsed);
            Assert.NotNull(parsed.Measurements);
            Assert.Contains("35C-23-36", parsed.Measurements);

            var vm = new EditorStateViewModel();
            vm.Articles.Add(parsed);
            vm.SelectedArticle = parsed;
            Assert.Equal("35C-23-36", vm.SelectedArticle.Measurements0);
        }
    }
}

