using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace IndexEditor.Shared
{
    public static class FolderMetadataParser
    {
        // Strict parse for folder basename metadata (magazine, volume, number).
        // Valid format: "MagazineName 17-03, 1982" (magazine name, 2-digit volume '-' 2-digit number, ',' 4-digit year)
        public static (string mag, string vol, string num) ParseFolderMetadata(string folderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderName))
                    return (folderName ?? string.Empty, "—", "—");

                // Strict pattern: magazine name (anything), whitespace, 2 digits, '-', 2 digits, ',', 4-digit year
                var pattern = @"^(?<mag>.+?)\s+(?<vol>\d{2})-(?<num>\d{2}),\s*(?<year>\d{4})\s*$";
                var m = Regex.Match(folderName.Trim(), pattern);
                if (m.Success)
                {
                    var mag = m.Groups["mag"].Value.Trim();
                    var vol = m.Groups["vol"].Value;
                    var num = m.Groups["num"].Value;
                    return (mag, vol, num);
                }

                // Not a strict match: return the original folderName and placeholders
                return (folderName, "—", "—");
            }
            catch
            {
                return (folderName ?? string.Empty, "—", "—");
            }
        }
    }
}
