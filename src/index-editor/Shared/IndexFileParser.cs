using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexEditor.Shared
{
    public static class IndexFileParser
    {
        // Delegate to shared common parser utilities to avoid duplication
        public static List<int> ParsePageNumbers(string pageStr, out bool hasError)
        {
            return Common.Shared.IndexParserUtilities.ParsePageNumbers(pageStr, out hasError);
        }

        public static List<string> SplitRespectingEscapedCommas(string line)
        {
            return Common.Shared.IndexParserUtilities.SplitRespectingEscapedCommas(line);
        }

        public static Common.Shared.ArticleLine? ParseArticleLine(string line)
        {
            var parts = SplitRespectingEscapedCommas(line);
            // Accept 6 or 7 column formats used in index files:
            // - 6 columns: missing measurements -> we'll append an empty measurements field
            // - 7 columns: canonical: pages,category,title,models,ages,contributors,measurements
            // - 8+ columns: legacy format (photographers,authors) — treat as an error to avoid ambiguity
            // If the line has fewer than 6 fields, treat missing fields as blank (pad with empty strings)
            if (parts.Count < 6)
            {
                var needed = 6 - parts.Count;
                for (int i = 0; i < needed; i++)
                    parts.Add(string.Empty);
            }
            if (parts.Count == 6)
            {
                // append empty measurements field
                parts.Add(string.Empty);
            }
            else if (parts.Count >= 8)
            {
                // Legacy 8-field lines are not supported — surface as a format error so UI shows overlay for correction
                throw new FormatException($"Unsupported _index.txt line format: found {parts.Count} comma-separated fields (legacy 8-field format). Use the canonical 7-field format: pages,category,title,modelNames,ages,contributors,measurements. Line: '{line}'");
            }

            var article = new Common.Shared.ArticleLine();
            article.Pages = ParsePageNumbers(parts[0], out bool hasError);
            article.HasPageNumberError = hasError;
            if (article.Pages.Count == 0)
                return null;

            // Populate segments from pages so '|' separated segments are visible in the UI
            try
            {
                var pages = article.Pages;
                try { if (article.Segments == null) article.Segments = new System.Collections.ObjectModel.ObservableCollection<Common.Shared.Segment>(); } catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: ensure segments", ex); }
                try { article.Segments.Clear(); } catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: clear segments", ex); }
                if (pages != null && pages.Count > 0)
                {
                    pages.Sort();
                    int i = 0;
                    while (i < pages.Count)
                    {
                        int start = pages[i];
                        int end = start;
                        i++;
                        while (i < pages.Count && pages[i] == end + 1)
                        {
                            end = pages[i];
                            i++;
                        }
                        try { article.Segments.Add(new Common.Shared.Segment(start, end)); } catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: add segment", ex); }
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: outer", ex); }

            article.Category = parts[1] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(article.Category))
                return null;
            if (article.Category.Equals("Contents", StringComparison.OrdinalIgnoreCase))
                article.Category = "Index";
            article.Title = parts[2] ?? string.Empty;
            article.ModelNames = parts[3].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            if (!string.IsNullOrWhiteSpace(parts[4]))
            {
                var ageParts = parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                article.Ages = ageParts.Select(ap => int.TryParse(ap, out var iv) ? (int?)iv : null).ToList();
            }

            // Handle contributor(s) and measurements depending on number of parts
            if (parts.Count == 7)
            {
                // 7-column canonical format: parts[5]=contributors, parts[6]=measurements
                if (!string.IsNullOrWhiteSpace(parts[5]))
                    article.Contributors = parts[5].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                if (!string.IsNullOrWhiteSpace(parts[6]))
                    article.Measurements = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            if (article.ModelNames == null || article.ModelNames.Count == 0)
                article.ModelNames = new List<string> { string.Empty };
            if (article.Contributors == null || article.Contributors.Count == 0)
                article.Contributors = new List<string> { string.Empty };
            if (article.Measurements == null || article.Measurements.Count == 0)
                article.Measurements = new List<string> { string.Empty };
            if (article.Ages == null || article.Ages.Count == 0)
                article.Ages = new List<int?> { null };

            // No legacy fallback required: Contributors is canonical. If parsing needs to notify user, the caller will handle it.

            return article;
        }
    }
}
