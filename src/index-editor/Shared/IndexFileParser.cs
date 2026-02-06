using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexEditor.Shared
{
    public static class IndexFileParser
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

        public static Common.Shared.ArticleLine? ParseArticleLine(string line)
        {
            var parts = SplitRespectingEscapedCommas(line);
            if (parts.Count < 2)
                return null;
            var article = new Common.Shared.ArticleLine();
            article.Pages = ParsePageNumbers(parts[0], out bool hasError);
            article.HasPageNumberError = hasError;
            if (article.Pages.Count == 0)
                return null;
            article.Category = parts.Count > 1 ? parts[1] : "";
            if (string.IsNullOrWhiteSpace(article.Category))
                return null;
            if (article.Category.Equals("Contents", StringComparison.OrdinalIgnoreCase))
                article.Category = "Index";
            article.Title = parts.Count > 2 ? parts[2] : "";
            if (parts.Count > 3)
                article.ModelNames = parts[3].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (parts.Count > 4)
            {
                var ageParts = parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var ages = new List<int?>();
                foreach (var ap in ageParts)
                {
                    if (int.TryParse(ap, out int a))
                        ages.Add(a);
                    else
                        ages.Add(null);
                }
                article.Ages = ages;
            }
            if (parts.Count > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                article.Photographers = parts[5].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (parts.Count > 6)
                article.Measurements = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            if (article.ModelNames == null || article.ModelNames.Count == 0)
                article.ModelNames = new List<string> { string.Empty };
            if (article.Photographers == null || article.Photographers.Count == 0)
                article.Photographers = new List<string> { string.Empty };
            if (article.Measurements == null || article.Measurements.Count == 0)
                article.Measurements = new List<string> { string.Empty };
            if (article.Ages == null || article.Ages.Count == 0)
                article.Ages = new List<int?> { null };

            return article;
        }
    }
}
