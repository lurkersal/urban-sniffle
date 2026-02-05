using System;
using System.Collections.Generic;
using System.Linq;
using Common.Shared;
namespace IndexEditor.Shared
{


    public static class IndexContentLineParser
    {
        public static Common.Shared.ArticleLine? Parse(string line)
        {
            var parts = line.Split(',');
            if (parts.Length < 3) return null;
            var pages = ParsePages(parts[0]);
            var category = parts[1].Trim();
            var title = parts[2].Trim();
            var models = parts.Length > 3 ? parts[3].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() : new List<string>();
            var photographers = parts.Length > 4 ? parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() : new List<string>();
            var authors = parts.Length > 5 ? parts[5].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() : new List<string>();
            return new Common.Shared.ArticleLine
            {
                Pages = pages,
                Category = category,
                Title = title,
                ModelNames = models,
                Photographers = photographers,
                Authors = authors
            };
        }

        private static List<int> ParsePages(string pageField)
        {
            var result = new List<int>();
            var ranges = pageField.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var range in ranges)
            {
                var nums = range.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (nums.Length == 2 && int.TryParse(nums[0], out int start) && int.TryParse(nums[1], out int end))
                {
                    for (int i = start; i <= end; i++) result.Add(i);
                }
                else if (nums.Length == 1 && int.TryParse(nums[0], out int single))
                {
                    result.Add(single);
                }
            }
            return result;
        }
    }
}
