using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IndexEditor.Shared
{
    public static class ImageHelper
    {
        // Candidate filename patterns for pages
        public static IEnumerable<string> CandidatePaths(string folder, int page)
        {
            yield return Path.Combine(folder, page.ToString() + ".jpg");
            yield return Path.Combine(folder, page.ToString() + ".png");
            yield return Path.Combine(folder, page.ToString("D2") + ".jpg");
            yield return Path.Combine(folder, page.ToString("D2") + ".png");
            yield return Path.Combine(folder, page.ToString("D3") + ".jpg");
            yield return Path.Combine(folder, page.ToString("D3") + ".png");
            yield return Path.Combine(folder, "page-" + page.ToString() + ".jpg");
            yield return Path.Combine(folder, "p" + page.ToString() + ".jpg");
        }

        public static string? FindImagePath(string folder, int page)
        {
            try
            {
                foreach (var p in CandidatePaths(folder, page))
                    if (File.Exists(p)) return p;
            }
            catch { }
            return null;
        }

        public static bool ImageExists(string folder, int page)
        {
            return FindImagePath(folder, page) != null;
        }

        public static int? FindFirstImageInFolder(string folder, int startPage = 1, int maxSearch = 2000)
        {
            if (string.IsNullOrWhiteSpace(folder)) return null;
            try
            {
                for (int p = startPage; p <= maxSearch; p++)
                    if (ImageExists(folder, p)) return p;
            }
            catch { }
            return null;
        }

        public static int? FindNearestExistingPageBothDirections(string folder, int startPage, int maxRadius = 2000)
        {
            try
            {
                if (ImageExists(folder, startPage)) return startPage;
                for (int r = 1; r <= maxRadius; r++)
                {
                    var f = startPage + r;
                    if (f > 0 && ImageExists(folder, f)) return f;
                    var b = startPage - r;
                    if (b > 0 && ImageExists(folder, b)) return b;
                }
            }
            catch { }
            return null;
        }
    }
}

