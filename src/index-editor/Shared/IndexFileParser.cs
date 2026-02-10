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
            if (parts.Count < 2)
                return null;
            var article = new Common.Shared.ArticleLine();
            article.Pages = ParsePageNumbers(parts[0], out bool hasError);
            article.HasPageNumberError = hasError;
            if (article.Pages.Count == 0)
                return null;

            // Populate segments from pages so '|' separated segments are visible in the UI
            try
            {
                var pages = article.Pages;
                // Ensure the Segments collection exists and update it in-place so UI bindings receive collection change notifications
                try { if (article.Segments == null) article.Segments = new System.Collections.ObjectModel.ObservableCollection<Common.Shared.Segment>(); } catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: ensure segments", ex); }
                // Clear existing collection if possible
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
                        // Stored segments are closed ranges, so set End to the final page
                        try { article.Segments.Add(new Common.Shared.Segment(start, end)); } catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: add segment", ex); }
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: outer", ex); }

            article.Category = parts.Count > 1 ? parts[1] : "";
            if (string.IsNullOrWhiteSpace(article.Category))
                return null;
            if (article.Category.Equals("Contents", StringComparison.OrdinalIgnoreCase))
                article.Category = "Index";
            article.Title = parts.Count > 2 ? parts[2] : "";
            if (parts.Count > 3)
                article.ModelNames = parts[3].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            // columns: 4=modelNames, 5=ages, 6=photographers, 7=authors, 8=measurements (0-based indices 3..7)
            if (parts.Count > 4 && !string.IsNullOrWhiteSpace(parts[4]))
            {
                // ages are numeric or empty; allow missing entries
                var ageParts = parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                article.Ages = ageParts.Select(ap => int.TryParse(ap, out var iv) ? (int?)iv : null).ToList();
            }
            // Common formats vary: some index files have 7 columns (pages,category,title,models,ages,photographers,measurements)
            // while others use 8 columns with an authors column before measurements.
            if (parts.Count > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                article.Photographers = parts[5].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            if (parts.Count == 7)
            {
                // 7-column format: treat parts[6] as Measurements
                if (!string.IsNullOrWhiteSpace(parts[6]))
                    article.Measurements = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
            else
            {
                // 8+ column format: parts[6]=Authors, parts[7]=Measurements
                if (parts.Count > 6 && !string.IsNullOrWhiteSpace(parts[6]))
                    article.Authors = parts[6].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                if (parts.Count > 7 && !string.IsNullOrWhiteSpace(parts[7]))
                    article.Measurements = parts[7].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            if (article.ModelNames == null || article.ModelNames.Count == 0)
                article.ModelNames = new List<string> { string.Empty };
            if (article.Photographers == null || article.Photographers.Count == 0)
                article.Photographers = new List<string> { string.Empty };
            if (article.Authors == null || article.Authors.Count == 0)
                article.Authors = new List<string> { string.Empty };
            if (article.Measurements == null || article.Measurements.Count == 0)
                article.Measurements = new List<string> { string.Empty };
            if (article.Ages == null || article.Ages.Count == 0)
                article.Ages = new List<int?> { null };

            // Fallback: some older index files place the author in the photographers column for Humour entries.
            try
            {
                var cat = (article.Category ?? string.Empty).Trim().ToLowerInvariant();
                bool authorsEmpty = article.Authors == null || article.Authors.All(a => string.IsNullOrWhiteSpace(a));
                bool photographersHave = article.Photographers != null && article.Photographers.Any(p => !string.IsNullOrWhiteSpace(p));
                if (cat == "humour" && authorsEmpty && photographersHave)
                {
                    // Copy photographers to authors so the editor's Author field is populated
                    article.Authors = article.Photographers.Select(p => p).ToList();
                }
            }
            catch (Exception ex) { DebugLogger.LogException("IndexFileParser.ParseArticleLine: humour fallback", ex); }

            return article;
        }
    }
}
