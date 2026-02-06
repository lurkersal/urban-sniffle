using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Common.Shared
{
    public static class MeasurementsValidator
    {
        // Normalize a user-entered measurements string into canonical form expected by the parser.
        // - normalize Unicode dashes to ASCII hyphen
        // - collapse surrounding whitespace around hyphens
        // - remove 'cm' suffixes
        // - collapse multiple separators into single '-'
        // - remove stray spaces and normalize cup letters to uppercase, removing spaces between number and cup
        public static string NormalizeMeasurement(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Basic normalizations
            var s = input.Trim();
            s = s.Replace('\u2013', '-') // en-dash
                 .Replace('\u2014', '-') // em-dash
                 .Replace('\u2212', '-'); // minus sign

            // Remove any 'cm' occurrences (users sometimes paste with cm)
            s = System.Text.RegularExpressions.Regex.Replace(s, "(?i)cm", "");

            // Normalize separators: replace any sequence of hyphens/spaces with single '-'
            s = System.Text.RegularExpressions.Regex.Replace(s, "\\s*[-]+\\s*", "-");

            // Trim parts and normalize bust token (remove space between number and cup, uppercase cup)
            var parts = s.Split('-').Select(p => p.Trim()).ToArray();
            if (parts.Length > 0)
            {
                // Normalize first part (bust + optional cup)
                var bust = parts[0];
                // If there's a space between number and letters, remove it: '36 B' -> '36B'
                bust = System.Text.RegularExpressions.Regex.Replace(bust, "^(\\d{1,3})(?:\\.\\d+)?\\s+([A-Za-z]{1,2})$", m => m.Groups[1].Value + m.Groups[2].Value.ToUpperInvariant());
                // Uppercase any cup letters already adjacent: '36dd' -> '36DD'
                bust = System.Text.RegularExpressions.Regex.Replace(bust, "^(\\d{1,3}(?:\\.\\d+)?)([A-Za-z]{1,2})$", m => m.Groups[1].Value + m.Groups[2].Value.ToUpperInvariant());
                parts[0] = bust;
            }

            // Rejoin with single hyphen
            return string.Join("-", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        public static string FormatHint => "Format: bust(+cup)-waist-hip (integers only) — e.g. '36B-28-38' (separator must be '-')";

        // Acceptable patterns (common):
        //  - "36B-28-38"  -> bust with cup, waist, hip
        //  - "36-28-38"   -> bust, waist, hip
        //  - separator: '-' (required)
        //  - do not allow 'cm' suffix
        //  - allow decimals
        // Returns true if the measurements represent: bust(+cup optional), waist, hip

        private static readonly Regex _splitRegex = new("-", RegexOptions.Compiled);
        // integers only
        private static readonly Regex _bustWithCupRegex = new(@"^(?<num>\d{1,3})(?<cup>[A-Za-z]{1,2})?$", RegexOptions.Compiled);
        private static readonly Regex _numberRegex = new(@"^(?<num>\d{1,3})$", RegexOptions.Compiled);

        public static bool TryParseMeasurements(string? input, out double bustValue, out string? bustCup, out double waistValue, out double hipValue, out string? error)
        {
            bustValue = waistValue = hipValue = 0;
            bustCup = null;
            error = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Measurements empty";
                return false;
            }

            // Normalize common unicode dash characters to ASCII hyphen so users copying text
            // that contains en-dash/em-dash or minus sign still parse correctly.
            var normalized = input.Trim()
                .Replace('\u2013', '-') // en-dash
                .Replace('\u2014', '-') // em-dash
                .Replace('\u2212', '-'); // minus sign

            var parts = _splitRegex.Split(normalized);
            // Trim whitespace around each token so inputs like "34C - 22 - 34" still parse
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
            if (parts.Length < 3)
            {
                error = $"Expected three parts separated by '-': bust(+cup)-waist-hip. Example: 36B-28-38. {FormatHint}";
                return false;
            }

            // Prefer first three tokens
            var bustPart = parts[0];
            var waistPart = parts[1];
            var hipPart = parts[2];

            // Parse bust (number with optional cup letter)
            var m = _bustWithCupRegex.Match(bustPart);
            if (!m.Success)
            {
                // maybe a raw number (no 'cm' allowed)
                m = _numberRegex.Match(bustPart);
                if (!m.Success)
                {
                    error = "Invalid bust format (expected number optionally followed by cup letter)";
                    return false;
                }
            }

            if (!double.TryParse(m.Groups["num"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var b))
            {
                error = "Invalid bust number";
                return false;
            }
            bustValue = b;
            if (m.Groups["cup"].Success && !string.IsNullOrWhiteSpace(m.Groups["cup"].Value))
                bustCup = m.Groups["cup"].Value.ToUpperInvariant();

            // waist
            var wm = _numberRegex.Match(waistPart);
            if (!wm.Success || !double.TryParse(wm.Groups["num"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var w))
            {
                error = "Invalid waist number";
                return false;
            }
            waistValue = w;

            // hip
            var hm = _numberRegex.Match(hipPart);
            if (!hm.Success || !double.TryParse(hm.Groups["num"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var h))
            {
                error = "Invalid hip number";
                return false;
            }
            hipValue = h;

            // Basic sanity ranges (accept either inches or cm; numbers in plausible range):
            // We'll accept values between 20 and 250 for any measurement to be permissive.
            if (!IsPlausible(bustValue) || !IsPlausible(waistValue) || !IsPlausible(hipValue))
            {
                error = "One or more measurements are outside plausible human ranges";
                return false;
            }

            // Finally, check relative plausibility: waist <= bust+20 and hip >= waist
            if (waistValue > 1.5 * bustValue)
            {
                // unlikely: waist much larger than bust
                error = "Waist seems implausibly large relative to bust";
                return false;
            }
            if (hipValue < waistValue - 5)
            {
                error = "Hip seems implausibly small relative to waist";
                return false;
            }

            return true;
        }

        private static bool IsPlausible(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return false;
            return v >= 20 && v <= 250;
        }
    }
}
