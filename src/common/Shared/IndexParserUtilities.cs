using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Shared
{
    public static class IndexParserUtilities
    {
        // Parse page text like "1|2-4" into list of ints
        public static List<int> ParsePageNumbers(string pageStr, out bool hasError)
        {
            var pages = new List<int>();
            hasError = false;
            if (string.IsNullOrWhiteSpace(pageStr))
            {
                hasError = true;
                return pages;
            }
            var parts = pageStr.Split('|');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    hasError = true;
                    continue;
                }
                if (trimmed.Contains('-'))
                {
                    var range = trimmed.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end) && start <= end)
                    {
                        for (int i = start; i <= end; i++)
                            pages.Add(i);
                    }
                    else
                    {
                        hasError = true;
                    }
                }
                else if (int.TryParse(trimmed, out int singlePage))
                {
                    pages.Add(singlePage);
                }
                else
                {
                    hasError = true;
                }
            }
            // Ensure distinct sorted
            pages = pages.Distinct().OrderBy(p => p).ToList();
            if (pages.Count == 0) hasError = true;
            return pages;
        }

        // Split respecting escaped commas (\,)
        public static List<string> SplitRespectingEscapedCommas(string line)
        {
            var parts = new List<string>();
            var currentPart = new System.Text.StringBuilder();
            bool escaped = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (escaped)
                {
                    currentPart.Append(c);
                    escaped = false;
                }
                else if (c == '\\' && i + 1 < line.Length)
                {
                    escaped = true;
                }
                else if (c == ',')
                {
                    parts.Add(currentPart.ToString().Trim());
                    currentPart.Clear();
                }
                else
                {
                    currentPart.Append(c);
                }
            }
            parts.Add(currentPart.ToString().Trim());
            return parts;
        }
    }
}

