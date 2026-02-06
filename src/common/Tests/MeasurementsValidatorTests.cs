using Xunit;
using Common.Shared;

namespace Common.Tests
{
    public class MeasurementsValidatorTests
    {
        [Theory]
        [InlineData("36B-28-38")]
        [InlineData("36-28-38")]
        [InlineData("34C-22-34")]
        [InlineData("36DD-28-38")] // two-letter cup should be accepted
        [InlineData("34C–22–34")] // en-dash should normalize
        [InlineData("34C - 22 - 34")] // spaces around hyphen should be trimmed
        public void ValidMeasurements_Accepted(string input)
        {
            var ok = MeasurementsValidator.TryParseMeasurements(input, out var bust, out var cup, out var waist, out var hip, out var err);
            Assert.True(ok, err);
            Assert.InRange(bust, 20, 250);
            Assert.InRange(waist, 20, 250);
            Assert.InRange(hip, 20, 250);
        }

        [Theory]
        [InlineData("")]
        [InlineData("36B/28/38")] // wrong separator
        [InlineData("36B-28cm-38")] // cm not allowed
        [InlineData("36B-28")] // missing part
        [InlineData("5-4-3")] // implausible range
        [InlineData("36.5B-28-38")] // decimals now rejected
        public void InvalidMeasurements_Rejected(string input)
        {
            var ok = MeasurementsValidator.TryParseMeasurements(input, out var bust, out var cup, out var waist, out var hip, out var err);
            Assert.False(ok);
            Assert.False(string.IsNullOrEmpty(err));
        }
    }
}
