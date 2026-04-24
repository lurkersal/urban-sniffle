using System;
using System.Linq;
using Xunit;
using Common.Shared;

namespace Common.Tests
{
    public class PipeSeparatedValuesTests
    {
        [Fact]
        public void ModelName0_SupportsPipeSeparatedValues()
        {
            var article = new ArticleLine();
            
            // Set pipe-separated model names
            article.ModelName0 = "Sarah|Jennifer|Amanda";
            
            // Verify the list is populated correctly
            Assert.Equal(3, article.ModelNames.Count);
            Assert.Equal("Sarah", article.ModelNames[0]);
            Assert.Equal("Jennifer", article.ModelNames[1]);
            Assert.Equal("Amanda", article.ModelNames[2]);
            
            // Verify getter returns pipe-separated string
            Assert.Equal("Sarah|Jennifer|Amanda", article.ModelName0);
        }

        [Fact]
        public void Age0_SupportsPipeSeparatedValues()
        {
            var article = new ArticleLine();
            
            // Set pipe-separated ages
            article.Age0 = "23|25|27";
            
            // Verify the list is populated correctly
            Assert.Equal(3, article.Ages.Count);
            Assert.Equal(23, article.Ages[0]);
            Assert.Equal(25, article.Ages[1]);
            Assert.Equal(27, article.Ages[2]);
            
            // Verify getter returns pipe-separated string
            Assert.Equal("23|25|27", article.Age0);
        }

        [Fact]
        public void Measurements0_SupportsPipeSeparatedValues()
        {
            var article = new ArticleLine();
            
            // Set pipe-separated measurements
            article.Measurements0 = "36B-28-38|34C-24-34|35D-26-36";
            
            // Verify the list is populated correctly
            Assert.Equal(3, article.Measurements.Count);
            Assert.Equal("36B-28-38", article.Measurements[0]);
            Assert.Equal("34C-24-34", article.Measurements[1]);
            Assert.Equal("35D-26-36", article.Measurements[2]);
            
            // Verify getter returns pipe-separated string
            Assert.Equal("36B-28-38|34C-24-34|35D-26-36", article.Measurements0);
        }

        [Fact]
        public void Validation_HandlesMultipleMeasurements()
        {
            var article = new ArticleLine
            {
                Category = "Group",
                Pages = { 10, 11, 12 }
            };
            
            // Set valid measurements
            article.Measurements0 = "36B-28-38|34C-24-34";
            article.Validate();
            
            Assert.False(article.HasMeasurementsError);
            Assert.Null(article.MeasurementsErrorMessage);
        }

        [Fact]
        public void Validation_ReportsErrorsForInvalidMeasurements()
        {
            var article = new ArticleLine
            {
                Category = "Model",
                Pages = { 10 }
            };
            
            // Set one valid and one invalid measurement
            article.Measurements0 = "36B-28-38|invalid";
            article.Validate();
            
            Assert.True(article.HasMeasurementsError);
            Assert.Contains("Measurement 2", article.MeasurementsErrorMessage);
        }

        [Fact]
        public void PipeSeparatedValues_HandlesWhitespace()
        {
            var article = new ArticleLine();
            
            // Set with spaces around pipes
            article.ModelName0 = "Sarah | Jennifer | Amanda";
            
            // Whitespace should be trimmed
            Assert.Equal(3, article.ModelNames.Count);
            Assert.Equal("Sarah", article.ModelNames[0]);
            Assert.Equal("Jennifer", article.ModelNames[1]);
            Assert.Equal("Amanda", article.ModelNames[2]);
        }

        [Fact]
        public void GroupCategory_ShowsMeasurements()
        {
            var article = new ArticleLine
            {
                Category = "Group",
                Pages = { 10 }
            };
            
            article.Measurements0 = "36B-28-38|34C-24-34";
            article.Validate();
            
            // Group category should allow measurements
            Assert.False(article.HasMeasurementsError);
        }

        [Fact]
        public void GroupCategory_SupportsAllModelFields()
        {
            var article = new ArticleLine
            {
                Category = "Group",
                Pages = { 10, 11, 12 }
            };
            
            // Set all fields as pipe-separated
            article.ModelName0 = "Sarah|Jennifer|Amanda";
            article.Age0 = "23|25|27";
            article.Measurements0 = "36B-28-38|34C-24-34|35D-26-36";
            article.Contributor0 = "John Smith";
            
            article.Validate();
            
            // Verify all fields are populated correctly
            Assert.Equal(3, article.ModelNames.Count);
            Assert.Equal(3, article.Ages.Count);
            Assert.Equal(3, article.Measurements.Count);
            Assert.Equal("John Smith", article.Contributors[0]);
            
            // Verify no validation errors
            Assert.False(article.HasValidationError);
            Assert.False(article.HasMeasurementsError);
        }
    }
}

