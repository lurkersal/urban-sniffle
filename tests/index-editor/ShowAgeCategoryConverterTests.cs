using System;
using Xunit;
using IndexEditor.Views;

namespace IndexEditor.Tests
{
    public class ShowAgeCategoryConverterTests
    {
        private readonly ShowAgeCategoryConverter _converter = new ShowAgeCategoryConverter();

        [Theory]
        [InlineData("Model", true)]
        [InlineData("model", true)]
        [InlineData("MODEL", true)]
        [InlineData("Cover", true)]
        [InlineData("cover", true)]
        [InlineData("Group", true)]
        [InlineData("group", true)]
        [InlineData("GROUP", true)]
        public void ShowAge_ReturnsTrueForModelCoverGroup(string category, bool expected)
        {
            var result = _converter.Convert(category, typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Wives", false)]
        [InlineData("wives", false)]
        [InlineData("Letters", false)]
        [InlineData("letters", false)]
        [InlineData("Editorial", false)]
        [InlineData("Feature", false)]
        [InlineData("Fiction", false)]
        [InlineData("Humour", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void ShowAge_ReturnsFalseForOtherCategories(string category, bool expected)
        {
            var result = _converter.Convert(category, typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(expected, result);
        }
    }
}

