using System;
using System.Collections.Generic;
using FindLinks.Services;

namespace FindLinks
{
    public class FindLinksApp
    {
        private readonly IDatabaseRepository _dbRepo;
        private readonly IOcrService _ocrService;
        private readonly IContentParser _contentParser;
        private readonly IssueLinkParser _linkParser;

        public FindLinksApp(IDatabaseRepository dbRepo, IOcrService ocrService, IContentParser contentParser, IssueLinkParser linkParser)
        {
            _dbRepo = dbRepo;
            _ocrService = ocrService;
            _contentParser = contentParser;
            _linkParser = linkParser;
        }

        public void Run(string magazineType, string volume, string? number, bool force)
        {
            int magazineId = _dbRepo.GetMagazineId(magazineType);
            if (magazineId == 0)
            {
                Console.Error.WriteLine($"ERROR: Magazine '{magazineType}' not found in the database. Please check the name or add it to the database.");
                return;
            }

            if (string.IsNullOrWhiteSpace(volume))
            {
                RunAllVolumesAndNumbers(magazineType, magazineId, force);
                return;
            }

            if (string.IsNullOrWhiteSpace(number))
            {
                RunAllNumbers(magazineType, magazineId, volume, force);
                return;
            }

            if (!int.TryParse(volume, out int volNum) || !int.TryParse(number, out int numNum))
            {
                Console.WriteLine("Volume and number must be integers.");
                return;
            }
            RunSingleIssue(magazineType, magazineId, volNum, numNum, force);
        }

        private void RunAllVolumesAndNumbers(string magazineType, int magazineId, bool force)
        {
            var issues = _dbRepo.GetAllIssuesForMagazine(magazineId);
            foreach (var (vol, num) in issues)
            {
                RunSingleIssue(magazineType, magazineId, vol, num, force);
            }
        }

        private void RunAllNumbers(string magazineType, int magazineId, string volume, bool force)
        {
            if (!int.TryParse(volume, out int volNum))
            {
                Console.WriteLine("Volume must be an integer.");
                return;
            }
            var numbers = _dbRepo.GetNumbersForMagazineVolume(magazineId, volNum);
            foreach (var num in numbers)
            {
                RunSingleIssue(magazineType, magazineId, volNum, num, force);
            }
        }

        private void RunSingleIssue(string magazineType, int magazineId, int volume, int number, bool force)
        {
            int? issueId = _dbRepo.GetExistingIssueId(magazineId, volume, number);
            if (issueId == null)
            {
                Console.Error.WriteLine($"ERROR: Issue not found in database: {magazineType} Vol {volume} No {number}. Please add this issue to the database before running find-links.");
                return;
            }
            if (!force && _dbRepo.GetLinkScanPerformed(issueId.Value))
            {
                Console.WriteLine($"Link scan already performed for {magazineType} Vol {volume} No {number}. Skipping scan. Use -f to force rescan.");
                return;
            }
            var pages = _dbRepo.GetPagesForIssue(issueId.Value);
            var pageCategories = _dbRepo.GetPageCategoriesForIssue(issueId.Value);
            var pageImagePaths = _dbRepo.GetPageImagePathsForIssue(issueId.Value);
            var validCategories = new HashSet<string>(new[] { "editorial", "letters", "model", "motoring", "Wives", "Group" }, StringComparer.OrdinalIgnoreCase);
            int totalPages = pages.Count;
            int processedPages = 0;
            var allLinkedIssues = new HashSet<(int magazineId, int volume, int number, int page)>();
            foreach (var page in pages)
            {
                DrawProgressBar(++processedPages, totalPages, magazineType, volume, number);
                if (!pageCategories.TryGetValue(page, out var category) ||
                    category.Equals("Cover", StringComparison.OrdinalIgnoreCase) ||
                    category.Equals("Contents", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!pageImagePaths.TryGetValue(page, out var imagePath) || string.IsNullOrEmpty(imagePath))
                {
                    Console.WriteLine($"Image for page {page} not found in database.");
                    continue;
                }
                var imageRoot = Environment.GetEnvironmentVariable("MAGAZINE_IMAGE_ROOT");
                string fullImagePath = imagePath;
                if (!System.IO.Path.IsPathRooted(imagePath))
                {
                    if (string.IsNullOrEmpty(imageRoot))
                    {
                        Console.Error.WriteLine($"ERROR: MAGAZINE_IMAGE_ROOT environment variable is not set, but is required for relative image paths (page {page}, image '{imagePath}').");
                        continue;
                    }
                    fullImagePath = System.IO.Path.Combine(imageRoot, imagePath);
                }
                var ocrText = _ocrService.ExtractText(fullImagePath);
                var entries = _contentParser.ParseContentsPage(ocrText);
                var links = _linkParser.FindIssueLinks(ocrText);
                foreach (var (vol, num) in links.Distinct())
                {
                    if (num >= 1 && num <= 14 && allLinkedIssues.Contains((magazineId, vol, num, page)) == false)
                    {
                        allLinkedIssues.Add((magazineId, vol, num, page));
                    }
                }
            }
            if (allLinkedIssues.Count > 0)
            {
                // Only distinct IssueLinks will be inserted due to HashSet
                try
                {
                    _dbRepo.InsertIssueLinks(issueId.Value, allLinkedIssues);
                    _dbRepo.SetLinkScanPerformed(issueId.Value, true);
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
                {
                    Console.WriteLine($"WARNING: Duplicate IssueLink detected for {magazineType} Vol {volume} No {number}. Skipping duplicate(s).");
                }
            }
        }

        private void DrawProgressBar(int progress, int total, string magazineType, int volume, int number)
        {
            int barWidth = 40;
            double percent = (double)progress / total;
            int pos = (int)(barWidth * percent);
            string bar;
            if (pos < barWidth)
                bar = new string('=', pos) + '>' + new string(' ', barWidth - pos - 1);
            else
                bar = new string('=', barWidth);
            Console.Write($"\r[{bar}]");
            Console.Write($" {progress}/{total} ({percent:P0}) | {magazineType} Vol {volume} No {number}");
            if (progress == total) Console.WriteLine();
        }
    }
}
