using System;
using Xunit;
using IndexEditor.Shared;
using Common.Shared;

namespace IndexEditor.Tests
{
    public class IndexFileMeasurementsTests
    {
        [Fact]
        public void ParseArticleLine_ParsesMeasurements_From7ColumnLine()
        {
            var line = "80-85,Model,Louise,Louise Cohen,23,John Allum,35C-23-36";
            var parsed = IndexFileParser.ParseArticleLine(line);
            Assert.NotNull(parsed);
            Assert.Equal("Model", parsed.Category);
            Assert.Equal("Louise", parsed.Title);
            Assert.NotNull(parsed.Measurements);
            Assert.True(parsed.Measurements.Count >= 1);
            Assert.Contains("35C-23-36", parsed.Measurements);
        }

        [Fact]
        public void ParseArticleLine_8ColumnLine_IsUnsupported()
        {
            // legacy 8-field format should now be rejected
            var line = "5,Feature,Title,ModelName,23,Photographer,Author,36B-28-38";
            Assert.Throws<FormatException>(() => IndexFileParser.ParseArticleLine(line));
        }

        [Fact]
        public void ParseArticleLine_EmptyMeasurements_DefaultsToEmptyString()
        {
            // Use canonical 7-field with empty contributor and measurements fields: 4 trailing commas after title -> total 7 fields
            var line = "1,Feature,NoMeasurements,,,,";
            var parsed = IndexFileParser.ParseArticleLine(line);
            Assert.NotNull(parsed);
            Assert.NotNull(parsed.Measurements);
            Assert.Equal(1, parsed.Measurements.Count);
            Assert.Equal(string.Empty, parsed.Measurements[0]);
        }
    }
}
